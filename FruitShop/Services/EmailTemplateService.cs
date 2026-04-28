using FruitShop.Models.DAL;
using FruitShop.Models.Entities;

namespace FruitShop.Services;

public interface IEmailTemplateService
{
    Task SendOrderConfirmationAsync(int orderId, int userId);
    Task SendStatusUpdateAsync(int orderId, string newStatus);
    Task SendWelcomeEmailAsync(string email, string fullName);
}

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailService _emailService;
    private readonly UserRepository _userRepo;
    private readonly OrderRepository _orderRepo;

    public EmailTemplateService(
        IEmailService emailService,
        UserRepository userRepo,
        OrderRepository orderRepo)
    {
        _emailService = emailService;
        _userRepo     = userRepo;
        _orderRepo    = orderRepo;
    }

    public async Task SendOrderConfirmationAsync(int orderId, int userId)
    {
        try
        {
            var user  = _userRepo.GetById(userId);
            var order = _orderRepo.GetById(orderId);
            if (user == null || order == null || string.IsNullOrEmpty(user.Email)) return;

            var itemsHtml = new System.Text.StringBuilder();
            foreach (var item in order.OrderDetails)
            {
                itemsHtml.Append("<tr style='border-bottom:1px solid #eee;'>");
                itemsHtml.Append($"<td style='padding:10px;'>{item.FruitName}</td>");
                itemsHtml.Append($"<td style='padding:10px;text-align:center;'>{item.Quantity} {item.Unit}</td>");
                itemsHtml.Append($"<td style='padding:10px;text-align:right;'>{item.UnitPrice:N0}đ</td>");
                itemsHtml.Append($"<td style='padding:10px;text-align:right;font-weight:bold;'>{item.Subtotal:N0}đ</td>");
                itemsHtml.Append("</tr>");
            }

            string discountRow = "";
            if (order.DiscountAmount > 0)
                discountRow = $"<tr><td colspan='3' style='padding:6px 10px;text-align:right;color:#EF4444;'>Giảm giá coupon:</td><td style='padding:6px 10px;text-align:right;color:#EF4444;'>-{order.DiscountAmount:N0}đ</td></tr>";
            string pointsRow = "";
            if (order.PointsDiscount > 0)
                pointsRow = $"<tr><td colspan='3' style='padding:6px 10px;text-align:right;color:#3B82F6;'>Điểm tích lũy:</td><td style='padding:6px 10px;text-align:right;color:#3B82F6;'>-{order.PointsDiscount:N0}đ</td></tr>";

            string paymentBadge = order.PaymentMethod switch
            {
                "Cash"     => "Tiền mặt",
                "Transfer" => "Chuyển khoản",
                "QR"       => "QR Code",
                _          => order.PaymentMethod ?? "—"
            };

            string body = @"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:linear-gradient(135deg,#10B981,#06B6D4);padding:30px 40px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:24px;'>FruitShop</h1>
    <p style='color:rgba(255,255,255,0.85);margin:8px 0 0;'>Xác nhận đặt hàng thành công</p>
  </div>
  <div style='background:#fff;padding:30px 40px;border-radius:0 0 12px 12px;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <p style='margin:0 0 20px;font-size:15px;'>Chào <strong>" + user.FullName + @"</strong>,</p>
    <p style='margin:0 0 20px;font-size:15px;color:#374151;'>Cảm ơn bạn đã đặt hàng tại <strong>FruitShop</strong>! Đơn hàng của bạn đã được tiếp nhận và đang chờ xác nhận.</p>
    <div style='background:#F0FDF4;border-radius:8px;padding:16px 20px;margin-bottom:24px;'>
      <table style='width:100%;font-size:14px;'>
        <tr><td style='padding:4px 0;color:#6B7280;'><strong>Mã đơn hàng:</strong></td><td style='padding:4px 0;text-align:right;font-weight:bold;color:#10B981;'>#" + order.OrderId + @"</td></tr>
        <tr><td style='padding:4px 0;color:#6B7280;'><strong>Ngày đặt:</strong></td><td style='padding:4px 0;text-align:right;'>" + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</td></tr>
        <tr><td style='padding:4px 0;color:#6B7280;'><strong>Thanh toán:</strong></td><td style='padding:4px 0;text-align:right;'>" + paymentBadge + @"</td></tr>
        <tr><td style='padding:4px 0;color:#6B7280;'><strong>Địa chỉ giao:</strong></td><td style='padding:4px 0;text-align:right;'>" + (order.ShippingAddress ?? "—") + @"</td></tr>
      </table>
    </div>
    <table style='width:100%;border-collapse:collapse;margin-bottom:20px;'>
      <thead>
        <tr style='background:#F3F4F6;'>
          <th style='padding:10px;text-align:left;font-size:12px;color:#6B7280;text-transform:uppercase;'>Sản phẩm</th>
          <th style='padding:10px;font-size:12px;color:#6B7280;text-transform:uppercase;text-align:center;'>SL</th>
          <th style='padding:10px;font-size:12px;color:#6B7280;text-transform:uppercase;text-align:right;'>Đơn giá</th>
          <th style='padding:10px;font-size:12px;color:#6B7280;text-transform:uppercase;text-align:right;'>Thành tiền</th>
        </tr>
      </thead>
      <tbody>" + itemsHtml.ToString() + @"</tbody>
      <tfoot style='font-weight:bold;'>
        <tr><td colspan='3' style='padding:10px;text-align:right;'>Tổng cộng:</td><td style='padding:10px;text-align:right;color:#10B981;font-size:18px;'>" + order.TotalAmount.ToString("N0") + @"đ</td></tr>
        " + discountRow + @"
        " + pointsRow + @"
      </tfoot>
    </table>
    " + (string.IsNullOrEmpty(order.Note) ? "" : $"<p style='font-size:13px;color:#6B7280;'><strong>Ghi chú:</strong> {order.Note}</p>") + @"
    <p style='font-size:14px;color:#374151;border-top:1px solid #E5E7EB;padding-top:20px;margin-top:20px;'>Bạn có thể theo dõi trạng thái đơn hàng tại trang lịch sử đơn hàng.</p>
    <p style='font-size:13px;color:#9CA3AF;text-align:center;margin-top:24px;'>— FruitShop Team —<br/><span style='font-size:12px;'>Cửa hàng trái cây tươi ngon hàng đầu Việt Nam</span></p>
  </div>
</div>";

            await _emailService.SendEmailAsync(user.Email, $"[FruitShop] Xác nhận đơn hàng #{orderId}", body);
        }
        catch { }
    }

    public async Task SendStatusUpdateAsync(int orderId, string newStatus)
    {
        try
        {
            var order = _orderRepo.GetById(orderId);
            if (order == null) return;
            var user = _userRepo.GetById(order.UserId);
            if (user == null || string.IsNullOrEmpty(user.Email)) return;

            string icon = newStatus switch { "Confirmed" => "✅", "Shipping" => "🚚", "Delivered" => "🎉", "Cancelled" => "❌", _ => "📋" };
            string desc = newStatus switch
            {
                "Confirmed" => "đã được xác nhận và đang được chuẩn bị",
                "Shipping"  => "đang được giao đến địa chỉ của bạn",
                "Delivered" => "đã giao hàng thành công",
                "Cancelled" => "đã bị hủy",
                _          => $"có trạng thái mới: {newStatus}"
            };
            string color = newStatus switch { "Confirmed" => "#3B82F6", "Shipping" => "#F59E0B", "Delivered" => "#10B981", "Cancelled" => "#EF4444", _ => "#6B7280" };
            string statusText = order.GetStatusText();

            string body = @"
<div style='font-family:Arial,sans-serif;max-width:500px;margin:0 auto;'>
  <div style='background:" + color + @";padding:30px 40px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:22px;'>" + icon + @" Cập nhật đơn hàng #" + orderId + @"</h1>
  </div>
  <div style='background:#fff;padding:30px 40px;border-radius:0 0 12px 12px;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <p style='font-size:15px;'>Chào <strong>" + user.FullName + @"</strong>,</p>
    <div style='background:#F9FAFB;border-left:4px solid " + color + @";padding:16px 20px;border-radius:0 8px 8px 0;margin:20px 0;'>
      <p style='margin:0;font-size:15px;color:#374151;'>Đơn hàng <strong>#" + orderId + @"</strong> " + desc + @".</p>
      <p style='margin:8px 0 0;font-size:14px;color:#6B7280;'>Tổng tiền: <strong style='color:" + color + @";font-size:16px;'>" + order.TotalAmount.ToString("N0") + @"đ</strong></p>
    </div>
    <p style='font-size:14px;color:#374151;'>Cảm ơn bạn đã tin tưởng FruitShop!</p>
    <p style='font-size:13px;color:#9CA3AF;text-align:center;margin-top:24px;'>— FruitShop Team —</p>
  </div>
</div>";

            await _emailService.SendEmailAsync(user.Email, $"[FruitShop] Đơn hàng #{orderId} — {statusText}", body);
        }
        catch { }
    }

    public async Task SendWelcomeEmailAsync(string email, string fullName)
    {
        try
        {
            string body = @"
<div style='font-family:Arial,sans-serif;max-width:500px;margin:0 auto;'>
  <div style='background:linear-gradient(135deg,#10B981,#06B6D4);padding:30px 40px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:24px;'>FruitShop</h1>
    <p style='color:rgba(255,255,255,0.85);margin:8px 0 0;'>Chào mừng bạn!</p>
  </div>
  <div style='background:#fff;padding:30px 40px;border-radius:0 0 12px 12px;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <p style='font-size:15px;'>Xin chào <strong>" + fullName + @"</strong>,</p>
    <p style='font-size:14px;color:#374151;'>Cảm ơn bạn đã đăng ký tài khoản tại <strong>FruitShop</strong> — cửa hàng trái cây tươi ngon hàng đầu Việt Nam.</p>
    <p style='font-size:14px;color:#374151;'>Bạn có thể đặt hàng ngay với hơn 100+ loại trái cây nhập khẩu và nội địa chất lượng cao.</p>
    <div style='background:#F0FDF4;border-radius:8px;padding:16px 20px;margin:20px 0;text-align:center;'>
      <p style='margin:0;font-size:13px;color:#374151;'>Tích điểm đổi ưu đãi — Giao hàng tận nơi</p>
    </div>
    <p style='font-size:14px;color:#374151;'>Chúc bạn có những trải nghiệm mua sắm tuyệt vời!</p>
    <p style='font-size:13px;color:#9CA3AF;text-align:center;margin-top:24px;'>— FruitShop Team —</p>
  </div>
</div>";

            await _emailService.SendEmailAsync(email, "Chào mừng đến với FruitShop!", body);
        }
        catch { }
    }
}
