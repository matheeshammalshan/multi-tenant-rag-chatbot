using Application.DTOs;
using Application.Interfaces.Integrations;
using Application.Interfaces.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Documents
{
    public class DocumentProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory scopes;
        private readonly ILogger<DocumentProcessingWorker> logger;
        private readonly IConfiguration configuration;
        public DocumentProcessingWorker(IServiceScopeFactory scopes, ILogger<DocumentProcessingWorker> logger, IConfiguration configuration)
        {
            this.scopes = scopes;
            this.logger = logger;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
                    var now = DateTime.UtcNow;
                    var candidate = await db.DocumentProcessingJobs.AsNoTracking().Where(x => x.CompletedAtUtc == null && x.AvailableAtUtc <= now && (x.LeaseUntilUtc == null || x.LeaseUntilUtc < now)).OrderBy(x => x.AvailableAtUtc).FirstOrDefaultAsync(stoppingToken);
                    if (candidate == null)
                    {
                        await Task.Delay(2000, stoppingToken);
                        continue;
                    }

                    var lease = Guid.NewGuid();
                    var claimed = await db.DocumentProcessingJobs.Where(x => x.Id == candidate.Id && x.CompletedAtUtc == null && (x.LeaseUntilUtc == null || x.LeaseUntilUtc < now)).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseId, lease).SetProperty(x => x.LeaseUntilUtc, now.AddMinutes(5)).SetProperty(x => x.Attempts, x => x.Attempts + 1), stoppingToken);
                    if (claimed == 1)
                    {
                        await ProcessAsync(candidate.Id, lease, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Document queue is unavailable");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task ProcessAsync(Guid jobId, Guid lease, CancellationToken stoppingToken)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var heartbeat = RenewLeaseAsync(jobId, lease, linked);
            try
            {
                using var scope = scopes.CreateScope();
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<AppDb>();
                var job = await db.DocumentProcessingJobs.Include(x => x.Document).FirstAsync(x => x.Id == jobId, linked.Token);
                var document = job.Document;
                using var content = services.GetRequiredService<IFileService>().OpenRead(document.FileUrl!);
                var request = new DocumentDto
                {
                    Id = document.Id,
                    ChatBotId = document.ChatBotId,
                    ReaderType = Enum.Parse<ReaderType>(document.ReaderType),
                    File = new DocumentUpload(document.Name!, content)
                };
                var chunks = await services.GetRequiredService<IChunckCreater>().ChunkCreate(request);
                if (chunks.Length == 0)
                {
                    throw new InvalidOperationException("The document contains no extractable text.");
                }

                if (chunks.Length > 10000)
                {
                    throw new InvalidOperationException("The document has too many chunks.");
                }

                for (var index = 0; index < chunks.Length; index++)
                {
                    linked.Token.ThrowIfCancellationRequested();
                    var vector = await services.GetRequiredService<IEmbeddingsCreater>().CreateEmbeddings(chunks[index]);
                    if (vector == null || vector.Length == 0)
                    {
                        throw new InvalidOperationException("Embedding service returned no vector.");
                    }

                    await services.GetRequiredService<ISavingEmbeddings>().CreateEmbeddingsOnDb(request, vector, chunks[index], index);
                }

                await using var transaction = await db.Database.BeginTransactionAsync(linked.Token);
                var finished = await db.DocumentProcessingJobs.Where(x => x.Id == jobId && x.LeaseId == lease).ExecuteUpdateAsync(s => s.SetProperty(x => x.CompletedAtUtc, DateTime.UtcNow).SetProperty(x => x.LeaseUntilUtc, (DateTime? )null), linked.Token);
                if (finished != 1)
                {
                    return;
                }

                await db.Document.Where(x => x.Id == document.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Proceed").SetProperty(x => x.Chunks, chunks.Length).SetProperty(x => x.Error, (string? )null), linked.Token);
                await transaction.CommitAsync(linked.Token);
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Document processing failed for job {JobId}", jobId);
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDb>();
                var job = await db.DocumentProcessingJobs.Include(x => x.Document).FirstAsync(x => x.Id == jobId, stoppingToken);
                var terminal = job.Attempts >= 3;
                await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                var updated = await db.DocumentProcessingJobs.Where(x => x.Id == jobId && x.LeaseId == lease).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseId, (Guid? )null).SetProperty(x => x.LeaseUntilUtc, (DateTime? )null).SetProperty(x => x.CompletedAtUtc, terminal ? DateTime.UtcNow : (DateTime? )null).SetProperty(x => x.AvailableAtUtc, DateTime.UtcNow.AddSeconds(Math.Clamp(configuration.GetValue("Documents:RetryDelaySeconds", 30), 1, 300))), stoppingToken);
                if (updated != 1)
                {
                    return;
                }

                var error = terminal ? "Document processing failed. Check the file and service availability." : null;
                await db.Document.Where(x => x.Id == job.DocumentId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, terminal ? "Failed" : "Processing").SetProperty(x => x.Error, error), stoppingToken);
                await transaction.CommitAsync(stoppingToken);
            }
            finally
            {
                linked.Cancel();
                try
                {
                    await heartbeat;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task RenewLeaseAsync(Guid jobId, Guid lease, CancellationTokenSource cancellation)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
                while (await timer.WaitForNextTickAsync(cancellation.Token))
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
                    if (await db.DocumentProcessingJobs.Where(x => x.Id == jobId && x.LeaseId == lease && x.CompletedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseUntilUtc, DateTime.UtcNow.AddMinutes(5)), cancellation.Token) != 1)
                    {
                        cancellation.Cancel();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Document processing lease could not be renewed");
                cancellation.Cancel();
            }
        }
    }
}
