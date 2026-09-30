---
controller: SeedingInfoController
module: sales
service: Contact
man_hinh_ui: "Sale > Danh sách số Seeding (danh-sach-seeding-info.component + edit)"
muc_dich: "Quản lý danh sách số điện thoại 'seeding' (số nội bộ/test không tính là khách thật): CRUD và kiểm tra một số có nằm trong danh sách seeding không."
alias: [SeedingInfo, "số seeding", seeding, "danh sách seeding"]
auth: public
state_machine: []
deprecated: false
entities: [SeedingInfo]
stored_procs: []
depends: [SeedingInfoService]
cross_module_in: []
cross_module_out: []
---

# SeedingInfo — Danh sách số Seeding

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới số seeding | Lưu | POST `ThemMoiAsync` | `SeedingInfoController_ThemMoiAsync` (DV-AD-TONG/CN/MCN, QL-SALE, SALE-LEAD, MKT-LEAD, QL-MKT, MKT) | public | form thêm | Insert số seeding theo đơn vị. | EF | — |
| 2 | Tìm theo điều kiện | Danh sách / Lọc | POST `TimTheoDieuKienAsync` | `SeedingInfoController_TimTheoDieuKienAsync` (như #1) | public | grid seeding | Trả danh sách số seeding theo filter. | EF | — |
| 3 | Xóa 1 / nhiều | Xóa | POST `DeleteOneAsync`, POST `DeleteMutipleAsync` | `SeedingInfoController_DeleteOneAsync` / `_DeleteMutipleAsync` (như #1) | public | dòng / chọn nhiều | Xóa 1 hoặc nhiều số seeding theo id(s). | EF | — |
| 4 | Kiểm tra số là seeding | — | GET `CheckPhoneNumberSeeding/{phoneNumber}` | `[SBAuthorize]` (login) | public | check khi nhập số | Trả true/false: số điện thoại có tồn tại trong DB seeding của đơn vị không. | EF | Key role gốc đã comment → chỉ còn `[SBAuthorize]` rỗng |

## Quan hệ
- **Gọi sang:** `SeedingInfoService`.
- **Dữ liệu (entities):** SeedingInfo.
- **Ripple / đồng bộ:** `CheckPhoneNumberSeeding` dùng để loại số seeding khỏi luồng tạo contact/chia số (số seeding không tính là khách thật).

## Lỗi đã biết / lưu ý kỹ thuật
- `SeedingInfoController.cs:83` — `CheckPhoneNumberSeeding` key permission gốc đã comment, chỉ còn `[SBAuthorize]` rỗng (chỉ cần login) → khác mức quyền các action CRUD.
