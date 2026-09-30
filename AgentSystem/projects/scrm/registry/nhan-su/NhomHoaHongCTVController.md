---
controller: NhomHoaHongCTVController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Nhóm hoa hồng (danh sách + thêm/sửa cấu hình nhóm; nút Sao chép / Áp dụng theo phân loại)"
muc_dich: "CRUD nhóm hoa hồng CTV (master cấu hình), bật/tắt sử dụng, sao chép cấu hình sang nhóm khác, áp dụng cấu hình theo phân loại, và import từ Excel. Mỗi CTV thuộc 1 nhóm → quyết định % hoa hồng."
alias: [NhomHoaHongCTV, "nhóm hoa hồng", "cấu hình hoa hồng CTV"]
auth: public
state_machine: [IsSuDung (bật/tắt)]
deprecated: false
entities: [NhomHoaHongCTV, CauHinhHoaHongSanPhamCTV, CauHinhHoaHongDoanhSoCTV, CongTacVien]
stored_procs: [PRC_CommissionFee_DeleteNhomHoaHong_ById]
depends: [PhanLoaiNhom, GemBox (Excel)]
cross_module_in: []
cross_module_out: [NhomHoaHongCTVSanPham, NhomHoaHongCTVDoanhSo, NhomHoaHongCTVChienDich (cấu hình con), CongTacVien (gán nhóm)]
---

# NhomHoaHongCTV — Quản lý nhóm hoa hồng cộng tác viên (master)

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới nhóm | Lưu | POST `ThemMoi` | `NhomHoaHongCTV_ThemMoi` (AD-TONG/CN/MCN) | public | Validate + insert nhóm (gắn phân loại, cờ IsSuDungHoaHongChienDich...). | EF | — |
| 2 | Cập nhật nhóm | Lưu | POST `CapNhat` | `NhomHoaHongCTV_CapNhat` | public | Update nhóm theo Id + đơn vị. | EF | — |
| 3 | Xóa nhóm | Xóa | POST `Xoa/{id}` | `NhomHoaHongCTV_Xoa` | public | Xóa nhóm + cấu hình con qua SP. | SP `PRC_CommissionFee_DeleteNhomHoaHong_ById` | — |
| 4 | Lấy theo Id | Mở edit | POST `LayTheoId/{id}` | `NhomHoaHongCTV_LayTheoId` | public | Trả chi tiết nhóm. | EF | — |
| 5 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `TimTheoDieuKien` | `NhomHoaHongCTV_TimTheoDieuKien` | public | Lọc nhóm (non-admin ép theo chi nhánh). | EF | — |
| 6 | Autocomplete nhóm | (combo) | POST `TimKiemAutoComplete` | `NhomHoaHongCTV_TimKiemAutoComplete` | public | Autocomplete. | EF | — |
| 7 | Bật/tắt sử dụng | Toggle IsSuDung | POST `CapNhatTrangThaiSuDung/{id}/{loaiTrangThai}` | `NhomHoaHongCTV_CapNhatTrangThaiSuDung` | public | Đổi trạng thái sử dụng theo `loaiTrangThai`. | EF | — |
| 8 | Sao chép cấu hình | Sao chép | POST `Copy` | `NhomHoaHongCTV_Copy` | public | `Copy(payload)` — nhân bản cấu hình nhóm sang nhóm/đích mới. | service | — |
| 9 | Áp dụng theo phân loại | Áp dụng | POST `Apply` | `NhomHoaHongCTV_Apply` | public | `Apply(payload)` — áp cấu hình nhóm theo phân loại nhóm. | service | — |
| 10 | Tải template import | Tải file mẫu | POST `DownloadFileTemplateImport` | `NhomHoaHongCTV_DownloadFileTemplateImport` | public | `ImportNhomHoaHongCTV.xlsx`. | GemBox | — |
| 11 | Import nhóm (Excel) | Import | POST `Import` (IFormFile) | `NhomHoaHongCTV_Import` | public | Đọc + ghi nhóm; dòng lỗi lưu theo id. | service Import | — |
| 12 | Tải dữ liệu import lỗi | Tải file lỗi | GET `DownloadDataWrong/{id}` | `NhomHoaHongCTV_DownloadDataWrong` | public | `DataImportNhomHoaHongCTVFail.xlsx`. | GemBox | GET tải file |

## Quan hệ
- **Gọi sang:** SP xóa nhóm (dọn cấu hình con), GemBox import/export.
- **Dữ liệu (entities):** `NhomHoaHongCTV` (master); cấu hình con: `CauHinhHoaHongSanPhamCTV`, `CauHinhHoaHongDoanhSoCTV`, cấu hình chiến dịch (qua NhomHoaHongCTVChienDich). Cờ `IsSuDungHoaHongChienDich` quyết định nhóm có dùng hoa hồng theo chiến dịch hay không.
- **Ripple/đồng bộ:** mỗi CTV gắn 1 nhóm → đổi cấu hình nhóm ảnh hưởng % hoa hồng tính ở `CommissionFeeUpdateOrder`. Xóa nhóm phải dọn cấu hình con (qua SP).

## Lỗi đã biết / lưu ý kỹ thuật
- `NhomHoaHongCTVController.cs:147` — `DownloadDataWrong` dùng HTTP GET.
- Xóa nhóm đi qua SP `PRC_CommissionFee_DeleteNhomHoaHong_ById` (không EF cascade) → nếu CTV còn gán nhóm bị xóa, cần kiểm tra ràng buộc trước khi xóa.
- Cờ `IsSuDungHoaHongChienDich=true` ở nhóm là điều kiện để `CommissionFeeUpdateOrder` sinh `DonHangChiTietChienDich`; tắt cờ → đơn không phát sinh hoa hồng chiến dịch dù chiến dịch có cấu hình.
