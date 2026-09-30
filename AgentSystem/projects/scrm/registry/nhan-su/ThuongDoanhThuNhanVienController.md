---
controller: ThuongDoanhThuNhanVienController
module: nhan-su
service: Branch
man_hinh_ui: "Nhân sự > Hồ sơ nhân viên > tab Thưởng doanh thu (danh sách thưởng doanh thu của NV + biểu đồ thống kê doanh thu phân loại / theo 6 tháng)"
muc_dich: "Tra cứu thưởng doanh thu theo nhân viên + thống kê doanh thu phân loại theo loại đối tượng và group theo 6 tháng gần nhất."
alias: [ThuongDoanhThuNhanVien, "thưởng doanh thu", "thưởng doanh thu nhân viên", "thống kê doanh thu NV"]
auth: public
state_machine: []
deprecated: false
entities: [ThuongDoanhThuNhanVien]
stored_procs: []
depends: [SBBaseController<ThuongDoanhThuNhanVien,ThuongDoanhThuNhanVienDto>]
cross_module_in: []
cross_module_out: []
---

# ThuongDoanhThuNhanVien — Thưởng & thống kê doanh thu theo nhân viên

> Kế thừa `SBBaseController<ThuongDoanhThuNhanVien, ThuongDoanhThuNhanVienDto>` → có sẵn CRUD base login-only (`TimTheoId/LayTatCa/ThemMoi/CapNhatThongTin/Xoa`). 3 action dưới đây là phần tra cứu/thống kê có key.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thưởng doanh thu theo NV | Xem chi tiết | GET `GetThuongDoanhThuByNhanVien?idNhanVien` | `ThuongDoanhThuNhanVien_GetThuongDoanhThuByNhanVien` | public | Danh sách thưởng doanh thu của 1 NV (theo `CurrentUser`). `idNhanVien` nullable. | EF | — |
| 2 | Thống kê doanh thu phân loại | (biểu đồ) | POST `ThongKeDoanhThuPhanLoai` | `ThuongDoanhThuNhanVien_ThongKeDoanhThuPhanLoai` | public | Thống kê doanh thu của NV phân loại theo loại đối tượng. | EF | — |
| 3 | Thống kê doanh thu 6 tháng | (biểu đồ) | POST `ThongKeDoanhThu` | `ThuongDoanhThuNhanVien_ThongKeDoanhThu` | public | Group doanh thu NV theo 6 tháng gần nhất. | EF | — |
| 4 | CRUD base (kế thừa) | — | GET `TimTheoId/{id}`, `LayTatCa`, POST `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` | (base `[SBAuthorize]`, login-only) | mixed | Kế thừa SBBaseController; không phải luồng nghiệp vụ chính. | EF | Base endpoint không key |

## Quan hệ
- **Gọi sang:** không (EF nội bộ Branch).
- **Dữ liệu (entities):** `ThuongDoanhThuNhanVien` (bản ghi thưởng doanh thu của NV; thống kê group theo đối tượng + thời gian).
- **Ripple/đồng bộ:** không phát event — controller đọc/thống kê.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuongDoanhThuNhanVienController.cs:14` — kế thừa SBBaseController → 5 endpoint base login-only không key. Tra cứu/thống kê dùng 3 action có key.
- `ThuongDoanhThuNhanVienController.cs:24` — `idNhanVien` nullable; service tự xử fallback theo `CurrentUser` khi null.
