using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FruitShop.Filters
{
    public class AuditLogFilterAttribute : ActionFilterAttribute
    {
        private readonly string _actionDesc;

        public AuditLogFilterAttribute(string actionDesc = "")
        {
            _actionDesc = actionDesc;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Chỉ ghi log nếu action thực thi thành công (không có exception)
            // Và chỉ ghi log các thao tác làm thay đổi dữ liệu (POST, DELETE, PUT)
            var method = context.HttpContext.Request.Method;
            if (context.Exception == null && (method == "POST" || method == "DELETE" || method == "PUT"))
            {
                var auditRepo = context.HttpContext.RequestServices.GetService<AuditLogRepository>();
                if (auditRepo != null)
                {
                    var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
                    var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
                    var userId = context.HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                    
                    // Lấy các tham số query hoặc route, thường là ID
                    var idParam = context.RouteData.Values["id"]?.ToString() ?? context.HttpContext.Request.Query["id"].ToString();
                    
                    string details = _actionDesc;
                    if (string.IsNullOrEmpty(details))
                    {
                        details = $"Executed {method} {actionName}";
                        if (!string.IsNullOrEmpty(idParam))
                        {
                            details += $" on ID: {idParam}";
                        }
                    }

                    var log = new AuditLog
                    {
                        UserId = userId,
                        ControllerName = controllerName,
                        ActionName = actionName,
                        Details = details,
                        IpAddress = ipAddress
                    };

                    auditRepo.Insert(log);
                }
            }
            
            base.OnActionExecuted(context);
        }
    }
}
