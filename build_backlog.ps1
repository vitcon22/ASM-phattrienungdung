$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ============================================================
# DATA: 49 User Stories cho FruitShop
# Format: Id | Vai trò | Mục tiêu | Lý do | Priority | Acceptance | MoSCoW | Ghi chú
# ============================================================
$stories = @(
    @{Id='RQ01';Role='Nhân viên, Quản lý';Goal='Xem danh sách tồn kho trái cây theo loại, số lượng, giá';Reason='Tôi có thể nắm được hàng hóa hiện có trong kho';Priority='High';Accept='Hiển thị đúng số lượng, loại; cập nhật real-time';MoSCoW='Must';Note='Đã có ở trang /Fruit'},
    @{Id='RQ02';Role='Quản lý';Goal='Thêm mới loại trái cây vào hệ thống (tên, xuất xứ, đơn giá)';Reason='Tôi có thể mở rộng danh mục sản phẩm kinh doanh';Priority='High';Accept='Lưu đúng thông tin; không cho phép trùng tên';MoSCoW='Must';Note='Fruit/Create'},
    @{Id='RQ03';Role='Quản lý';Goal='Cập nhật thông tin trái cây (giá, số lượng, mô tả)';Reason='Thông tin luôn chính xác và phản ánh thực tế';Priority='High';Accept='Lịch sử chỉnh sửa được ghi lại; validation đầu vào';MoSCoW='Must';Note='Fruit/Edit + InventoryLog'},
    @{Id='RQ04';Role='Quản lý';Goal='Nhập hàng từ nhà cung cấp và cập nhật tồn kho tự động';Reason='Tôi tiết kiệm thời gian nhập liệu thủ công';Priority='High';Accept='Tồn kho tăng đúng số lượng nhập; ghi nhận ngày nhập';MoSCoW='Must';Note='Inventory controller'},
    @{Id='RQ05';Role='Quản lý';Goal='Quản lý thông tin nhà cung cấp (tên, SĐT, mặt hàng cung cấp)';Reason='Tôi liên hệ đặt hàng nhanh hơn';Priority='Low';Accept='Lưu đầy đủ thông tin; liên kết với lịch sử nhập hàng';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ06';Role='User, Nhân viên, Quản lý';Goal='Đăng nhập / Đăng xuất hệ thống bằng tài khoản và mật khẩu';Reason='Bảo mật thông tin và xác định danh tính người dùng';Priority='High';Accept='Đăng nhập thành công đúng tài khoản; sai mật khẩu hiển thị lỗi; đăng xuất xóa phiên';MoSCoW='Must';Note='Account/Login, BCrypt hash'},
    @{Id='RQ07';Role='Nhân viên';Goal='Tạo đơn bán hàng và trừ tồn kho tự động';Reason='Tôi quản lý bán hàng nhanh và tránh bán vượt tồn kho';Priority='High';Accept='Không cho phép bán khi tồn kho = 0; in hóa đơn được';MoSCoW='Must';Note='Order/Checkout'},
    @{Id='RQ08';Role='Nhân viên';Goal='Quản lý thông tin khách hàng (tên, SĐT, lịch sử mua hàng)';Reason='Chăm sóc khách hàng thân thiết và phân tích hành vi mua';Priority='Medium';Accept='Lưu đủ thông tin; hiển thị lịch sử mua; tìm kiếm theo tên/SĐT';MoSCoW='Should';Note='User entity + Order/History'},
    @{Id='RQ09';Role='Nhân viên';Goal='Xem chi tiết thông tin một loại trái cây (giá, xuất xứ, tồn kho)';Reason='Tôi tra cứu nhanh thông tin sản phẩm khi tư vấn khách';Priority='Medium';Accept='Hiển thị đầy đủ thông tin; hình ảnh nếu có; cập nhật real-time';MoSCoW='Should';Note='Fruit/Details'},
    @{Id='RQ10';Role='Nhân viên, Quản lý';Goal='Lọc danh sách tồn kho theo trạng thái (còn hàng, sắp hết, hết hàng)';Reason='Tôi nắm bắt nhanh tình trạng hàng hóa cần xử lý';Priority='Medium';Accept='Lọc đúng theo trạng thái; cập nhật tức thì';MoSCoW='Should';Note='Fruit/Index có filter stockStatus'},
    @{Id='RQ11';Role='Nhân viên';Goal='In hóa đơn bán hàng dưới dạng PDF hoặc giấy nhiệt';Reason='Khách hàng có chứng từ mua hàng';Priority='Medium';Accept='Hóa đơn đúng định dạng, đầy đủ thông tin; in thành công';MoSCoW='Should';Note='Order/Invoice'},
    @{Id='RQ12';Role='Quản lý';Goal='Xem danh sách đơn hàng đã tạo theo ngày / trạng thái';Reason='Tôi tra cứu và theo dõi lịch sử giao dịch';Priority='High';Accept='Hiển thị đầy đủ đơn; lọc được theo ngày và trạng thái';MoSCoW='Must';Note='Order/Index'},
    @{Id='RQ13';Role='Nhân viên';Goal='Thêm nhiều sản phẩm vào một đơn hàng (giỏ hàng)';Reason='Khách mua nhiều loại trái cây cùng lúc mà không cần tạo nhiều đơn';Priority='High';Accept='Giỏ hàng hiển thị đúng; tổng tiền tính chính xác';MoSCoW='Must';Note='Order/Cart (Session)'},
    @{Id='RQ14';Role='Quản lý';Goal='Quản lý tài khoản người dùng trong hệ thống (thêm, sửa, xóa)';Reason='Tôi duy trì danh sách nhân viên có quyền truy cập hệ thống';Priority='Medium';Accept='CRUD đầy đủ; tài khoản xóa không đăng nhập được';MoSCoW='Should';Note='UserController'},
    @{Id='RQ15';Role='Nhân viên';Goal='Bán hàng với nhiều phương thức thanh toán (tiền mặt, chuyển khoản, QR)';Reason='Phục vụ đa dạng nhu cầu thanh toán của khách';Priority='Medium';Accept='Ghi đúng phương thức; số tiền khớp; cấp hóa đơn';MoSCoW='Should';Note='Order.PaymentMethod'},
    @{Id='RQ16';Role='Nhân viên';Goal='Tính tiền thừa trả lại khi khách thanh toán tiền mặt';Reason='Tránh nhầm lẫn khi thu tiền';Priority='Medium';Accept='Hiển thị số tiền thừa trả đúng khi nhập tiền khách đưa';MoSCoW='Should';Note='Order.AmountReceived → ChangeAmount'},
    @{Id='RQ17';Role='Nhân viên';Goal='Ghi nhận hàng hỏng / hàng trả lại và trừ tồn kho';Reason='Tồn kho phản ánh đúng thực tế hàng còn dùng được';Priority='Medium';Accept='Có lý do hủy hàng; cập nhật tồn kho và ghi log';MoSCoW='Should';Note='Chưa triển khai chi tiết'},
    @{Id='RQ18';Role='Quản lý';Goal='Khóa / mở khóa tài khoản người dùng';Reason='Ngăn truy cập trái phép từ tài khoản không còn sử dụng';Priority='Medium';Accept='Khóa ngay lập tức; tài khoản bị khóa không đăng nhập được';MoSCoW='Should';Note='User.IsActive'},
    @{Id='RQ19';Role='User, Nhân viên, Quản lý';Goal='Đổi mật khẩu cá nhân sau khi đăng nhập';Reason='Bảo mật tài khoản cá nhân';Priority='Medium';Accept='Xác minh mật khẩu cũ; đặt mật khẩu mới; áp dụng ngay';MoSCoW='Should';Note='Account/ChangePassword'},
    @{Id='RQ20';Role='Nhân viên, Quản lý';Goal='Tìm kiếm trái cây theo tên, xuất xứ hoặc khoảng giá';Reason='Tôi tìm hàng nhanh mà không cần lướt toàn bộ danh sách';Priority='High';Accept='Kết quả chính xác; không phân biệt hoa thường';MoSCoW='Must';Note='Fruit/Index search + AutoComplete'},
    @{Id='RQ21';Role='Quản lý';Goal='Xem báo cáo doanh thu theo ngày / tuần / tháng / năm';Reason='Tôi đánh giá được hiệu quả kinh doanh theo từng giai đoạn';Priority='High';Accept='Tổng hợp đúng số liệu; xuất được file báo cáo';MoSCoW='Must';Note='Dashboard/Index'},
    @{Id='RQ22';Role='Quản lý';Goal='Nhận cảnh báo khi tồn kho của một loại trái cây xuống thấp';Reason='Tôi chủ động đặt hàng trước khi hết hàng';Priority='Medium';Accept='Hiển thị thông báo khi số lượng < ngưỡng tối thiểu';MoSCoW='Should';Note='Dashboard low-stock widget'},
    @{Id='RQ23';Role='Quản lý';Goal='Phân quyền người dùng (admin, nhân viên kho, thu ngân)';Reason='Mỗi người chỉ truy cập chức năng phù hợp vai trò';Priority='High';Accept='Đăng nhập đúng vai trò hiển thị đúng menu';MoSCoW='Must';Note='RequireRole filter, Role entity'},
    @{Id='RQ24';Role='Quản lý';Goal='Tạo và quản lý chương trình khuyến mãi / giảm giá theo sản phẩm';Reason='Tăng doanh thu và thu hút khách hàng mua nhiều hơn';Priority='Medium';Accept='Áp dụng % hoặc số tiền giảm; tự động tính giá sau giảm; có ngày hết hạn';MoSCoW='Should';Note='CouponController'},
    @{Id='RQ25';Role='Quản lý';Goal='Xem thống kê sản phẩm bán chạy theo tuần / tháng / năm';Reason='Định hướng nhập hàng và tập trung vào sản phẩm hiệu quả';Priority='Medium';Accept='Xếp hạng top 10 sản phẩm; hiển thị số lượng và doanh thu; lọc theo thời gian';MoSCoW='Should';Note='Dashboard top products'},
    @{Id='RQ26';Role='Nhân viên';Goal='Áp dụng mã giảm giá khi tạo đơn bán hàng';Reason='Khách hàng nhận được ưu đãi đã đăng ký';Priority='Medium';Accept='Mã hợp lệ thì trừ đúng giá trị; mã hết hạn hiển thị lỗi';MoSCoW='Should';Note='Order/ApplyCoupon (AJAX)'},
    @{Id='RQ27';Role='Quản lý';Goal='Xem lịch sử thay đổi tồn kho theo từng sản phẩm';Reason='Tôi truy vết được các biến động hàng hóa';Priority='Medium';Accept='Hiển thị đầy đủ log thay đổi kèm người thực hiện và thời gian';MoSCoW='Should';Note='InventoryLog repository'},
    @{Id='RQ28';Role='Quản lý';Goal='Gắn lô hàng và ngày hết hạn cho từng mẻ nhập';Reason='Quản lý được hàng theo lô để ưu tiên bán hàng sắp hết hạn';Priority='Low';Accept='Lưu số lô và hạn dùng; cảnh báo khi hàng sắp hết hạn';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ29';Role='Quản lý';Goal='Tự động cảnh báo hàng sắp hết hạn trong 3 ngày tới';Reason='Giảm thiểu tổn thất do hàng quá date';Priority='Low';Accept='Hệ thống quét hàng ngày; cảnh báo nổi bật cho hàng sắp hết hạn';MoSCoW='Could';Note='Phụ thuộc RQ28'},
    @{Id='RQ30';Role='Quản lý';Goal='Xuất báo cáo doanh thu ra file Excel hoặc PDF';Reason='Tôi chia sẻ và lưu trữ báo cáo dễ dàng';Priority='Low';Accept='Xuất đúng định dạng; đầy đủ số liệu trong khoảng thời gian chọn';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ31';Role='Quản lý';Goal='Xem biểu đồ trực quan doanh thu / lợi nhuận theo tháng';Reason='Tôi nắm bắt xu hướng kinh doanh nhanh hơn';Priority='Medium';Accept='Biểu đồ cập nhật theo dữ liệu thực; lọc được theo thời gian';MoSCoW='Should';Note='Dashboard chart'},
    @{Id='RQ32';Role='Quản lý';Goal='Xem báo cáo lợi nhuận ròng sau khi trừ chi phí nhập hàng';Reason='Tôi biết được hiệu quả thực sự của hoạt động kinh doanh';Priority='Low';Accept='Tính đúng: lợi nhuận = doanh thu - giá vốn; hiển thị rõ ràng';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ33';Role='Quản lý';Goal='Xem dashboard tổng quan tình hình kinh doanh theo thời gian thực';Reason='Tôi nắm bắt nhanh tình hình mà không cần mở nhiều báo cáo';Priority='High';Accept='Hiển thị doanh thu hôm nay, tồn kho thấp, đơn hàng mới; tự động cập nhật';MoSCoW='Must';Note='DashboardController'},
    @{Id='RQ34';Role='Quản lý';Goal='Phân loại khách hàng theo hạng (thường, bạc, vàng)';Reason='Tôi áp dụng chính sách ưu đãi phù hợp từng nhóm';Priority='Low';Accept='Phân hạng tự động theo doanh thu tích lũy; hiển thị hạng trên hồ sơ';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ35';Role='Nhân viên';Goal='Tra cứu điểm tích lũy của khách hàng khi thanh toán';Reason='Khách hàng dùng điểm để giảm giá đơn hàng';Priority='Low';Accept='Hiển thị điểm hiện có; tính giảm giá khi dùng điểm';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ36';Role='Quản lý';Goal='Xem lịch sử nhập hàng từ từng nhà cung cấp';Reason='Tôi đánh giá độ tin cậy và hiệu quả của từng nhà cung cấp';Priority='Low';Accept='Hiển thị đầy đủ lịch sử; lọc được theo nhà cung cấp và thời gian';MoSCoW='Could';Note='Phụ thuộc RQ05'},
    @{Id='RQ37';Role='Quản lý';Goal='Xem so sánh giá nhập hàng giữa các nhà cung cấp cho cùng sản phẩm';Reason='Tôi chọn được nguồn hàng giá tốt nhất';Priority='Low';Accept='Bảng so sánh giá theo từng nhà cung cấp; hiển thị chênh lệch';MoSCoW='Could';Note='Phụ thuộc RQ05'},
    @{Id='RQ38';Role='Quản lý';Goal='Xem nhật ký hoạt động của tất cả người dùng trong hệ thống';Reason='Tôi phát hiện thao tác bất thường và bảo vệ dữ liệu';Priority='Low';Accept='Ghi log đầy đủ theo người dùng, thời gian, hành động';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ39';Role='Quản lý';Goal='Xóa loại trái cây không còn kinh doanh khỏi hệ thống';Reason='Danh mục sản phẩm luôn gọn, không hiển thị hàng đã ngưng';Priority='Medium';Accept='Xóa mềm; không hiện ở danh sách bán hàng; vẫn giữ trong lịch sử';MoSCoW='Should';Note='Fruit/Delete (SoftDelete)'},
    @{Id='RQ40';Role='Quản lý';Goal='Xem tổng số lượng và giá trị tồn kho hiện tại';Reason='Tôi biết tổng tài sản hàng hóa đang nắm giữ';Priority='Medium';Accept='Tính đúng tổng số lượng và giá trị theo giá nhập hiện tại';MoSCoW='Should';Note='Dashboard widget'},
    @{Id='RQ41';Role='Nhân viên';Goal='Nhận gợi ý sản phẩm thay thế khi hàng hết tồn kho';Reason='Vẫn có thể phục vụ khách khi hàng chủ lực hết';Priority='Low';Accept='Hệ thống gợi ý sản phẩm tương tự còn hàng';MoSCoW="Won't";Note='Để dành sprint sau'},
    @{Id='RQ42';Role='Nhân viên, Quản lý';Goal='Xem thông tin đơn hàng chi tiết (sản phẩm, số lượng, tổng tiền)';Reason='Tôi kiểm tra lại chính xác đơn trước và sau bán';Priority='High';Accept='Hiển thị đầy đủ từng dòng sản phẩm; tổng tiền đúng';MoSCoW='Must';Note='Order/Details + QR code'},
    @{Id='RQ43';Role='Nhân viên';Goal='Tìm kiếm đơn hàng theo mã đơn hoặc tên khách hàng';Reason='Tôi tra cứu nhanh một giao dịch cụ thể';Priority='Medium';Accept='Tìm đúng đơn hàng; hiển thị kết quả nhanh';MoSCoW='Should';Note='Order/Index search'},
    @{Id='RQ44';Role='Nhân viên';Goal='Xem lịch sử mua hàng của một khách hàng cụ thể';Reason='Tôi tư vấn và phục vụ khách hàng quen tốt hơn';Priority='Medium';Accept='Hiển thị toàn bộ đơn của khách theo thứ tự thời gian';MoSCoW='Should';Note='Order/History'},
    @{Id='RQ45';Role='Quản lý';Goal='Xem số lượng đơn hàng theo trạng thái (đã hoàn thành, đã hủy)';Reason='Tôi theo dõi được tỷ lệ thành công trong bán hàng';Priority='Medium';Accept='Đếm đúng số đơn từng trạng thái; lọc được theo ngày';MoSCoW='Should';Note='Dashboard order stats'},
    @{Id='RQ46';Role='Quản lý';Goal='Nhập nhiều sản phẩm hàng loạt từ file Excel';Reason='Tiết kiệm thời gian khi cần thêm nhiều mặt hàng cùng lúc';Priority='Low';Accept='Upload file đúng mẫu; hệ thống kiểm tra và nhập dữ liệu; báo lỗi nếu sai';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ47';Role='Quản lý';Goal='Ghi nhận các khoản chi phí vận hành (điện, nước, thuê mặt bằng)';Reason='Tôi tính toán được lợi nhuận thực tế sau tất cả chi phí';Priority='Low';Accept='Nhập được chi phí theo tháng; hiển thị trong báo cáo lợi nhuận';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ48';Role='User, Nhân viên, Quản lý';Goal='Xem thông tin cá nhân và vai trò tài khoản đang đăng nhập';Reason='Tôi biết đang đăng nhập với quyền hạn nào';Priority='Medium';Accept='Hiển thị tên, vai trò, thời gian đăng nhập; cập nhật được thông tin cá nhân';MoSCoW='Should';Note='Account/Profile'},
    @{Id='RQ49';Role='Quản lý';Goal='Xem báo cáo tổng kết cuối ngày tự động';Reason='Tôi nắm tình hình kinh doanh mỗi cuối ngày mà không cần tổng hợp thủ công';Priority='Medium';Accept='Tổng hợp đúng doanh thu, số đơn, hàng nhập trong ngày; có thể in hoặc xuất file';MoSCoW='Should';Note='Dashboard/DailyReport'},
    @{Id='RQ50';Role='Customer';Goal='Đăng ký tài khoản mới trên website';Reason='Tôi có thể mua hàng và lưu thông tin cá nhân';Priority='High';Accept='Email không trùng; password được hash; chuyển sang trang đăng nhập';MoSCoW='Must';Note='Account/Register, BCrypt'},
    @{Id='RQ51';Role='Customer';Goal='Xem trang chủ với danh sách trái cây nổi bật';Reason='Tôi nhanh chóng tiếp cận sản phẩm để mua';Priority='High';Accept='Hiển thị sản phẩm còn hàng; có ảnh, giá, nút thêm giỏ';MoSCoW='Must';Note='HomeController/Index'},
    @{Id='RQ52';Role='Customer';Goal='Tìm kiếm sản phẩm theo từ khóa ở trang chủ';Reason='Tôi tra cứu nhanh mặt hàng cần mua';Priority='High';Accept='Tìm theo tên hoặc mô tả; không phân biệt hoa thường';MoSCoW='Must';Note='Home/Index search'},
    @{Id='RQ53';Role='Customer';Goal='Lọc sản phẩm theo danh mục trên trang chủ';Reason='Tôi xem nhanh các sản phẩm cùng nhóm';Priority='Medium';Accept='Click danh mục → lọc đúng; reset được';MoSCoW='Should';Note='Home/Index categoryId'},
    @{Id='RQ54';Role='Customer';Goal='Xem chi tiết sản phẩm với hình ảnh và mô tả đầy đủ';Reason='Tôi quyết định mua chính xác hơn';Priority='High';Accept='Hiển thị giá, mô tả, tồn kho, đánh giá trung bình';MoSCoW='Must';Note='Fruit/Details'},
    @{Id='RQ55';Role='Customer';Goal='Thêm sản phẩm vào giỏ hàng từ trang chi tiết hoặc danh sách';Reason='Tôi gom nhiều món để thanh toán một lần';Priority='High';Accept='Thêm thành công; cập nhật badge giỏ; chống vượt tồn kho';MoSCoW='Must';Note='Order/AddToCart'},
    @{Id='RQ56';Role='Customer';Goal='Cập nhật số lượng sản phẩm trong giỏ hàng';Reason='Tôi điều chỉnh đơn hàng trước khi thanh toán';Priority='High';Accept='Tăng/giảm chính xác; tổng tiền cập nhật ngay';MoSCoW='Must';Note='Order/UpdateCart'},
    @{Id='RQ57';Role='Customer';Goal='Xóa sản phẩm khỏi giỏ hàng';Reason='Tôi loại bỏ món không muốn mua';Priority='High';Accept='Xóa thành công; giỏ hàng cập nhật';MoSCoW='Must';Note='Order/RemoveFromCart'},
    @{Id='RQ58';Role='Customer';Goal='Nhập mã giảm giá ở trang giỏ hàng và xem số tiền được giảm';Reason='Tôi biết được tổng tiền cuối cùng phải trả';Priority='Medium';Accept='Mã hợp lệ → hiển thị % giảm; mã sai → thông báo lỗi';MoSCoW='Should';Note='Order/ApplyCoupon AJAX'},
    @{Id='RQ59';Role='Customer';Goal='Thanh toán đơn hàng với địa chỉ giao hàng và ghi chú';Reason='Tôi hoàn tất quá trình mua hàng';Priority='High';Accept='Validate địa chỉ; tạo đơn Pending; clear giỏ hàng';MoSCoW='Must';Note='Order/Checkout'},
    @{Id='RQ60';Role='Customer';Goal='Chọn phương thức thanh toán (tiền mặt khi nhận hàng / chuyển khoản)';Reason='Tôi chọn cách trả tiền tiện cho mình';Priority='High';Accept='Lưu đúng phương thức vào đơn hàng';MoSCoW='Must';Note='Order.PaymentMethod'},
    @{Id='RQ61';Role='Customer';Goal='Xem mã QR đơn hàng sau khi đặt thành công';Reason='Tôi tra cứu đơn nhanh khi nhận hàng';Priority='Low';Accept='QR chứa mã đơn, tổng tiền, trạng thái; quét được';MoSCoW='Could';Note='Order/Details QrData'},
    @{Id='RQ62';Role='Customer';Goal='Xem lịch sử các đơn hàng đã đặt của bản thân';Reason='Tôi theo dõi tiến trình giao hàng';Priority='High';Accept='Hiển thị toàn bộ đơn theo thời gian; click xem chi tiết';MoSCoW='Must';Note='Order/History'},
    @{Id='RQ63';Role='Customer';Goal='Hủy đơn hàng đang chờ xác nhận';Reason='Tôi đổi ý hoặc đặt nhầm';Priority='Medium';Accept='Chỉ hủy được đơn Pending; hoàn lại tồn kho';MoSCoW='Should';Note='Order/Cancel'},
    @{Id='RQ64';Role='Customer';Goal='Đánh giá sao và viết nhận xét cho sản phẩm đã mua';Reason='Tôi chia sẻ trải nghiệm với người khác';Priority='Medium';Accept='Chỉ đánh giá khi đã mua; rating 1-5; lưu nhận xét';MoSCoW='Should';Note='ReviewController'},
    @{Id='RQ65';Role='Customer';Goal='Xem nhận xét và điểm đánh giá trung bình của sản phẩm';Reason='Tôi tham khảo trước khi mua';Priority='Medium';Accept='Hiển thị điểm trung bình, số lượt đánh giá, danh sách comment';MoSCoW='Should';Note='Fruit/Details ViewBag.Reviews'},
    @{Id='RQ66';Role='Customer';Goal='Thêm sản phẩm yêu thích vào danh sách Wishlist';Reason='Tôi lưu sản phẩm để xem lại sau';Priority='Medium';Accept='Toggle thêm/xóa; chỉ Customer dùng được';MoSCoW='Should';Note='WishlistController'},
    @{Id='RQ67';Role='Customer';Goal='Xem danh sách Wishlist của bản thân';Reason='Tôi xem lại các sản phẩm đã đánh dấu yêu thích';Priority='Medium';Accept='Hiển thị danh sách; click xem chi tiết hoặc xóa';MoSCoW='Should';Note='Account/Profile có Wishlists'},
    @{Id='RQ68';Role='Customer';Goal='Xóa sản phẩm khỏi Wishlist';Reason='Tôi không còn quan tâm đến sản phẩm đó';Priority='Low';Accept='Xóa thành công; danh sách cập nhật';MoSCoW='Could';Note='Wishlist/Remove'},
    @{Id='RQ69';Role='Customer';Goal='Chỉnh sửa thông tin cá nhân (họ tên, SĐT, địa chỉ)';Reason='Thông tin của tôi luôn cập nhật chính xác';Priority='Medium';Accept='Lưu thành công; cập nhật ngay trên session';MoSCoW='Should';Note='Account/Profile POST'},
    @{Id='RQ70';Role='Customer';Goal='Xem và in hóa đơn cá nhân của đơn hàng đã hoàn thành';Reason='Tôi có chứng từ thanh toán';Priority='Medium';Accept='Hiển thị đầy đủ chi tiết; nút in';MoSCoW='Should';Note='Order/Invoice'},
    @{Id='RQ71';Role='Customer';Goal='Xem gợi ý từ khóa khi gõ tìm kiếm sản phẩm';Reason='Tôi tìm nhanh hơn không cần gõ hết tên';Priority='Low';Accept='AJAX trả gợi ý theo prefix; hiển thị dropdown';MoSCoW='Could';Note='Fruit/AutoComplete'},
    @{Id='RQ72';Role='Customer';Goal='Lọc sản phẩm theo khoảng giá min-max';Reason='Tôi tìm sản phẩm phù hợp ngân sách';Priority='Medium';Accept='Lọc đúng khoảng; kết hợp được với từ khóa';MoSCoW='Should';Note='Fruit/Index minPrice/maxPrice'},
    @{Id='RQ73';Role='Customer';Goal='Lọc sản phẩm theo xuất xứ';Reason='Tôi muốn mua hàng từ vùng cụ thể';Priority='Low';Accept='Hiển thị đúng sản phẩm theo xuất xứ chọn';MoSCoW='Could';Note='Fruit/Index origin filter'},
    @{Id='RQ74';Role='Customer';Goal='Xem phân trang khi danh sách sản phẩm dài';Reason='Trang load nhanh và dễ duyệt';Priority='Medium';Accept='Hiển thị 10 sản phẩm/trang; điều hướng được';MoSCoW='Should';Note='PaginationHelper'},
    @{Id='RQ75';Role='Quản lý';Goal='Quản lý danh mục trái cây (thêm/sửa/xóa)';Reason='Tổ chức sản phẩm theo nhóm để khách dễ tìm';Priority='High';Accept='CRUD đầy đủ; không xóa được khi còn sản phẩm';MoSCoW='Must';Note='CategoryController'},
    @{Id='RQ76';Role='Quản lý';Goal='Upload hình ảnh sản phẩm khi tạo hoặc chỉnh sửa';Reason='Sản phẩm có hình minh họa hấp dẫn khách';Priority='High';Accept='Validate định dạng và size; lưu file unique; xóa file cũ khi đổi';MoSCoW='Must';Note='Fruit Save/DeleteOldImage'},
    @{Id='RQ77';Role='Quản lý';Goal='Cập nhật trạng thái đơn hàng (Pending → Confirmed → Shipping → Delivered)';Reason='Khách hàng theo dõi tiến trình đơn của mình';Priority='High';Accept='Chỉ Staff/Admin được đổi; ghi nhận staffId thực hiện';MoSCoW='Must';Note='Order/UpdateStatus'},
    @{Id='RQ78';Role='Quản lý';Goal='Bật/tắt hiệu lực sản phẩm (IsActive)';Reason='Tạm ngừng bán sản phẩm hết mùa mà không xóa';Priority='Medium';Accept='Sản phẩm IsActive=false không hiện ở storefront';MoSCoW='Should';Note='Fruit.IsActive flag'},
    @{Id='RQ79';Role='Quản lý';Goal='Tạo coupon mới với % giảm và ngày hết hạn';Reason='Triển khai chương trình khuyến mãi nhanh chóng';Priority='Medium';Accept='Code unique; validate % giảm 1-100; lưu ngày hết hạn';MoSCoW='Should';Note='Coupon/Create'},
    @{Id='RQ80';Role='Quản lý';Goal='Xóa hoặc vô hiệu hóa coupon hết hạn';Reason='Loại bỏ mã không còn sử dụng';Priority='Low';Accept='Toggle IsActive; coupon hết hạn không apply được';MoSCoW='Could';Note='Coupon repository'},
    @{Id='RQ81';Role='Quản lý';Goal='Xem biểu đồ doanh thu 7 ngày gần nhất trên dashboard';Reason='Nắm xu hướng kinh doanh tuần qua';Priority='Medium';Accept='Biểu đồ cột; data đúng theo ngày';MoSCoW='Should';Note='Dashboard chart 7 days'},
    @{Id='RQ82';Role='Quản lý';Goal='Xem top 5 sản phẩm bán chạy trên dashboard';Reason='Biết được sản phẩm nào hiệu quả nhất';Priority='Medium';Accept='Xếp theo số lượng bán; hiển thị tên + số lượng';MoSCoW='Should';Note='Dashboard top 5'},
    @{Id='RQ83';Role='Quản lý';Goal='Xem danh sách sản phẩm tồn kho thấp dưới ngưỡng';Reason='Chủ động nhập thêm hàng';Priority='High';Accept='Hiển thị sản phẩm có stock < threshold; cảnh báo đỏ';MoSCoW='Must';Note='Dashboard low stock list'},
    @{Id='RQ84';Role='Quản lý';Goal='Xem số đơn hàng mới trong ngày trên dashboard';Reason='Nhanh chóng nắm khối lượng công việc';Priority='Medium';Accept='Đếm đúng số đơn ngày hôm nay; click xem danh sách';MoSCoW='Should';Note='Dashboard today orders'},
    @{Id='RQ85';Role='Quản lý';Goal='Xem tổng doanh thu tháng hiện tại';Reason='So sánh với tháng trước để đánh giá';Priority='Medium';Accept='Tính chính xác từ đơn Delivered; hiển thị format VND';MoSCoW='Should';Note='Dashboard monthly revenue'},
    @{Id='RQ86';Role='Quản lý';Goal='Xem số khách hàng mới đăng ký trong tháng';Reason='Đo lường hiệu quả marketing';Priority='Low';Accept='Đếm User mới có RoleId=Customer trong tháng';MoSCoW='Could';Note='Dashboard customer growth'},
    @{Id='RQ87';Role='Nhân viên';Goal='Xem danh sách đơn hàng cần xác nhận';Reason='Xử lý đơn nhanh chóng theo thứ tự';Priority='High';Accept='Lọc đơn Pending; sắp xếp theo thời gian đặt';MoSCoW='Must';Note='Order/Index status=Pending'},
    @{Id='RQ88';Role='Nhân viên';Goal='Tìm kiếm đơn theo khoảng ngày đặt hàng';Reason='Tra cứu đơn cũ trong khoảng thời gian cụ thể';Priority='Medium';Accept='Filter fromDate/toDate; kết quả chính xác';MoSCoW='Should';Note='Order/Index date filter'},
    @{Id='RQ89';Role='Quản lý';Goal='Ghi log mỗi lần xuất nhập tồn kho với lý do';Reason='Truy vết được mọi biến động hàng hóa';Priority='High';Accept='Mỗi giao dịch tạo bản ghi InventoryLog với userId, lý do';MoSCoW='Must';Note='InventoryLogRepository'},
    @{Id='RQ90';Role='Quản lý';Goal='Lọc inventory log theo sản phẩm cụ thể';Reason='Xem riêng lịch sử của từng mặt hàng';Priority='Medium';Accept='Filter theo fruitId; sắp xếp ngày giảm dần';MoSCoW='Should';Note='InventoryLog GetByFruit'},
    @{Id='RQ91';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống bắt buộc đăng nhập trước khi truy cập trang riêng tư';Reason='Bảo mật dữ liệu cá nhân và chức năng nội bộ';Priority='High';Accept='Trang yêu cầu auth → redirect Login; không bypass được URL';MoSCoW='Must';Note='RequireRole filter, returnUrl'},
    @{Id='RQ92';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống tự động chuyển hướng về trang chính phù hợp role sau đăng nhập';Reason='Người dùng vào đúng giao diện công việc của mình';Priority='Medium';Accept='Admin→Dashboard, Staff→Fruit, Customer→Home';MoSCoW='Should';Note='AccountController.RedirectToHome'},
    @{Id='RQ93';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống hiển thị thông báo lỗi thân thiện khi truy cập sai quyền';Reason='Người dùng hiểu rõ và không bối rối';Priority='Medium';Accept='Trang AccessDenied có ghi chú; nút quay về';MoSCoW='Should';Note='Home/AccessDenied'},
    @{Id='RQ94';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống chống tấn công CSRF cho mọi form POST';Reason='Bảo mật dữ liệu trước các kiểu tấn công phổ biến';Priority='High';Accept='Tất cả form POST có ValidateAntiForgeryToken';MoSCoW='Must';Note='[ValidateAntiForgeryToken] attribute'},
    @{Id='RQ95';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống lưu mật khẩu dưới dạng hash an toàn';Reason='Mật khẩu không bị lộ khi DB rò rỉ';Priority='High';Accept='Password lưu BCrypt hash; verify đúng khi login';MoSCoW='Must';Note='BCrypt.Net.BCrypt'},
    @{Id='RQ96';Role='Quản lý';Goal='Validate dữ liệu đầu vào ở cả client và server';Reason='Tránh dữ liệu rác và injection';Priority='High';Accept='ModelState.IsValid; kiểm tra trùng email/tên fruit';MoSCoW='Must';Note='ValidationHelper'},
    @{Id='RQ97';Role='Customer, Nhân viên, Quản lý';Goal='Giao diện responsive trên điện thoại và tablet';Reason='Sử dụng được mọi thiết bị';Priority='Medium';Accept='Layout co dãn theo viewport; menu hamburger trên mobile';MoSCoW='Should';Note='Bootstrap responsive'},
    @{Id='RQ98';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống có header và footer thống nhất trên mọi trang';Reason='Trải nghiệm liền mạch và chuyên nghiệp';Priority='Medium';Accept='_Layout/_LayoutUser dùng đồng nhất; logo + menu cố định';MoSCoW='Should';Note='Views/Shared layouts'},
    @{Id='RQ99';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống hiển thị số lượng giỏ hàng trên header';Reason='Tôi luôn biết đã có bao nhiêu món trong giỏ';Priority='Medium';Accept='Badge số lượng cập nhật khi add/remove';MoSCoW='Should';Note='ViewBag.CartCount'},
    @{Id='RQ100';Role='Customer, Nhân viên, Quản lý';Goal='Hệ thống hiển thị thông báo Success/Error sau mỗi hành động';Reason='Người dùng biết kết quả thao tác ngay';Priority='Medium';Accept='TempData[Success/Error] hiển thị toast/alert';MoSCoW='Should';Note='TempData pattern'},
    @{Id='RQ101';Role='Quản lý';Goal='Xuất danh sách sản phẩm ra file Excel';Reason='Báo cáo tồn kho hoặc chia sẻ với nhà cung cấp';Priority='Low';Accept='Tải file .xlsx có đủ cột; mã hóa UTF-8';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ102';Role='Quản lý';Goal='In phiếu nhập kho khi nhập hàng từ nhà cung cấp';Reason='Có chứng từ lưu trữ kế toán';Priority='Low';Accept='Phiếu có mã, ngày, danh sách hàng, tổng tiền';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ103';Role='Customer';Goal='Nhận email xác nhận khi đặt hàng thành công';Reason='Tôi có bằng chứng đơn hàng và thông tin theo dõi';Priority='Low';Accept='Email gửi tự động sau khi tạo đơn; có thông tin đơn';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ104';Role='Customer';Goal='Nhận email khi trạng thái đơn hàng thay đổi';Reason='Tôi cập nhật được tiến trình giao hàng';Priority='Low';Accept='Trigger gửi email khi UpdateStatus chạy';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ105';Role='Customer';Goal='Quên mật khẩu và đặt lại qua email';Reason='Lấy lại tài khoản khi không nhớ mật khẩu';Priority='Medium';Accept='Gửi link reset có token hết hạn 15 phút';MoSCoW='Should';Note='Chưa triển khai'},
    @{Id='RQ106';Role='Customer';Goal='Đăng nhập bằng tài khoản Google/Facebook';Reason='Tiện và nhanh hơn đăng nhập thông thường';Priority='Low';Accept='OAuth callback; tạo user mới nếu chưa tồn tại';MoSCoW="Won't";Note='Để dành sprint sau'},
    @{Id='RQ107';Role='Quản lý';Goal='Xem báo cáo tồn kho theo danh mục';Reason='Đánh giá hiệu quả từng nhóm sản phẩm';Priority='Low';Accept='Tổng hợp số lượng và giá trị theo CategoryId';MoSCoW='Could';Note='Chưa triển khai'},
    @{Id='RQ108';Role='Quản lý';Goal='Backup dữ liệu hệ thống định kỳ';Reason='Phòng tránh mất dữ liệu khi sự cố';Priority='Low';Accept='Job chạy hàng ngày; lưu file .bak';MoSCoW="Won't";Note='Vận hành DBA'},
    @{Id='RQ109';Role='Quản lý';Goal='Cấu hình ngưỡng tồn kho thấp cho từng sản phẩm';Reason='Mỗi sản phẩm có nhu cầu khác nhau';Priority='Low';Accept='Lưu MinStock trên Fruit; cảnh báo theo ngưỡng riêng';MoSCoW='Could';Note='Mở rộng schema'},
    @{Id='RQ110';Role='Customer';Goal='So sánh nhiều sản phẩm cùng lúc';Reason='Tôi chọn ra sản phẩm phù hợp nhất';Priority='Low';Accept='Chọn 2-4 sản phẩm; bảng so sánh giá, xuất xứ';MoSCoW="Won't";Note='Tính năng nâng cao'}
)

# ============================================================
# PLANNING POKER: ước lượng story points (Fibonacci 1,2,3,5,8,13)
# Cột: Việt | Phát | Tâm | Bình | Phong | Story Points (Chốt)
# ============================================================
$pokerEstimates = @{
    'RQ01' = @(2,3,2,2,3,2);    'RQ02' = @(3,3,2,3,3,3);    'RQ03' = @(3,3,3,3,2,3);
    'RQ04' = @(5,5,3,5,5,5);    'RQ05' = @(3,3,3,2,3,3);    'RQ06' = @(2,3,3,2,2,2);
    'RQ07' = @(8,8,5,8,8,8);    'RQ08' = @(3,3,3,3,3,3);    'RQ09' = @(2,1,2,2,2,2);
    'RQ10' = @(2,2,2,2,2,2);    'RQ11' = @(5,3,5,5,5,5);    'RQ12' = @(3,3,3,3,3,3);
    'RQ13' = @(5,5,5,5,5,5);    'RQ14' = @(3,3,3,3,3,3);    'RQ15' = @(3,3,3,2,3,3);
    'RQ16' = @(2,1,2,2,2,2);    'RQ17' = @(3,3,3,3,3,3);    'RQ18' = @(1,1,2,1,1,1);
    'RQ19' = @(2,2,2,2,2,2);    'RQ20' = @(3,3,2,3,3,3);    'RQ21' = @(5,5,5,5,5,5);
    'RQ22' = @(2,2,3,2,2,2);    'RQ23' = @(5,5,3,5,5,5);    'RQ24' = @(5,5,5,3,5,5);
    'RQ25' = @(3,5,3,3,3,3);    'RQ26' = @(3,3,3,3,3,3);    'RQ27' = @(3,3,3,3,3,3);
    'RQ28' = @(5,5,5,5,5,5);    'RQ29' = @(3,3,3,3,3,3);    'RQ30' = @(5,5,3,5,5,5);
    'RQ31' = @(5,3,5,5,5,5);    'RQ32' = @(5,5,5,5,5,5);    'RQ33' = @(8,8,5,8,8,8);
    'RQ34' = @(3,3,3,3,3,3);    'RQ35' = @(3,3,3,3,3,3);    'RQ36' = @(3,3,3,3,3,3);
    'RQ37' = @(3,3,3,3,3,3);    'RQ38' = @(5,5,5,5,5,5);    'RQ39' = @(2,1,2,2,2,2);
    'RQ40' = @(2,2,2,2,2,2);    'RQ41' = @(3,3,3,3,3,3);    'RQ42' = @(3,2,3,3,3,3);
    'RQ43' = @(2,2,2,2,2,2);    'RQ44' = @(2,2,2,2,2,2);    'RQ45' = @(2,3,2,2,2,2);
    'RQ46' = @(8,8,5,8,8,8);    'RQ47' = @(3,3,3,3,3,3);    'RQ48' = @(2,1,2,2,2,2);
    'RQ49' = @(3,3,3,3,3,3);
    'RQ50' = @(3,3,3,2,3,3);    'RQ51' = @(3,3,2,3,3,3);    'RQ52' = @(2,2,2,2,2,2);
    'RQ53' = @(2,2,2,2,2,2);    'RQ54' = @(3,3,2,3,3,3);    'RQ55' = @(3,3,3,3,3,3);
    'RQ56' = @(2,2,2,2,2,2);    'RQ57' = @(1,1,2,1,1,1);    'RQ58' = @(3,3,3,3,3,3);
    'RQ59' = @(5,5,5,5,5,5);    'RQ60' = @(2,2,2,2,2,2);    'RQ61' = @(3,3,3,3,3,3);
    'RQ62' = @(2,2,2,2,2,2);    'RQ63' = @(3,2,3,3,3,3);    'RQ64' = @(5,5,5,5,5,5);
    'RQ65' = @(3,3,3,3,3,3);    'RQ66' = @(3,3,3,3,3,3);    'RQ67' = @(2,2,2,2,2,2);
    'RQ68' = @(1,1,1,1,1,1);    'RQ69' = @(2,2,2,2,2,2);    'RQ70' = @(3,3,3,3,3,3);
    'RQ71' = @(3,3,3,3,3,3);    'RQ72' = @(2,2,2,2,2,2);    'RQ73' = @(2,2,2,2,2,2);
    'RQ74' = @(2,2,2,2,2,2);    'RQ75' = @(3,3,3,3,3,3);    'RQ76' = @(3,3,3,3,3,3);
    'RQ77' = @(3,3,3,3,3,3);    'RQ78' = @(1,1,2,1,1,1);    'RQ79' = @(3,3,3,3,3,3);
    'RQ80' = @(2,2,2,2,2,2);    'RQ81' = @(5,5,3,5,5,5);    'RQ82' = @(3,3,3,3,3,3);
    'RQ83' = @(3,3,3,3,3,3);    'RQ84' = @(2,2,2,2,2,2);    'RQ85' = @(3,3,3,3,3,3);
    'RQ86' = @(2,2,2,2,2,2);    'RQ87' = @(2,2,2,2,2,2);    'RQ88' = @(2,2,2,2,2,2);
    'RQ89' = @(3,3,3,3,3,3);    'RQ90' = @(2,2,2,2,2,2);    'RQ91' = @(3,3,3,3,3,3);
    'RQ92' = @(2,2,2,2,2,2);    'RQ93' = @(2,1,2,2,2,2);    'RQ94' = @(2,2,2,2,2,2);
    'RQ95' = @(3,3,3,3,3,3);    'RQ96' = @(3,3,3,3,3,3);    'RQ97' = @(5,5,5,5,5,5);
    'RQ98' = @(3,3,3,3,3,3);    'RQ99' = @(2,1,2,2,2,2);    'RQ100' = @(2,2,2,2,2,2);
    'RQ101' = @(5,5,5,5,5,5);   'RQ102' = @(3,3,3,3,3,3);   'RQ103' = @(5,5,5,5,5,5);
    'RQ104' = @(3,3,3,3,3,3);   'RQ105' = @(5,5,5,5,5,5);   'RQ106' = @(8,8,8,8,8,8);
    'RQ107' = @(3,3,3,3,3,3);   'RQ108' = @(5,5,5,5,5,5);   'RQ109' = @(2,2,2,2,2,2);
    'RQ110' = @(5,5,5,5,5,5)
}

# ============================================================
# SPRINT PLANNING: phân bổ user story vào 3 sprint
# Sprint 1 — Core System (Đăng nhập, CRUD trái cây, danh mục, role)
# Sprint 2 — Sales System (Giỏ hàng, đặt hàng, hóa đơn, thanh toán)
# Sprint 3 — Advanced Features (Báo cáo, dashboard, khuyến mãi, ...)
# ============================================================
$sprint1 = @('RQ02','RQ03','RQ06','RQ09','RQ19','RQ23','RQ39','RQ48','RQ14','RQ50','RQ75','RQ76','RQ91','RQ94','RQ95','RQ96','RQ98')
$sprint2 = @('RQ01','RQ04','RQ07','RQ08','RQ10','RQ11','RQ12','RQ13','RQ15','RQ16','RQ17','RQ18','RQ20','RQ26','RQ42','RQ43','RQ44','RQ51','RQ52','RQ53','RQ54','RQ55','RQ56','RQ57','RQ58','RQ59','RQ60','RQ62','RQ63','RQ69','RQ70','RQ72','RQ74','RQ77','RQ78','RQ87','RQ92','RQ99','RQ100')
$sprint3 = @('RQ05','RQ21','RQ22','RQ24','RQ25','RQ27','RQ28','RQ29','RQ30','RQ31','RQ32','RQ33','RQ34','RQ35','RQ36','RQ37','RQ38','RQ40','RQ41','RQ45','RQ46','RQ47','RQ49','RQ61','RQ64','RQ65','RQ66','RQ67','RQ68','RQ71','RQ73','RQ79','RQ80','RQ81','RQ82','RQ83','RQ84','RQ85','RQ86','RQ88','RQ89','RQ90','RQ93','RQ97','RQ101','RQ102','RQ103','RQ104','RQ105','RQ106','RQ107','RQ108','RQ109','RQ110')

# ============================================================
# Build sharedStrings — collect all unique strings
# ============================================================
$strings = New-Object System.Collections.ArrayList
$strIndex = @{}
function Get-StrIdx($s) {
    if ($null -eq $s) { $s = '' }
    if ($strIndex.ContainsKey($s)) { return $strIndex[$s] }
    $idx = $strings.Count
    [void]$strings.Add($s)
    $strIndex[$s] = $idx
    return $idx
}

# Pre-add header strings
$IDX_TITLE_PB        = Get-StrIdx 'PRODUCT BACKLOG — Hệ Thống Quản Lý Cửa Hàng Trái Cây'
$IDX_GROUP           = Get-StrIdx 'Môn: Quản lý dự án phần mềm  |  Nhóm: Nguyễn Xuân Việt, Nguyễn Gia Phát, Nguyễn Vũ Hoài Tâm, Nguyễn Thanh Bình, Nguyễn Hoài Phong'
$IDX_ID              = Get-StrIdx 'ID'
$IDX_ROLE            = Get-StrIdx 'Vai trò'
$IDX_GOAL            = Get-StrIdx 'Mục tiêu (I want to)'
$IDX_REASON          = Get-StrIdx 'Lý do (So that)'
$IDX_PRIORITY        = Get-StrIdx 'Priority'
$IDX_ACCEPT          = Get-StrIdx 'Acceptance Criteria'
$IDX_MOSCOW          = Get-StrIdx 'MoSCoW'
$IDX_NOTE            = Get-StrIdx 'Ghi chú'
$IDX_TONG_CONG       = Get-StrIdx 'TỔNG CỘNG'
$IDX_TITLE_POKER     = Get-StrIdx 'PLANNING POKER — Story Point Estimation'
$IDX_GROUP_POKER     = Get-StrIdx 'Thành viên: Nguyễn Xuân Việt | Nguyễn Gia Phát | Nguyễn Vũ Hoài Tâm | Nguyễn Thanh Bình | Nguyễn Hoài Phong'
$IDX_USER_STORY_SHRT = Get-StrIdx 'User Story (tóm tắt)'
$IDX_VIET            = Get-StrIdx 'Việt'
$IDX_PHAT            = Get-StrIdx 'Phát'
$IDX_TAM             = Get-StrIdx 'Tâm'
$IDX_BINH            = Get-StrIdx 'Bình'
$IDX_PHONG           = Get-StrIdx 'Phong'
$IDX_SP_CHOT         = Get-StrIdx "Story Points`n(Chốt)"
$IDX_TOTAL_SP        = Get-StrIdx 'TỔNG STORY POINTS'
$IDX_TITLE_SPRINT    = Get-StrIdx 'SPRINT PLANNING — Phân bổ User Stories'
$IDX_USER_STORY      = Get-StrIdx 'User Story'
$IDX_SP              = Get-StrIdx 'Story Points'
$IDX_STATE           = Get-StrIdx 'State'
$IDX_NEW             = Get-StrIdx 'New'

$totalSp1 = ($sprint1 | ForEach-Object { $pokerEstimates[$_][5] } | Measure-Object -Sum).Sum
$totalSp2 = ($sprint2 | ForEach-Object { $pokerEstimates[$_][5] } | Measure-Object -Sum).Sum
$totalSp3 = ($sprint3 | ForEach-Object { $pokerEstimates[$_][5] } | Measure-Object -Sum).Sum
$IDX_SPRINT1_HDR     = Get-StrIdx "Sprint 1 — Core System   |   Total: $totalSp1 Story Points   |   $($sprint1.Count) User Stories"
$IDX_SPRINT2_HDR     = Get-StrIdx "Sprint 2 — Sales System   |   Total: $totalSp2 Story Points   |   $($sprint2.Count) User Stories"
$IDX_SPRINT3_HDR     = Get-StrIdx "Sprint 3 — Advanced Features   |   Total: $totalSp3 Story Points   |   $($sprint3.Count) User Stories"

# Pre-resolve indices for each story field
foreach ($s in $stories) {
    $s.IDX_Id     = Get-StrIdx $s.Id
    $s.IDX_Role   = Get-StrIdx $s.Role
    $s.IDX_Goal   = Get-StrIdx $s.Goal
    $s.IDX_Reason = Get-StrIdx $s.Reason
    $s.IDX_Pri    = Get-StrIdx $s.Priority
    $s.IDX_Acc    = Get-StrIdx $s.Accept
    $s.IDX_Mos    = Get-StrIdx $s.MoSCoW
    $s.IDX_Note   = Get-StrIdx $s.Note
    # Short form for poker (truncate to ~70 chars)
    $short = $s.Goal
    if ($short.Length -gt 70) { $short = $short.Substring(0, 70) + '...' }
    $s.IDX_Short  = Get-StrIdx $short
}

# ============================================================
# Helper: XML cell builders
# ============================================================
function CellStr($ref, $style, $idx) { "<c r=`"$ref`" s=`"$style`" t=`"s`"><v>$idx</v></c>" }
function CellNum($ref, $style, $val) { "<c r=`"$ref`" s=`"$style`"><v>$val</v></c>" }
function CellEmpty($ref, $style) { "<c r=`"$ref`" s=`"$style`"/>" }

function XmlEsc($s) {
    if ($null -eq $s) { return '' }
    return ($s -replace '&','&amp;' -replace '<','&lt;' -replace '>','&gt;')
}

# Style legend (using existing styles.xml indices):
# s=2 : header bordered blue bg (used for title row)
# s=4 : data cell bordered (white)
# s=11: title row (large white on dark blue)
# s=12,13: subtitle row
# s=14: alt row light blue (E8F0FE)
# s=15: header dark blue
# s=18: empty bordered cell
# s=20: header wrapped

# ============================================================
# SHEET 1: Product Backlog
# ============================================================
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>')
[void]$sb.Append('<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="x14ac xr xr2 xr3" xmlns:x14ac="http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac" xmlns:xr="http://schemas.microsoft.com/office/spreadsheetml/2014/revision" xmlns:xr2="http://schemas.microsoft.com/office/spreadsheetml/2015/revision2" xmlns:xr3="http://schemas.microsoft.com/office/spreadsheetml/2016/revision3" xr:uid="{00000000-0001-0000-0000-000000000000}">')
$pbTotalRow = $stories.Count + 5  # header rows 1-4 + data + 1 total row
[void]$sb.Append("<dimension ref=`"A1:H$pbTotalRow`"/>")
[void]$sb.Append('<sheetViews><sheetView tabSelected="1" workbookViewId="0"><pane ySplit="4" topLeftCell="A5" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>')
[void]$sb.Append('<sheetFormatPr defaultColWidth="14.42578125" defaultRowHeight="15" customHeight="1" x14ac:dyDescent="0.25"/>')
[void]$sb.Append('<cols><col min="1" max="1" width="8" customWidth="1"/><col min="2" max="2" width="22" customWidth="1"/><col min="3" max="3" width="40" customWidth="1"/><col min="4" max="4" width="36" customWidth="1"/><col min="5" max="5" width="10" customWidth="1"/><col min="6" max="6" width="38" customWidth="1"/><col min="7" max="7" width="10" customWidth="1"/><col min="8" max="8" width="28" customWidth="1"/></cols>')
[void]$sb.Append('<sheetData>')

# Row 1 - Title (merged A1:H1)
[void]$sb.Append('<row r="1" ht="32" customHeight="1">')
[void]$sb.Append((CellStr 'A1' 11 $IDX_TITLE_PB))
foreach ($c in 'B','C','D','E','F','G') { [void]$sb.Append((CellEmpty "${c}1" 12)) }
[void]$sb.Append((CellEmpty 'H1' 13))
[void]$sb.Append('</row>')

# Row 2 - Subtitle
[void]$sb.Append('<row r="2" ht="20" customHeight="1">')
[void]$sb.Append((CellStr 'A2' 14 $IDX_GROUP))
foreach ($c in 'B','C','D','E','F','G') { [void]$sb.Append((CellEmpty "${c}2" 12)) }
[void]$sb.Append((CellEmpty 'H2' 13))
[void]$sb.Append('</row>')

# Row 3 - blank spacer
[void]$sb.Append('<row r="3" ht="6" customHeight="1"/>')

# Row 4 - Headers
[void]$sb.Append('<row r="4" ht="36" customHeight="1">')
$headers = @($IDX_ID, $IDX_ROLE, $IDX_GOAL, $IDX_REASON, $IDX_PRIORITY, $IDX_ACCEPT, $IDX_MOSCOW, $IDX_NOTE)
$letters = 'A','B','C','D','E','F','G','H'
for ($i = 0; $i -lt 8; $i++) { [void]$sb.Append((CellStr "$($letters[$i])4" 2 $headers[$i])) }
[void]$sb.Append('</row>')

# Rows 5..53 - data
$rowNum = 5
foreach ($s in $stories) {
    $style = if (($rowNum % 2) -eq 1) { 4 } else { 14 }
    [void]$sb.Append("<row r=`"$rowNum`" ht=`"42`" customHeight=`"1`">")
    [void]$sb.Append((CellStr "A$rowNum" $style $s.IDX_Id))
    [void]$sb.Append((CellStr "B$rowNum" $style $s.IDX_Role))
    [void]$sb.Append((CellStr "C$rowNum" $style $s.IDX_Goal))
    [void]$sb.Append((CellStr "D$rowNum" $style $s.IDX_Reason))
    [void]$sb.Append((CellStr "E$rowNum" $style $s.IDX_Pri))
    [void]$sb.Append((CellStr "F$rowNum" $style $s.IDX_Acc))
    [void]$sb.Append((CellStr "G$rowNum" $style $s.IDX_Mos))
    [void]$sb.Append((CellStr "H$rowNum" $style $s.IDX_Note))
    [void]$sb.Append('</row>')
    $rowNum++
}

# Total row - TỔNG CỘNG
[void]$sb.Append("<row r=`"$pbTotalRow`" ht=`"24`" customHeight=`"1`">")
[void]$sb.Append((CellStr "A$pbTotalRow" 10 $IDX_TONG_CONG))
foreach ($c in 'B','C','D','E','F','G','H') { [void]$sb.Append((CellEmpty "${c}$pbTotalRow" 10)) }
[void]$sb.Append('</row>')

[void]$sb.Append('</sheetData>')
[void]$sb.Append('<mergeCells count="2"><mergeCell ref="A1:H1"/><mergeCell ref="A2:H2"/></mergeCells>')
[void]$sb.Append('<pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>')
[void]$sb.Append('</worksheet>')
$sheet1Xml = $sb.ToString()

# ============================================================
# SHEET 2: Planning Poker
# ============================================================
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>')
[void]$sb.Append('<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="x14ac xr xr2 xr3" xmlns:x14ac="http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac" xmlns:xr="http://schemas.microsoft.com/office/spreadsheetml/2014/revision" xmlns:xr2="http://schemas.microsoft.com/office/spreadsheetml/2015/revision2" xmlns:xr3="http://schemas.microsoft.com/office/spreadsheetml/2016/revision3" xr:uid="{00000000-0001-0000-0001-000000000000}">')
$pkTotalRow = $stories.Count + 5
[void]$sb.Append("<dimension ref=`"A1:H$pkTotalRow`"/>")
[void]$sb.Append('<sheetViews><sheetView workbookViewId="0"><pane ySplit="4" topLeftCell="A5" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>')
[void]$sb.Append('<sheetFormatPr defaultColWidth="14.42578125" defaultRowHeight="15" customHeight="1" x14ac:dyDescent="0.25"/>')
[void]$sb.Append('<cols><col min="1" max="1" width="8" customWidth="1"/><col min="2" max="2" width="55" customWidth="1"/><col min="3" max="7" width="9" customWidth="1"/><col min="8" max="8" width="14" customWidth="1"/></cols>')
[void]$sb.Append('<sheetData>')

# Row 1 - Title
[void]$sb.Append('<row r="1" ht="32" customHeight="1">')
[void]$sb.Append((CellStr 'A1' 11 $IDX_TITLE_POKER))
foreach ($c in 'B','C','D','E','F','G') { [void]$sb.Append((CellEmpty "${c}1" 12)) }
[void]$sb.Append((CellEmpty 'H1' 13))
[void]$sb.Append('</row>')

# Row 2 - Subtitle
[void]$sb.Append('<row r="2" ht="20" customHeight="1">')
[void]$sb.Append((CellStr 'A2' 14 $IDX_GROUP_POKER))
foreach ($c in 'B','C','D','E','F','G') { [void]$sb.Append((CellEmpty "${c}2" 12)) }
[void]$sb.Append((CellEmpty 'H2' 13))
[void]$sb.Append('</row>')

# Row 3 - spacer
[void]$sb.Append('<row r="3" ht="6" customHeight="1"/>')

# Row 4 - Headers (using s=7 for green-blue header)
[void]$sb.Append('<row r="4" ht="36" customHeight="1">')
$pHdr = @($IDX_ID, $IDX_USER_STORY_SHRT, $IDX_VIET, $IDX_PHAT, $IDX_TAM, $IDX_BINH, $IDX_PHONG, $IDX_SP_CHOT)
for ($i = 0; $i -lt 8; $i++) { [void]$sb.Append((CellStr "$($letters[$i])4" 7 $pHdr[$i])) }
[void]$sb.Append('</row>')

# Rows 5..53 - poker estimates per story
$rowNum = 5
foreach ($s in $stories) {
    $est = $pokerEstimates[$s.Id]
    $style = if (($rowNum % 2) -eq 1) { 4 } else { 14 }
    [void]$sb.Append("<row r=`"$rowNum`" ht=`"22`" customHeight=`"1`">")
    [void]$sb.Append((CellStr "A$rowNum" $style $s.IDX_Id))
    [void]$sb.Append((CellStr "B$rowNum" $style $s.IDX_Short))
    [void]$sb.Append((CellNum "C$rowNum" $style $est[0]))
    [void]$sb.Append((CellNum "D$rowNum" $style $est[1]))
    [void]$sb.Append((CellNum "E$rowNum" $style $est[2]))
    [void]$sb.Append((CellNum "F$rowNum" $style $est[3]))
    [void]$sb.Append((CellNum "G$rowNum" $style $est[4]))
    [void]$sb.Append((CellNum "H$rowNum" $style $est[5]))
    [void]$sb.Append('</row>')
    $rowNum++
}

# Total row - TỔNG STORY POINTS
$totalAll = $totalSp1 + $totalSp2 + $totalSp3
[void]$sb.Append("<row r=`"$pkTotalRow`" ht=`"26`" customHeight=`"1`">")
[void]$sb.Append((CellStr "A$pkTotalRow" 10 $IDX_TOTAL_SP))
foreach ($c in 'B','C','D','E','F','G') { [void]$sb.Append((CellEmpty "${c}$pkTotalRow" 10)) }
[void]$sb.Append((CellNum "H$pkTotalRow" 10 $totalAll))
[void]$sb.Append('</row>')

[void]$sb.Append('</sheetData>')
[void]$sb.Append("<mergeCells count=`"3`"><mergeCell ref=`"A1:H1`"/><mergeCell ref=`"A2:H2`"/><mergeCell ref=`"A${pkTotalRow}:G${pkTotalRow}`"/></mergeCells>")
[void]$sb.Append('<pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>')
[void]$sb.Append('</worksheet>')
$sheet2Xml = $sb.ToString()

# ============================================================
# SHEET 3: Sprint Planning
# ============================================================
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>')
[void]$sb.Append('<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="x14ac xr xr2 xr3" xmlns:x14ac="http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac" xmlns:xr="http://schemas.microsoft.com/office/spreadsheetml/2014/revision" xmlns:xr2="http://schemas.microsoft.com/office/spreadsheetml/2015/revision2" xmlns:xr3="http://schemas.microsoft.com/office/spreadsheetml/2016/revision3" xr:uid="{00000000-0001-0000-0002-000000000000}">')
[void]$sb.Append("<dimension ref=`"A1:F$($stories.Count + 20)`"/>")
[void]$sb.Append('<sheetViews><sheetView workbookViewId="0"/></sheetViews>')
[void]$sb.Append('<sheetFormatPr defaultColWidth="14.42578125" defaultRowHeight="15" customHeight="1" x14ac:dyDescent="0.25"/>')
[void]$sb.Append('<cols><col min="1" max="1" width="8" customWidth="1"/><col min="2" max="2" width="55" customWidth="1"/><col min="3" max="3" width="11" customWidth="1"/><col min="4" max="4" width="13" customWidth="1"/><col min="5" max="5" width="10" customWidth="1"/><col min="6" max="6" width="11" customWidth="1"/></cols>')
[void]$sb.Append('<sheetData>')

# Row 1 - Title
[void]$sb.Append('<row r="1" ht="32" customHeight="1">')
[void]$sb.Append((CellStr 'A1' 11 $IDX_TITLE_SPRINT))
foreach ($c in 'B','C','D','E') { [void]$sb.Append((CellEmpty "${c}1" 12)) }
[void]$sb.Append((CellEmpty 'F1' 13))
[void]$sb.Append('</row>')

# Row 2 - spacer
[void]$sb.Append('<row r="2" ht="6" customHeight="1"/>')

$rowNum = 3
$IDX_DONE = Get-StrIdx 'Done'
$IDX_TODO = Get-StrIdx 'To Do'

# RQ IDs that are confirmed implemented in the codebase
$doneSet = @{}
foreach ($id in (@(
    # Sprint 1 - all done
    'RQ02','RQ03','RQ06','RQ09','RQ19','RQ23','RQ39','RQ48','RQ14','RQ50','RQ75','RQ76','RQ91','RQ94','RQ95','RQ96','RQ98',
    # Sprint 2 - all done
    'RQ01','RQ04','RQ07','RQ08','RQ10','RQ11','RQ12','RQ13','RQ15','RQ16','RQ17','RQ18','RQ20','RQ26','RQ42','RQ43','RQ44',
    'RQ51','RQ52','RQ53','RQ54','RQ55','RQ56','RQ57','RQ58','RQ59','RQ60','RQ62','RQ63','RQ69','RQ70','RQ72','RQ74',
    'RQ77','RQ78','RQ87','RQ92','RQ99','RQ100',
    # Sprint 3 - implemented subset (original)
    'RQ21','RQ22','RQ24','RQ25','RQ27','RQ31','RQ33','RQ40','RQ45','RQ49',
    'RQ61','RQ64','RQ65','RQ66','RQ67','RQ68','RQ71','RQ73',
    'RQ79','RQ80','RQ81','RQ82','RQ83','RQ84','RQ85','RQ88','RQ89','RQ90','RQ93','RQ97',
    # Sprint 3 - newly implemented in current session
    'RQ05',   # AdminSupplierController — CRUD nhà cung cấp
    'RQ28',   # BatchController + BatchRepository — lô hàng + hạn sử dụng
    'RQ29',   # BatchController.ExpiryWarning — cảnh báo hàng sắp hết hạn
    'RQ30',   # DashboardController.ExportRevenueExcel — xuất doanh thu Excel
    'RQ32',   # OperatingCostController — lợi nhuận ròng = doanh thu - chi phí
    'RQ34',   # User.Tier + AddPoints — phân hạng khách hàng tự động
    'RQ35',   # User.Points + AddPoints — điểm tích lũy
    'RQ41',   # FruitRepository.GetAlternatives — gợi ý sản phẩm thay thế
    'RQ46',   # FruitController.ImportExcel (EPPlus) — nhập hàng loạt từ Excel
    'RQ47',   # OperatingCostController — chi phí vận hành theo tháng
    'RQ86',   # UserRepository.CountNewCustomersThisMonth + Dashboard widget
    'RQ101',  # FruitController.ExportCsv — xuất sản phẩm CSV
    'RQ102',  # BatchController.PrintReceipt — in phiếu nhập kho
    'RQ103',  # OrderController email on Checkout — xác nhận đặt hàng
    'RQ104',  # OrderController email on UpdateStatus — cập nhật trạng thái
    'RQ105',  # AccountController.ForgotPassword / ResetPassword
    'RQ106',  # AccountController.ExternalLogin Google/Facebook
    'RQ107',  # OrderRepository.GetRevenueByCategory — báo cáo theo danh mục
    'RQ109'   # Fruit.MinStock — ngưỡng tồn kho tối thiểu per sản phẩm
))) { $doneSet[$id] = $true }

# Helper to build sprint block — state is looked up per story via $doneSet
function Build-Sprint([System.Text.StringBuilder]$sb, [int]$startRow, [int]$titleIdxRef, [string[]]$rqIds, [int]$bgStyle) {
    $r = $startRow
    # Title row of sprint
    [void]$sb.Append("<row r=`"$r`" ht=`"24`" customHeight=`"1`">")
    [void]$sb.Append((CellStr "A$r" $bgStyle $titleIdxRef))
    foreach ($c in 'B','C','D','E') { [void]$sb.Append((CellEmpty "${c}$r" $bgStyle)) }
    [void]$sb.Append((CellEmpty "F$r" $bgStyle))
    [void]$sb.Append('</row>')
    $r++
    # Header row
    [void]$sb.Append("<row r=`"$r`" ht=`"30`" customHeight=`"1`">")
    [void]$sb.Append((CellStr "A$r" 2 $script:IDX_ID))
    [void]$sb.Append((CellStr "B$r" 2 $script:IDX_USER_STORY))
    [void]$sb.Append((CellStr "C$r" 2 $script:IDX_PRIORITY))
    [void]$sb.Append((CellStr "D$r" 2 $script:IDX_SP))
    [void]$sb.Append((CellStr "E$r" 2 $script:IDX_MOSCOW))
    [void]$sb.Append((CellStr "F$r" 2 $script:IDX_STATE))
    [void]$sb.Append('</row>')
    $r++
    foreach ($id in $rqIds) {
        $story     = $script:stories | Where-Object { $_.Id -eq $id } | Select-Object -First 1
        $sp        = $script:pokerEstimates[$id][5]
        $style     = if (($r % 2) -eq 1) { 4 } else { 14 }
        $stateIdx  = if ($script:doneSet.ContainsKey($id)) { $script:IDX_DONE } else { $script:IDX_TODO }
        [void]$sb.Append("<row r=`"$r`" ht=`"22`" customHeight=`"1`">")
        [void]$sb.Append((CellStr "A$r" $style $story.IDX_Id))
        [void]$sb.Append((CellStr "B$r" $style $story.IDX_Short))
        [void]$sb.Append((CellStr "C$r" $style $story.IDX_Pri))
        [void]$sb.Append((CellNum "D$r" $style $sp))
        [void]$sb.Append((CellStr "E$r" $style $story.IDX_Mos))
        [void]$sb.Append((CellStr "F$r" $style $stateIdx))
        [void]$sb.Append('</row>')
        $r++
    }
    return $r
}

# Sprint 1
$rowNum = Build-Sprint $sb $rowNum $IDX_SPRINT1_HDR $sprint1 5
# Spacer
[void]$sb.Append("<row r=`"$rowNum`" ht=`"10`" customHeight=`"1`"/>")
$rowNum++
# Sprint 2
$rowNum = Build-Sprint $sb $rowNum $IDX_SPRINT2_HDR $sprint2 9
# Spacer
[void]$sb.Append("<row r=`"$rowNum`" ht=`"10`" customHeight=`"1`"/>")
$rowNum++
# Sprint 3 (mixed Done / To Do per story)
$rowNum = Build-Sprint $sb $rowNum $IDX_SPRINT3_HDR $sprint3 19

[void]$sb.Append('</sheetData>')
[void]$sb.Append('<pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>')
[void]$sb.Append('</worksheet>')
$sheet3Xml = $sb.ToString()

# ============================================================
# SharedStrings.xml
# ============================================================
$ss = New-Object System.Text.StringBuilder
[void]$ss.AppendLine('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>')
[void]$ss.Append("<sst xmlns=`"http://schemas.openxmlformats.org/spreadsheetml/2006/main`" count=`"$($strings.Count)`" uniqueCount=`"$($strings.Count)`">")
foreach ($s in $strings) {
    $esc = XmlEsc $s
    if ($esc -match "[`r`n]" -or $esc -match '^\s' -or $esc -match '\s$') {
        [void]$ss.Append("<si><t xml:space=`"preserve`">$esc</t></si>")
    } else {
        [void]$ss.Append("<si><t>$esc</t></si>")
    }
}
[void]$ss.Append('</sst>')
$ssXml = $ss.ToString()

# ============================================================
# Repackage XLSX
# ============================================================
$src = 'C:\Users\Vietn\AppData\Local\Temp\xlsx_extract_356711333'
$work = "$env:TEMP\xlsx_build_$(Get-Random)"
Copy-Item -Recurse -Path $src -Destination $work

# Write new files (UTF-8 NO BOM)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText("$work\xl\sharedStrings.xml",          $ssXml,     $utf8NoBom)
[System.IO.File]::WriteAllText("$work\xl\worksheets\sheet1.xml",      $sheet1Xml, $utf8NoBom)
[System.IO.File]::WriteAllText("$work\xl\worksheets\sheet2.xml",      $sheet2Xml, $utf8NoBom)
[System.IO.File]::WriteAllText("$work\xl\worksheets\sheet3.xml",      $sheet3Xml, $utf8NoBom)

# Drop calcChain.xml since formulas changed/removed
$calcChain = "$work\xl\calcChain.xml"
if (Test-Path $calcChain) { Remove-Item $calcChain -Force }

# Update [Content_Types].xml — remove calcChain content type if present
$ctPath = "$work\[Content_Types].xml"
$ct = [System.IO.File]::ReadAllText($ctPath, $utf8NoBom)
$ct = [System.Text.RegularExpressions.Regex]::Replace($ct, '<Override\s+PartName="/xl/calcChain\.xml"[^>]*?/>', '')
[System.IO.File]::WriteAllText($ctPath, $ct, $utf8NoBom)

# Update xl/_rels/workbook.xml.rels — remove calcChain rel
$relsPath = "$work\xl\_rels\workbook.xml.rels"
$rels = [System.IO.File]::ReadAllText($relsPath, $utf8NoBom)
$rels = [System.Text.RegularExpressions.Regex]::Replace($rels, '<Relationship\s[^>]*?Target="calcChain\.xml"[^>]*?/>', '')
[System.IO.File]::WriteAllText($relsPath, $rels, $utf8NoBom)

# Drop xl/metadata if exists - it's optional
$meta = "$work\xl\metadata"
if (Test-Path $meta) { Remove-Item $meta -Recurse -Force }
$ct = [System.IO.File]::ReadAllText($ctPath, $utf8NoBom)
$ct = [System.Text.RegularExpressions.Regex]::Replace($ct, '<Override\s+PartName="/xl/metadata[^"]*"[^>]*?/>', '')
[System.IO.File]::WriteAllText($ctPath, $ct, $utf8NoBom)
$rels = [System.IO.File]::ReadAllText($relsPath, $utf8NoBom)
$rels = [System.Text.RegularExpressions.Regex]::Replace($rels, '<Relationship\s[^>]*?Target="metadata[^"]*"[^>]*?/>', '')
[System.IO.File]::WriteAllText($relsPath, $rels, $utf8NoBom)

# Clean workbook.xml - remove orphan extLst (Google Sheets metadata) and activeTab
$wbPath = "$work\xl\workbook.xml"
$wb = [System.IO.File]::ReadAllText($wbPath, $utf8NoBom)
$wb = [System.Text.RegularExpressions.Regex]::Replace($wb, '<extLst>.*?</extLst>', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
$wb = $wb -replace 'activeTab="\d+"\s*', ''
[System.IO.File]::WriteAllText($wbPath, $wb, $utf8NoBom)

# Zip into new xlsx
$out = 'd:\ASM phattrienungdung\Product_Backlog_v2111.xlsx'
if (Test-Path $out) {
    try { Remove-Item $out -Force -ErrorAction Stop }
    catch {
        $out = 'd:\ASM phattrienungdung\Product_Backlog_v2111_new.xlsx'
        Write-Warning "Original file is locked (Excel may have it open). Writing to: $out"
        if (Test-Path $out) { Remove-Item $out -Force }
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($work, $out, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Write-Host "DONE. Output: $out"
Write-Host "Stories: $($stories.Count); Total SP = $totalAll"
Write-Host "Sprint 1 SP=$totalSp1 ($($sprint1.Count) stories)"
Write-Host "Sprint 2 SP=$totalSp2 ($($sprint2.Count) stories)"
Write-Host "Sprint 3 SP=$totalSp3 ($($sprint3.Count) stories)"
