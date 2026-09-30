---
controller: LuongNhanVienController
module: nhan-su
service: Branch
man_hinh_ui: "Lương > Bảng lương nhân viên (danh sách bảng lương, thêm/xóa bảng, chi tiết lương theo NV, tạo lương tự động, duyệt/chuyển trạng thái bảng lương)"
muc_dich: "Quản lý bảng lương nhân viên: tìm/thêm/xóa bảng lương, xem & lưu chi tiết lương theo từng NV, tạo lương chi tiết tự động cho cả bảng, và cập nhật trạng thái bảng lương (nháp/duyệt/chốt)."
alias: [LuongNhanVien, "bảng lương", "lương nhân viên", "tính lương", "bảng lương NV"]
auth: public
state_machine: [trạng thái bảng lương theo type (CapNhatTrangThaiBangLuong)]
deprecated: false
entities: [LuongNhanVien, LuongChiTietNhanVien]
stored_procs: []
depends: [GioLamViecNhanVien (công đã duyệt), CaiDatLuongCoBan, NhanVien/User (Core)]
cross_module_in: []
cross_module_out: []
---

# LuongNhanVien — Bảng lương nhân viên

> Không có `[SBAuthorize]` controller-level; mỗi action khai báo key riêng (đầy đủ, không bị comment). Mỗi bảng lương = 1 kỳ; chi tiết lương tách theo từng NV. Có trạng thái bảng lương đổi qua `CapNhatTrangThaiBangLuong(type)`.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm bảng lương theo điều kiện | Danh sách | POST `TimTheoDieuKien` | `LuongNhanVien_TimTheoDieuKien` | public | — | Trả danh sách bảng lương theo bộ lọc (kỳ/chi nhánh). | EF | — |
| 2 | Thêm bảng lương | Thêm mới | POST `ThemMoi` | `LuongNhanVien_ThemMoi` | public | — | Insert 1 bảng lương mới. | EF | — |
| 3 | Xóa bảng lương | Xóa | POST `Xoa/{id}` | `LuongNhanVien_Xoa` | public | — | Xóa bảng lương theo id. | EF | — |
| 4 | Chi tiết lương (theo id) | Chi tiết | GET `GetChiTietLuong?id=` | `LuongNhanVien_GetChiTietLuong` | public | — | Trả chi tiết lương theo id (kèm CurrentUser context). | EF | — |
| 5 | Chi tiết bảng lương | Mở bảng | GET `GetDetailBangLuong?id=` | `LuongNhanVien_GetDetailBangLuong` | public | — | Trả chi tiết toàn bảng lương + danh sách NV. | EF | — |
| 6 | Lấy lương theo 1 NV | (mở dòng NV) | GET `LayThongTinLuongTheoNv?idNhanVien=&idBangLuong=` | `LuongNhanVien_LayThongTinLuongTheoNv` | public | — | Trả chi tiết lương 1 NV trong bảng. | EF | — |
| 7 | Lưu lương theo NV | Lưu | POST `SaveLuongTheoNhanVien` | `LuongNhanVien_SaveLuongTheoNhanVien` | public | — | Lưu/cập nhật chi tiết lương 1 NV trong bảng. | EF | — |
| 8 | Tạo lương chi tiết tự động | Tạo tự động | GET `TaoLuongChiTietNhanVienTuDong?idBangLuong=` | `LuongNhanVien_TaoLuongChiTietNhanVienTuDong` | public | — | Sinh chi tiết lương tự động cho toàn bộ NV của bảng (từ cài đặt lương + công). | EF | — |
| 9 | Đổi trạng thái bảng lương | Duyệt/Chốt | GET `CapNhatTrangThaiBangLuong?idBangLuong=&type=` | `LuongNhanVien_CapNhatTrangThaiBangLuong` | public | theo `type` truyền vào | Chuyển bảng lương sang trạng thái tương ứng `type`. | EF | type là int — mapping trạng thái ở service |

## Quan hệ
- **Gọi sang:** Core/User (NV), nguồn công từ `GioLamViecNhanVien` + cài đặt từ `CaiDatLuongCoBan` (qua service tạo lương tự động).
- **Dữ liệu (entities):** `LuongNhanVien` (bảng lương theo kỳ) + `LuongChiTietNhanVien` (chi tiết từng NV).
- **Ripple/đồng bộ:** không phát event; bảng lương chốt là kết quả cuối của chuỗi chấm công → lương.

## Luồng xuyên module
- Tạo bảng lương → `TaoLuongChiTietNhanVienTuDong` lấy cài đặt lương cơ bản (CaiDatLuongCoBan) + công đã duyệt (GioLamViecNhanVien) → sinh chi tiết từng NV → `SaveLuongTheoNhanVien` chỉnh tay → `CapNhatTrangThaiBangLuong` để chốt.

## Lỗi đã biết / lưu ý kỹ thuật
- `LuongNhanVienController.cs:89` — `CapNhatTrangThaiBangLuong` nhận `idBangLuong`/`type` nullable; mapping trạng thái nằm ở service, controller không kiểm tra null trước khi gọi.
