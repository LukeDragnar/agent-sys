---
controller: GoiCuocChatHubController
module: he-thong
service: Core
man_hinh_ui: "Quản trị hệ thống (QuanTriArea) > Gói cước ChatHub (phía ADMIN HỆ THỐNG): cập nhật khuyến mãi gói cước, duyệt / từ chối gói cước do đơn vị đăng ký"
muc_dich: "Bản Areas/SanboxArea (route api/QuanTriArea/GoiCuocChatHub/...) phía ADMIN HỆ THỐNG: cập nhật cấu hình khuyến mãi gói cước chat hub, duyệt hoặc từ chối gói cước đơn vị đã đăng ký. Tách biệt với bản Controllers (đơn vị tạo/tìm/xem)."
alias: ["gói cước chathub admin", "duyệt gói chathub", "khuyến mãi chathub", "QuanTriArea GoiCuocChatHub"]
auth: public
state_machine: [DangKy, Duyet, TuChoi]
deprecated: false
entities: [GoiCuocChatHub]
stored_procs: []
depends: ["IGoiCuocChatHubService"]
cross_module_in: ["FE Quản trị hệ thống/Gói cước ChatHub gọi duyệt/từ chối/cập nhật khuyến mãi (role HT-AD, HT-KT)"]
cross_module_out: ["Duyệt → gói cước đơn vị chuyển active (phản ánh về bản Controllers phía đơn vị)"]
---

# GoiCuocChatHubController (SanboxArea) — Duyệt / khuyến mãi gói cước ChatHub (phía admin hệ thống)

Bản **Areas/SanboxArea**, route `api/QuanTriArea/GoiCuocChatHub/...` (LƯU Ý: KHÔNG có `[Area]` attribute, route gắn cứng `api/QuanTriArea/[controller]`), `[TienIch("GoiCuocChatHubController", ..., MaTienIchSCRM.TI_HeThong, true)]`. Kế thừa `BaseController`. Phía **admin hệ thống** (`HT-AD`, `HT-KT`) xử lý gói cước chat hub do đơn vị đăng ký: cập nhật khuyến mãi, **duyệt**, **từ chối**. Dùng chung `IGoiCuocChatHubService` với bản đơn vị nhưng gọi method khác (UpdateKhuyenMai / DuyetGoiCuoc / TuChoiGoiCuoc).

> **Bản đôi:** node này là biến thể SanboxArea (suffix `_Core` để tránh trùng tên file với bản Controllers). Bản phía đơn vị (tạo/tìm/xem): `GoiCuocChatHubController.md`. Hai bản KHÁC endpoint/role — không phải copy y hệt.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật khuyến mãi | Cập nhật khuyến mãi | POST `api/QuanTriArea/GoiCuocChatHub/UpdateKhuyenMai` | `GoiCuocChatHub_QuanTri_UpdateKhuyenMai` (HT-AD) | public | — | `UpdateKhuyenMai(GoiCuocChatHubDto, CurrentUser)` → cập nhật cấu hình khuyến mãi gói cước chat hub. | EF `GoiCuocChatHub` | — |
| 2 | Duyệt gói cước | Duyệt | POST `api/QuanTriArea/GoiCuocChatHub/DuyetGoiCuoc/{id}` | `GoiCuocChatHub_QuanTri_DuyetGoiCuoc` (HT-AD, HT-KT) | public | gói đang chờ duyệt | `DuyetGoiCuoc(id, CurrentUser)` → duyệt gói cước → chuyển active. | EF `GoiCuocChatHub` | — |
| 3 | Từ chối gói cước | Từ chối | POST `api/QuanTriArea/GoiCuocChatHub/TuChoiGoiCuoc` | `GoiCuocChatHub_QuanTri_TuChoiGoiCuoc` (HT-AD, HT-KT) | public | gói đang chờ duyệt | `TuChoiGoiCuoc(GoiCuocChatHubTuChoiDto, CurrentUser)` → từ chối (kèm lý do trong DTO). | EF `GoiCuocChatHub` | — |

## Quy tắc nghiệp vụ (state)
- Gói cước đơn vị đăng ký (bản Controllers `Create`) → admin hệ thống **Duyệt** (active) hoặc **Từ chối**.
- Cập nhật khuyến mãi giới hạn role `HT-AD`; duyệt/từ chối cho `HT-AD`, `HT-KT`.

## Quan hệ
- **Gọi sang:** `IGoiCuocChatHubService` (UpdateKhuyenMai / DuyetGoiCuoc / TuChoiGoiCuoc).
- **Dữ liệu (entities):** `GoiCuocChatHub`. DTO: `GoiCuocChatHubDto`, `GoiCuocChatHubTuChoiDto`.
- **Ripple / đồng bộ:** Duyệt → gói cước đơn vị active → ảnh hưởng số nhân viên chat hub được phép (phản ánh về bản Controllers phía đơn vị).

## Luồng xuyên module
- (Đơn vị) `Create` gói cước → trạng thái chờ duyệt → (Admin) bản này `DuyetGoiCuoc`/`TuChoiGoiCuoc`. Duyệt = active gói; Từ chối = giữ nguyên/không active.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas/SanboxArea/GoiCuocChatHubController.cs:11` — route khai báo `[Route("api/QuanTriArea/[controller]")]` KHÔNG kèm `[Area("SanboxArea")]` → path thực tế là `api/QuanTriArea/GoiCuocChatHub/...`, KHÔNG phải `api/SanboxArea/...`. Đừng suy ra path từ namespace.
- Trùng TÊN class với bản `Controllers/GoiCuocChatHubController.cs` nhưng khác namespace/route/role — dễ nhầm khi search; node này tách `_Core` để phân biệt.
- `[TienIch(..., TI_HeThong, true)]` đánh dấu khu QuanTriArea (admin) — ẩn khỏi menu thường của đơn vị (bản Controllers dùng `TI_ChatHub`).
