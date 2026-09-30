---
controller: ConfigSMSController
module: he-thong
service: Core
man_hinh_ui: "Quản trị > cấu hình SMS (QuanTriArea — TienIch TI_HeThong); endpoint GetByMa dùng nội bộ service đọc cấu hình SMS theo mã"
muc_dich: "CRUD cấu hình SMS (ConfigSMS) phục vụ gửi tin nhắn; có thêm tìm theo điều kiện (grid) và lấy cấu hình theo mã (GetByMa)."
alias: ["ConfigSMS", "cấu hình SMS", "cài đặt SMS", "nhà cung cấp SMS"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigSMS]
stored_procs: []
depends: ["IConfigSMSService", "AutoMapper (ConfigSMSDto ↔ ConfigSMS)"]
cross_module_in: ["Service gửi SMS gọi GetByMa để lấy cấu hình SMS theo mã (anonymous)"]
cross_module_out: ["Nhà cung cấp SMS gateway (gián tiếp qua service đọc cấu hình)"]
---

# ConfigSMSController — Quản lý cấu hình SMS

Controller area `Core.API.Areas.SanboxArea`, route thật `[Route("api/QuanTriArea/[controller]")]` → path gốc `/api/QuanTriArea/ConfigSMS/...` (KHÔNG có `[Area]` attribute, dù namespace là SanboxArea). Kế thừa `BaseController` (không phải `SBBaseController`) nên tự khai báo đủ CRUD. Controller-level có `[SBAuthorize]`; riêng `GetByMa` đánh `[AllowAnonymous]` → mở cho service đọc cấu hình SMS theo mã.

File: `Core/Core.API/Areas/SanboxArea/ConfigSMSController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Lưới danh sách | POST `/api/QuanTriArea/ConfigSMS/TimTheoDieuKien` | — (chỉ `[SBAuthorize]` controller) | internal | — | Lọc/paging theo `ConfigSMSFilterDto` → trả danh sách cấu hình SMS | EF (service) | Không có permission key riêng |
| 2 | Tìm theo Id | (mở form sửa) | GET `/api/QuanTriArea/ConfigSMS/TimTheoId/{id:Guid}` | — | internal | — | `GetByIdAsync(id)` → trả 1 ConfigSMS | EF | — |
| 3 | Lấy tất cả | (dropdown/list) | GET `/api/QuanTriArea/ConfigSMS/LayTatCa` | — | internal | — | `GetAllAsync()` → toàn bộ ConfigSMS | EF | — |
| 4 | Thêm mới | Nút "Thêm" | POST `/api/QuanTriArea/ConfigSMS/ThemMoi` | — | internal | — | Validate model → map Dto→Entity → `InsertAsync` | EF | — |
| 5 | Cập nhật | Nút "Lưu" | POST `/api/QuanTriArea/ConfigSMS/CapNhatThongTin` | — | internal | sửa bản ghi đã tồn tại | Validate → check tồn tại (`GetByIdAsync`) → `UpdateAsync`; không thấy → MESS_NotFound | EF | — |
| 6 | Xóa | Nút "Xóa" | POST `/api/QuanTriArea/ConfigSMS/Xoa/{id:Guid}` | — | internal | — | Check tồn tại → `DeleteAsync(id)` → trả true | EF | Check `data == null` sai: `GetByIdAsync` trả `MethodResult` (luôn != null) nên không bao giờ vào nhánh NotFound (xem Known-issue) |
| 7 | Lấy cấu hình theo mã | (không UI — service đọc) | GET `/api/QuanTriArea/ConfigSMS/GetByMa/{ma}` | — | anonymous (`[AllowAnonymous]`) | — | `GetCauHinhSMSByMaAsync(ma)` → cấu hình SMS theo mã | EF | Anonymous → lộ cấu hình SMS nếu mã đoán được |

## Quan hệ
- **Gọi sang:** `IConfigSMSService` (TimTheoDieuKien/GetById/GetAll/Insert/Update/Delete/GetCauHinhSMSByMa).
- **Dữ liệu (entities):** `ConfigSMS` (map qua `ConfigSMSDto`).
- **Ripple / đồng bộ:** cấu hình này được service gửi SMS đọc (qua GetByMa) → ảnh hưởng gateway gửi tin.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSMSController.cs:121-125` (Xoa) — `data` là `MethodResult<...>` luôn khác null → điều kiện `if (data == null)` không bao giờ đúng; muốn check tồn tại phải dùng `data.Data == null` như các controller khác.
- `ConfigSMSController.cs:135-136` (GetByMa) — `[AllowAnonymous]` mở public; nếu cấu hình SMS chứa key/secret gateway thì rò rỉ qua endpoint không auth.
- Không có permission key cho từng action (chỉ `[SBAuthorize]` controller-level) → mọi user authenticated đều CRUD được.
