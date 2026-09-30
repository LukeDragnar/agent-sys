---
controller: ContentWebsiteController
module: website
service: Website
man_hinh_ui: "Website > Quản lý content website — list nội dung/đoạn content cấu hình cho website; Thêm/Sửa/Xóa (một số thao tác chỉ SuperUser)"
muc_dich: "Quản lý các khối content website của đơn vị (CRUD): tìm theo điều kiện, thêm/sửa/xóa. Thêm/Xóa chỉ dành cho SuperUser; mọi thao tác đọc/sửa giới hạn theo đơn vị của user."
alias: ["content website", "nội dung website", "quản lý content", "cấu hình content"]
auth: public
state_machine: []
deprecated: false
entities: [ContentWebsite]
stored_procs: []
depends: [Website (ContentWebsiteService)]
cross_module_in: []
cross_module_out: []
---

# ContentWebsiteController — Quản lý content website

> `partial class : BaseCoreController`, route `api/ContentWebsite`, `[TienIchAttribute(..., MaTienIchSCRM.TI_Website)]`. Auth model **hỗn hợp**: vài key dành `DV-AD-TONG`, vài key `HT-AD`, và 2 thao tác (Thêm/Xóa) chặn cứng thêm bằng `IsSuperUser` ngoài permission key.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (list content) | POST `api/ContentWebsite/TimTheoDieuKien` | `ContentWebsite_TimTheoDieuKien` | public | luôn | List content theo đơn vị + filter (kèm CurrentUser) | EF | - |
| 2 | Thêm mới content | "Thêm mới" | POST `api/ContentWebsite/ThemMoi` | `ContentWebsite_ThemMoi` (`HT-AD`) | public | chỉ SuperUser | Nếu `!IsSuperUser` → trả lỗi "Bạn không có quyền thêm mới"; ngược lại tạo content | EF | - |
| 3 | Tìm theo id | "Sửa"/xem → popup | GET `api/ContentWebsite/TimTheoId/{id}` | `ContentWebsite_TimTheoId` | public | per-row | Lấy theo id; chỉ trả nếu `IdDonVi == CurrentUser.DonViId`, ngược lại "Không tìm thấy Q&A!" (MESS_NotFound) | EF | message ghi "Q&A" — copy nhầm label |
| 4 | Cập nhật content | "Lưu" (popup sửa) | POST `api/ContentWebsite/CapNhatThongTin` | `ContentWebsite_CapNhatThongTin` (`HT-AD,DV-AD-TONG`) | public | per-row | Cập nhật content (kèm CurrentUser) | EF | - |
| 5 | Xóa content | "Xóa" (icon delete) | POST `api/ContentWebsite/Xoa/{id}` | `ContentWebsite_Xoa` (`HT-AD`) | public | chỉ SuperUser | `!IsSuperUser` → lỗi "Bạn không có quyền xóa"; check item tồn tại + đúng `IdDonVi` (NotFound nếu sai) → xóa | EF | - |

## Quy tắc nghiệp vụ (gating)
1. **Thêm mới** (`ThemMoiAsync`): `CurrentUser.IsSuperUser == false` → chặn ("Bạn không có quyền thêm mới"); true → tạo. Vậy permission key `HT-AD` chưa đủ — còn phải IsSuperUser.
2. **Xóa** (`XoaTheoId`): `IsSuperUser == false` → chặn; rồi `GetByIdAsync` → nếu `Data == null` hoặc `IdDonVi != CurrentUser.DonViId` → NotFound; mới `DeleteAsync`.
3. **TimTheoId**: chỉ trả data nếu `data.IdDonVi == CurrentUser.DonViId` (cô lập đơn vị) — ngược lại trả lỗi NotFound dù record tồn tại.

## Quan hệ
- **Gọi sang:** nội bộ `ContentWebsiteService`.
- **Dữ liệu (entities):** `ContentWebsite` (cô lập theo `IdDonVi`).
- **Ripple / đồng bộ:** content hiển thị trên storefront (qua tầng render website đọc theo đơn vị); sửa/xóa ảnh hưởng nội dung trang.

## Lỗi đã biết / lưu ý kỹ thuật
- Message lỗi của `TimTheoId` ghi nhầm "Không tìm thấy Q&A!" (đúng phải là content website) (`ContentWebsiteController.cs:57`).
- Auth không đồng nhất giữa các method: Thêm=`HT-AD`, Sửa=`HT-AD,DV-AD-TONG`, Xóa=`HT-AD`, Tìm=`DV-AD-TONG` — cộng thêm gate `IsSuperUser` cho Thêm/Xóa. Khi phân quyền role cần xét cả 2 lớp.
- Method CRUD là `virtual` → có thể bị override ở lớp con; trace nhớ kiểm tra lớp kế thừa.
