---
controller: TheDichVuKhachHangLichSuController
module: khach-hang
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Thẻ dịch vụ của khách hàng (thedichvu-khachhang) → popup 'Lịch sử' (thedichvu-khachhang-lichsu.component) — xem thông tin thẻ + lịch sử giao dịch (nạp / dùng / hoàn) của 1 thẻ dịch vụ khách đang sở hữu."
muc_dich: "Tra cứu LỊCH SỬ GIAO DỊCH của 1 thẻ dịch vụ khách hàng (TheDichVuKhachHang): trả thông tin tổng quan thẻ (số tiền/số lần còn lại, hạn dùng, khách, chi nhánh) + danh sách giao dịch tác động (nạp tiền/bonus, trừ tiền, trừ lần) lấy từ MongoDB. Read-only."
alias: ["lịch sử thẻ dịch vụ", "lịch sử thẻ khách hàng", "lịch sử giao dịch thẻ", "lịch sử sử dụng thẻ", "lịch sử nạp thẻ", "giao dịch thẻ dịch vụ"]
auth: public
state_machine: []
deprecated: false
entities: [TheDichVuKhachHang, TheDichVuKhachHangLichSuGiaoDich]
stored_procs: []
depends: [WarehouseProduct, Customer (KhachHang/TimTheoIdBase), Core/ChiNhanh (TimTheoId), MongoDB (lịch sử giao dịch thẻ)]
cross_module_in: [thedichvu-khachhang screen (FE — popup lịch sử)]
cross_module_out: []
---

# TheDichVuKhachHangLichSuController — Lịch sử giao dịch thẻ dịch vụ của khách

> Controller READ-ONLY 1 endpoint: xem lịch sử của 1 thẻ dịch vụ khách hàng. Phần "thông tin thẻ" lấy từ SQL (`TheDichVuKhachHang`) + làm giàu tên khách (Customer) và tên chi nhánh (Core); phần "lịch sử giao dịch" lấy từ bảng `TheDichVuKhachHangLichSuGiaoDich`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem lịch sử giao dịch thẻ KH | "Lịch sử" (popup thẻ KH) → `LichSuDungThe` | POST `api/TheDichVuKhachHangLichSu/TimTheoDieuKien` | `TheDichVuKhachHangLichSu_TimTheoDieuKien` | public | có `IdTheDichVuKhachHang` | `IdTheDichVuKhachHang` rỗng → "Phải chọn thẻ dịch vụ muốn xem!"; lấy thẻ theo id+đơn vị (ko thấy → "Không tìm thấy thẻ dịch vụ!"); gọi Customer `KhachHang/TimTheoIdBase` + Core `ChiNhanh/TimTheoId` làm giàu tên; lọc lịch sử theo TuNgay/DenNgay/Keyword(MaDonHang) + phân trang; trả `{ ThongTinThe, LichSu[] }` kèm TextImpact (loại tác động) | EF (TheDichVuKhachHang + LichSuGiaoDich) + HTTP (Customer, Core) | Gọi `_clientHelper.PostAsync(...).Result` / `.GetAsync(...).Result` (block async) — nguy cơ deadlock/giảm hiệu năng (Service:96,98) |

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có endpoint private. Controller chỉ phục vụ 1 màn FE (popup lịch sử thẻ). Method `GetByIdDonHangAndDelete` tồn tại trong service nhưng **không expose qua controller** và `throw NotImplementedException` (Service:66-70) — dead code.

## Quy tắc nghiệp vụ (gating)
- **Bắt buộc chọn thẻ**: thiếu `IdTheDichVuKhachHang` → chặn ngay.
- **Cô lập đơn vị**: thẻ phải thuộc `IdDonVi` của user (đoạn lọc theo chi nhánh non-admin đã bị comment — Service:80-83 → hiện KHÔNG giới hạn theo chi nhánh, mọi user trong đơn vị xem được lịch sử thẻ bất kỳ).
- **Filter lịch sử**: TuNgay/DenNgay so theo `.Date` của NgayTao; Keyword tìm trong `MaDonHang` (lowercase contains).

## Quan hệ
- **Gọi sang:** Customer `KhachHang/TimTheoIdBase` (tên/SĐT khách), Core `ChiNhanh/TimTheoId` (tên chi nhánh).
- **Dữ liệu (entities):** `TheDichVuKhachHang` (đầu thẻ khách: số tiền/số lần còn lại, SoTienDaDungKhoiTao, SoTienKhuyenMai*, NgayHetHan, TrangThai, LoaiThe), `TheDichVuKhachHangLichSuGiaoDich` (mỗi giao dịch: SoTienNap/SoTienTacDong/SoTienThuong/SoLanTacDong, KieuLoai = TypeImpact, MaDonHang/IdDonHang). TextImpact map từ enum `EnumTypeImpact_Transaction`.
- **Ripple / đồng bộ:** không có (read-only). Lịch sử được GHI bởi các luồng nạp/dùng thẻ ở Order/thu ngân (ngoài controller này); đây chỉ là màn tra cứu.

## Lỗi đã biết / lưu ý kỹ thuật
- **Block async bằng `.Result`**: `TheDichVuKhachHangLichSuService.cs:96` (Customer) và `:98` (ChiNhanh) gọi `.Result` trên Task → nguy cơ deadlock/thread-pool starvation, nên `await`.
- **Filter theo chi nhánh bị comment** (Service:80-83) → trong cùng đơn vị, user chi nhánh khác vẫn xem được lịch sử thẻ không thuộc chi nhánh mình — cần xác nhận có chủ ý (rủi ro lộ dữ liệu).
- **Không null-check kết quả HTTP**: `khachHang`/`chiNhanh` dùng trực tiếp `.Data.HoVaTen` / `.TenChiNhanh` → nếu Customer/Core trả null hoặc fail → NullReferenceException (Service:96-120).
- **Dead code**: `GetByIdDonHangAndDelete` (Service:66-70) gọi Mongo rồi `throw NotImplementedException`, không dùng — nên dọn.
- **Module file đặt ở `khach-hang`** dù service là WarehouseProduct: vì nghiệp vụ thuộc thẻ dịch vụ CỦA KHÁCH HÀNG (tra cứu từ hồ sơ thẻ KH). Controller-level chỉ có `[TienIch(..., TI_Kho)]`, không có `[SBAuthorize]` ở class → quyền khai báo per-method.
