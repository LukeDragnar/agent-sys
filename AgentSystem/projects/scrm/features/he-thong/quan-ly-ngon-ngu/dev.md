# Dev — Quản lý ngôn ngữ (i18n)

> Service: **Core** (BE .NET). Controller chính `LangController` (route `api/Lang`) + `MobileDictionaryController` (route `api/MobileDictionary`, nhãn/cấu hình app). Logic nằm ở service; controller chỉ chuyển tiếp. Chỉ dành cho dev.

## Màn hình & route (FE)
- **Web:** Hệ thống → Quản lý ngôn ngữ (list + form thêm/sửa/xóa key; nút Gen file lang, Import lang theo ngôn ngữ).
- **Web:** Quản trị → Quản lý Dictionary (`{domain}/quantri/quanLyDictionary`) cho từ điển mobile.
- Route FE cụ thể *cần xác nhận trong scrm_frontend* (chưa trace trong registry node).

## Endpoint / Controller (trace registry)

### `LangController` — `[TienIch("LangController","Quản lý ngôn ngữ", TI_Chung)]`, route `api/Lang`
Auth **mixed**: phần quản trị `[SBAuthorize]` (chỉ login, **không** permission key); `GET api/Lang/{lang}` là `[AllowAnonymous]`.

| Hành động | HTTP path | Auth | Ghi chú |
|---|---|---|---|
| Danh sách key dịch | POST `api/Lang/TimTheoDieuKien` | login | `TimTheoDieuKienAsync(LangFilterDto)` |
| Tải file ngôn ngữ (FE i18n) | GET `api/Lang/{lang}` | anonymous | đọc `ContentRootPath/Resources/i18n/{lang}` → JSON, không có → 404 |
| Thêm key | POST `api/Lang/InsertLang` | login | `InsertLang(LangModel)` |
| Sửa key | POST `api/Lang/UpdateLang` | login | `UpdateLang(LangModel)` |
| Thêm/Sửa hàng loạt | POST `api/Lang/InsertOrUpdateLang` | login | upsert `RequestLangModel.ListLangModel` |
| Xóa key | POST `api/Lang/DeleteLang/{key}` | login | theo `KeyLang` |
| Lấy theo key | GET `api/Lang/GetLangByKey/{key}` | login | — |
| Xem chi tiết | GET `api/Lang/XemChiTiet?key=` | login | đa ngôn ngữ của 1 key |
| **Sinh file ngôn ngữ** | POST `api/Lang/GenFileLang` | login | build file i18n tĩnh từ DB → `Resources/i18n` (nguồn cho `GET {lang}`) |
| Import bản dịch | POST `api/Lang/ImportLang/{lang}` | login | `ImportLang(IFormFile, lang)` → nạp DB |

### `MobileDictionaryController` — route `api/MobileDictionary` (từ điển nhãn/cấu hình mobile)
**Toàn bộ `[SBAuthorize]` đã bị comment** → CRUD hiện không gate quyền; 2 endpoint `[AllowAnonymous]` (LayTatCa / LayTatCa_Encrypt). Có cặp `_Encrypt` (AES-ECB-128). Entities: `MobileDictionary` (Ma, Value, DefaultValue, MoTa). CRUD: `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}`, `TimTheoDieuKien`, `TimTheoId/{id}`, `GetDictionaryByMa`.

## Entities
- `NgonNgu`, `NgonNguTranslate` (key → giá trị theo ngôn ngữ). DTO: `LangModel` (Lang + KeyLang + ValueLang), `RequestLangModel`, `LangFilterDto`.
- `MobileDictionary` (Ma, Value, DefaultValue, MoTa).

## Luồng code
- CRUD/Gen/Import → `INgonNguTranslateService`; đọc file → `IWebHostEnvironment` (`Resources/i18n`).
- `GenFileLang` build file i18n tĩnh từ DB → `Resources/i18n`. FE gọi `GET api/Lang/{lang}` (anonymous, khi load app) đọc file. **Sửa DB không phản ánh UI cho tới khi Gen lại.**

## Bug-prone / lưu ý (path:line)
- **`LangController.cs:35-46`** — `GET api/Lang/{lang}` anonymous nối path bằng `{lang}` từ client, **không validate** → nguy cơ **path traversal** (đọc file ngoài `Resources/i18n` nếu `lang` chứa `../`). Nên whitelist mã ngôn ngữ. *(Security session xử lý — không sửa ở đây.)*
- **`LangController.cs:26-117`** — các action quản trị chỉ `[SBAuthorize]` (login), **không permission key** → mọi user đăng nhập đều CRUD/Gen/Import. Nên gắn key + role HT-AD. *Cần product xác nhận.*
- **`LangController.cs:11`** — namespace `File.API.Controllers` (lệch so với các controller Core `Core.API.Controllers`) → dễ nhầm khi tìm.
- **Sửa DB không tự áp UI**: phải `GenFileLang` mới sinh lại file → triage "sửa nhãn không đổi" luôn check Gen trước.
- **`MobileDictionaryController.cs:17,56,68,117,132,153,165`** — toàn bộ `[SBAuthorize]` bị comment → CRUD từ điển mobile **không gate quyền**. Cần xác nhận có gating ở gateway không.
- **`MobileDictionaryController.cs:139`** — `CapNhatThongTin` trả `CODE_Error` khi ModelState invalid, còn `ThemMoi` trả `CODE_Model_UnValidate` → mã lỗi không nhất quán.
- **`MobileDictionaryController.cs:170-174`** — `TimTheoId` nhánh null bọc cả object MethodResult thay vì NotFound như controller khác.
- `_Encrypt` dùng AES **ECB** (lộ pattern) — lưu ý khi nâng cấp bảo mật.

## File source (path:line)
- `LangController.cs` (`{lang}` anonymous `:35-46`; action quản trị `:26-117`; namespace `:11`) — BE service **Core** (route `api/Lang`).
- `MobileDictionaryController.cs` (`[SBAuthorize]` comment `:17,56,68,117,132,153,165`; `CapNhatThongTin` `:139`; `TimTheoId` `:170-174`) — service Core, route `api/MobileDictionary`.
- Đường dẫn gốc theo BE `D:\PROJECT\Git\_SourceForCode\Group2\scrm` (*đường dẫn file đầy đủ trace trong source khi cần*).
- Registry node: `projects/scrm/registry/he-thong/LangController.md`, `projects/scrm/registry/he-thong/MobileDictionaryController.md`.
