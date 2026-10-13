using Application.Interfaces.Integrations;
using Application.Configuration;
using Infrastructure.Configuration;
using Api.Filters;
using Api.WebSockets;
using Infrastructure.Observers;
using Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    Log.Information("Starting web application");
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog();
    builder.Services.AddOpenApi();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();
    builder.Services.AddSingleton<IDocumentNotifier, WebSocketDocumentNotifier>();
    builder.Services.AddControllers(options => options.Filters.Add<ApiKey>());

    var app = builder.Build();
    app.UseWebSockets();
    app.Map("/ws/chat", WebSocketHandler.Handle);

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        db.Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment()) app.MapOpenApi();
    // Construct the observer once to subscribe it to document processing events.
    _ = app.Services.GetRequiredService<DocAndChunksObserver>();
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
