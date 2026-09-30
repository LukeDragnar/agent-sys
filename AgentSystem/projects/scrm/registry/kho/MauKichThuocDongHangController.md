---
controller: MauKichThuocDongHangController
module: kho
service: OrderLogistic
man_hinh_ui: "Cấu hình > Mẫu kích thước đóng hàng (CRUD danh mục mẫu kích thước/khối lượng kiện hàng theo chi nhánh) — list + Thêm/Sửa/Xóa + đặt mặc định; dropdown chọn mẫu khi đóng gói đơn."
muc_dich: "CRUD danh mục mẫu kích thước đóng hàng (dài/rộng/cao/khối lượng kiện) theo đơn vị + chi nhánh: thêm/sửa/xóa, đặt mẫu mặc định, tìm theo điều kiện, dropdown chọn nhanh khi đăng đơn. Có check trùng tên + cache theo chi nhánh."
alias: ["mẫu kích thước đóng hàng", "MauKichThuocDongHang", "kích thước kiện hàng", "mẫu đóng gói", "dropdown mẫu kích thước"]
auth: public
state_machine: []
deprecated: false
entities: [MauKichThuocDongHang]
stored_procs: []
depends:
  - "IMauKichThuocDongHangService (CRUD MauKichThuocDongHang)"
  - "ICommonCacheService (MauKichThuocDongHang_RemoveByIdChiNhanh — xóa cache sau mọi thay đổi; GetMauKichThuocCacheByChiNhanh — đọc dropdown từ cache)"
cross_module_in: []
cross_module_out: []
---

# MauKichThuocDongHangController — Mẫu kích thước đóng hàng (CRUD danh mục)

> TienIch `MauKichThuocDongHangController` / `MaTienIchSCRM.TI_OrderLogistic`. CRUD danh mục đơn giản theo đơn vị+chi nhánh, có cache invalidation sau mỗi thay đổi.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới mẫu | "Thêm mới" | POST `api/MauKichThuocDongHang/ThemMoi` | `MauKichThuocDongHang_ThemMoi` | public | - | Gán IdDonVi/IdChiNhanh theo user, **check trùng tên** (lỗi nếu trùng), validate ModelState, cắt Ten >255 ký tự → insert → xóa cache chi nhánh | EF + cache | Label nhãn UI ghi nhầm "ghi chú giao hàng" (copy-paste) |
| 2 | Cập nhật mẫu | "Sửa" | POST `api/MauKichThuocDongHang/CapNhatThongTin` | `MauKichThuocDongHang_CapNhatThongTin` | public | - | Validate ModelState, cắt Ten >255 → update → xóa cache chi nhánh | EF + cache | KHÔNG check trùng tên khi sửa (chỉ check ở Thêm mới) |
| 3 | Đặt mẫu mặc định | switch "Mặc định" | POST `api/MauKichThuocDongHang/CapNhatIsDefault` | `MauKichThuocDongHang_CapNhatIsDefault` | public | - | Set mẫu mặc định theo đơn vị+chi nhánh | EF | - |
| 4 | Xóa mẫu | "Xóa" | POST `api/MauKichThuocDongHang/Xoa/{id}` | `MauKichThuocDongHang_Xoa` | public | - | Check tồn tại (lỗi NotFound nếu null) → xóa → xóa cache chi nhánh | EF + cache | - |
| 5 | Chi tiết theo Id | "Xem" | GET `api/MauKichThuocDongHang/TimTheoId/{id}` | `MauKichThuocDongHang_TimTheoId` | public | - | Trả 1 mẫu theo Id | EF | - |
| 6 | Tìm theo điều kiện | "Tìm kiếm" | POST `api/MauKichThuocDongHang/TimTheoDieuKien` | `MauKichThuocDongHang_TimTheoDieuKien` | public | - | List mẫu phân trang theo điều kiện | EF | - |
| 7 | Dropdown chọn mẫu | (dropdown khi đóng gói/đăng đơn) | GET `api/MauKichThuocDongHang/DropDownMauKichThuoc` | `[SBAuthorize]` (mở) | public | - | Trả list mẫu của chi nhánh **từ cache** (GetMauKichThuocCacheByChiNhanh) | Cache | Quyền rỗng (chỉ đăng nhập) — chấp nhận được cho dropdown |

## Quan hệ
- **Gọi sang:** `IMauKichThuocDongHangService` (CRUD), `ICommonCacheService` (cache theo chi nhánh).
- **Dữ liệu (entities):** `MauKichThuocDongHang` (Ten, kích thước/khối lượng, IsDefault, IdDonVi, IdChiNhanh).
- **Ripple / đồng bộ:** mọi thao tác ghi (Thêm/Sửa/Xóa) → `MauKichThuocDongHang_RemoveByIdChiNhanh` invalidate cache → dropdown load lại.

## Lỗi đã biết / lưu ý kỹ thuật
- Check trùng tên chỉ có ở `ThemMoi` (MauKichThuocDongHangController.cs:34-38); `CapNhatThongTin` không check → có thể tạo 2 mẫu trùng tên qua đường sửa.
- Nhãn `SBAuthorize` description ghi "ghi chú giao hàng" cho cả 7 action (copy-paste sai từ controller DMGhiChuGiaoHang) — không ảnh hưởng chức năng nhưng dễ gây nhầm khi map quyền.
- Cắt `Ten` về 255 ký tự thủ công trước insert/update (cs:43,58).
