---
controller: LienHeController
module: website
service: Website
man_hinh_ui: "Website bán hàng (storefront public) — các trang nội dung tĩnh/CMS: Liên hệ, Giới thiệu, Tuyển dụng, Phương thức thanh toán, Hủy giao dịch, Chăm sóc, Bảo mật thông tin, Hướng dẫn thanh toán VNPay, Giao hàng, Đổi trả, Bảo hành, Hệ thống cửa hàng. Theme động (ThemeCode)."
muc_dich: "Render các trang nội dung/chính sách tĩnh của website bán hàng public theo theme động của đơn vị (mỗi action = 1 trang cshtml). Thuần hiển thị, không xử lý nghiệp vụ."
alias: ["liên hệ", "giới thiệu", "tuyển dụng", "chính sách", "phương thức thanh toán", "đổi trả", "bảo hành", "giao hàng", "bảo mật", "hệ thống cửa hàng", "trang tĩnh website"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LinkWebsite]
stored_procs: []
depends: [LinkWebsiteService]
cross_module_in: []
cross_module_out: []
---

# LienHeController — Trang nội dung/chính sách (storefront public)

> MVC `Controller` (trả về **View** Razor). `[TokenAuthentication]` cấp controller (soft filter, không chặn) → auth thực tế = `anonymous`. Mọi action cùng 1 khuôn: resolve đơn vị + theme theo **domain** (`LinkWebsiteService.GetDataByDomain`), nếu domain không map được → `NotFound()`, ngược lại render trang `Home/<Tên>.cshtml` theo `ThemeCode`. Không có nghiệp vụ ghi dữ liệu — thuần CMS tĩnh.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Trang Liên hệ | menu "Liên hệ" | GET `LienHe` | — | anonymous | luôn | Render `Home/LienHe.cshtml` theo theme | EF (đơn vị theo domain) | - |
| 2 | Trang Giới thiệu | "Giới thiệu" | GET `GioiThieu` | — | anonymous | luôn | Render `Home/GioiThieu.cshtml` | EF | - |
| 3 | Trang Tuyển dụng | "Tuyển dụng" | GET `TuyenDung` | — | anonymous | luôn | Render `Home/TuyenDung.cshtml` | EF | comment "hiện tại chưa có nên return View lỗi" (:68) — view có thể chưa hoàn thiện |
| 4 | Phương thức thanh toán | footer "Thanh toán" | GET `PhuongThucThanhToan` | — | anonymous | luôn | Render `Home/PhuongThucThanhToan.cshtml` | EF | - |
| 5 | Hủy giao dịch | footer "Hủy giao dịch" | GET `HuyGiaoDich` | — | anonymous | luôn | Render `Home/HuyGiaoDich.cshtml` | EF | - |
| 6 | Chăm sóc khách hàng | footer "Chăm sóc" | GET `Care` | — | anonymous | luôn | Render `Home/Care.cshtml` | EF | - |
| 7 | Bảo mật thông tin | footer "Bảo mật" | GET `BaoMatThongTin` | — | anonymous | luôn | Render `Home/BaoMatThongTin.cshtml` | EF | - |
| 8 | Hướng dẫn thanh toán VNPay | footer | GET `HuongDanThanhToanVnPay` | — | anonymous | luôn | Render `Home/HuongDanThanhToanVnPay.cshtml` | EF | - |
| 9 | Giao hàng | footer "Giao hàng" | GET `GiaoHang` | — | anonymous | luôn | Render `Home/GiaoHang.cshtml` | EF | - |
| 10 | Đổi trả | footer "Đổi trả" | GET `DoiTra` | — | anonymous | luôn | Render `Home/DoiTra.cshtml` | EF | - |
| 11 | Bảo hành | footer "Bảo hành" | GET `BaoHanh` | — | anonymous | luôn | Render `Home/BaoHanh.cshtml` | EF | - |
| 12 | Hệ thống cửa hàng | "Hệ thống cửa hàng" | GET `HeThongCuaHang` | — | anonymous | luôn | Render `Home/HeThongCuaHang.cshtml` | EF | - |

## Quan hệ
- **Gọi sang:** chỉ `ILinkWebsiteService.GetDataByDomain` (resolve đơn vị + theme + dữ liệu chung theo domain). Không gọi service nghiệp vụ khác.
- **Dữ liệu (entities):** chỉ đọc cấu hình LinkWebsite/đơn vị để chọn theme; không entity nghiệp vụ.
- **Ripple / đồng bộ:** không ghi → không ripple.

## Lỗi đã biết / lưu ý kỹ thuật
- **12 action gần như copy-paste hoàn toàn** (chỉ khác tên view cshtml). Có thể refactor về 1 action chung `Page(string viewName)` — hiện lặp lại y hệt khối resolve domain + deserialize model.
- **`TuyenDung`** có comment "hiện tại chưa có nên return View lỗi" (`LienHeController.cs:68`) — view có thể chưa có nội dung thật.
- **Nội dung trang là tĩnh trong cshtml theo theme**, không lấy từ DB CMS → muốn đổi nội dung phải sửa view theo từng ThemeCode.
