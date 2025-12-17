using System.Net;
using System.Text.Json;

namespace BooleanCompletenessBack
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (ClientException cliEx)
            {
                // Устанавливаем статус 400 и возвращаем JSON

                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                httpContext.Response.ContentType = "application/json";

                var errorResponse = new { errorMsg = cliEx.Message };
                var jsonResponse = JsonSerializer.Serialize(errorResponse);

                await httpContext.Response.WriteAsync(jsonResponse);
            }
            catch (Exception ex)
            {
                // Логируем полный стек-трейс на консоль (или в logger)
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

                // Устанавливаем статус 500 и возвращаем JSON
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "application/json";

                var errorResponse = new { errorMsg = "Ошибка сервера" };
                var jsonResponse = JsonSerializer.Serialize(errorResponse);

                await httpContext.Response.WriteAsync(jsonResponse);
            }
        }
    }
}
