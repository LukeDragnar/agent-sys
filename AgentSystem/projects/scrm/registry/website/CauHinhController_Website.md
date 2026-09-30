---
controller: CauHinhController
module: website
service: Website
man_hinh_ui: "Website > Cấu hình service website — màn cấu hình chung của module website cho đơn vị (lưu dạng JsonSetting), nút Lưu cấu hình."
muc_dich: "Lưu/đọc cấu hình chung của service Website cho 1 đơn vị: toàn bộ setting serialize thành JSON trong 1 bản ghi CauHinh duy nhất / đơn vị; cache redis 1 ngày; fallback record IdDonVi=Guid.Empty làm default."
alias: ["cấu hình website", "cấu hình service website", "cau hinh website", "website setting"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [Website, Redis]
cross_module_in: [Website (GetAsync(idDonVi) — các service website đọc cấu hình)]
cross_module_out: []
---

# CauHinhController (Website) — Cấu hình service website

> Tên file `CauHinhController_Website.md` vì `CauHinhController` tồn tại ở nhiều module (collision). Đây là bản của **service Website**. Kế thừa `SBBaseController<CauHinh, CauHinh>`. `[TienIchAttribute("CauHinh_Website_Controller", "Quản lý cấu hình service", MaTienIchSCRM.TI_Website)]`. Attribute `[SBAuthorize("CauHinhController")]` cấp controller bị comment → auth theo từng method.
>
> Mô hình lưu: **1 đơn vị = 1 bản ghi CauHinh**, mọi setting nén vào cột `JsonSetting` (deserialize ra `CauHinhSettingDto`). Cache redis key `CK_CauHinhWebsiteSetting_<idDonVi>` (TTL 1 ngày).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình | "Lưu cấu hình" | POST `api/CauHinh/Save` | `CauHinh_Website_LuuCauHinh` | public | luôn | Validate (ValidationSaveAsync hiện chỉ check null); tìm CauHinh theo IdDonVi user → không có thì insert, có thì update JsonSetting; clear cache redis của đơn vị | EF | ValidationSaveAsync gần như rỗng — chỉ check inSetting null (CauHinhService.cs:107-121); logic superuser/HT_AD → IdDonVi=Guid.Empty bị comment |
| 2 | Lấy cấu hình | (load màn cấu hình) | GET `api/CauHinh/Get` | `[SBAuthorize]` rỗng | public | luôn | Lấy theo IdDonVi user: ưu tiên cache redis; miss → đọc DB, set cache; nếu đơn vị chưa có record (và không phải Guid.Empty) → fallback đọc record `IdDonVi = Guid.Empty` làm default | EF + Redis | quyền không đồng nhất với Save (Get để `[SBAuthorize]` rỗng, key gốc `CauHinh_Website_LayCauHinhKho` đã comment) |

> Các endpoint `TimTheoDieuKien`, `UpdateNhanVienCauHinh` (CauHinhController.cs:43-61) đã comment toàn bộ → không hoạt động.

## Quan hệ
- **Gọi sang:** `CauHinhService` (repo `CauHinh`) + `IRedisCacheService`.
- **Dữ liệu (entities):** `CauHinh` (IdDonVi, JsonSetting[json], IsDelete). 1 bản ghi sống / đơn vị.
- **Ripple / đồng bộ:** các service Website khác đọc cấu hình qua `CauHinhService.GetAsync(idDonVi)`; cache redis được clear khi Save.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhService.cs:107-121` — `ValidationSaveAsync` chỉ kiểm tra `inSetting is null`; mọi rule khác (ví dụ MaxCustomerInGroup) đã bị comment → không validate giá trị setting.
- `CauHinhService.cs:58-64, 133-139` — nhánh "superuser/HT_AD làm việc với IdDonVi=Guid.Empty" đã comment → mọi user (kể cả admin) đều thao tác trên record của chính DonViId của mình.
- `CauHinhService.cs:156-211` (`GetAsync(idDonVi)`) — fallback đệ quy sang `GetAsync(Guid.Empty)` khi đơn vị chưa có cấu hình; nếu Guid.Empty cũng rỗng thì trả `new CauHinhSettingDto()` (default rỗng), tránh vòng lặp vô hạn nhờ check `idDonVi == Guid.Empty`.
- Quyền không đồng nhất: `Save` có key `CauHinh_Website_LuuCauHinh` (DV-AD-TONG), `Get` chỉ `[SBAuthorize]` rỗng.
