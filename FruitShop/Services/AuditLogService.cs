using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FruitShop.Services;

/// <summary>
/// Service xử lý audit log - tách khỏi AuditLogFilter và Controller
/// </summary>
public interface IAuditLogService
{
    void Log(HttpContext context, string action, string details = "");
    void Log(int? userId, string controller, string action, string? details, string? ipAddress);
}

public class AuditLogService : IAuditLogService
{
    private readonly AuditLogRepository _repo;

    public AuditLogService(AuditLogRepository repo)
    {
        _repo = repo;
    }

    public void Log(HttpContext context, string action, string details = "")
    {
        var userId     = context.Session.GetInt32("UserId");
        var ipAddress  = context.Connection.RemoteIpAddress?.ToString();
        var controller = context.Request.RouteValues["controller"]?.ToString() ?? "Unknown";
        var idParam    = context.Request.RouteValues["id"]?.ToString()
                         ?? context.Request.Query["id"].ToString();

        var logDetails = string.IsNullOrEmpty(details)
            ? $"Executed {context.Request.Method} {action}"
            : details;

        if (!string.IsNullOrEmpty(idParam))
            logDetails += $" on ID: {idParam}";

        _repo.Insert(new AuditLog
        {
            UserId        = userId,
            ControllerName = controller,
            ActionName    = action,
            Details      = logDetails,
            IpAddress    = ipAddress
        });
    }

    public void Log(int? userId, string controller, string action, string? details, string? ipAddress)
    {
        _repo.Insert(new AuditLog
        {
            UserId        = userId,
            ControllerName = controller,
            ActionName    = action,
            Details      = details,
            IpAddress    = ipAddress
        });
    }
}
