---
controller: TuongTacKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang › quanly-khachhang-detail › tab 'Lịch sử tương tác' (lichsu-tuongtac) — XEM nhật ký tương tác/thay đổi của KH"
muc_dich: "Ghi & tra cứu nhật ký tương tác khách hàng (audit log thay đổi/đơn hàng). Ghi log đa phần do service khác gọi private; UI chỉ tra cứu + lấy danh mục kiểu tương tác/tác động."
alias: ["Lịch sử tương tác", "Tương tác KH", "Nhật ký KH", "Customer interaction log", "Field changes"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangLichSuTuongTac]
stored_procs: []
depends:
  - ILogTuongTacKhachHangService
  - "MongoDB collection (Models.Core.CollectionMongoDB.Customer — KhachHangLichSuTuongTac)"
  - BaseController                 # CurrentUser
cross_module_in:
  - "Order / module bán hàng (gọi SaveLogTuongTacKhachHang, DeleteLogTuongTacKhachHangByIdDonHang, UpdateLogTuongTacKhachHangByIdContact qua PrivateSBAuthorize)"
cross_module_out: []
---

# TuongTacKhachHangController — Lịch sử tương tác khách hàng

> Hai nhóm endpoint rõ rệt: **private (ghi log)** do service khác gọi, và **public (tra cứu + danh mục)** cho tab Lịch sử tương tác. Không có logic tier/ngưỡng → bỏ "Quy tắc nghiệp vụ".

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu lịch sử tương tác | Lưới tab "Lịch sử tương tác" | `POST TimTheoDieuKien` | `[SBAuthorize]` (không key riêng — login-only) | public | Mở tab trong KH360 | ModelState invalid → lỗi; `TimTheoDieuKien(payload, CurrentUser)` — scope theo user. | Mongo (`KhachHangLichSuTuongTac`) | Không có permission key riêng → ai đăng nhập cũng xem được (cần xác nhận có ý đồ) |
| 2 | Danh mục kiểu/loại tương tác | Dropdown lọc "Loại tương tác" | `POST DanhSachKieuLoaiTuongTac` | `[SBAuthorize]` (login-only) | public | — | `DanhSachKieuLoaiTuongTac()` — list enum/danh mục. | — | — |
| 3 | Danh mục kiểu/loại tác động | Dropdown lọc "Loại tác động" | `POST DanhSachKieuLoaiTacDong` | `[SBAuthorize]` (login-only) | public | — | `DanhSachKieuLoaiTacDong()`. | — | — |
| 4 | Xem chi tiết field thay đổi | Popup "Chi tiết thay đổi" | `POST GetFieldChanges` | `[SBAuthorize]` (login-only) | public | — | ModelState invalid → lỗi; `GetFieldChanges(payload, CurrentUser)` — diff các field đã đổi. | Mongo | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Ghi log tương tác:** `POST SaveLogTuongTacKhachHang` (`[PrivateSBAuthorize]`) — service khác (vd Order khi tạo/sửa đơn) đẩy 1 bản ghi `KhachHangLichSuTuongTac`. Trả `true` cố định (fire-and-forget, `_service.SaveLog...` gọi không await kết quả).
- [internal] **Xóa log theo đơn hàng:** `POST DeleteLogTuongTacKhachHangByIdDonHang/{id:Guid}` (`[PrivateSBAuthorize]`) — khi đơn hàng bị hủy/xóa, dọn log tương tác gắn `idDonHang`.
- [internal] **Gán lại log theo contact:** `POST UpdateLogTuongTacKhachHangByIdContact/{idContact:Guid}/{idKhachHang:Guid}` (`[PrivateSBAuthorize]`) — merge/gán log của 1 contact sang 1 KH (vd khi gộp contact thành KH).

## Quan hệ
- **Gọi sang:** `ILogTuongTacKhachHangService`.
- **Dữ liệu (entities):** `KhachHangLichSuTuongTac` (collection Mongo `Models.Core.CollectionMongoDB.Customer`).
- **Ripple / đồng bộ:** ghi/xóa log do Order & các module nghiệp vụ trigger qua private endpoint; controller này không phát event ngược lại.

## Luồng xuyên module
1. **Order tạo/sửa/hủy đơn → ghi vết KH:** module bán hàng gọi `SaveLogTuongTacKhachHang` (tạo log) hoặc `DeleteLogTuongTacKhachHangByIdDonHang` (hủy đơn) → tab Lịch sử tương tác của KH cập nhật.
2. **Gộp contact thành KH:** gọi `UpdateLogTuongTacKhachHangByIdContact(idContact, idKhachHang)` → các log trước đây gắn contact được gán về KH chuẩn.

## Lỗi đã biết / lưu ý kỹ thuật
- `TuongTacKhachHangController.cs:22-31` & `:34-43` & `:46-55` — 3 endpoint private trả `true` cứng sau khi gọi service (không await/không kiểm tra kết quả) → caller không biết log có ghi/xóa thành công hay không (fire-and-forget).
- `TuongTacKhachHangController.cs:57,69,81,93` — các action UI dùng `[SBAuthorize]` **không kèm permission key** → chỉ chặn ở mức đăng nhập, không phân quyền theo role (khác hầu hết controller khác trong module).
