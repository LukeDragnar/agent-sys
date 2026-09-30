---
controller: DomainController
module: website
service: Website
man_hinh_ui: "Website > Quản lý domain — list domain (tên miền) của đơn vị; Thêm/Sửa/Xóa per-row + dropdown chọn domain khi gán link website"
muc_dich: "Quản lý danh mục domain (tên miền) của đơn vị để gán cho website (CRUD): tìm theo điều kiện, thêm/sửa/xóa với check trùng tên; cung cấp danh sách domain khả dụng để gán link website."
alias: ["domain", "tên miền", "quản lý domain", "gán domain", "domain website"]
auth: public
state_machine: []
deprecated: false
entities: [Domain]
stored_procs: []
depends: [Website (DomainService)]
cross_module_in: []
cross_module_out: [LinkWebsite (domain được gán cho link website của đơn vị)]
---

# DomainController — Quản lý domain (tên miền website)

> `partial class : BaseCoreController`, route `api/Domain`, `[TienIchAttribute(..., MaTienIchSCRM.TI_Website)]`. Đây là danh mục **tên miền** (entity `Domain`: Ten, IsSuDung), KHÔNG phải cấu hình CORS. Phần lớn quyền là `DV-AD-TONG`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (list domain) | POST `api/Domain/TimTheoDieuKien` | `Domain_TimTheoDieuKien` | public | luôn | List domain theo `IdDonVi` của user, filter keyword (Ten.Contains), sort theo NgayCapNhat giảm dần, phân trang | EF | - |
| 2 | Lấy domain để gán link website | dropdown chọn domain (màn cấu hình link) | POST `api/Domain/GetDomainToSetLinkWebsite` | `Domain_GetDomainToSetLinkWebsite` (mở rộng DV-AD/KT/QLDV/NV) | public | - | List domain `IsSuDung == true` của đơn vị **HOẶC `IdDonVi == Guid.Empty`** (domain dùng chung), keyword filter; KHÔNG phân trang (đoạn skip/take bị comment) | EF | - |
| 3 | Thêm mới domain | "Thêm mới" → popup | POST `api/Domain/ThemMoi` | `Domain_ThemMoi` | public | luôn | Validate Ten không rỗng → trim → check trùng `Ten` (toàn hệ thống, không lọc đơn vị) → InsertAsync (gán IdDonVi = đơn vị user) | EF | check trùng tên không lọc theo đơn vị (xem lưu ý) |
| 4 | Tìm theo id | "Sửa"/xem → popup | GET `api/Domain/TimTheoId/{id}` | `Domain_TimTheoId` | public | per-row | Lấy theo id; chỉ trả nếu `IdDonVi == CurrentUser.DonViId`, ngược lại "Không tìm thấy nhà cung cấp!" (NotFound) | EF | message ghi "nhà cung cấp" — copy nhầm label |
| 5 | Cập nhật domain | "Lưu" (popup sửa) | POST `api/Domain/CapNhatThongTin` | `Domain_CapNhatThongTin` | public | per-row | Validate Id + Ten → tìm record đúng id + đúng đơn vị (NotFound nếu thiếu) → check trùng Ten (loại trừ chính nó) → cập nhật Ten + IsSuDung | EF | - |
| 6 | Xóa domain | "Xóa" (icon delete) | POST `api/Domain/Xoa/{id}` | `Domain_Xoa` | public | per-row | Lấy theo id; nếu null hoặc `IdDonVi != CurrentUser.DonViId` → NotFound; ngược lại DeleteAsync | EF | - |

## Quy tắc nghiệp vụ (gating / validate)
1. **ThemMoi**: `Ten` rỗng/whitespace → lỗi "'Tên domain' không được để trống!"; trim; nếu `AnyAsync(x => x.Ten == model.Ten)` (KHÔNG lọc đơn vị) → lỗi "'Tên domain' đã tồn tại!"; insert với IdDonVi = đơn vị user. (Validate phone/email đã comment.)
2. **CapNhatThongTin**: Id rỗng → lỗi; Ten rỗng → lỗi; tìm record theo `Id + IdDonVi == DonViId` (NotFound nếu không có → chặn sửa chéo đơn vị); check trùng `Ten` với `Id != model.Id` → lỗi; chỉ cập nhật `Ten`, `IsSuDung` (các field NguoiLienHe/Sdt/DiaChi/Email đã comment).
3. **GetDomainToSetLinkWebsite**: query `IsSuDung == true` AND (`IdDonVi == DonViId` OR `IdDonVi == Guid.Empty`) → domain dùng chung toàn hệ thống (IdDonVi rỗng) cũng hiện ra để gán.

## Quan hệ
- **Gọi sang:** nội bộ `DomainService` (Website service).
- **Dữ liệu (entities):** `Domain` (Ten, IsSuDung, IdDonVi). Cô lập đơn vị cho read/update/delete; riêng list-gán-link còn lấy domain IdDonVi rỗng.
- **Ripple / đồng bộ:** domain được gán cho link website của đơn vị (GetDomainToSetLinkWebsite phục vụ màn cấu hình link). Đổi/xóa domain đang gán → ảnh hưởng cấu hình website.

## Lỗi đã biết / lưu ý kỹ thuật
- **Check trùng tên khi ThemMoi không lọc theo đơn vị** (`DomainService.cs:91`): `AnyAsync(x => x.Ten == model.Ten)` xét toàn bảng → 2 đơn vị khác nhau không thể tạo cùng tên domain. Có thể đúng chủ ý (tên miền là duy nhất toàn cục) nhưng khác cách cô lập đơn vị ở update/delete — cần xác nhận.
- Message `TimTheoId` ghi nhầm "Không tìm thấy nhà cung cấp!" thay vì domain (`DomainController.cs:59`).
- `GetDomainToSetLinkWebsite` đã comment phân trang (`DomainService.cs:74`) → trả toàn bộ kết quả, không giới hạn; với đơn vị nhiều domain dropdown có thể nặng.
- CapNhat chỉ map Ten + IsSuDung; các field liên hệ (NguoiLienHe, Sdt, DiaChi, Email) bị comment cả ở entity update lẫn validate → UI gửi lên cũng không lưu (`DomainService.cs:130-141`).
- Method CRUD `virtual` → kiểm tra lớp con khi trace.
