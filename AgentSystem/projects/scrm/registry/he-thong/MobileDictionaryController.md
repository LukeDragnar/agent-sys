---
controller: MobileDictionaryController
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý Dictionary (WEB: {domain}/quantri/quanLyDictionary): grid lọc + form Thêm/Sửa/Xóa; ngoài ra phục vụ app mobile lấy từ điển cấu hình (LayTatCa / LayTatCaBaTruong / GetDictionaryByMa, có bản _Encrypt mã hóa)"
muc_dich: "Quản lý 'từ điển' cấu hình mobile (MobileDictionary): cặp Mã–Value–DefaultValue–MoTa. App mobile đọc toàn bộ từ điển để hiển thị label/cấu hình; trang quản trị CRUD. Có cặp endpoint _Encrypt (AES-ECB-128) cho client mã hóa."
alias: ["dictionary", "mobile dictionary", "từ điển mobile", "cấu hình label mobile", "MobileDictionary"]
auth: mixed
state_machine: []
deprecated: false
entities: [MobileDictionary]
stored_procs: []
depends: ["IMobileDictionaryService", "AES_ECB_128 (mã hóa)", "AutoMapper", "Core.DataContext (SQL)"]
cross_module_in: ["App mobile gọi LayTatCa / LayTatCaBaTruong / GetDictionaryByMa (+ bản _Encrypt) để lấy từ điển", "Trang quản trị quanLyDictionary gọi CRUD"]
cross_module_out: []
---

# MobileDictionaryController — Từ điển cấu hình mobile (CRUD + đọc cho app)

Quản lý `MobileDictionary` (Mã, Value, DefaultValue, MoTa). Phục vụ 2 nhóm: **app mobile** đọc từ điển (có bản `_Encrypt` mã hóa AES-ECB-128 + camelCase JSON), và **trang quản trị** CRUD. **Toàn bộ `[SBAuthorize(...)]` đã bị comment** → controller hiện thực tế chỉ còn `[AllowAnonymous]` (2 endpoint) + các endpoint **không có auth attribute nào** (mặc định không gate vì không có `[SBAuthorize]` controller-level). Đây là bản trong thư mục `Controllers` (route `api/MobileDictionary`); biến thể quản trị import/backup nằm ở `MobileDictionaryController_Core` (SanboxArea).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả | (app mobile) | GET `api/MobileDictionary/LayTatCa` | — | anonymous (`[AllowAnonymous]`) | — | `GetAllAsync()` — toàn bộ từ điển. | EF `MobileDictionary` | Public, không auth |
| 2 | Lấy tất cả (mã hóa) | (app mobile) | GET `api/MobileDictionary/LayTatCa_Encrypt` | — | anonymous | — | `GetAllAsync()` → serialize camelCase → `AES_ECB_128.Encrypt_Data`. | EF | Public; trả chuỗi mã hóa |
| 3 | Lấy 3 trường | (app mobile gọn) | GET `api/MobileDictionary/LayTatCaBaTruong` | — (key đã comment) | public (không gate) | — | `LayTatCaAsync()` — chỉ Mã/Value/DefaultValue. | EF | Không còn `[SBAuthorize]` (đã comment) |
| 4 | Thêm mới | Thêm (QTHT) | POST `api/MobileDictionary/ThemMoi` · `/ThemMoi_Encrypt` | — (key đã comment) | public (không gate) | ModelState valid | `_Encrypt`: decrypt từng field (Ma/Value/MoTa/DefaultValue) trước → map → `InsertAsync`. Bản thường: map thẳng → `InsertAsync`. | EF | Không gate quyền; 2 biến thể thường/encrypt |
| 5 | Sửa | Lưu (QTHT) | POST `api/MobileDictionary/CapNhatThongTin` | — (key đã comment) | public (không gate) | ModelState valid + tồn tại Id | Map → `GetByIdAsync`; null → `MESS_NotFound` (404); có → `UpdateAsync`. | EF | ModelState lỗi trả `CODE_Error` (không phải `CODE_Model_UnValidate` như ThemMoi) — lệch convention |
| 6 | Xóa | Xóa (QTHT) | POST `api/MobileDictionary/Xoa/{id:Guid}` | — (key đã comment) | public (không gate) | Tồn tại Id | `GetByIdAsync` null → `MESS_NotFound`; có → `DeleteAsync` → `MESS_Delete_Success`. | EF | Không gate quyền |
| 7 | Lọc theo điều kiện | Grid lọc | POST `api/MobileDictionary/TimTheoDieuKien` | — (key đã comment) | public (không gate) | — | `TimTheoDieuKienAsync(gridInfo)`. | EF | — |
| 8 | Chi tiết theo Id | Mở chi tiết | GET `api/MobileDictionary/TimTheoId/{id:Guid}` | — (key đã comment) | public (không gate) | — | `GetByIdAsync`; có → map `MobileDictionaryResDto`. | EF | Nhánh null trả thẳng `data` (kết quả service) thay vì NotFound |
| 9 | Lấy theo Mã | (tra cứu 1 key) | POST `api/MobileDictionary/GetDictionaryByMa` · `/GetDictionaryByMa_Encrypt` | — | public (không gate) | — | `GetDictionaryByMa(key)`; bản `_Encrypt`: decrypt key vào, encrypt kết quả ra. | EF | — |

## Quan hệ
- **Gọi sang:** `IMobileDictionaryService` (CRUD + tra cứu), `AES_ECB_128` (encrypt/decrypt), AutoMapper.
- **Dữ liệu (entities):** `MobileDictionary` (Ma, Value, DefaultValue, MoTa).
- **Ripple / đồng bộ:** app mobile cache từ điển này để render label/cấu hình; thay đổi ở trang quản trị → ảnh hưởng hiển thị app.

## Lỗi đã biết / lưu ý kỹ thuật
- `MobileDictionaryController.cs:17,56,68,117,132,153,165` — **toàn bộ `[SBAuthorize(...)]` bị comment** → endpoint CRUD (Thêm/Sửa/Xóa) hiện **không gate quyền** (chỉ những endpoint có `[AllowAnonymous]` là cố ý public). Rủi ro: ai gọi được API cũng sửa/xóa từ điển. Cần xác nhận có gating ở tầng khác (gateway) không.
- `MobileDictionaryController.cs:139` `CapNhatThongTin` trả `Contants.CODE_Error` khi ModelState invalid, trong khi `ThemMoi` trả `Contants.CODE_Model_UnValidate` → mã lỗi không nhất quán giữa create/update.
- `MobileDictionaryController.cs:170-174` `TimTheoId` nhánh `data.Data == null` trả `ResultWithData(data)` (bọc cả object MethodResult) thay vì trả NotFound như các controller khác.
- `_Encrypt` endpoints dùng `AES_ECB_128` (ECB mode) — ECB yếu về mặt mật mã (lộ pattern); lưu ý nếu nâng cấp bảo mật.
