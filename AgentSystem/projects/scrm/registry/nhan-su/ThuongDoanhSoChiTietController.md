---
controller: ThuongDoanhSoChiTietController
module: nhan-su
service: Branch
man_hinh_ui: "nội bộ — không có UI riêng (chi tiết bậc/đối tượng của cấu hình thưởng doanh số, quản lý gián tiếp qua màn ThuongDoanhSo)"
muc_dich: "Controller CRUD trống cho entity ThuongDoanhSoChiTiet — chỉ kế thừa SBBaseController, không khai báo action riêng. Chi tiết thưởng doanh số thực tế ghi/đọc qua ThuongDoanhSoController."
alias: [ThuongDoanhSoChiTiet, "chi tiết thưởng doanh số", "bậc thưởng doanh số chi tiết"]
auth: mixed
state_machine: []
deprecated: false
entities: [ThuongDoanhSoChiTiet]
stored_procs: []
depends: [SBBaseController<ThuongDoanhSoChiTiet,ThuongDoanhSoChiTietDto>, ThuongDoanhSo]
cross_module_in: [ThuongDoanhSo (ghi/đọc chi tiết bậc + đối tượng)]
cross_module_out: []
---

# ThuongDoanhSoChiTiet — Chi tiết bậc thưởng doanh số (CRUD base trống)

> Controller chỉ kế thừa `SBBaseController<ThuongDoanhSoChiTiet, ThuongDoanhSoChiTietDto>` (22 dòng, không action riêng). Toàn bộ endpoint đến từ base; controller-level `[SBAuthorize]` của base → login-only, KHÔNG có key phân quyền. Dữ liệu chi tiết thật được ghi/đọc gián tiếp qua `ThuongDoanhSoController` (replace-all).

## Hành động (kế thừa từ SBBaseController)
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lấy theo Id | — | GET `TimTheoId/{id:Guid}` | (base `[SBAuthorize]`) | mixed | Trả 1 chi tiết theo id (map DTO). | EF | login-only, không key |
| 2 | Lấy tất cả | — | GET `LayTatCa` | (base `[SBAuthorize]`) | mixed | Trả toàn bộ (không lọc đơn vị ở base). | EF | login-only, không lọc tenant |
| 3 | Thêm mới | — | POST `ThemMoi` | (base `[SBAuthorize]`) | mixed | Insert 1 chi tiết. | EF | login-only, không key |
| 4 | Cập nhật | — | POST `CapNhatThongTin` | (base `[SBAuthorize]`) | mixed | Update theo Id (check tồn tại). | EF | login-only, không key |
| 5 | Xóa | — | POST `Xoa/{id:Guid}` | (base `[SBAuthorize]`) | mixed | Xóa theo id. | EF | login-only, không key |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** `ThuongDoanhSoChiTiet` (con của `ThuongDoanhSo`: panel NV/dịch vụ/SP/thẻ/combo + bậc DoanhSoTu/DoanhSoDen/GiaTriThuong/DonViThuong).
- **Ripple/đồng bộ:** ghi/đọc thực tế qua `ThuongDoanhSoController.ThemMoiAsync` (replace-all theo chi nhánh).

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuongDoanhSoChiTietController.cs:11` — controller trống chỉ có CRUD base login-only, KHÔNG key phân quyền và `LayTatCa` không lọc tenant ở base → tránh expose nếu route bật. Nghiệp vụ dùng `ThuongDoanhSoController`.
