---
controller: TkNganHangHeThongController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (toàn bộ controller bị comment; bản hoạt động nằm ở service WebhookBank/Payment.API, module ke-toan)"
muc_dich: "(Đã vô hiệu trong Core) Dự kiến CRUD tài khoản ngân hàng hệ thống/chi nhánh + set mặc định + lấy theo đơn vị/chi nhánh — toàn bộ bị comment, đã migrate sang service WebhookBank."
alias: ["TkNganHangHeThong", "TKNganHang", "tài khoản ngân hàng chi nhánh", "ngân hàng hệ thống"]
auth: internal
state_machine: []
deprecated: true
entities: [TkNganHangHeThong]
stored_procs: []
depends: ["ITkNganHangHeThongService", "BaseController"]
cross_module_in: []
cross_module_out: ["Bản hoạt động hiện ở registry/ke-toan/TkNganHangHeThongController.md (service WebhookBank/Payment.API) — Core version đã khóa cứng"]
---

# TkNganHangHeThongController — (Vô hiệu trong Core / đã migrate)

Toàn bộ thân controller trong `TkNganHangHeThongController.cs` (dòng 13-137) bị comment — không expose endpoint nào từ service **Core**. Logic CRUD tài khoản ngân hàng chi nhánh + set mặc định + private get-by-đơn-vị đã được **migrate sang service WebhookBank (Payment.API)**, xem `registry/ke-toan/TkNganHangHeThongController.md`. Node này (`_Core`) ghi nhận bản Core đã khóa cứng để AI bỏ qua.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | — | Toàn bộ controller bị comment — không expose endpoint từ Core. | — | deprecated: true — dùng bản WebhookBank ở ke-toan |

## Quan hệ
- **Ripple / đồng bộ:** bản hoạt động ở service WebhookBank (`ke-toan/TkNganHangHeThongController.md`) — CRUD TK ngân hàng chi nhánh, set mặc định, get theo đơn vị/chi nhánh (private).

## Lỗi đã biết / lưu ý kỹ thuật
- `TkNganHangHeThongController.cs:13-137` toàn bộ định nghĩa class + ~10 endpoint (TimTheoDieuKien, ThemMoi, Xoa, SetMacDinh, GetListNganHangByType, CapNhatThongTin, TimTheoId, GetListTaiKhoanByChiNhanh, GetListTaiKhoanByDonVi, GetAllByDonViPrivateAsync, GetByIdPrivateAsync) bị comment.
- Trùng tên file với controller WebhookBank ở ke-toan → node này suffix `_Core`; tránh triage nhầm sang bản Core.
