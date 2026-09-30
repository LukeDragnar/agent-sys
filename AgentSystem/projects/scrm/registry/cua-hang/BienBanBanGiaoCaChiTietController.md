---
controller: BienBanBanGiaoCaChiTietController
module: cua-hang
service: WarehouseProduct
man_hinh_ui: "nội bộ / quản trị — không có màn list độc lập cho người dùng thường; CRUD chi tiết phục vụ admin (Thêm/Xóa chỉ super user HT-AD). Người dùng cửa hàng thao tác chi tiết qua màn Biên bản bàn giao ca (BienBanBanGiaoCaController.ThemMoiChiTietAsync). Tiện ích TI_Order ('Đơn hàng post chi tiết')."
muc_dich: "CRUD chi tiết biên bản bàn giao ca (mỗi dòng = 1 sản phẩm/khoản nhận trong ca). Thêm/Xóa giới hạn super user; tìm/cập nhật mở rộng admin đơn vị."
alias: ["chi tiết biên bản bàn giao ca", "chi tiết giao ca", "BB giao ca chi tiết", "dòng biên bản ca"]
auth: public               # [SBAuthorize(key, ..., roles)]; ThemMoi/Xoa chỉ HT-AD + chặn cứng !IsSuperUser
state_machine: []
deprecated: false
entities: [BienBanBanGiaoCaChiTiet]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [BienBanBanGiaoCa (thêm chi tiết qua ThemMoiChiTietAsync ở controller cha)]
cross_module_out: []
---

# BienBanBanGiaoCaChiTietController — Chi tiết biên bản bàn giao ca

> Quản chi tiết (dòng SP/khoản nhận) của biên bản giao ca (`partial class`, `TI_Order`). **Thêm/Xóa chặn cứng `!CurrentUser.IsSuperUser`** → chỉ super user (HT-AD) thao tác trực tiếp; người dùng cửa hàng thêm chi tiết qua `BienBanBanGiaoCaController.ThemMoiChiTietAsync`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (grid admin) | POST `api/BienBanBanGiaoCaChiTiet/TimTheoDieuKien` | `BienBanBanGiaoCaChiTiet_TimTheoDieuKien` (DV-AD-TONG) | public | - | List chi tiết theo filter + CurrentUser | EF | - |
| 2 | Thêm mới chi tiết | (admin) | POST `api/BienBanBanGiaoCaChiTiet/ThemMoi` | `BienBanBanGiaoCaChiTiet_ThemMoi` (HT-AD) | public | chỉ super user | **Chặn cứng** `!IsSuperUser` → "Bạn không có quyền thêm mới"; rồi insert | EF | double-gate: role HT-AD + check IsSuperUser |
| 3 | Tìm theo Id | (admin) | GET `api/BienBanBanGiaoCaChiTiet/TimTheoId/{id:Guid}` | `BienBanBanGiaoCaChiTiet_TimTheoId` (DV-AD-TONG) | public | per-row | Guard tenant (`IdDonVi == CurrentUser.DonViId`) → trả DTO; ngược lại lỗi (message copy nhầm: "Không tìm thấy Q&A!") | EF | message lỗi "Q&A" sai (controller:58) |
| 4 | Cập nhật thông tin | (admin) | POST `api/BienBanBanGiaoCaChiTiet/CapNhatThongTin` | `BienBanBanGiaoCaChiTiet_CapNhatThongTin` (HT-AD, DV-AD-TONG) | public | - | Update chi tiết theo `BienBanBanGiaoCaChiTietDto` | EF | KHÔNG chặn IsSuperUser (khác Thêm/Xóa) — DV-AD-TONG sửa được |
| 5 | Xóa chi tiết | (admin) | POST `api/BienBanBanGiaoCaChiTiet/Xoa/{id:Guid}` | `BienBanBanGiaoCaChiTiet_Xoa` (HT-AD) | public | chỉ super user | **Chặn cứng** `!IsSuperUser` → "Bạn không có quyền xóa"; rồi guard tenant; rồi xóa | EF | double-gate |

## Quan hệ
- **Gọi sang:** `IBienBanBanGiaoCaChiTietService` (toàn bộ).
- **Dữ liệu (entities):** `BienBanBanGiaoCaChiTiet` (IdDonVi; thuộc về 1 `BienBanBanGiaoCa`).
- **Ripple / đồng bộ:** chi tiết được thêm/cập nhật chủ yếu qua controller cha (`BienBanBanGiaoCaController.ThemMoiChiTietAsync` + `SetTongSanPhamNhanTheoIdVaKieuDuLieu`). Controller này là CRUD trực tiếp cho admin hệ thống.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/BienBanBanGiaoCaChiTietController.cs`
  - L58 message lỗi `TimTheoId` ghi "Không tìm thấy Q&A!" (copy-paste từ controller khác) — chỉ label.
  - Thêm (L44-47) & Xóa (L71-74) **chặn cứng `!IsSuperUser`** dù key role là HT-AD → double-gate; Cập nhật (#4) KHÔNG chặn IsSuperUser (DV-AD-TONG được phép) — bất đối xứng có chủ ý? *cần xác nhận*.
  - L56, L76: guard tenant `IdDonVi == CurrentUser.DonViId` — invariant, giữ nguyên.
  - `partial class`, `TI_Order` ("Đơn hàng post chi tiết") — phân nhóm menu giống controller cha.
