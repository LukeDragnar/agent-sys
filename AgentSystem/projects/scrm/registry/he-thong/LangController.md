---
controller: LangController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản lý ngôn ngữ / Đa ngôn ngữ (key dịch i18n): list + form thêm/sửa/xóa key; nút Gen file lang, Import lang theo ngôn ngữ. Đồng thời endpoint công khai phục vụ FE tải file ngôn ngữ runtime."
muc_dich: "Quản lý từ điển đa ngôn ngữ (i18n) theo key: CRUD bản dịch, sinh (Gen) file ngôn ngữ tĩnh, import bản dịch theo từng ngôn ngữ; và phục vụ file JSON ngôn ngữ cho FE qua endpoint anonymous."
alias: ["Lang", "đa ngôn ngữ", "ngôn ngữ", "i18n", "translate", "ngôn ngữ dịch"]
auth: mixed
state_machine: []
deprecated: false
entities: [NgonNgu, NgonNguTranslate]
stored_procs: []
depends: ["INgonNguTranslateService", "IWebHostEnvironment (đọc file Resources/i18n)"]
cross_module_in: ["FE Hệ thống/Quản lý ngôn ngữ gọi CRUD key + Gen/Import", "Toàn bộ FE gọi GET /api/Lang/{lang} (anonymous) để tải file i18n khi khởi động app"]
cross_module_out: ["Sinh file ngôn ngữ tĩnh ra thư mục Resources/i18n → mọi màn hình FE đọc nhãn từ đây"]
---

# LangController — Quản lý đa ngôn ngữ (i18n)

CRUD từ điển dịch theo **key** (`LangModel` = Lang + KeyLang + ValueLang), kèm thao tác sinh file ngôn ngữ tĩnh (`GenFileLang`) và import bản dịch theo ngôn ngữ (`ImportLang/{lang}`). Toàn bộ logic ở `INgonNguTranslateService`; controller chỉ chuyển tiếp. Auth **mixed**: phần quản trị yêu cầu `[SBAuthorize]` (chỉ login, không gắn permission key cụ thể), riêng `GET /api/Lang/{lang}` là **anonymous** để FE tải file i18n trước khi đăng nhập. Controller gắn `[TienIch("LangController","Quản lý ngôn ngữ", TI_Chung)]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách key dịch | Grid / Lọc | POST `api/Lang/TimTheoDieuKien` | — (chỉ `[SBAuthorize]` login) | public | — | `TimTheoDieuKienAsync(LangFilterDto)` → list bản dịch theo điều kiện. | EF `NgonNguTranslate` | Không gắn permission key → mọi user login đều xem được |
| 2 | Tải file ngôn ngữ (FE i18n) | (không UI — app FE gọi) | GET `api/Lang/{lang}` | — | anonymous | — | Đọc `ContentRootPath/Resources/i18n/{lang}`; file tồn tại → trả JSON (`application/json`); không có → 404. | File hệ thống (không DB) | Đọc thẳng path theo `{lang}` từ client → nguy cơ path traversal nếu `lang` chứa `../`; không kiểm tra whitelist |
| 3 | Thêm key dịch | Thêm | POST `api/Lang/InsertLang` | — (login) | public | — | `InsertLang(LangModel)`. | EF | Không permission key |
| 4 | Sửa key dịch | Lưu | POST `api/Lang/UpdateLang` | — (login) | public | — | `UpdateLang(LangModel)`. | EF | Không permission key |
| 5 | Thêm/Sửa hàng loạt | Lưu nhiều | POST `api/Lang/InsertOrUpdateLang` | — (login) | public | — | `InsertOrUpdateLang(RequestLangModel)` — upsert danh sách `ListLangModel`. | EF | Upsert theo list, không transaction-aware ở controller (tin service) |
| 6 | Xóa key dịch | Xóa | POST `api/Lang/DeleteLang/{key}` | — (login) | public | — | `DeleteLang(key)` theo KeyLang. | EF | Xóa theo key — không cảnh báo FE còn dùng key |
| 7 | Lấy theo key | Mở chi tiết | GET `api/Lang/GetLangByKey/{key}` | — (login) | public | — | `GetLangByKey(key)`. | EF | — |
| 8 | Xem chi tiết | Xem | GET `api/Lang/XemChiTiet?key=` | — (login) | public | — | `XemChiTiet(key)` — chi tiết bản dịch (đa ngôn ngữ của 1 key). | EF | — |
| 9 | Sinh file ngôn ngữ | Gen file lang | POST `api/Lang/GenFileLang` | — (login) | public | — | `GenFileLang()` — build các file i18n tĩnh từ DB ra `Resources/i18n` (nguồn cho endpoint #2). | EF → File | Thao tác nặng, ghi file hệ thống; nên giới hạn quyền admin (hiện chỉ login) |
| 10 | Import bản dịch | Import lang | POST `api/Lang/ImportLang/{lang}` | — (login) | public | — | `ImportLang(IFormFile file, lang)` — nạp file bản dịch cho 1 ngôn ngữ vào DB. | File → EF | Không kiểm tra định dạng/permission ở controller |

## Quan hệ
- **Gọi sang:** `INgonNguTranslateService` (toàn bộ CRUD + Gen + Import); `IWebHostEnvironment` (đọc file `Resources/i18n`).
- **Dữ liệu (entities):** `NgonNgu`, `NgonNguTranslate` (key → giá trị theo từng ngôn ngữ). DTO: `LangModel`, `RequestLangModel`, `LangFilterDto`.
- **Ripple / đồng bộ:** `GenFileLang` ghi file i18n tĩnh → toàn bộ FE đọc nhãn từ các file này; sửa/xóa key trong DB chỉ có hiệu lực thực sự sau khi Gen lại file.

## Luồng xuyên module
- Sửa key dịch trong DB → bấm **Gen file lang** → service sinh lại file JSON trong `Resources/i18n` → FE gọi `GET /api/Lang/{lang}` (anonymous, khi load app) nhận nhãn mới. Nếu quên Gen, thay đổi DB **không** phản ánh trên UI.

## Lỗi đã biết / lưu ý kỹ thuật
- `LangController.cs:35-46` — `GET /api/Lang/{lang}` anonymous nối path bằng `{lang}` từ client không validate → nguy cơ **path traversal** (đọc file ngoài `Resources/i18n` nếu `lang` chứa `../`); nên whitelist mã ngôn ngữ.
- `LangController.cs:26-117` — các action quản trị chỉ `[SBAuthorize]` (login) **không có permission key** trong khi đây là cấu hình hệ thống → mọi user đăng nhập đều CRUD/Gen/Import được; cân nhắc gắn key + role HT-AD.
- `LangController.cs:11` — namespace là `File.API.Controllers` (lệch so với các controller Core khác `Core.API.Controllers`) — có thể do copy/generate; không ảnh hưởng route nhưng dễ gây nhầm khi tìm.
- Thay đổi DB không tự áp UI: phải gọi `GenFileLang` mới sinh lại file i18n (lưu ý khi triage "sửa nhãn không thấy đổi").
