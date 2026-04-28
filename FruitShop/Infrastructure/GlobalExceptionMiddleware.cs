using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using System.Text.Json;

namespace FruitShop.Infrastructure;

/// <summary>
/// Middleware xử lý exception toàn cục - ghi log và trả response统一的 lỗi
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception at {Path} {Method}",
                context.Request.Path, context.Request.Method);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ArgumentNullException  => (HttpStatusCode.BadRequest,  "Tham số không hợp lệ."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Bạn không có quyền truy cập."),
            KeyNotFoundException  => (HttpStatusCode.NotFound,     "Không tìm thấy dữ liệu."),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.")
        };

        context.Response.StatusCode = (int)statusCode;

        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message,
                statusCode = (int)statusCode
            });
        }
        else
        {
            context.Items["ErrorMessage"] = message;
            context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(message)}");
        }
    }
}

/// <summary>
/// Filter xử lý exception trong action (bắt exception không bị filter middleware che)
/// </summary>
public class ExceptionHandlerFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionHandlerFilter> _logger;

    public ExceptionHandlerFilter(ILogger<ExceptionHandlerFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        _logger.LogError(ex, "Action exception: {Controller}.{Action}",
            context.RouteData.Values["controller"],
            context.RouteData.Values["action"]);

        context.ExceptionHandled = true;

        if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.Result = new JsonResult(new { success = false, message = ex.Message });
        }
        else
        {
            context.Result = new RedirectToActionResult("Error", "Home",
                new { message = ex.Message });
        }
    }
}
