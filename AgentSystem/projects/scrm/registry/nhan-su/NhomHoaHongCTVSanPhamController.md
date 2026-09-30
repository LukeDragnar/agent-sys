---
controller: NhomHoaHongCTVSanPhamController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Nhóm hoa hồng > tab Hoa hồng theo sản phẩm (lưới SP/dịch vụ, nhập %HH; nút Áp dụng toàn nhóm / Xóa)"
muc_dich: "Cấu hình hoa hồng theo từng sản phẩm/dịch vụ cho 1 nhóm hoa hồng CTV: thêm mới, áp dụng cho toàn bộ nhóm, xóa từng dòng hoặc xóa toàn nhóm; tìm kiếm theo điều kiện."
alias: [NhomHoaHongCTVSanPham, "hoa hồng sản phẩm", "cấu hình HH theo SP"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhHoaHongSanPhamCTV, NhomHoaHongCTV]
stored_procs: [PRC_CommissionFee_GetCauHinh_NhomHoaHongCTV_SanPham]
depends: [NhomHoaHongCTV, WarehouseProduct/SanPham (đối tượng SP/dịch vụ)]
cross_module_in: []
cross_module_out: [CommissionFeeUpdateOrder (% theo SP để tính HH), CongTacVien (GetStats_SanPham)]
---

# NhomHoaHongCTVSanPham — Cấu hình hoa hồng theo sản phẩm/dịch vụ

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Xóa cấu hình HH SP toàn nhóm | Xóa toàn nhóm | POST `HoaHongSanPhamClearToanBoNhom` | `NhomHoaHongCTVSanPham_HoaHongSanPhamClearToanBoNhom` | public | Xóa toàn bộ cấu hình HH sản phẩm của nhóm. | EF | — |
| 2 | Xóa cấu hình HH SP (dòng) | Xóa | POST `HoaHongSanPhamClear` | `NhomHoaHongCTVSanPham_HoaHongSanPhamClear` | public | Xóa dòng cấu hình HH sản phẩm chỉ định. | EF | — |
| 3 | Thêm mới cấu hình HH SP | Lưu | POST `HoaHongSanPhamThemMoi` | `NhomHoaHongCTVSanPham_HoaHongSanPhamThemMoi` | public | Thêm cấu hình HH cho SP/dịch vụ trong nhóm. | EF | — |
| 4 | Áp dụng HH SP toàn nhóm | Áp dụng toàn nhóm | POST `HoaHongSanPhamApDungToanBoNhom` | `NhomHoaHongCTVSanPham_HoaHongSanPhamApDungToanBoNhom` | public | Áp 1 mức HH cho toàn bộ SP/dịch vụ của nhóm (set hàng loạt). | EF | — |
| 5 | Tìm theo điều kiện | Lọc/lưới | POST `TimKiemTheoDieuKien` | `NhomHoaHongCTVSanPham_TimKiemTheoDieuKien` | public | Trả cấu hình HH theo SP của nhóm theo bộ lọc. | EF (+ SP cấu hình) | — |

## Quan hệ
- **Gọi sang:** EF; cấu hình tổng hợp được đọc lại qua SP `PRC_CommissionFee_GetCauHinh_NhomHoaHongCTV_SanPham` (ở NhomHoaHongCTVService).
- **Dữ liệu (entities):** `CauHinhHoaHongSanPhamCTV` (mỗi dòng = 1 SP/dịch vụ × % HH), thuộc `NhomHoaHongCTV`. `IdDoiTuong` + `LoaiDoiTuong` (SanPham/DichVu/Combo/The) xác định đối tượng.
- **Ripple/đồng bộ:** % HH theo SP được `CommissionFeeUpdateOrder` dùng (khớp `IdDoiTuong + LoaiDoiTuong` từng dòng đơn) và báo cáo `GetStats_SanPham_*` của CongTacVien.

## Lỗi đã biết / lưu ý kỹ thuật
- "Áp dụng toàn nhóm" và "Xóa toàn nhóm" là thao tác hàng loạt trên cả nhóm — không có xác nhận cấp dòng; cần guard ở UI để tránh ghi đè ngoài ý muốn.
- `LoaiDoiTuong` phải khớp đúng giá trị enum khi tra cứu ở pipeline tính HH (CommissionFeeUpdateOrder lọc theo `IdDoiTuong && LoaiDoiTuong`); lệch loại → dòng đơn không khớp cấu hình → HH = 0.
