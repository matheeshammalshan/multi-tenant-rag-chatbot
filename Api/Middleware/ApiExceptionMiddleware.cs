using Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Api.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ApiExceptionMiddleware> logger;
        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (!context.Response.HasStarted)
            {
                int status = 500;
                string detail = "An unexpected error occurred.";
                ApiException? apiException = exception as ApiException;
                DbUpdateException? databaseException = exception as DbUpdateException;

                if (apiException != null)
                {
                    status = apiException.StatusCode;
                    detail = apiException.Message;
                }
                else if (databaseException != null)
                {
                    MySqlException? mysqlException = databaseException.InnerException as MySqlException;
                    if (mysqlException != null && mysqlException.Number == 1062)
                    {
                        status = 409;
                        detail = "Resource already exists.";
                    }
                }
                if (status >= 500)
                {
                    logger.LogError(exception, "Request failed: {Method} {Path}", context.Request.Method, context.Request.Path);
                }
                else
                {
                    logger.LogInformation("Request rejected with HTTP {Status}: {Method} {Path}", status, context.Request.Method, context.Request.Path);
                }

                await Results.Problem(detail: detail, statusCode: status).ExecuteAsync(context);
            }
        }
    }
}
