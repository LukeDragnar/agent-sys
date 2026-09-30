---
controller: CauHinhKhuyenMaiTienIchChatController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Cấu hình khuyến mãi tiện ích ChatHub — biến thể super admin (api/QuanTriArea/CauHinhKhuyenMaiTienIchChat)"
muc_dich: "CRUD cấu hình khuyến mãi cho tiện ích ChatHub: thêm/sửa, lấy theo id, lấy theo loại tiện ích (công khai), tìm theo điều kiện."
alias: ["CauHinhKhuyenMaiTienIchChat", "cấu hình khuyến mãi tiện ích chathub", "khuyến mãi chathub", "KM chathub"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhKhuyenMaiTienIchChat]
stored_procs: []
depends: ["ICauHinhKhuyenMaiTienIchChatService", "CurrentUser (audit)"]
cross_module_in: ["Màn mua/đăng ký tiện ích ChatHub gọi LayTheoLoaiTienIch (anonymous) để hiển thị khuyến mãi"]
cross_module_out: ["Khuyến mãi cấu hình ở đây quyết định giảm giá khi đơn vị mua/gia hạn tiện ích ChatHub"]
---

# CauHinhKhuyenMaiTienIchChatController — Cấu hình khuyến mãi tiện ích ChatHub

Biến thể **Area** `Core.API.Areas.SanboxArea`, route thật `[Route("api/QuanTriArea/[controller]")]` → path gốc `/api/QuanTriArea/CauHinhKhuyenMaiTienIchChat/...`. Có `[TienIch("CauHinhKhuyenMaiTienIchChat", ..., MaTienIchSCRM.TI_HeThong, true)]`. Kế thừa `BaseController`. CRUD có permission key riêng (`CauHinhKhuyenMaiTienIchChat_*`, role `HT-AD,HT-KT`); `LayTheoLoaiTienIch` là `[AllowAnonymous]` cho màn mua tiện ích đọc khuyến mãi công khai. Cùng khuôn với `CauHinhGiaBanTienIchChatHubController`.

File: `Core/Core.API/Areas/SanboxArea/CauHinhKhuyenMaiTienIchChatController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới cấu hình khuyến mãi | Thêm mới | POST `/api/QuanTriArea/CauHinhKhuyenMaiTienIchChat/Create` | `CauHinhKhuyenMaiTienIchChat_Create` | mixed | — | `Create(model, CurrentUser)` → tạo bản ghi khuyến mãi mới, set audit | EF (service) | — |
| 2 | Cập nhật cấu hình khuyến mãi | Cập nhật | POST `/api/QuanTriArea/CauHinhKhuyenMaiTienIchChat/Update` | `CauHinhKhuyenMaiTienIchChat_Update` | mixed | — | `Update(model, CurrentUser)` → cập nhật khuyến mãi theo id trong model | EF (service) | — |
| 3 | Lấy chi tiết theo id | (mở form sửa) | POST `/api/QuanTriArea/CauHinhKhuyenMaiTienIchChat/GetById/{id:Guid}` | `CauHinhKhuyenMaiTienIchChat_GetById` | mixed | — | `GetById(id, CurrentUser)` → trả 1 cấu hình theo id | EF (service) | — |
| 4 | Lấy theo loại tiện ích (công khai) | (màn mua tiện ích) | POST `/api/QuanTriArea/CauHinhKhuyenMaiTienIchChat/LayTheoLoaiTienIch/{loaiTienIch}` | — (`[AllowAnonymous]`) | anonymous | — | `LayTheoLoaiTienIch(loaiTienIch)` → khuyến mãi theo loại tiện ích, không cần đăng nhập | EF (service) | Public — bất kỳ ai đọc được khuyến mãi theo loại |
| 5 | Tìm theo điều kiện (danh sách) | Tìm kiếm | POST `/api/QuanTriArea/CauHinhKhuyenMaiTienIchChat/SearchByConditions` | `CauHinhKhuyenMaiTienIchChat_SearchByConditions` | mixed | — | `SearchByConditions(model, CurrentUser)` → danh sách cấu hình theo filter | EF (service) | — |

## Quan hệ
- **Gọi sang:** `ICauHinhKhuyenMaiTienIchChatService` (Create / Update / GetById / LayTheoLoaiTienIch / SearchByConditions).
- **Dữ liệu (entities):** `CauHinhKhuyenMaiTienIchChat`; DTO `CauHinhKhuyenMaiTienIchChatDto`, filter `CauHinhKhuyenMaiTienIchChatFilter` (namespace `Models.Core.DTO.Core.Client.GoiCuocTienIchChatHub`).
- **Ripple / đồng bộ:** khuyến mãi đọc ở đây áp vào luồng mua/gia hạn tiện ích ChatHub của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhKhuyenMaiTienIchChatController.cs:42-48` — `LayTheoLoaiTienIch` là `[AllowAnonymous]`, lộ khuyến mãi công khai (có chủ đích).
- Cặp song sinh nghiệp vụ với `CauHinhGiaBanTienIchChatHubController` (cùng pattern, cùng namespace SanboxArea).
