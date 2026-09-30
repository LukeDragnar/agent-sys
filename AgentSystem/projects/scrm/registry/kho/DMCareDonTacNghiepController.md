---
controller: DMCareDonTacNghiepController
module: kho
service: OrderLogistic
man_hinh_ui: "Khu vực Sandbox/Quản trị hệ thống (HT) > Danh mục care đơn tác nghiệp — CRUD danh mục loại care đơn tác nghiệp kho; KHÔNG phải màn vận hành của đơn vị (chỉ HT-AD/super dùng)"
muc_dich: "CRUD danh mục 'care đơn tác nghiệp' (DMCareDonTacNghiep) — danh mục dùng chung do quản trị hệ thống (HT-AD) quản lý; xóa thì xóa cache care đơn tác nghiệp."
alias: ["danh mục care đơn tác nghiệp", "DM care đơn", "loại care đơn tác nghiệp", "cấu hình care đơn HT"]
auth: internal
state_machine: []
deprecated: false
entities: [DMCareDonTacNghiep]
stored_procs: []
depends:
  - "IDMCareDonTacNghiepService (OrderLogistic.Service/Generated)"
  - "ICommonCacheService (CareDonTacNghiep_RemoveByHT — clear cache sau khi xóa)"
cross_module_in:
  - "FE Sandbox/Quản trị HT — màn danh mục care đơn tác nghiệp (chỉ HT-AD)"
cross_module_out: []
---

# DMCareDonTacNghiepController — Danh mục care đơn tác nghiệp (Sandbox/HT)

> **Area:** `OrderLogistic.API.Areas.SandboxArea` — route prefix **`api/SanboxArea/DMCareDonTacNghiep`** (lưu ý chính tả "Sanbox" trong route, không phải "Sandbox"). `[TienIchAttribute(..., TI_HeThong, true)]` → tiện ích hệ thống (cờ `true`). Mọi action chỉ role **HT-AD**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | grid + filter | POST `api/SanboxArea/DMCareDonTacNghiep/TimTheoDieuKien_SA` | `DMCareDonTacNghiep_TimTheoDieuKien_SA` | internal | màn DM (HT) | Validate ModelState → list DM care đơn tác nghiệp theo filter (phân trang) | EF | - |
| 2 | Thêm mới | "Thêm mới" | POST `api/SanboxArea/DMCareDonTacNghiep/ThemMoi_SA` | `DMCareDonTacNghiep_ThemMoi_SA` | internal | - | Validate → map DTO→entity → insert | EF | - |
| 3 | Cập nhật | "Lưu" | POST `api/SanboxArea/DMCareDonTacNghiep/CapNhatThongTin_SA` | `DMCareDonTacNghiep_CapNhatThongTin_SA` | internal | có item | Validate → map DTO→entity → update | EF | - |
| 4 | Xóa | "Xóa" | POST `api/SanboxArea/DMCareDonTacNghiep/Xoa_SA/{id}` | `DMCareDonTacNghiep_Xoa_SA` | internal | item tồn tại | GetById → nếu null trả MESS_NotFound; Delete + `CareDonTacNghiep_RemoveByHT()` clear cache → trả thành công | EF + cache | - |
| 5 | Lấy theo Id | mở item | GET `api/SanboxArea/DMCareDonTacNghiep/GetById_SA/{id}` | `DMCareDonTacNghiep_GetById_SA` | internal | - | Trả 1 DM care đơn tác nghiệp theo Id | EF | - |

## Quan hệ
- **Gọi sang:** `IDMCareDonTacNghiepService` (CRUD); `ICommonCacheService.CareDonTacNghiep_RemoveByHT()` invalidate cache sau khi xóa.
- **Dữ liệu (entities):** `DMCareDonTacNghiep` (map DTO↔entity qua AutoMapper).
- **Ripple / đồng bộ:** chỉ clear cache khi xóa (chưa thấy clear cache ở Thêm/Cập nhật → xem Lỗi đã biết).

## Lỗi đã biết / lưu ý kỹ thuật
- Route prefix viết **`SanboxArea`** (thiếu chữ 'd', DMCareDonTacNghiepController.cs:16) — typo cố hữu, FE phải gọi đúng `api/SanboxArea/...` chứ không phải `Sandbox`.
- Chỉ **Xóa** mới gọi `CareDonTacNghiep_RemoveByHT()` clear cache; **Thêm/Cập nhật KHÔNG** invalidate cache (DMCareDonTacNghiepController.cs:50,63) → DM mới thêm/sửa có thể chưa phản ánh ngay nơi khác đọc cache.
- Toàn bộ là tiện ích hệ thống `TI_HeThong` + role `HT-AD` → danh mục dùng chung, không thuộc tác nghiệp của đơn vị.
