---
controller: DonViController
module: he-thong
service: AuthenCustomer
man_hinh_ui: "Nội bộ — duyệt mã đơn vị (service-to-service từ back-office) + app khách đọc cấu hình order của đơn vị. Không có màn quản trị riêng trong service này."
muc_dich: "Đồng bộ/duyệt mã đơn vị (DonVi) vào DB AuthenCustomer để cho phép khách đăng nhập, và lấy cấu hình đặt hàng (CauHinhOrder) của đơn vị cho app khách."
alias: ["duyệt mã đơn vị", "đồng bộ đơn vị authen customer", "cấu hình order đơn vị", "ApproveCode"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonVi]
stored_procs: []
depends: ["SQL Server (AuthenConnectStr) — EF Core (DonVi)", "Order service /api/CauHinh/GetCauHinhOrder/{idDonVi} (qua ClientHelper)"]
cross_module_in: ["Back-office / service quản lý đơn vị → ApproveCode (PrivateSBAuthorize) khi duyệt/đồng bộ mã đơn vị", "App mobile khách → GetCauHinhOrder"]
cross_module_out: ["Order service (đọc cấu hình đặt hàng của đơn vị)", "Gián tiếp: bật/tắt khả năng login khách (IsDuyetMaDonVi, NgayHetHanSuDung)"]
---

# DonViController — Duyệt đơn vị & cấu hình order (app khách)

Controller nhỏ (2 endpoint) cầu nối **đơn vị (DonVi)** giữa hệ back-office và DB của AuthenCustomer. Bản ghi `DonVi` ở đây quyết định khách có login được không (`IsDuyetMaDonVi`, `NgayHetHanSuDung` — xem `AuthenController_AuthenCustomer`). File có hậu tố `_AuthenCustomer` để tránh đụng `DonViController` của các module khác. Hai mức auth khác nhau: `ApproveCode` là service-to-service (`[PrivateSBAuthorize]`), `GetCauHinhOrder` là khách đã login (`[SBAuthorizeCustomer]`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Duyệt / đồng bộ mã đơn vị | (nội bộ, từ back-office) | POST `api/DonVi/ApproveCode` | — | internal `[PrivateSBAuthorize]` | Khi back-office duyệt mã đơn vị | `LstDonVi` rỗng → "Vui lòng chọn dữ liệu". Chặn trùng MaDonVi (khác Id) → "Mã đơn vị đã tồn tại". Build danh sách DonVi (Id/MaDonVi/IsDuyetMaDonVi/NgayHetHanSuDung) → `InsertAsync(list)` (upsert theo Id). | EF (DonVi) | `InsertAsync` cho cả bản ghi đã tồn tại Id — phụ thuộc BaseService upsert; chỉ set 4 field |
| 2 | Lấy cấu hình order đơn vị | (app load màn đặt hàng) | POST `api/DonVi/GetCauHinhOrder` | — | public (login) `[SBAuthorizeCustomer]` | App khách | Gọi Order service `GetCauHinhOrder/{CurrentCustomer.DonViId}` → trả `CauHinhSettingDto`. Lỗi/null → `ERROR_HANDLER_SYSTEM`. | HTTP Order service | Phụ thuộc Order service up; idDonVi lấy từ JWT khách |

## Quy tắc nghiệp vụ (duyệt đơn vị)
1. `ApproveCode`: `LstDonVi==null` → bỏ qua check rỗng (chú ý: chỉ chặn khi `!= null && !Any()`); rỗng → lỗi "Vui lòng chọn dữ liệu".
2. Trùng MaDonVi với đơn vị khác (Id khác) → lỗi.
3. Mỗi item set: Id, MaDonVi, IsDuyetMaDonVi, NgayHetHanSuDung → quyết định login khách (đơn vị chưa duyệt / hết hạn → khách không login được).

## Quan hệ
- **Gọi sang:** Order service (`/api/CauHinh/GetCauHinhOrder/{idDonVi}`).
- **Dữ liệu (entities):** `DonVi` (MaDonVi, IsDuyetMaDonVi, NgayHetHanSuDung).
- **Ripple / đồng bộ:** đổi `IsDuyetMaDonVi`/`NgayHetHanSuDung` ảnh hưởng trực tiếp gating đăng nhập ở `AuthenController_AuthenCustomer` (login/register/refresh đều check đơn vị).

## Lỗi đã biết / lưu ý kỹ thuật
- `DonViService.cs:57` — check `req.LstDonVi != null && !req.LstDonVi.Any()` mới báo "Vui lòng chọn dữ liệu"; nếu `LstDonVi==null` thì rơi xuống `req.LstDonVi.Any(...)` ở dòng 62 → **NullReferenceException** khi gửi list null.
- `DonViService.cs:79` — `InsertAsync(lstDonVi)` dùng để upsert mã đơn vị; hành vi insert-vs-update phụ thuộc BaseService (cần xác nhận tránh tạo trùng).
