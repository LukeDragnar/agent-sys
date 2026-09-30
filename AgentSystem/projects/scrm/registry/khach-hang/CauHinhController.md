---
controller: CauHinhController
module: khach-hang
service: Customer
man_hinh_ui: "nội bộ — không có UI riêng; cấu hình service Customer (gating 'xem KH toàn đơn vị', quyền xuất Excel) đọc/ghi qua màn hình admin cấu hình + dùng làm flag cho các màn KH360"
muc_dich: "Quản lý cấu hình của service Customer: cho phép xem khách hàng toàn đơn vị, cho lead/quản lý xem thông tin KH của nhân viên, kiểm tra quyền xuất Excel; cung cấp cấu hình private cho service khác"
alias: [CauHinh, "Cấu hình khách hàng", CauHinhCustomer, "Config Customer"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhSetting]
stored_procs: []
depends: [ICauHinhService, BaseController]
cross_module_in: ["(private) service khác gọi PrivateGetConfig / GetCauHinhChoPhepXemKhachHangToanDonVi_ByIdDonVi để lấy cấu hình theo đơn vị"]
cross_module_out: ["KhachHang/KH360 (cấu hình 'xem KH toàn đơn vị' & 'lead xem KH nhân viên' chi phối scope dữ liệu khi search KH)"]
---

# CauHinh — Cấu hình service khách hàng

> Controller nhỏ (8 endpoint). Chủ yếu là **flag gating** chi phối phạm vi xem khách hàng + quyền xuất Excel; không có entity nghiệp vụ nặng.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình KH | (load form cấu hình) | `GET Get` | — (`[SBAuthorize]`, key gốc `CauHinh_Customer_LayCauHinhKhachHang` đang comment) | public | — | `GetAsync(CurrentUser)` trả cấu hình theo đơn vị user | EF | Key quyền chi tiết bị comment → chỉ cần đăng nhập (cần xác nhận có chủ ý) |
| 2 | Lưu cấu hình khởi tạo | Nút "Lưu cấu hình" | `POST SaveCauHinhInit` | CauHinh_Customer_LuuCauHinhKhachHang | public | — | ModelState invalid → MESS_Model_UnValidate; hợp lệ → `SaveCauHinhInit(inSetting, CurrentUser)` | EF | — |
| 3 | Lấy cấu hình khởi tạo | (load init) | `GET GetCauHinhInit` | — (`[SBAuthorize]`) | public | — | `GetCauHinhInit(CurrentUser)` | EF | — |
| 4 | Cờ "xem KH toàn đơn vị" | (gating list KH) | `GET GetCauHinhChoPhepXemKhachHangToanDonVi` | CauHinh_Customer_GetCauHinhChoPhepXemKhachHangToanDonVi | public | — | Trả flag cho phép user xem KH toàn đơn vị (không bó theo nhân viên) | EF | — |
| 5 | Cờ "lead/QL xem KH của NV" | (gating list KH) | `GET GetCauHinhChoPhepLeadQuanLyXemThongTinKhachHangCuaNhanvien` | — (`[SBAuthorize]`) | public | — | Trả flag cho lead/quản lý xem thông tin KH của nhân viên dưới quyền | EF | — |
| 6 | Kiểm tra quyền xuất Excel | (ẩn/hiện nút Xuất Excel) | `GET CheckQuyenXuatExcel` | — (`[SBAuthorize]`) | public | — | `CheckQuyenXuatExcel(CurrentUser)` → FE ẩn/hiện nút export | EF | — |

## Endpoint nội bộ (private / service-to-service)

- [internal] Service khác lấy cấu hình theo đơn vị: `GET GetCauHinhChoPhepXemKhachHangToanDonVi_ByIdDonVi/{id}` (`[PrivateSBAuthorize]`) — không dùng CurrentUser mà nhận idDonVi từ caller.
- [internal] `POST PrivateGetConfig` (`[PrivateSBAuthorize]`) — wrapper private của `GetAsync(CurrentUser)`, cho service khác lấy toàn bộ cấu hình.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)

- **Cờ "xem KH toàn đơn vị"** (`GetCauHinhChoPhepXemKhachHangToanDonVi`): bật → user thấy toàn bộ KH của đơn vị; tắt → chỉ thấy KH được gán/của chính mình. Đây là flag chi phối scope ở các màn KH360.
- **Cờ "lead/QL xem KH của NV"** (`...LeadQuanLyXemThongTinKhachHangCuaNhanvien`): bật → lead/quản lý xem được KH thuộc nhân viên dưới quyền.
- **CheckQuyenXuatExcel**: quyết định ẩn/hiện chức năng xuất Excel KH ở FE.
- Field cấu hình cụ thể nằm trong `ICauHinhService` (cần xác nhận tên cột trong CauHinhSetting).

## Quan hệ
- **Gọi sang:** `ICauHinhService` (toàn bộ logic đọc/ghi cấu hình).
- **Dữ liệu (entities):** cấu hình service Customer (CauHinhSetting — cần xác nhận tên entity chính xác).
- **Ripple / đồng bộ:** flag cấu hình chi phối scope dữ liệu ở `KhachHangController` (KH360) — đổi cấu hình → đổi tập KH user nhìn thấy.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:17` — `[SBAuthorize("CauHinhController")]` controller-level bị comment; phân quyền dựa hoàn toàn vào từng action.
- `CauHinhController.cs:28-29` (`Get`) — key quyền chi tiết `CauHinh_Customer_LayCauHinhKhachHang` bị comment, chỉ còn `[SBAuthorize]` → bất kỳ user đăng nhập đọc được cấu hình (cần xác nhận chủ ý).
- `CauHinhController.cs:67` (`..._ByIdDonVi`) & `:83` (`PrivateGetConfig`) — endpoint private nhận/lấy cấu hình theo đơn vị cho service khác; thay đổi shape cấu hình ảnh hưởng các caller cross-service.
