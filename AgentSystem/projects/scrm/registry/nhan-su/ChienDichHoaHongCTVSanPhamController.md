---
controller: ChienDichHoaHongCTVSanPhamController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Chiến dịch hoa hồng > tab Hoa hồng theo sản phẩm (lưới SP × nhóm; nút Áp dụng toàn nhóm / Xóa)"
muc_dich: "Cấu hình % hoa hồng theo sản phẩm/dịch vụ cho 1 chiến dịch (theo nhóm hoa hồng): thêm mới, áp dụng toàn nhóm, xóa từng dòng / xóa toàn nhóm, tìm kiếm theo điều kiện."
alias: [ChienDichHoaHongCTVSanPham, "HH sản phẩm theo chiến dịch", "cấu hình SP chiến dịch"]
auth: public
state_machine: []
deprecated: false
entities: [ChienDichHoaHongCTVSanPham, ChienDichHoaHongCTV, NhomHoaHongCTV]
stored_procs: [PRC_CommissionFee_GetCauHinh_ChienDich]
depends: [ChienDichHoaHongCTV, NhomHoaHongCTV, WarehouseProduct/SanPham]
cross_module_in: []
cross_module_out: [CommissionFeeUpdateOrder (% theo SP/chiến dịch để tính DonHangChiTietChienDich)]
---

# ChienDichHoaHongCTVSanPham — Cấu hình % hoa hồng theo SP cho chiến dịch

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Xóa cấu hình SP toàn nhóm | Xóa toàn nhóm | POST `ChienDichHoaHongSanPhamClearToanBoNhom` | `ChienDichHoaHongCTVSanPham_ChienDichHoaHongSanPhamClearToanBoNhom` | public | Xóa toàn bộ cấu hình HH SP của chiến dịch theo nhóm. | EF | — |
| 2 | Xóa cấu hình SP (dòng) | Xóa | POST `ChienDichHoaHongSanPhamClear` | `ChienDichHoaHongCTVSanPham_ChienDichHoaHongSanPhamClear` | public | Xóa dòng cấu hình HH SP chỉ định. | EF | — |
| 3 | Thêm mới cấu hình SP | Lưu | POST `ChienDichHoaHongSanPhamThemMoi` | `ChienDichHoaHongCTVSanPham_ChienDichHoaHongSanPhamThemMoi` | public | Thêm cấu hình % HH (HoaHongCTV, HoaHongLienKet) cho SP trong chiến dịch. | EF | — |
| 4 | Áp dụng SP toàn nhóm | Áp dụng toàn nhóm | POST `ChienDichHoaHongSanPhamApDungToanBoNhom` | `ChienDichHoaHongCTVSanPham_ChienDichHoaHongSanPhamApDungToanBoNhom` | public | Set 1 mức % cho toàn bộ SP của nhóm trong chiến dịch (hàng loạt). | EF | — |
| 5 | Tìm theo điều kiện | Lưới/Lọc | POST `TimKiemTheoDieuKien` | `ChienDichHoaHongCTVSanPham_TimKiemTheoDieuKien` | public | Trả cấu hình HH SP theo chiến dịch/nhóm theo bộ lọc. | EF (+ SP) | — |

## Quan hệ
- **Gọi sang:** EF; cấu hình tổng hợp đọc lại qua SP `PRC_CommissionFee_GetCauHinh_ChienDich`.
- **Dữ liệu (entities):** `ChienDichHoaHongCTVSanPham` (mỗi dòng = chiến dịch × nhóm × SP × %HoaHongCTV/%HoaHongLienKet). DTO `ChienDichHoaHongCTVSanPhamDto` mang `IdChienDichHoaHongCTV`, `IdNhomHoaHongCTV`, `IdDoiTuong`, `LoaiDoiTuong`, `HoaHongCTV`, `HoaHongLienKet`.
- **Ripple/đồng bộ:** đây là nguồn % thực để `CommissionFeeUpdateOrder` tính `GiaTriHoaHongCTV_*` / `GiaTriHoaHongLienKet_*` cho `DonHangChiTietChienDich` (khớp `IdDoiTuong + LoaiDoiTuong + IdNhomHoaHongCTV`).

## Lỗi đã biết / lưu ý kỹ thuật
- Cấu hình ở đây chỉ phát huy khi: nhóm bật `IsSuDungHoaHongChienDich`, chiến dịch `IsSuDung=true`, và `Now ∈ [TuNgay, DenNgay]` (SP lọc theo `NgayApDung=Now`).
- Thao tác "Áp dụng/Xóa toàn nhóm" là batch — không xác nhận từng dòng; cần cẩn trọng khi nhiều SP.
- Khớp HH ở pipeline dùng đồng thời `IdDoiTuong + LoaiDoiTuong + IdNhomHoaHongCTV`; lệch bất kỳ field nào → dòng đơn không nhận % → hoa hồng chiến dịch = 0.
