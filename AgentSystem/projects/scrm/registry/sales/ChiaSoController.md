---
controller: ChiaSoController
module: sales
service: Contact
man_hinh_ui: "Chia số > Chia số tự động > (popup) Lịch sử log chia số của contact (chia-so/chia-so-tu-dong/chia-so-log)"
muc_dich: "Tra cứu log lịch sử chia số (lead) của 1 contact để biết contact đã được chia về sale/nhóm nào, theo kiểu chia nào."
alias: [ChiaSo, "chia số", "chia lead", "log chia số", "lịch sử chia số"]
auth: public
state_machine: []
deprecated: false
entities: [Contact_CSLogs, Contact]
stored_procs: []
depends: ["ChiaSoV2Service (IChiaSoV2Service — GetLogChiaSoByIdContactAsync)"]
cross_module_in: []
cross_module_out: []
---

# ChiaSo — Lịch sử / log chia số contact

> Controller cực mỏng (1 endpoint, 29 dòng). Toàn bộ nghiệp vụ chia số (phân bổ lead về sale/nhóm) nằm ở `CauHinhChiaSoController` + các service `ChiaSoV2Service` / `ChiaSoTheoNhomService` / `ChiaSoPUSHSALEService`. Controller này CHỈ phục vụ tra cứu log của 1 contact.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem log chia số theo contact | (popup) Lịch sử log chia số | GET `GetLogChiaSoByIdContact/{idContact}` | — (chỉ `[SBAuthorize]` rỗng) | public | Mở từ popup log trong màn chia số tự động | `ChiaSoV2Service.GetLogChiaSoByIdContactAsync(idContact, CurrentUser)` — trả bản ghi log chia số (`Contact_CSLogsInfo`) gần nhất/ chi tiết cho contact. | EF | Quyền chỉ `[SBAuthorize]` rỗng → mọi user login gọi được (không gắn permission key) |

## Quan hệ
- **Gọi sang:** `IChiaSoV2Service.GetLogChiaSoByIdContactAsync`.
- **Dữ liệu (entities):** `Contact_CSLogs` (log lịch sử chia số), liên kết `Contact` qua `idContact`.
- **Ripple / đồng bộ:** không — chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `ChiaSoController.cs:22` — endpoint đọc log chỉ dùng `[SBAuthorize]` rỗng (không có permission key), khác hẳn cụm `CauHinhChiaSoController` đều keyed → bất kỳ user login nào cũng tra được log chia số của contact bất kỳ truyền vào (không thấy filter đơn vị ở tầng controller; filter nằm trong service qua `CurrentUser`).
- `ChiaSoController.cs:16` — tham số constructor đặt nhầm tên `cauHinhChiaSoService` cho field `_chiaSoService` (cosmetic, không ảnh hưởng runtime).
- FE gọi qua route rút gọn `{table}/n/{idContact}` (`chiaso.service.ts`) — tên method `n` bị minify/obfuscate, khó trace.
