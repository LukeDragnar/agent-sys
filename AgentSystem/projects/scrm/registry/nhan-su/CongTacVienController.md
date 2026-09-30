---
controller: CongTacVienController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Cộng tác viên (danh sách + thêm/sửa); tab thống kê hoa hồng theo CTV (sản phẩm / doanh số / chiến dịch)"
muc_dich: "CRUD cộng tác viên (CTV / nhân viên giới thiệu), import từ Excel, và tra cứu thống kê hoa hồng theo từng CTV (sản phẩm, doanh số, chiến dịch, bản liên kết)."
alias: [CongTacVien, CTV, "cộng tác viên", "nhân viên giới thiệu", "affiliate"]
auth: mixed
state_machine: []
deprecated: false
entities: [CongTacVien, NhomHoaHongCTV, DonHang, DonHangChiTiet, DonHangChiTietChienDich]
stored_procs: [PRC_CommissionFee_GetStats_CTV, PRC_CommissionFee_GetStats_CTV_LienKet, PRC_CommissionFee_GetStats_CTV_ChienDich, PRC_CommissionFee_GetStat_DoanhSo_ByIdCTV, PRC_CommissionFee_GetStat_DoanhSo_ByIdCTV_LienKet]
depends: [NhomHoaHongCTV, GemBox (Excel import/export)]
cross_module_in: [Core/User (IdUser của nhân viên), Order (đẩy đơn gắn IdCongTacVien)]
cross_module_out: []
---

# CongTacVien — Quản lý cộng tác viên & thống kê hoa hồng theo CTV

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới CTV | Lưu | POST `ThemMoi` | `CongTacVien_ThemMoi` (AD-TONG/CN/MCN) | public | Validate + insert CTV (gắn nhóm hoa hồng, kiểu loại CTV/nhân viên). | EF | — |
| 2 | Cập nhật CTV | Lưu | POST `CapNhat` | `CongTacVien_CapNhat` | public | Update theo Id + đơn vị. | EF | — |
| 3 | Xóa CTV | Xóa | POST `Xoa/{id}` | `CongTacVien_Xoa` | public | Xóa CTV. | EF | — |
| 4 | Lấy theo Id | Mở edit | POST `LayTheoId/{id}` | `CongTacVien_LayTheoId` | public | Trả chi tiết CTV. | EF | — |
| 5 | Check tồn tại theo Id | — | POST `CheckById/{id}` | `[SBAuthorize()]` rỗng | mixed | Chỉ cần login; check CTV còn dùng được. | EF | Quyền lỏng (chỉ login) |
| 6 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `TimTheoDieuKien` | `CongTacVien_TimTheoDieuKien` | public | Lọc CTV (non-admin ép theo chi nhánh). | EF | — |
| 7 | Autocomplete CTV | (combo) | POST `TimKiemAutoComplete` | `[SBAuthorize()]` rỗng (key cũ comment) | mixed | Autocomplete; quyền đã bị nới về chỉ-login. | EF | Quyền key cũ bị comment, để rỗng |
| 8 | Thống kê hoa hồng sản phẩm theo CTV | Xem chi tiết HH sản phẩm | POST `GetStats_SanPham_ByCTV` | `CongTacVien_GetStats_SanPham_ByCTV` | public | Báo cáo HH sản phẩm của CTV. | SP `PRC_CommissionFee_GetStats_CTV` | — |
| 9 | Thống kê HH sản phẩm (liên kết) | — | POST `GetStats_SanPham_LienKet_ByCTV` | `CongTacVien_GetStats_SanPham_LienKet_ByCTV` | public | Báo cáo HH sản phẩm bản liên kết. | SP `PRC_CommissionFee_GetStats_CTV_LienKet` | — |
| 10 | Thống kê HH doanh số theo CTV | — | POST `GetStats_DoanhSo_ByCTV` | `CongTacVien_GetStats_DoanhSo_ByCTV` | public | Báo cáo HH theo bậc doanh số. | SP `PRC_CommissionFee_GetStat_DoanhSo_ByIdCTV` | — |
| 11 | Thống kê HH doanh số (liên kết) | — | POST `GetStats_DoanhSo_LienKet_ByCTV` | `CongTacVien_GetStats_DoanhSo_LienKet_ByCTV` | public | Bản liên kết. | SP `PRC_CommissionFee_GetStat_DoanhSo_ByIdCTV_LienKet` | — |
| 12 | Thống kê HH chiến dịch theo CTV | — | POST `GetStats_ChienDich_ByCTV` | `CongTacVien_GetStats_ChienDich_ByCTV` | public | Tổng hợp HH theo chiến dịch. | SP `PRC_CommissionFee_GetStats_CTV_ChienDich` | — |
| 13 | Chi tiết HH chiến dịch theo CTV | — | POST `GetStats_ChienDich_Detail_ByCTV` | `CongTacVien_GetStats_ChienDich_Detail_ByCTV` | public | Chi tiết từng dòng HH chiến dịch. | SP (ChienDich chi tiết) | — |
| 14 | Lấy KM theo CTV | — | POST `GetListKhuyenMaiTheoCTV` | `[SBAuthorize()]` rỗng | mixed | Trả cấu hình hoa hồng sản phẩm gắn CTV. | EF | Quyền lỏng |
| 15 | Tải template import | Tải file mẫu | POST `DownloadFileTemplateImport` | `CongTacVien_DownloadFileTemplateImport` (AD-TONG) | public | Xuất file Excel mẫu `ImportCongTacVien.xlsx`. | GemBox | — |
| 16 | Import CTV (Excel) | Import | POST `Import` (IFormFile) | `CongTacVien_Import` (AD-TONG) | public | Đọc file, validate, ghi CTV; dòng lỗi lưu lại theo id. | `ICongTacVienService.Import` | — |
| 17 | Tải dữ liệu import lỗi | Tải file lỗi | GET `DownloadDataWrong/{id}` | `CongTacVien_DownloadDataWrong` (AD-TONG) | public | Xuất `DataImportCongTacVienFail.xlsx`. | GemBox | GET cho thao tác tải |

## Quan hệ
- **Gọi sang:** SP báo cáo thống kê (5 PRC ở trên) qua SqlSugar; GemBox cho import/export Excel.
- **Dữ liệu (entities):** `CongTacVien` (CTV + nhân viên `KieuLoai=NhanVien`, gắn `IdNhomHoaHongCTV`, `IdUser`, `MaCTV`); báo cáo đọc `DonHang*` đã sinh bởi CommissionFeeUpdateOrder.
- **Ripple/đồng bộ:** không sinh side-effect cross-service; là điểm đọc cuối của pipeline hoa hồng.

## Lỗi đã biết / lưu ý kỹ thuật
- `CongTacVienController.cs:71,96,186` — `CheckById`, `TimKiemAutoComplete`, `GetListKhuyenMaiTheoCTV` dùng `[SBAuthorize()]` rỗng (chỉ cần login) — `TimKiemAutoComplete` còn để lại key cũ ở comment dòng 95.
- `CongTacVienController.cs:218` — `DownloadDataWrong` dùng HTTP GET (tải file theo id, không phân biệt theo body).
- Toàn bộ GetStats_* phụ thuộc dữ liệu do `CommissionFeeUpdateOrder` ghi; nếu đơn chưa được đẩy/đẩy lỗi thì báo cáo trống dù đơn đã bán.
