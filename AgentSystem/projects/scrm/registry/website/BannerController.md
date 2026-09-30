---
controller: BannerController
module: website
service: Website
man_hinh_ui: "Website > Quản lý banner — list banner theo vị trí/trạng thái, nút Thêm mới + Sửa/Xóa per-row + popup nhập banner (ảnh web/mobile, vị trí, thời gian hiển thị, link)"
muc_dich: "CRUD banner hiển thị trên website/app của đơn vị: tên, vị trí (EnumViTriBanner), ảnh web + ảnh mobile, khoảng thời gian hiển thị, link/kiểu mở link, số thứ tự, trạng thái sử dụng."
alias: ["banner", "quản lý banner", "banner website", "link website", "slide banner"]
auth: public
state_machine: []
deprecated: false
entities: [Banner]
stored_procs: []
depends: [Website]
cross_module_in: []
cross_module_out: [Website/CommonService (GetViTriBanner / GetKieuBanner cấp enum cho dropdown banner)]
---

# BannerController — Quản lý banner website

> Controller `partial` (`public partial class BannerController`) — có thể còn endpoint ở file partial khác. `[TienIchAttribute("BannerController", "Quản lý link website", MaTienIchSCRM.TI_Website)]`. Mọi truy vấn lọc theo `IdDonVi` của user đăng nhập.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (list banner) | POST `api/Banner/TimTheoDieuKien` | `Banner_TimTheoDieuKien` | public | luôn | List banner theo đơn vị, filter KeyWord (Ten.Contains) / ViTri / IsSuDung; sort ViTri desc → Stt desc → NgayCapNhat desc; phân trang; gán TenViTri từ enum | EF | - |
| 2 | Thêm mới banner | "Thêm mới" → popup | POST `api/Banner/ThemMoi` | `Banner_ThemMoi` | public | luôn | Validate Ten/ViTri/NgayBatDau bắt buộc; NgayKetThuc < NgayBatDau → lỗi; set IdDonVi + IdChiNhanh từ user; insert | EF | thông báo lỗi ngày viết ngược nghĩa: chặn `NgayKetThuc < NgayBatDau` nhưng message ghi "Ngày bắt đầu không được lớn hơn ngày kết thúc" (BannerService.cs:98) |
| 3 | Sửa / mở chi tiết | "Sửa" (icon edit, per-row) | GET `api/Banner/TimTheoId/{id:Guid}` + POST `CapNhatThongTin` | `Banner_TimTheoId` / `Banner_CapNhatThongTin` | public | per-row | TimTheoId trả DTO nếu record thuộc đúng IdDonVi (sai đơn vị → "Không tìm thấy nhà cung cấp!"); CapNhatThongTin validate lại + cập nhật các field (ảnh web/mobile, link, mô tả, giới thiệu, stt...) | EF | message "Không tìm thấy nhà cung cấp!" sai ngữ cảnh (đây là banner, copy nhầm — BannerController.cs:48) |
| 4 | Xóa banner | "Xóa" (icon delete, per-row) | POST `api/Banner/Xoa/{id:Guid}` | `Banner_Xoa` | public | per-row | Check record tồn tại + đúng IdDonVi → DeleteAsync; sai đơn vị → MESS_NotFound | EF | - |

## Quan hệ
- **Gọi sang:** nội bộ service `Website` (repo `Banner`). Enum vị trí/kiểu banner lấy qua `CommonService` (`GetViTriBanner`, `GetKieuBanner`).
- **Dữ liệu (entities):** `Banner` (Ten, ViTri, NgayBatDau, NgayKetThuc, Image, ImageMobile, MoTa, GioiThieu, IsSuDung, Type, Stt, Link, KieuMoLink, IdDonVi, IdChiNhanh).
- **Ripple / đồng bộ:** banner được render ra trang chủ website/app theo ViTri + IsSuDung; không có consumer/event cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `BannerController.cs:48` & `BannerService.cs:135` — message "Không tìm thấy nhà cung cấp!" copy nhầm từ controller nhà cung cấp; thực chất là banner.
- `BannerService.cs:94-98` — điều kiện chặn đúng (`NgayKetThuc < NgayBatDau`) nhưng nội dung message diễn đạt ngược, dễ gây nhầm khi đọc log.
- Quyền `ThemMoi`/`CapNhatThongTin`/`Xoa` chỉ cấp cho `DV-AD-TONG`; các thao tác đọc mở rộng cho nhiều role kế toán/QLDV.
- Nhiều method `virtual` → có thể bị override ở lớp kế thừa; khi trace kiểm tra lớp con.
