---
controller: ThuongDoanhSoController
module: nhan-su
service: Branch
man_hinh_ui: "Cấu hình > Thưởng doanh số (danh sách cấu hình + panel chọn NV/dịch vụ/sản phẩm/thẻ/combo + bảng bậc doanh số)"
muc_dich: "Cấu hình thưởng theo bậc doanh số: mỗi cấu hình gắn nhóm NV + nhóm đối tượng (dịch vụ/SP/thẻ/combo) + các mốc bậc (DoanhSoTu→DoanhSoDen → giá trị thưởng theo VND/%). Lưu là replace-all theo chi nhánh."
alias: [ThuongDoanhSo, "thưởng doanh số", "thưởng theo doanh số", "bậc thưởng doanh số", "cấu hình thưởng doanh số"]
auth: public
state_machine: []
deprecated: false
entities: [ThuongDoanhSo, ThuongDoanhSoChiTiet]
stored_procs: []
depends: [User (Core), DichVu/SanPham/TheDichVu/ComBoSpa (WarehouseProduct qua ClientHelper)]
cross_module_in: []
cross_module_out: [WarehouseProduct (GetDichVuByIds / GetListSanPhamByIds / GetTheDichVuByIds / GetComboByIds), Core/User (GetUserById)]
---

# ThuongDoanhSo — Cấu hình thưởng theo bậc doanh số

> Controller-level `[SBAuthorize]` (login-only mặc định) + kế thừa `SBBaseController<ThuongDoanhSo, ThuongDoanhSoDto>` → có sẵn CRUD base (`TimTheoId/LayTatCa/ThemMoi/CapNhatThongTin/Xoa`). Nghiệp vụ thật dùng 2 action override có key.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lưu (replace-all) cấu hình thưởng DS | Lưu | POST `ThemMoiAsync` | `ThuongDoanhSo_ThemMoiAsync` | public | Validate bậc → XÓA toàn bộ cấu hình của chi nhánh hiện tại → tạo mới list (transaction). | EF (transaction) | Replace-all: lưu = xóa hết cũ rồi ghi mới |
| 2 | Lấy danh sách cấu hình thưởng DS | Danh sách | POST `GetListThuongDoanhSo` | `ThuongDoanhSo_GetListThuongDoanhSo` | public | Trả list cấu hình + denormalize tên NV/dịch vụ/SP/thẻ/combo qua cross-service. | EF + ClientHelper | Cần Core+WarehouseProduct sống mới hiện đủ tên |
| 3 | CRUD base (kế thừa) | — | GET `TimTheoId/{id}`, `LayTatCa`, POST `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` | (chỉ `[SBAuthorize]` base, login-only) | mixed | Kế thừa SBBaseController; không dùng cho luồng cấu hình bậc. | EF | Base endpoint không key |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
Validate trong `InsertMuilpleAsync` (ThuongDoanhSoService.cs:225) — chạy TRƯỚC khi replace-all:
1. `ModelThuongDoanhSos == null` → lỗi "Dữ liệu không được phép null".
2. Mỗi cấu hình: `TenDoanhSo` rỗng → lỗi "Tên cấu hình tiền thưởng không được phép bỏ trống".
3. Mỗi bậc (`PointItems`): nếu `DoanhSoDen` / `DoanhSoTu` / `GiaTriThuong` null hoặc = 0 → lỗi "Dữ liệu cài đặt doanh số không hợp lệ".
4. `DoanhSoDen <= DoanhSoTu` → lỗi "Giá trị doanh thu từ không thể lớn hơn giá trị doanh thu đến" (yêu cầu khoảng tăng dần, không overlap về biên).
5. Nếu `DonViThuong == PhanTram` và `GiaTriThuong > 100` → lỗi "Giá trị thưởng không được > 100%".
6. Pass hết → trong transaction: RemoveRange toàn bộ `ThuongDoanhSo` + `ThuongDoanhSoChiTiet` cũ của (`IdDonVi`, `IdChiNhanh`) → tạo mới. Chi tiết tách theo panel: NhanVien / DichVu / SanPham / The / Combo (KieuLoaiObject) + PointItems (bậc).

## Luồng xuyên module
- Lưu cấu hình → chỉ ghi DB Branch (không phát event). Đọc danh sách (`GetThuongDoanhSoData`) gọi RA: Core `GetUserById`, WarehouseProduct `GetDichVuByIds/GetListSanPhamByIds/GetTheDichVuByIds/GetComboByIds` để hiển thị tên đối tượng. Cross-service lỗi → panel tương ứng để trống nhưng vẫn trả cấu hình.

## Quan hệ
- **Gọi sang:** Core/User, WarehouseProduct (lấy tên đối tượng — qua `IClientHelper` + `ServiceSettings`).
- **Dữ liệu (entities):** `ThuongDoanhSo` (1 cấu hình theo chi nhánh) + `ThuongDoanhSoChiTiet` (panel NV/đối tượng + các bậc doanh số).
- **Ripple/đồng bộ:** không phát event; cấu hình bậc là đầu vào tính thưởng doanh số cho NV.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuongDoanhSoService.cs:269-297` — Lưu là **replace-all theo chi nhánh**: xóa hết cấu hình cũ rồi ghi mới. Gửi list rỗng/thiếu = xóa sạch cấu hình chi nhánh.
- `ThuongDoanhSoService.cs:409` — `throw ex;` trong catch (mất stack trace gốc) — anti-pattern, nên `throw;`.
- `ThuongDoanhSoController.cs:15` — `[SBAuthorize]` controller-level + base SBBaseController phát sinh endpoint base login-only không key (audit cần lưu ý).
