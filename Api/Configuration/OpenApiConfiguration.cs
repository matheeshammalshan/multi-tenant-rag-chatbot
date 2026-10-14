using Api.Filters;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace Api.Configuration
{
    public static class OpenApiConfiguration
    {
        public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
        {
            return services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "Chatbot API";
                    document.Info.Version = "v1";
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
                    document.Components.SecuritySchemes["ChatbotApiKey"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "x-api-key",
                        Description = "The chatbot's generated API key."
                    };
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Management session. Browser login also sets an HttpOnly session cookie."
                    };
                    return Task.CompletedTask;
                });
                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    if (context.Description.ActionDescriptor.EndpointMetadata.OfType<RequiredApiKeyAttribute>().Any())
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ChatbotApiKey" } }] = Array.Empty<string>() });
                    }
                    else if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
                    }

                    return Task.CompletedTask;
                });
            });
        }
    }
}
