---
controller: CauHinhGiaBanTienIchChatHubController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Cấu hình giá bán tiện ích ChatHub — biến thể super admin (api/QuanTriArea/CauHinhGiaBanTienIchChatHub)"
muc_dich: "CRUD cấu hình giá bán cho tiện ích ChatHub (gói cước tiện ích): thêm/sửa, lấy theo id, lấy theo loại tiện ích, tìm theo điều kiện."
alias: ["CauHinhGiaBanTienIchChatHub", "cấu hình giá bán tiện ích chathub", "giá bán chathub", "gói cước chathub"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhGiaBanTienIchChatHub]
stored_procs: []
depends: ["ICauHinhGiaBanTienIchChatHubService", "CurrentUser (audit)"]
cross_module_in: ["Màn mua/đăng ký tiện ích ChatHub gọi LayTheoLoaiTienIch (anonymous) để hiển thị giá bán"]
cross_module_out: ["Giá bán cấu hình ở đây quyết định số tiền tính phí khi đơn vị mua/gia hạn tiện ích ChatHub"]
---

# CauHinhGiaBanTienIchChatHubController — Cấu hình giá bán tiện ích ChatHub

Biến thể **Area** `Core.API.Areas.SanboxArea`, route thật `[Route("api/QuanTriArea/[controller]")]` → path gốc `/api/QuanTriArea/CauHinhGiaBanTienIchChatHub/...`. Có `[TienIch("CauHinhGiaBanTienIchChatHub", ..., MaTienIchSCRM.TI_HeThong, true)]`. Kế thừa `BaseController`. Các action CRUD có permission key riêng (`CauHinhGiaBanTienIchChatHub_*`, role `HT-AD,HT-KT`); riêng `LayTheoLoaiTienIch` là `[AllowAnonymous]` để màn mua tiện ích đọc giá công khai.

File: `Core/Core.API/Areas/SanboxArea/CauHinhGiaBanTienIchChatHubController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới cấu hình giá bán | Thêm mới | POST `/api/QuanTriArea/CauHinhGiaBanTienIchChatHub/Create` | `CauHinhGiaBanTienIchChatHub_Create` | mixed | — | `Create(model, CurrentUser)` → tạo bản ghi giá bán mới, set audit người tạo | EF (service) | — |
| 2 | Cập nhật cấu hình giá bán | Cập nhật | POST `/api/QuanTriArea/CauHinhGiaBanTienIchChatHub/Update` | `CauHinhGiaBanTienIchChatHub_Update` | mixed | — | `Update(model, CurrentUser)` → cập nhật giá bán theo id trong model | EF (service) | — |
| 3 | Lấy chi tiết theo id | (mở form sửa) | POST `/api/QuanTriArea/CauHinhGiaBanTienIchChatHub/GetById/{id:Guid}` | `CauHinhGiaBanTienIchChatHub_GetById` | mixed | — | `GetById(id, CurrentUser)` → trả 1 cấu hình theo id | EF (service) | — |
| 4 | Lấy theo loại tiện ích (công khai) | (màn mua tiện ích) | POST `/api/QuanTriArea/CauHinhGiaBanTienIchChatHub/LayTheoLoaiTienIch/{loaiTienIch}` | — (`[AllowAnonymous]`) | anonymous | — | `LayTheoLoaiTienIch(loaiTienIch)` → giá bán theo loại tiện ích, không cần đăng nhập | EF (service) | Public — bất kỳ ai đọc được giá theo loại |
| 5 | Tìm theo điều kiện (danh sách) | Tìm kiếm | POST `/api/QuanTriArea/CauHinhGiaBanTienIchChatHub/SearchByConditions` | `CauHinhGiaBanTienIchChatHub_SearchByConditions` | mixed | — | `SearchByConditions(model, CurrentUser)` → danh sách cấu hình theo filter | EF (service) | — |

## Quan hệ
- **Gọi sang:** `ICauHinhGiaBanTienIchChatHubService` (Create / Update / GetById / LayTheoLoaiTienIch / SearchByConditions).
- **Dữ liệu (entities):** `CauHinhGiaBanTienIchChatHub`; DTO `CauHinhGiaBanTienIchChatHubDto`, filter `CauHinhGiaBanTienIchChatHubFilter` (namespace `Models.Core.DTO.Core.Client.GoiCuocTienIchChatHub`).
- **Ripple / đồng bộ:** giá bán đọc ở đây dùng cho luồng mua/gia hạn tiện ích ChatHub của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhGiaBanTienIchChatHubController.cs:42-48` — `LayTheoLoaiTienIch` là `[AllowAnonymous]`, lộ giá bán công khai (có chủ đích cho màn mua tiện ích).
- Cặp song sinh nghiệp vụ với `CauHinhKhuyenMaiTienIchChatController` (cùng pattern, cùng namespace SanboxArea).
