---
controller: HoaHongGioiThieuController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Nguồn giới thiệu > (mở 1 nguồn) > tab Hoa hồng giới thiệu (báo cáo hoa hồng theo nguồn + khoảng ngày)"
muc_dich: "Tra cứu/báo cáo chi tiết hoa hồng giới thiệu đã phát sinh theo từng nguồn giới thiệu trong khoảng thời gian (hoa hồng được sinh tự động khi thanh toán đơn có nguồn giới thiệu)."
alias: ["hoa hồng giới thiệu", "HoaHongGioiThieu", "commission", "báo cáo hoa hồng"]
auth: public
state_machine: []
deprecated: false
entities: [HoaHongGioiThieu, NguonGioiThieu, CauHinhHoaHongGioiThieu]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_HoaHongGioiThieu — đơn vị phải mua tiện ích)"
  - "CommonService (PrivateGetDichVu/SanPham/The/ComboByIds — quy đổi tên/mã đối tượng)"
cross_module_in: []
cross_module_out: []
---

# HoaHongGioiThieu — Báo cáo hoa hồng giới thiệu

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo hoa hồng theo nguồn | Danh sách (theo nguồn + ngày) | POST `TimTheoDieuKien_TheoIdNguonGT` | `HoaHongGioiThieu_TimTheoDieuKien_TheoIdNguonGT` (AD-TONG/CN/MCN) | public | Bắt buộc `IdNguonGioiThieu` + `TuNgay`/`DenNgay`. Lọc HoaHongGioiThieu theo `IdDonVi` (từ token) + nguồn + ngày. Có lọc thêm keyword (theo MaDonHang) + KieuLoai. Quy đổi tên/mã đối tượng (DichVu/SanPham/Combo/The) qua CommonService. | EF (TableWithNolock) + private call | Validate truyền thiếu trả message tiếng Việt, không phải ModelState |

## Quy tắc nghiệp vụ (sinh hoa hồng — logic ở PromotionUpdateWithBillService.HoaHongGioiThieuUpdate)
> Controller này CHỈ tra cứu. Hoa hồng được **sinh tự động khi thanh toán** đơn có `IdNguonGioiThieu`:
1. `IdNguonGioiThieu == null` → không sinh. `ListDoiTuong` rỗng → không sinh.
2. Nguồn giới thiệu phải tồn tại theo `IdDonVi` → không thì bỏ.
3. Lấy `CauHinhHoaHongGioiThieu` theo `IdNguonGioiThieu`; rỗng → bỏ.
4. Với mỗi dòng đơn: tìm cấu hình khớp `IdDoiTuong + KieuLoai (Type)`; có → tính hoa hồng:
   - `DonViTinh == Tien` → `HoaHong = GiaTri * SoLuong`
   - ngược lại (phần trăm) → `HoaHong = Math.Floor(ThanhTien * GiaTri / 100)`
5. Khi **hủy đơn** → toàn bộ HoaHongGioiThieu của `IdDonHang` bị **xóa** (PromotionRollback).

## Quan hệ
- **Gọi sang:** CommonService (PrivateGetDichVuByIds / PrivateGetSanPhamByIds / PrivateGetTheByIds / PrivateGetComboByIds — chạy `Task.WhenAll`, lấy tên+mã đối tượng).
- **Dữ liệu (entities):** `HoaHongGioiThieu` (mỗi dòng = 1 hoa hồng phát sinh, gắn IdDonHang/IdDoiTuong/KieuLoai/HoaHong), `NguonGioiThieu`, `CauHinhHoaHongGioiThieu`.
- **Ripple/đồng bộ:** không phát event; là bảng đọc. Nguồn ghi là PromotionUpdateWithBillService (insert khi TT, delete khi hủy).

## Lỗi đã biết / lưu ý kỹ thuật
- `HoaHongGioiThieuService.cs:78-82` — `query` filter `IdDonVi == userLogin.DonViId` từ token (đúng), nhưng so ngày `x.NgayTao.Value.Date` không guard `NgayTao == null` → NRE nếu có bản ghi NgayTao null.
- `HoaHongGioiThieuService.cs:94-102` — set `Sorts` rồi vẫn `.OrderBy(x => x.NgayTao)` đè lên ở câu phân trang → cột sort của user bị bỏ qua (sort luôn theo NgayTao asc).
- `HoaHongGioiThieuService.cs:151-205` — nếu BẤT KỲ private call (DV/SP/The/Combo) fail → nhánh else chỉ set KieuLoaiText, **không có tên/mã đối tượng** (TenDoiTuong/MaDoiTuong rỗng) → UI hiển thị thiếu.
- `HoaHongGioiThieu` hoa hồng % bị `Math.Floor` khi sinh (PromotionUpdateWithBillService.cs:2241) → tổng báo cáo có thể lệch vài đồng so với tính tay.
