using System.Net;
using System.Text.Json;
using Platform.Identity.Application.Exceptions;

namespace identity.api.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
                await HandleExceptionAsync(context, ex).ConfigureAwait(false);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var problem = new
            {
                type = "about:blank",
                title = "An error occurred",
                status = 500,
                detail = exception.Message,
                traceId = context.TraceIdentifier
            };

            var status = HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case ValidationException:
                    status = HttpStatusCode.BadRequest;
                    problem = new { type = "https://api.platform/errors/validation", title = "Validation error", status = (int)status, detail = exception.Message, traceId = context.TraceIdentifier };
                    break;
                case ResourceNotFoundException:
                    status = HttpStatusCode.NotFound;
                    problem = new { type = "https://api.platform/errors/not-found", title = "Resource not found", status = (int)status, detail = exception.Message, traceId = context.TraceIdentifier };
                    break;
                case ConflictException:
                    status = HttpStatusCode.Conflict;
                    problem = new { type = "https://api.platform/errors/conflict", title = "Conflict", status = (int)status, detail = exception.Message, traceId = context.TraceIdentifier };
                    break;
                case UnauthorizedException:
                    status = HttpStatusCode.Unauthorized;
                    problem = new { type = "https://api.platform/errors/unauthorized", title = "Unauthorized", status = (int)status, detail = exception.Message, traceId = context.TraceIdentifier };
                    break;
                case ForbiddenException:
                    status = HttpStatusCode.Forbidden;
                    problem = new { type = "https://api.platform/errors/forbidden", title = "Forbidden", status = (int)status, detail = exception.Message, traceId = context.TraceIdentifier };
                    break;
                case IdentityException idEx:
                    status = HttpStatusCode.BadGateway;
                    var detail = idEx.Message;
                    problem = new { type = "https://api.platform/errors/Identity", title = "Identity provider error", status = (int)status, detail, traceId = context.TraceIdentifier };
                    break;
                default:
                    status = HttpStatusCode.InternalServerError;
                    problem = new { type = "https://api.platform/errors/internal", title = "Internal server error", status = (int)status, detail = "An unexpected error occurred.", traceId = context.TraceIdentifier };
                    break;
            }

            var json = JsonSerializer.Serialize(problem);
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;
            return context.Response.WriteAsync(json);
        }
    }
}
