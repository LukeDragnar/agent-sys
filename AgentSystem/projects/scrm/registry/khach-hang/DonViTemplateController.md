---
controller: DonViTemplateController
module: khach-hang
service: WarehouseProduct
man_hinh_ui: "WarehouseProduct route `warehouse-product/donvi-template` (Quản lý template — template thẻ/combo của đơn vị: lưới Tên template/Ảnh nền/Sử dụng/Mặc định + Thêm/Sửa/Thiết kế mẫu/Xóa, toggle Mặc định) · `warehouse-product/admin-template` (Quản lý template mẫu — template hệ thống dùng chung, có nút Copy về đơn vị). 2 màn switch nhau qua nút Template mẫu / Template đơn vị. Template mặc định được dùng khi in/dùng combo thẻ cho KH ở màn combo-spa-khachhang"
muc_dich: "CRUD template (mẫu thẻ/combo có ảnh nền + tiêu đề + kích thước) theo đơn vị; quản template mẫu hệ thống (admin) + cho đơn vị copy về; chọn 1 template mặc định để in thẻ/combo cho khách hàng"
alias: ["DonViTemplate", "template thẻ", "template combo", "mẫu thẻ", "template đơn vị", "template mẫu", "admin template"]
auth: public
state_machine: []
deprecated: false
entities: [DonViTemplate]
stored_procs: []
depends:
  - IDonViTemplateService     # toàn bộ nghiệp vụ (Generated, EF qua IRepository<DonViTemplate>)
  - BaseController            # CurrentUser, ResponseResult
cross_module_in:
  - WarehouseProduct/combo-spa-khachhang (FE gọi LayTemplateMacDinh khi dùng/in combo thẻ cho KH)
cross_module_out: []
---

# DonViTemplate — Quản lý template thẻ/combo (mẫu in) theo đơn vị

> Controller nhỏ (8 endpoint), 100% EF qua `IRepository<DonViTemplate>` — KHÔNG có SP.
> 2 lớp template: **đơn vị** (`IdDonVi != null`, do đơn vị tự tạo) và **mẫu hệ thống** (`IdDonVi == null/Empty`, admin tạo, đơn vị Copy về). Phân biệt nhau bằng `IdDonVi` chứ không phải cột type.
> Phân quyền controller-level: cùng 1 role `DV-AD-TONG` cho hầu hết action; `LayTemplateMacDinh` mở cho mọi user đăng nhập.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | List template đơn vị (phân trang) | Lưới "Quản lý template" (donvi-template) | `POST TimTheoDieuKien` | DonViTemplate_TimTheoDieuKien (DV-AD-TONG) | public | màn donvi-template | ModelState invalid → MESS_Model_UnValidate. SuperUser → mọi template có `IdDonVi != null`; user thường → chỉ `IdDonVi == DonViId`. Sort cứng `IsMacDinh desc, NgayCapNhat desc` (bỏ qua Sorts client) | EF `TableWithNolock` | Nhánh có-Sorts và không-Sorts CODE Y HỆT → tham số Sorts client vô tác dụng (service:65-72) |
| 2 | List template mẫu hệ thống (admin) | Lưới "Quản lý template mẫu" (admin-template) | `POST TimTheoDieuKienAdmin` | DonViTemplate_TimTheoDieuKienAdmin (DV-AD-TONG) | public | màn admin-template | Chỉ lấy `IdDonVi == null/Empty`. User KHÔNG-SuperUser còn lọc thêm `SuDung == true`. Sort cứng `NgayTao asc` | EF | Nhánh Sorts/không-Sorts code y hệt (service:198-205) |
| 3 | Lấy chi tiết theo id | (mở form sửa / thiết kế) | `GET TimTheoId/{id:Guid}` | DonViTemplate_TimTheoId (DV-AD-TONG) | public | — | Trả DonViTemplateDto theo id | EF `GetByIdAsync` | Check scope `IdDonVi != CurrentUser.DonViId` là **dead code** — nằm SAU `return` của nhánh `Data != null`, không bao giờ chạy; nếu `Data == null` thì `item.Data.IdDonVi` ném NRE (controller:43-51) |
| 4 | Thêm mới template | Nút "Thêm mới" → modal "Thêm mới template" | `POST ThemMoi` | DonViTemplate_ThemMoi (DV-AD-TONG) | public | — | Validate: TenTemplate, ChieuCao>0, ChieuRong>0, AnhNenUrl bắt buộc. Pass → map DTO→entity, **gán `IdDonVi = CurrentUser.DonViId`** rồi Insert → trả Id mới | EF `InsertAsync` | — |
| 5 | Cập nhật template | Nút "Sửa" → modal "Cập nhật template" | `POST CapNhatThongTin` | DonViTemplate_CapNhatThongTin (DV-AD-TONG) | public | — | Validate type=2 (thêm Id bắt buộc). Load bản ghi: SuperUser theo Id; user thường theo Id + DonViId. Không thấy → "Không tìm thấy dữ liệu". UpdateAsync chỉ MarkDirty: TenTemplate/AnhNenUrl/TieuDe/TieuDeCss/SuDung/ChieuCao/ChieuRong | EF `UpdateAsync` | `IsMacDinh` & `IdDonVi` KHÔNG nằm trong MarkDirty → update thường không đổi mặc định (đúng ý, đổi mặc định đi đường #7) |
| 6 | Xóa template | Nút "Xóa" → confirm "Bạn có chắc muốn xóa?" | `POST Xoa/{id:Guid}` | DonViTemplate_Xoa (DV-AD-TONG) | public | — | GetById null → MESS_NotFound; có → DeleteAsync → MESS_Delete_Success | EF `DeleteAsync` | Xóa thật (hard/soft tùy BaseService) — KHÔNG kiểm tra template đang là mặc định hay đang được dùng |
| 7 | Đặt/bỏ mặc định | Toggle cột "Mặc định" trên lưới | `POST CapNhatMacDinh` | DonViTemplate_CapNhatMacDinh (DV-AD-TONG) | public | — | Id + IsMacDinh bắt buộc. Load theo Id + DonViId (không có → lỗi). Nếu set true mà **đã có template mặc định khác** trong đơn vị → "Đã có template mặc định rồi. Vui lòng bỏ mặc định template cũ". OK → MarkDirty IsMacDinh, update | EF | Mỗi đơn vị tối đa 1 mặc định, ràng buộc bằng code (không unique index) — race 2 request song song có thể lọt 2 mặc định |
| 8 | Lấy template mặc định của đơn vị | (in/dùng combo thẻ cho KH) | `POST LayTemplateMacDinh` | **không key** (`[SBAuthorize()]`) | public | gọi từ màn combo-spa-khachhang | Trả 1 bản ghi `IsMacDinh == true && IdDonVi == DonViId`; không có → "Không tìm thấy template mặc định" | EF | Auth nới lỏng: chỉ cần đăng nhập, key gốc `DonViTemplate_LayTemplateMacDinh` bị comment (controller:94-95) |
| 9 | Copy template mẫu về đơn vị | Nút "Copy" (admin-template) | `POST CoppyMau/{id:Guid}` | DonViTemplate_CoppyMau (DV-AD-TONG) | public | màn admin-template | SuperUser → "Bạn không được coppy". Lấy mẫu hệ thống (`IdDonVi == null/Empty`) theo id; clone, **gán `IdDonVi = DonViId`**, Id mới, NgayCapNhat=now → Insert bản riêng cho đơn vị | EF `InsertAsync` | **BE path = `CoppyMau` (2 chữ p), FE service gọi `CopyMau/{id}` (1 chữ p)** → nút Copy ở FE gọi sai URL → 404, chức năng copy đang HỎNG (FE donviTemplate.service.ts:67) |

## Quy tắc nghiệp vụ (gating)

- **Phân lớp template theo `IdDonVi`** (không có cột type): `IdDonVi != null` = template riêng đơn vị; `IdDonVi == null/Empty` = template mẫu hệ thống (admin). List đơn vị (#1) chỉ lấy nhóm có IdDonVi; list admin (#2) chỉ lấy nhóm IdDonVi null.
- **Scope theo SuperUser:** SuperUser xem được template của mọi đơn vị (#1) và không bị lọc `SuDung` (#2); ngược lại SuperUser KHÔNG được Copy mẫu (#9 chặn).
- **Lọc `SuDung`:** ở list admin (#2), chỉ user thường mới bị ẩn template `SuDung != true`; SuperUser thấy hết.
- **Mặc định duy nhất/đơn vị (#7):** set true → AnyAsync tìm bản khác cùng đơn vị đang `IsMacDinh == true && Id != model.Id`; có → chặn. ⇒ muốn đổi mặc định phải bỏ cờ bản cũ trước.

## Quan hệ
- **Gọi sang:** `IDonViTemplateService` (Generated) → `IRepository<DonViTemplate>` (EF, NoLock). Không gọi service/microservice khác.
- **Dữ liệu (entities):** `DonViTemplate` (IdDonVi, TenTemplate, AnhNenUrl, TieuDe, TieuDeCss, SuDung, ChieuRong, ChieuCao, IsMacDinh) — kế thừa BaseEntity.
- **Ripple / đồng bộ:** không có consumer/event. Liên kết duy nhất: FE `combo-spa-khachhang` gọi `LayTemplateMacDinh` để lấy mẫu in thẻ/combo cho khách. Copy mẫu (#9) chỉ tạo bản sao nội bộ đơn vị (không đồng bộ ngược về mẫu gốc).

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `donviTemplate.service.ts:67` (FE) vs `DonViTemplateController.cs:115` (BE) — **mismatch `CopyMau` (FE) ≠ `CoppyMau` (BE)** → nút Copy mẫu hỏng (404). Sửa 1 trong 2 cho khớp.
- `DonViTemplateController.cs:43-51` (`TimTheoId`) — đoạn check scope `IdDonVi != CurrentUser.DonViId` là dead code (sau return); nếu `item.Data == null` thì `item.Data.IdDonVi` → NullReferenceException.
- `DonViTemplateController.cs:95` (`LayTemplateMacDinh`) — `[SBAuthorize()]` không key (key thật đã comment) → mọi user đăng nhập gọi được.
- `DonViTemplateService.cs:65-72` & `:198-205` — 2 nhánh if/else (có Sorts / không) viết code Y HỆT → tham số sort của client bị bỏ qua, luôn sort cứng.
- `DonViTemplateService.cs:165-171` (`CapNhatMacDinh`) — ràng buộc "1 mặc định/đơn vị" chỉ bằng AnyAsync trong code, không có unique index → race condition có thể tạo 2 mặc định.
- `DonViTemplateService.cs:212-216` (`CoppyMau`) — SuperUser bị chặn copy (đúng thiết kế: super không thuộc đơn vị nào nên không có IdDonVi đích).
- `DonViTemplate.cs:59-71` (`Clone` extension) — clone tay BỎ SÓT `ChieuRong`, `ChieuCao`, `IsMacDinh`; nhưng `CoppyMau` thực tế dùng AutoMapper (`Map<DonViTemplate,DonViTemplate>`) chứ không gọi `Clone` → extension này hiện không được dùng (rủi ro nếu sau này ai đó xài).
