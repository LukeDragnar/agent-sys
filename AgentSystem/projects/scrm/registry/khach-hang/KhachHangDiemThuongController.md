---
controller: KhachHangDiemThuongController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang › quanly-khachhang-detail › tab 'Lịch sử điểm thưởng' (lichsu-diemthuong) — chỉ XEM lịch sử cộng/trừ điểm"
muc_dich: "Tra cứu lịch sử biến động điểm thưởng của 1 khách hàng (read-only). KHÔNG tích/đổi điểm — chỉ đọc collection log trên MongoDB."
alias: ["Lịch sử điểm thưởng", "Điểm thưởng KH", "Tiền thưởng", "Loyalty point history", "Tích điểm"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangDiemThuongLichSu]
stored_procs: []
depends:
  - IKhachHangDiemThuongService
  - "MongoDB collection: KhachHangDiemThuongLichSu"
  - "Core service: api/ChiNhanh/GetListChiNhanhByIds, api/ChiNhanh/LayListChiNhanhPrivate (map tên chi nhánh)"
cross_module_in:
  - "AuthenCustomer / mobile (gọi GetLichSuDiemThuongByKhachHangPrivate qua PrivateSBAuthorize)"
cross_module_out: []
---

# KhachHangDiemThuongController — Lịch sử điểm thưởng khách hàng

> Controller MỎNG (2 endpoint). Chỉ ĐỌC log điểm thưởng từ Mongo. Logic **tích điểm / đổi điểm / quy đổi tiền↔điểm KHÔNG nằm ở đây** — nó nằm ở `ConfigDoiDiemController` (cấu hình) + flow bán hàng (Order/Invoice ghi log). Phần "Quy tắc nghiệp vụ" bên dưới là **cross-reference** để trace hiểu nguồn dữ liệu log.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu lịch sử điểm thưởng theo KH | Lưới tab "Lịch sử điểm thưởng" + filter từ ngày/đến ngày | `POST TimTheoDieuKien` | KhachHangDiemThuongController_TimTheoDieuKien (AD-TONG/QLDV/AD-CN/AD-MCN/KT/KT-TONG/NV/HT-AD/SALE*/MKT*) | public | Mở tab trong hồ sơ KH360 | ModelState invalid → `MESS_Model_UnValidate`. Lọc Mongo `KhachHangDiemThuongLichSu` theo `IdDonVi=userLogin.DonViId` + `IdKhachHang`, sort `NgayTao` desc, phân trang; enrich `TenChiNhanh` từ Core. | Mongo (`KhachHangDiemThuongLichSu`) | Xem bên dưới |
| 2 | Lấy lịch sử điểm thưởng (cross-service) | (không UI — gọi nội bộ) | `POST GetLichSuDiemThuongByKhachHangPrivate` | `[PrivateSBAuthorize]` | internal | — | Lọc theo `gridInfo.IdDonVi` + `gridInfo.IdKhachHang` (truyền từ caller, KHÔNG lấy từ token); enrich tên chi nhánh qua `LayListChiNhanhPrivate`; map sang `KhachHangDiemThuongTimKiemDto`. | Mongo (`KhachHangDiemThuongLichSu`) | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] App KH / AuthenCustomer gọi `GetLichSuDiemThuongByKhachHangPrivate` để hiển thị điểm thưởng phía khách hàng — `IdDonVi`/`IdKhachHang` lấy từ payload caller, không từ token đăng nhập.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng) — CROSS-REFERENCE (nguồn log, không thuộc controller này)
> Log mà controller này đọc được **sinh ra từ cấu hình quy đổi điểm** `ConfigDoiDiem` / `ConfigDoiDiemChiTiet` (entity `Customer.DataContext.Entity.ConfigDoiDiem.cs`, `ConfigDoiDiemChiTiet.cs`). Trace điểm thưởng phải mô phỏng theo các field cấu hình sau:

1. **Cho phép tích điểm?** `ConfigDoiDiem.IsAllowTichDiem` = false → KHÔNG cộng điểm (không sinh log dòng tích). true → tiếp.
2. **Tích điểm theo loại chứng từ** (bật/tắt độc lập): `IsTichDiemDichVu`, `IsTichDiemCombo`, `IsTichDiemSanPham`, `IsTichDiemThuKhac`, `IsTichDiemThe`. Loại nào = false thì doanh thu loại đó KHÔNG được quy ra điểm.
3. **Quy đổi tiền → điểm:** `ConfigDoiDiem.TienThanhDiem` (số tiền tương ứng 1 điểm). Điểm cộng ≈ doanh thu hợp lệ / `TienThanhDiem`.
4. **Cho phép đổi điểm?** `IsAllowDoiDiem` = false → không cho trừ/đổi điểm thành tiền. true → tiếp.
5. **Quy đổi điểm → tiền:** `ConfigDoiDiem.DiemThanhTien` (1 điểm = bao nhiêu tiền giảm). `IsThuNganLuaChonDoiDiem` = thu ngân được tự chọn dùng điểm hay không tại POS.
6. **Quy đổi nâng cao theo hạng/nhóm:** `ConfigDoiDiemChiTiet` — `IdHangKhachHang` / `IdNhomKhachHang` + `TiLeQuyDoiMotDonVi` + `LoaiQuyDoi` (1 = TienThanhDiem, 2 = DiemThanhTien). KH thuộc hạng/nhóm nào → áp tỉ lệ riêng thay vì tỉ lệ mặc định.

> ⚠️ Controller `KhachHangDiemThuongController` KHÔNG áp các rule này — nó chỉ phản ánh kết quả đã ghi vào Mongo. Để sửa logic tích/đổi điểm phải vào `ConfigDoiDiemController` + nơi ghi log (Order/Invoice/POS).

## Quan hệ
- **Gọi sang:** Core `api/ChiNhanh/GetListChiNhanhByIds` (public) & `api/ChiNhanh/LayListChiNhanhPrivate/{idDonVi}` (private) để map tên chi nhánh vào từng dòng log.
- **Dữ liệu (entities):** `KhachHangDiemThuongLichSu` (MongoDB collection `MongoDBCollection.KhachHangDiemThuongLichSu`) — fields chính: `IdDonVi`, `IdKhachHang`, `IdChiNhanh`, `NgayTao`.
- **Ripple / đồng bộ:** không phát event (read-only). Nguồn ghi log nằm ngoài controller.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhachHangDiemThuongService.cs:49` — filter theo `IdChiNhanh` đã **bị comment** → list trả về toàn bộ chi nhánh trong đơn vị, không giới hạn theo chi nhánh user đăng nhập.
- `KhachHangDiemThuongService.cs:54-58` — logic ngày **ngược chiều trực giác**: `TuNgay` dùng `Lte(NgayTao)` và `DenNgay` dùng `Gte(NgayTao)`. Nếu UI gửi TuNgay < DenNgay theo nghĩa thông thường thì filter sẽ rỗng (cần xác nhận semantics field hoặc đây là bug).
- `KhachHangDiemThuongService.cs:63,94` — `total` đếm theo `IdKhachHang` thôi (bỏ qua filter `IdDonVi` và khoảng ngày) → tổng số bản ghi (paging) **không khớp** với data đã lọc theo ngày.
- Auth `mixed`: action public dùng token (`userLogin.DonViId`), action private dùng `IdDonVi` từ payload → caller nội bộ phải truyền đúng đơn vị, không có scope tự động.
