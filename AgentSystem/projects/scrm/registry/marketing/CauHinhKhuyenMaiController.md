---
controller: CauHinhKhuyenMaiController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Khuyến mãi > Cấu hình chung (setting áp dụng cho toàn bộ KM của đơn vị)"
muc_dich: "Đọc & lưu cấu hình chung khuyến mãi của đơn vị (1 bản setting/đơn vị — quy tắc mặc định áp dụng cho KM)."
alias: ["cấu hình khuyến mãi", "CauHinhKhuyenMai", "setting KM", "cấu hình chung KM"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhKhuyenMai]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_KhuyenMai)"
cross_module_in: []
cross_module_out: []
---

# CauHinhKhuyenMai — Cấu hình chung khuyến mãi

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình | (load form) | GET `GetCauHinh` | `[SBAuthorize]` (chỉ login) | public | `GetCauHinh(CurrentUser)` — đọc setting KM của đơn vị hiện tại. | EF | quyền key bị comment |
| 2 | Lưu cấu hình | Lưu | POST `SaveCauHinh` | `[SBAuthorize]` (chỉ login) | public | `SaveCauHinh(inSetting, CurrentUser)` — upsert setting theo đơn vị. | EF | quyền key bị comment (gốc ngụ ý chỉ AD-TONG) |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** `CauHinhKhuyenMai` (1 bản setting/đơn vị).
- **Ripple/đồng bộ:** thay đổi setting ảnh hưởng cách tính/áp KM mặc định (đọc khi tạo KM/áp KM tùy field).

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhKhuyenMaiController.cs:20,29` — cả `GetCauHinh` và `SaveCauHinh` để `[SBAuthorize]` rỗng; comment gốc cho `SaveCauHinh` là `DV-AD-TONG` → hiện mọi user login đều lưu được cấu hình chung của đơn vị (khe hở phân quyền).
