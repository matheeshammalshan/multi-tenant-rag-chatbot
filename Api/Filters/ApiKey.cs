using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Application.Interfaces.Services;

namespace Api.Filters
{
    public class ApiKey : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endPoint = context.HttpContext.GetEndpoint();
            var requiredApiKey = endPoint?.Metadata.GetMetadata<RequiredApiKeyAttribute>();
            if (requiredApiKey == null)
            {
                await next();
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue("x-api-key", out var extractedKey))
            {
                context.Result = new ObjectResult(new ProblemDetails { Status = 401, Detail = "Chatbot API key is required." })
                {
                    StatusCode = 401
                };
                return;
            }

            context.HttpContext.Items["ApiKey"] = extractedKey.ToString();
            var chatbot = await context.HttpContext.RequestServices.GetRequiredService<IApiKeyService>().ResolveAsync(extractedKey.ToString());
            context.HttpContext.Items["ChatbotId"] = chatbot.ChatBotId;
            await next();
        }
    }
}
