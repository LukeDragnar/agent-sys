---
controller: CauHinhQuanTriController
module: kho
service: OrderLogistic
man_hinh_ui: "Cấu hình quản trị OrderLogistic > Thiết lập đơn (cấu hình thiết lập đơn hàng cấp quản trị) — chỉ CSKH / SuperUser thao tác lưu"
muc_dich: "Quản lý 'cấu hình thiết lập đơn' (CauHinhQuanTri/ThietLapDon) ở cấp quản trị đơn vị: check quyền user được thiết lập đơn, lấy và lưu cấu hình thiết lập đơn (gate cứng IsCSKH/IsSuperUser khi lưu)."
alias: ["cấu hình thiết lập đơn", "cấu hình quản trị order logistic", "thiết lập đơn hàng quản trị", "quyền thiết lập đơn"]
auth: internal
state_machine: []
deprecated: false
entities: [CauHinhQuanTri]
stored_procs: []
depends:
  - "ICauHinhQuanTriService (OrderLogistic.Services.Generated)"
cross_module_in:
  - "FE màn cấu hình quản trị OrderLogistic (CSKH/SuperUser)"
cross_module_out: []
---

# CauHinhQuanTriController — Cấu hình thiết lập đơn (quản trị)

> Controller **KHÔNG** có `[TienIchAttribute]` (dòng khai báo bị comment, CauHinhQuanTriController.cs:12) — không gắn vào tiện ích SCRM nào ở controller-level; chỉ gate bằng `[SBAuthorize]` mở + check `IsCSKH/IsSuperUser` trong code khi lưu.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Check quyền thiết lập đơn | gating ẩn/hiện nút | GET `api/CauHinhQuanTri/CheckQuyenUserThietLapDon` | `[SBAuthorize]` (mở) | internal | màn cấu hình | `KiemTraQuyenUser(DonViId, UserId)` → trả bool user có quyền thiết lập đơn | EF | - |
| 2 | Lấy cấu hình thiết lập đơn | load màn | GET `api/CauHinhQuanTri/GetCauHinhThietLapDon` | `[SBAuthorize]` (mở) | internal | - | Trả cấu hình thiết lập đơn của user/đơn vị | EF | - |
| 3 | Lưu cấu hình thiết lập đơn | "Lưu" | POST `api/CauHinhQuanTri/SaveCauHinhThietLapDon` | `[SBAuthorize]` (mở) + check code | internal | `IsCSKH==true` HOẶC `IsSuperUser==true` | Nếu user là CSKH/SuperUser → lưu cấu hình; ngược lại trả lỗi "Bạn không có quyền thực hiện chức năng này" | EF | Phân quyền **hardcode trong code** (không qua permission key), CauHinhQuanTriController.cs:49 |

## Quan hệ
- **Gọi sang:** `ICauHinhQuanTriService` (KiemTraQuyenUser, GetCauHinhThietLapDon, SaveCauHinhThietLapDon).
- **Dữ liệu (entities):** `CauHinhQuanTri` (cấu hình thiết lập đơn cấp quản trị).
- **Ripple / đồng bộ:** không có ripple cross-module trực tiếp ở controller này.

## Lỗi đã biết / lưu ý kỹ thuật
- Gate lưu bằng `IsCSKH/IsSuperUser` **hardcode trong action** thay vì permission key (CauHinhQuanTriController.cs:49) — không phân quyền chuẩn theo role key, khó audit.
- Controller không gắn `[TienIchAttribute]` (comment ở dòng 12) → không xuất hiện trong danh mục tiện ích SCRM; toàn bộ auth dựa `[SBAuthorize]` mở.
