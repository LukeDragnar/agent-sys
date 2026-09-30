---
alias: ["báo cáo marketing", "báo cáo MKT", "báo cáo lead theo nguồn", "báo cáo MKT theo sản phẩm", "thống kê marketing", "dashboard marketing"]
module: marketing
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Dashboard báo cáo lead theo nguồn & báo cáo MKT theo sản phẩm có key riêng (lọc theo user/nhóm MKT được phép xem); một số nút (chi tiết campaign, xuất Excel) chỉ yêu cầu đăng nhập — xem dev. Có giới hạn số người đọc báo cáo đồng thời."
phu_thuoc: ["Quản lý Lead / Contact (nguồn số liệu lead)", "Quản lý nguồn dữ liệu (landing) + ngân sách quảng cáo", "Đơn hàng / Đặt lịch (số liệu doanh số, tỷ lệ chốt)", "Cấu hình đơn vị (khoảng thời gian cho phép tìm kiếm, cơ cấu tổ chức MKT)"]
link_flow: ./flow.md
---

# Báo cáo Marketing

## Là gì
Đây là **bộ báo cáo phân tích kết quả Marketing**: lead đổ về theo nguồn nào, sản phẩm nào hiệu quả, ngân sách quảng cáo so với số contact và doanh thu ra sao. Mục tiêu là trả lời "tiền quảng cáo bỏ ra mang lại bao nhiêu khách và bao nhiêu đơn".

Các báo cáo chính:
- **Báo cáo lead theo nguồn (Dashboard Marketing):** gom lead theo từng nguồn / landing / chiến dịch, kèm số contact, tỷ lệ chốt, doanh số; có **biểu đồ số liệu landing 7 ngày gần nhất**, drill-down **chi tiết từng campaign**, và **xuất Excel**.
- **Báo cáo Marketing theo sản phẩm:** kết quả MKT gom theo sản phẩm (đơn hàng + số contact + landing + ngân sách), xuất Excel theo 2 cách gom (theo sản phẩm / theo nguồn dữ liệu).
- Cùng nhóm còn có: báo cáo doanh số tổng quan số liệu giao vận (góc nhìn MKT), báo cáo công việc MKT, báo cáo lead theo nhân sự MKT.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo lead theo nguồn | ✅ | có key riêng (lọc theo user/nhóm MKT được phép xem) |
| Xem biểu đồ landing 7 ngày | ✅ | có key riêng |
| Xem chi tiết từng campaign (drill-down) | ✅ | chỉ cần đăng nhập (chưa gắn key — xem dev) |
| Xuất Excel báo cáo lead theo nguồn | ✅ | chỉ cần đăng nhập (chưa gắn key) |
| Xem báo cáo Marketing theo sản phẩm | ✅ | có quyền riêng cho báo cáo này |
| Xuất Excel MKT theo SP (gom theo SP / theo nguồn) | ✅ | key riêng cho từng kiểu gom |
| Xem báo cáo doanh số giao vận (MKT view) / công việc MKT / lead theo nhân sự MKT | ✅ | key riêng từng báo cáo |
| Sửa / nhập dữ liệu trong báo cáo | ❌ | toàn bộ là **báo cáo chỉ đọc** — không ghi dữ liệu |

## Để làm gì / cho ai
- **Quản lý / Giám đốc Marketing:** đánh giá hiệu quả từng nguồn, từng landing, từng sản phẩm; quyết định tăng/giảm ngân sách kênh nào.
- **Nhân viên Marketing:** xem lead mình mang về, tỷ lệ chốt, đóng góp doanh số.
- **Ban lãnh đạo:** nhìn bức tranh chi phí quảng cáo so với doanh thu.

## Cách dùng
**Báo cáo lead theo nguồn:**
1. Vào **Marketing → Dashboard** (báo cáo lead theo nguồn).
2. Đặt bộ lọc: chi nhánh, phòng ban/nhóm/nhân viên MKT, từ–đến ngày, nguồn/landing/chiến dịch, điều kiện contact.
3. Xem lưới lead theo nguồn + biểu đồ landing 7 ngày. Bấm vào một dòng để xem **chi tiết campaign**.
4. Bấm **Xuất Excel** để tải báo cáo (`BaoCaoLeadTheoNguon.xlsx`).

**Báo cáo Marketing theo sản phẩm:**
1. Vào màn **Báo cáo MKT theo sản phẩm**.
2. Lọc: chi nhánh, từ–đến ngày, sản phẩm/nhóm sản phẩm, nguồn landing, nhân sự MKT → xem lưới.
3. **Xuất Excel** theo **gom theo sản phẩm** hoặc **gom theo nguồn dữ liệu**.

## Cần chú ý
- **Báo cáo chỉ đọc** — không sửa/nhập dữ liệu; mọi con số tính từ dữ liệu lead/đơn hàng/landing có sẵn.
- **Phạm vi xem theo vai trò:** mỗi người chỉ thấy số liệu của user/nhóm MKT mình được phép xem; admin thấy toàn đơn vị. Đây là phân quyền.
- **Có giới hạn số người đọc báo cáo cùng lúc:** nếu quá nhiều người mở báo cáo Marketing đồng thời, hệ thống có thể báo bận — thử lại sau.
- **Khoảng thời gian lọc bị giới hạn:** đơn vị có thể cấu hình chỉ cho tìm trong một khoảng ngày nhất định; vượt khoảng sẽ bị chặn.
- **Báo cáo nặng:** báo cáo lead theo nguồn gộp số liệu từ nhiều nguồn (contact, đơn hàng, giao vận, đặt lịch) nên có thể chạy chậm với khoảng ngày rộng — nên lọc hẹp lại.
- **Xuất Excel báo bận (503):** khi dịch vụ xuất đang quá tải, hệ thống báo "chưa xuất được lúc này" — thử lại sau.

## Ảnh hưởng tới
- **Quản lý Lead / Contact** — là nguồn số liệu lead cho báo cáo.
- **Quản lý nguồn dữ liệu (landing) & ngân sách quảng cáo** — quyết định cột "ngân sách" / "chi phí trên mỗi lead".
- **Đơn hàng / Đặt lịch** — cung cấp số liệu doanh số, tỷ lệ chốt để tính hiệu quả MKT.
