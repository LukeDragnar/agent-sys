---
controller: NotifyConfigController
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Cấu hình thông báo: lưới cấu hình + form Thêm/Sửa/Xóa; tra cứu cấu hình theo Mã"
muc_dich: "CRUD cấu hình thông báo (NotifyConfig): định nghĩa các loại/kênh thông báo theo Mã. Cung cấp cấu hình cho hệ thống bắn notification."
alias: ["cấu hình thông báo", "notify config", "config thông báo", "NotifyConfig"]
auth: public
state_machine: []
deprecated: false
entities: [NotifyConfig]
stored_procs: []
depends: ["INotifyConfigService", "BaseController", "AutoMapper"]
cross_module_in: ["FE Quản trị/Cấu hình thông báo gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "Service bắn thông báo gọi GetByMa lấy cấu hình theo Mã"]
cross_module_out: ["Cấu hình thông báo điều khiển kênh/nội dung notification → ảnh hưởng các luồng gửi thông báo cross-module"]
---

# NotifyConfigController — Cấu hình thông báo

Bản `Controllers/` (partial class), kế thừa `BaseController`. CRUD đầy đủ + `GetByMa`. **Toàn bộ `[SBAuthorize(...)]` đều bị comment** → endpoint mở (chỉ middleware mặc định) → auth = public.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/NotifyConfig/TimTheoDieuKien` | — (key đã comment) | public | — | Lọc cấu hình theo `NotifyConfigFilterDto`. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/NotifyConfig/TimTheoId/{id:Guid}` | — | public | — | `GetByIdAsync(id)`. | EF | — |
| 3 | Lấy tất cả | (dropdown) | GET `api/NotifyConfig/LayTatCa` | — | public | — | `GetAllAsync()`. | EF | — |
| 4 | Thêm mới | Thêm | POST `api/NotifyConfig/ThemMoi` | — | public | — | Valid → map DTO→entity → `InsertAsync`. | EF | — |
| 5 | Cập nhật | Sửa | POST `api/NotifyConfig/CapNhatThongTin` | — | public | — | Valid → map → kiểm tra tồn tại → `UpdateAsync`. | EF | — |
| 6 | Xóa | Xóa | POST `api/NotifyConfig/Xoa/{id:Guid}` | — | public | — | Kiểm tra tồn tại → `DeleteAsync`. | EF | Check `data == null` trên `MethodResult` (luôn khác null) thay vì `data.Data == null` → guard không hiệu lực |
| 7 | Lấy theo Mã | (tra cứu cấu hình) | GET `api/NotifyConfig/GetByMa?ma=` | — | public | — | `GetCauHinhByMaAsync(ma)` trả cấu hình theo Mã (service bắn thông báo dùng). | EF | — |

## Quan hệ
- **Gọi sang:** INotifyConfigService (EF), AutoMapper NotifyConfigDto↔NotifyConfig.
- **Dữ liệu (entities):** `NotifyConfig`.
- **Ripple / đồng bộ:** Cấu hình điều khiển kênh/nội dung thông báo → ảnh hưởng luồng gửi notification.

## Lỗi đã biết / lưu ý kỹ thuật
- Tất cả `[SBAuthorize(...)]` bị comment (dòng 36, 50, 63, 76, 96, 123) → CRUD cấu hình thông báo KHÔNG phân quyền theo role, chỉ chặn ở mức middleware mặc định.
- `Xoa` (dòng 124–132): kiểm tra `data == null` trên kết quả `MethodResult` (object luôn != null) thay vì `data.Data == null` → không bắt được trường hợp không tồn tại.
