using System.Text.Json.Serialization;
using Api.Configuration;
using Api.Filters;
using Api.Middleware;
using Api.Security;
using Application.Configuration;
using Application.Interfaces.Services;
using Infrastructure.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    if (builder.Configuration.GetValue<bool>("ContainerSetup:Enabled"))
    {
        ContainerSetup.ConfigureSigningKey(builder.Configuration);
    }
    builder.Services.AddSerilog();
    builder.Services.AddProblemDetails();
    builder.Services.AddApiDocumentation();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiSecurity(builder.Configuration);
    builder.Services.AddControllers(options => options.Filters.Add<ApiKey>()).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
    var app = builder.Build();
    if (builder.Configuration.GetValue<bool>("ContainerSetup:Enabled"))
    {
        await ContainerSetup.InitializeAsync(app.Services, builder.Configuration);
    }
    if (builder.Configuration.GetValue<bool>("bootstrap-owner"))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAuthService>().BootstrapOwnerAsync(builder.Configuration["BootstrapOwner:Email"] ?? throw new InvalidOperationException("Configure BootstrapOwner:Email."), builder.Configuration["BootstrapOwner:Password"] ?? throw new InvalidOperationException("Configure BootstrapOwner:Password."), builder.Configuration["BootstrapOwner:Name"] ?? "Console Owner");
        Log.Information("Platform owner created. Remove BootstrapOwner secrets after setup.");
        return;
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ApiExceptionMiddleware>();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseRouting();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseMiddleware<BrowserSecurityMiddleware>();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Chatbot API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Chatbot API";
            options.UseRequestInterceptor("function (request) { const token = document.cookie.split('; ').find(x => x.startsWith('csrf_token=')); if (token) request.headers['X-CSRF-Token'] = decodeURIComponent(token.substring(11)); return request; }");
        });
    }

    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();
    app.MapControllers();
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
