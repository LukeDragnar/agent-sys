---
controller: ChiNhanhController
module: he-thong
service: Core
man_hinh_ui: "Cấu hình hệ thống > Đơn vị > Chi nhánh (CRUD chi nhánh của đơn vị); bản QuanTriArea: Quản trị > Đơn vị > Chi nhánh (HT-AD/HT-KT quản trị toàn hệ thống)"
muc_dich: "CRUD chi nhánh + nhiều endpoint tra cứu/lọc danh sách chi nhánh (theo đơn vị, theo role, theo id). Có 2 class trùng tên: bản app (route api/ChiNhanh) và bản quản trị (route api/QuanTriArea/ChiNhanh)."
alias: ["chi nhánh", "ChiNhanh", "branch", "cửa hàng/chi nhánh đơn vị"]
auth: mixed
state_machine: []
deprecated: false
entities: [ChiNhanh]
stored_procs: []
depends: ["IChiNhanhService"]
cross_module_in: ["FE Đơn vị/Chi nhánh gọi CRUD + dropdown chi nhánh", "Module khác gọi private (LayListChiNhanhPrivate, TimTheoMa_Private, GetAllChiNhanhTheoDonViPrivate...) để map chi nhánh"]
cross_module_out: ["Đồng bộ vị trí/location chi nhánh (ChangeLocationChiNhanh) — AllowAnonymous", "Cung cấp danh sách chi nhánh cho SMS / Tổng đài IP"]
---

# ChiNhanhController — Quản lý chi nhánh

Có **2 class cùng tên `ChiNhanhController`** trong service Core:
- **Bản app** (`Controllers/ChiNhanhController.cs`, `[TienIch(..., TI_Chung)]`, route `api/ChiNhanh/...`): CRUD + nhiều endpoint tra cứu/dropdown cho người dùng đơn vị; phân quyền theo role đơn vị (DV-AD-TONG, DV-AD-CN...). Có nhiều endpoint `[PrivateSBAuthorize]` service-to-service.
- **Bản quản trị** (`Areas/SanboxArea/ChiNhanhController.cs`, `[TienIch(..., TI_HeThong, true)]`, route `api/QuanTriArea/ChiNhanh/...`): CRUD + tra cứu cho admin hệ thống (HT-AD, HT-KT). `Xoa` ở bản này gọi `DeleteAsync` (xóa cứng/service base) thay vì `Xoa(...)` như bản app.

Cả hai ủy quyền cho `IChiNhanhService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Grid/Lọc | POST `api/ChiNhanh/TimTheoDieuKien`, POST `api/QuanTriArea/ChiNhanh/TimTheoDieuKien` | `ChiNhanh_TimTheoDieuKien` | public | — | Lọc danh sách chi nhánh. Bản app: nếu user không phải super/CSKH/kế toán hệ thống → ép `req.IdDonVi = CurrentUser.DonViId` (luôn ép sau đó, xem known-issue). | EF `ChiNhanh` | bản app ép `req.IdDonVi` 2 lần (`Controllers/ChiNhanhController.cs:34,36`) → nhánh check role thừa, cuối cùng luôn dùng DonViId. |
| 2 | Xem chi tiết | Mở chi tiết | GET `api/ChiNhanh/TimTheoId/{id}`, GET `api/QuanTriArea/ChiNhanh/TimTheoId/{id}` | `ChiNhanh_TimTheoId` | public | — | Trả chi tiết 1 chi nhánh theo id. | EF `ChiNhanh` | — |
| 3 | Xem chi tiết (public) | — | GET `api/ChiNhanh/TimTheoIdPublic/{id}` | — | anonymous | — | Trả chi tiết chi nhánh không cần đăng nhập. | EF `ChiNhanh` | — |
| 4 | Thêm/Sửa/Xóa | Thêm/Sửa/Xóa | POST `.../ThemMoi`, POST `.../CapNhatThongTin`, POST `.../Xoa/{id}` (cả 2 route) | `ChiNhanh_ThemMoi` / `ChiNhanh_CapNhatThongTin` / `ChiNhanh_Xoa` | public | — | Bản app ThemMoi ép `req.IdDonVi = CurrentUser.DonViId`. Bản app Xoa → `_service.Xoa(id, user)`; bản quản trị Xoa → `_service.DeleteAsync(id)`. | EF `ChiNhanh` | — |
| 5 | Lấy tất cả (toàn hệ thống) | — | GET `api/QuanTriArea/ChiNhanh/LayTatca` | `ChiNhanh_LayTatCa` | public (HT-AD/HT-KT) | — | `GetAllAsync()` — chỉ bản quản trị. | EF `ChiNhanh` | — |
| 6 | Dropdown / danh sách theo đơn vị-role | Combobox chi nhánh | POST `.../LayListChiNhanh`, POST `.../LayListChiNhanhHasPermission`, GET `.../GetChiNhanhTheoDonVi`, POST `api/ChiNhanh/LayListChiNhanhTheoRole`, POST `.../GetListChiNhanhByIds` | nhiều key (`ChiNhanh_GetListChiNhanhByIds`, `ChiNhanh_GetChiNhanhTheoDonVi`, `ChiNhanh_LayListChiNhanhTheoRole`...) + 1 số chỉ `[SBAuthorize]` | public | tùy role | Lấy danh sách chi nhánh user được phép (theo DonViId/role) để đổ combobox. | EF `ChiNhanh` | nhiều endpoint chỉ `[SBAuthorize]` không có permission key riêng (LayListChiNhanh, LayListChiNhanhHasPermission...) → không phân quyền chi tiết. |
| 7 | Danh sách cho SMS / Tổng đài IP | — | POST `api/ChiNhanh/TongDaiIp_DanhSachChiNhanhTheoRole`, POST `api/ChiNhanh/Sms_DanhSachChiNhanhTheoRole` | — (`[SBAuthorize]`) | public | — | Danh sách chi nhánh custom (kèm 1 dòng id=empty đầu) cho lựa chọn SMS/Tổng đài. | EF `ChiNhanh` | — |
| 8 | Đổi vị trí chi nhánh | — | POST `api/ChiNhanh/ChangeLocationChiNhanh` | — | anonymous | ModelState valid | Cập nhật location chi nhánh (validate model trước). | EF `ChiNhanh` | AllowAnonymous cho thao tác ghi → cần xác minh có cơ chế bảo vệ khác. |

## Endpoint nội bộ (private / service-to-service)
- [internal] `api/ChiNhanh/LayListChiNhanhPrivate/{id}`, `LayListChiNhanhPhanLoaiPrivate`, `GetListTinhThanhChiNhanhPrivate/{id}`, `TimTheoIdPrivate/{id}`, `GetAllChiNhanhTheoDonViPrivate/{idDonVi}`, `SetChiNhanhTong/{id}`, `LayTenChiNhanh/{id}`, `TimTheoMa_Private/{idDonVi}/{maChiNhanh}`, `TimTheoMa_Private_V2` — tất cả `[PrivateSBAuthorize]`: cho module/website khác tra cứu chi nhánh theo id/mã/đơn vị.
- [internal] bản quản trị: `api/QuanTriArea/ChiNhanh/LayListChiNhanhAPI/{id}` `[PrivateSBAuthorize]`.

## Quan hệ
- **Gọi sang:** `IChiNhanhService` (toàn bộ).
- **Dữ liệu (entities):** `ChiNhanh`. DTO/Req: `ChiNhanhDto`, `ChiNhanhFilterReq`, `ChangeLocationChiNhanhPayload`, `ChiNhanhPhanLoaiPayload`, `ChiNhanhInfoByMaChiNhanhPayload`.
- **Ripple / đồng bộ:** đổi location chi nhánh; cung cấp dữ liệu cho SMS/Tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers/ChiNhanhController.cs:34-36` — `TimTheoDieuKienAsync` ép `req.IdDonVi` 2 lần → nhánh `if` kiểm tra role thành thừa, mọi non-super luôn bị giới hạn về DonViId của mình.
- `Controllers/ChiNhanhController.cs:40-76` — nhiều block comment trùng lặp endpoint `TimTheoIdPublic` (rác code).
- `Controllers/ChiNhanhController.cs:195` — `ChangeLocationChiNhanh` là `[AllowAnonymous]` nhưng ghi dữ liệu.
- Nhiều endpoint dropdown chỉ `[SBAuthorize]` không permission key → không phân quyền chi tiết.
