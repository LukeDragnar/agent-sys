---
controller: DMKenhTiepNhanController
module: sales
service: Contact
man_hinh_ui: "Sale > Quản lý kênh tiếp nhận (route landing-page/ql-kenh-tiep-nhan — DanhMucKenhTiepNhanComponent)"
muc_dich: "CRUD danh mục kênh tiếp nhận (nguồn vào của contact/khách: Facebook, Zalo, Hotline, Website...). Dùng để phân loại nguồn khách hàng/contact khi tiếp nhận."
alias: [DMKenhTiepNhan, "kênh tiếp nhận", "nguồn contact", "nguồn khách", "danh mục kênh"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMKenhTiepNhan]
stored_procs: []
depends:
  - "DMKenhTiepNhanService (TimTheoDieuKien/ThemMoi/CapNhat/Xoa/GetById)"
  - "ICommonCacheService (DMKenhTiepNhan_RemoveByDonVi — xóa cache sau khi xóa)"
  - "AutoMapper (DMKenhTiepNhanDto → DMKenhTiepNhan)"
cross_module_in:
  - "Contact/khách hàng gán IdKenhTiepNhan khi tạo contact (nguồn vào)"
  - "TimTheoId_Private (PrivateSBAuthorize) — service khác lấy chi tiết 1 kênh"
cross_module_out:
  - "Cache DMKenhTiepNhan theo đơn vị (chỉ clear ở Xóa)"
---

# DMKenhTiepNhan — Danh mục kênh tiếp nhận

> CRUD danh mục (98 dòng) cho entity `DMKenhTiepNhan` = nguồn tiếp nhận khách/contact (FB, Zalo, hotline, web...). Quyền dùng role **`HT-AD, DV-AD-TONG`** (khác họ tác nghiệp dùng AD-CN/MCN). Mỗi endpoint validate `ModelState` trước; map DTO qua AutoMapper.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách / Lọc | POST `TimTheoDieuKien` | `DMKenhTiepNhan_TimTheoDieuKien` (HT-AD, DV-AD-TONG) | public | Mở danh mục | ModelState invalid → lỗi; `TimTheoDieuKienAsync(grindInfo, CurrentUser)`. | EF | Param đặt tên sai `grindInfo` (vô hại) |
| 2 | Thêm mới | Lưu (thêm) | POST `ThemMoi` | `DMKenhTiepNhan_ThemMoi` (HT-AD, DV-AD-TONG) | public | Nút Thêm | ModelState check → map `DMKenhTiepNhanDto→DMKenhTiepNhan` → `ThemMoi(entity, CurrentUser)`. | EF | Không clear cache sau thêm (xem lỗi) |
| 3 | Cập nhật thông tin | Lưu (sửa) | POST `CapNhatThongTin` | `DMKenhTiepNhan_CapNhatThongTin` (HT-AD, DV-AD-TONG) | public | Form edit | ModelState check → map DTO → `CapNhatThongTin(entity, CurrentUser)`. | EF | Không clear cache sau sửa (xem lỗi) |
| 4 | Xóa | Xóa | POST `Xoa/{id:Guid}` | `DMKenhTiepNhan_Xoa` (HT-AD, DV-AD-TONG) | public | Nút Xóa | `GetByIdAsync` → null thì báo NotFound; `DeleteAsync(id)`; **clear cache `DMKenhTiepNhan_RemoveByDonVi`**; trả true. | EF | — |
| 5 | Chi tiết theo Id | Mở edit | GET `GetById/{id:Guid}` | `DMKenhTiepNhan_GetById` (HT-AD, DV-AD-TONG) | public | Mở 1 dòng | `GetByIdAsync(id)`. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `TimTheoId_Private/{id:Guid}` (`[PrivateSBAuthorize]`) — service khác lấy chi tiết 1 kênh tiếp nhận theo Id (snapshot).

## Quan hệ
- **Gọi sang:** `IDMKenhTiepNhanService`, `ICommonCacheService`, AutoMapper.
- **Dữ liệu (entities):** `DMKenhTiepNhan` (danh mục nguồn tiếp nhận theo đơn vị).
- **Ripple / đồng bộ:** cache kênh tiếp nhận theo đơn vị; contact tham chiếu kênh này làm nguồn vào.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMKenhTiepNhanController.cs:43,56` — `ThemMoi` và `CapNhatThongTin` **KHÔNG gọi `DMKenhTiepNhan_RemoveByDonVi`** sau khi ghi (chỉ `Xoa` clear cache ở dòng 78) → sau thêm/sửa, cache đơn vị **lệch** cho tới khi có thao tác xóa hoặc cache hết hạn. Nhất quán nên thêm clear cache ở 2 endpoint này.
- `DMKenhTiepNhanController.cs:30` — param `grindInfo` đặt sai chính tả (đúng là `gridInfo`) — vô hại, chỉ là mùi code.
- Quyền dùng `HT-AD, DV-AD-TONG` (admin hệ thống + admin tổng) — hẹp hơn họ tác nghiệp; non-admin đơn vị không CRUD được kênh tiếp nhận.
- Khác họ DM* tác nghiệp: controller này **không có bản sinh đôi SanboxArea** (chỉ 1 controller `api/DMKenhTiepNhan`).
