---
controller: GoiCuocChatHubController
module: he-thong
service: Core
man_hinh_ui: "Quản lý gói cước ChatHub (phía đơn vị) — đăng ký/tìm kiếm gói cước tiện ích chat hub, xem gói active hiện tại, cập nhật trạng thái nhân viên chat Facebook"
muc_dich: "Bản Controllers/ (route api/GoiCuocChatHub/...) phía ĐƠN VỊ: tạo/đăng ký gói cước tiện ích ChatHub, tra cứu, xóa, tìm kiếm, lấy gói active hiện tại của đơn vị + đếm số nhân viên chat hub. Tách biệt với bản SanboxArea (admin duyệt/khuyến mãi)."
alias: ["gói cước chathub", "ChatHub", "gói cước chat hub", "tiện ích chat hub"]
auth: public
state_machine: []
deprecated: false
entities: [GoiCuocChatHub]
stored_procs: []
depends: ["IGoiCuocChatHubService"]
cross_module_in: ["FE Tiện ích ChatHub (phía đơn vị) gọi tạo/tìm/xem gói cước; module chat Facebook gọi UpdateTrangThaiNhanVienChatFacebook / SoNhanVienChatHub"]
cross_module_out: ["Gói cước active điều khiển số nhân viên chat hub được phép online (ripple sang module chat/tiện ích)"]
---

# GoiCuocChatHubController (Controllers) — Gói cước tiện ích ChatHub (phía đơn vị)

Bản **Controllers/** (non-Area), route `api/GoiCuocChatHub/...`, `[TienIch("GoiCuocChatHubController", ..., MaTienIchSCRM.TI_ChatHub)]`. Kế thừa `BaseController` (KHÔNG kế thừa SBBaseController → không có CRUD chuẩn). Phía **đơn vị** quản lý gói cước tiện ích chat hub: tạo, xem theo id, xóa, tìm kiếm, lấy gói active hiện tại (theo id hoặc theo đơn vị hiện tại), cập nhật trạng thái nhân viên chat Facebook, đếm số nhân viên chat hub. Mọi method đều có `[SBAuthorize]` với permission key riêng + dùng `CurrentUser`.

> **Bản đôi:** node này là bản Controllers (phía đơn vị). Bản SanboxArea (admin duyệt/từ chối/khuyến mãi): `GoiCuocChatHubController_Core.md`. Hai bản KHÁC endpoint/role/route — không phải copy y hệt.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo gói cước | Tạo mới / Đăng ký | POST `api/GoiCuocChatHub/Create` | `GoiCuocChatHub_Create` (DV-AD) | public | — | `Create(GoiCuocChatHubDto, CurrentUser)` → tạo bản ghi đăng ký gói cước chat hub cho đơn vị. | EF `GoiCuocChatHub` | — |
| 2 | Xem theo id | (chi tiết) | POST `api/GoiCuocChatHub/GetById/{id}` | `GoiCuocChatHub_GetById` (HT-AD, HT-KT, DV-AD) | public | — | `GetById(id, CurrentUser)` → thông tin gói cước đã đăng ký. | EF `GoiCuocChatHub` | POST cho thao tác đọc theo id (lệch REST) |
| 3 | Xóa | Xóa | POST `api/GoiCuocChatHub/DeleteById/{id}` | `GoiCuocChatHub_DeleteById` (HT-AD, HT-KT, DV-AD) | public | — | `DeleteById(id, CurrentUser)` → xóa gói cước đã đăng ký. | EF `GoiCuocChatHub` | — |
| 4 | Tìm kiếm | Grid / Lọc | POST `api/GoiCuocChatHub/SearchByConditions` | `GoiCuocChatHub_SearchByConditions` (HT-AD, HT-KT, DV-AD) | public | nếu `IsAdmin` → ép `model.IdDonVi = CurrentUser.DonViId` | `SearchByConditions(model, CurrentUser)` → danh sách lọc; admin đơn vị bị bó scope theo đơn vị mình. | EF `GoiCuocChatHub` | xem lưu ý: cờ `IsAdmin` ép IdDonVi (logic gating) |
| 5 | Gói active hiện tại (theo id) | (nội bộ/màn hình) | POST `api/GoiCuocChatHub/GoiCuocHienTai/{id}` | `GoiCuocChatHub_GoiCuocHienTai` (HT-AD, HT-KT) | public | — | `GoiCuocHienTai(id)` → gói cước đang active theo id. | EF `GoiCuocChatHub` | — |
| 6 | Gói active hiện tại (đơn vị) | (màn hình đơn vị) | POST `api/GoiCuocChatHub/GoiCuocHienTai_Unit` | `GoiCuocChatHub_GoiCuocHienTais_Unit` (DV-AD) | public | — | `GoiCuocHienTai(CurrentUser.DonViId)` → gói active của đơn vị hiện tại. | EF `GoiCuocChatHub` | key có chữ "Tais" (typo nhẹ trong permission key) |
| 7 | Cập nhật TT nhân viên chat FB | (toggle online) | POST `api/GoiCuocChatHub/UpdateTrangThaiNhanVienChatFacebook/{id}` | `GoiCuocChatHub_UpdateTrangThaiNhanVienChatFacebook` (DV-AD) | public | — | `UpdateTrangThaiNhanVienChatFacebook(id, CurrentUser)` → đổi trạng thái nhân viên chat Facebook (online/offline). | EF | — |
| 8 | Số nhân viên chat hub | (super/đếm) | POST `api/GoiCuocChatHub/SoNhanVienChatHub/{loaiTienIchChatHub}` | `GoiCuocChatHub_SoNhanVienChatHub` (DV-AD) | public | — | `SoNhanVienChatHub(loaiTienIchChatHub, CurrentUser.DonViId)` → số nhân viên chat hub hiện tại của đơn vị theo loại tiện ích. | EF | — |

## Quy tắc nghiệp vụ (gating)
- `SearchByConditions`: nếu `CurrentUser.IsAdmin == true` → **ép** `model.IdDonVi = CurrentUser.DonViId` (admin đơn vị chỉ thấy gói của đơn vị mình); ngược lại (HT-AD/HT-KT toàn hệ thống) giữ filter người dùng nhập.
- Số nhân viên chat hub được kiểm soát theo gói cước active (`GoiCuocHienTai`) — là cơ sở gating số lượng nhân viên được phép chat.

## Quan hệ
- **Gọi sang:** `IGoiCuocChatHubService` (Create / GetById / DeleteById / SearchByConditions / GoiCuocHienTai / UpdateTrangThaiNhanVienChatFacebook / SoNhanVienChatHub).
- **Dữ liệu (entities):** `GoiCuocChatHub`. DTO: `GoiCuocChatHubDto`, `GoiCuocChatHubFilter` (namespace `Models.Core.DTO.Core.Client.GoiCuocTienIchChatHub`).
- **Ripple / đồng bộ:** gói cước active → giới hạn số nhân viên chat hub được online (ảnh hưởng module chat Facebook).

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers/GoiCuocChatHubController.cs:74` — permission key `GoiCuocChatHub_GoiCuocHienTais_Unit` có chữ "Tais" (typo) trong khi method/desc dùng "GoiCuocHienTai" → dễ nhầm khi map quyền.
- `Controllers/GoiCuocChatHubController.cs:28,53` — nhiều thao tác đọc theo id dùng HTTP POST (GetById, GoiCuocHienTai) → lệch REST, lưu ý khi trace/log.
- Trùng TÊN class với bản `Areas/SanboxArea/GoiCuocChatHubController.cs` (cùng `GoiCuocChatHubController`) nhưng khác namespace/route/role; node này tách bản Controllers, bản kia `_Core`.
