---
controller: GiaoHangTaiKhoanController
module: kho
service: WarehouseProduct
man_hinh_ui: "Giao vận > Quản lý tài khoản giao hàng (FE pages/delivery: delivery-account-list + delivery-account-detail) — list/CRUD tài khoản giao vận theo kho/chi nhánh; FE gọi qua BaseService (timTheoDieuKien/create/update/delete/getDetail). Ngoài ra dùng nội bộ bởi luồng đăng đơn (thu-kho-tac-nghiep, don-hang-giao-van)."
muc_dich: "Quản lý 'tài khoản giao hàng' = cấu hình map (đơn vị + chi nhánh + kho) ↔ tài khoản đối tác giao vận + hình thức giao hàng. Id của bản ghi này thay cho WarehouseId khi tra cứu chi tiết kết nối ở DeliveryConnect, nên là cầu nối cấu hình giữa kho và hệ giao vận."
alias: ["tài khoản giao hàng", "GiaoHangTaiKhoan", "tài khoản giao vận", "delivery account", "cấu hình tài khoản giao hàng", "tài khoản đối tác vận chuyển theo kho"]
auth: public
state_machine: []
deprecated: false
entities: [GiaoHangTaiKhoan]
stored_procs: []
depends:
  - "GiaoHangTaiKhoanService (IGiaoHangTaiKhoanService — TimTheoDieuKien/ThemMoi/Update/Delete/GetById/GetById_Private/GetThongTinTaiKhoanByKho)"
cross_module_in:
  - "OrderLogistic / DeliveryConnect (lấy GiaoHangTaiKhoan.Id thay WarehouseId khi tra chi tiết kết nối giao vận — qua TimTheoIdPrivate / TimTheoIdPrivate_V2)"
cross_module_out:
  - "DeliveryConnect (Id tài khoản giao hàng dùng làm khóa lấy cấu hình kết nối đối tác vận chuyển)"
  - "OrderLogistic (luồng đăng đơn / chọn phương thức giao hàng theo kho dùng GetThongTinTaiKhoanByKho)"
---

# GiaoHangTaiKhoanController — Quản lý tài khoản giao hàng (map kho ↔ tài khoản đối tác giao vận)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/list tài khoản giao hàng | "Tìm kiếm" → grid (delivery-account-list) | POST `api/GiaoHangTaiKhoan/TimTheoDieuKien` | `GiaoHangTaiKhoan_TimTheoDieuKien` (DV-AD-TONG/CN/MCN, DV-KT, DV-KT-TONG, DV-QLDV) | public | màn list | Lọc theo điều kiện trong đơn vị hiện tại + phân trang (FE gọi `timTheoDieuKien` của BaseService) | EF | - |
| 2 | Thêm mới tài khoản | "Thêm mới" (delivery-account-detail) | POST `api/GiaoHangTaiKhoan/ThemMoi` | `GiaoHangTaiKhoan_ThemMoi` (DV-AD-TONG/CN/MCN) | public | popup tạo mới | Validate ModelState (Ma ≤200, Ten ≤255, GiaoHangDoiTac* ≤200/400...) → tạo bản ghi map kho ↔ tài khoản giao vận; lỗi model → trả `MESS_Model_UnValidate`. FE gọi `create` → `ThemMoi` | EF | Quyền chặt hơn list: chỉ 3 role admin được tạo |
| 3 | Lấy chi tiết theo Id | (mở form sửa) | GET `api/GiaoHangTaiKhoan/TimTheoId/{id}` | `GiaoHangTaiKhoan_TimTheoId` (DV-AD-TONG/CN/MCN) | public | per-row | Trả chi tiết 1 tài khoản giao hàng. FE gọi `getDetail` → `TimTheoId/{id}` | EF | - |
| 4 | Cập nhật tài khoản | "Lưu" (delivery-account-detail) | POST `api/GiaoHangTaiKhoan/CapNhatThongTin` | `GiaoHangTaiKhoan_CapNhatThongTin` (DV-AD-TONG/CN/MCN) | public | popup sửa | Validate ModelState → cập nhật. FE gọi `update` → `CapNhatThongTin` | EF | - |
| 5 | Xóa tài khoản | menu "Xóa" (confirm) | POST `api/GiaoHangTaiKhoan/Xoa/{id}` | `GiaoHangTaiKhoan_Xoa` (DV-AD-TONG/CN/MCN) | public | per-row | Xóa bản ghi theo id. FE gọi `delete` → `Xoa/{id}` | EF | - |
| 6 | Lấy thông tin tài khoản theo kho | (luồng đăng đơn / chọn phương thức GH) | POST `api/GiaoHangTaiKhoan/GetThongTinTaiKhoanByKho?idKho=&maDoiTac=` | `[SBAuthorize]` (controller-level, mọi role có quyền GiaoHangTaiKhoan) | public | khi đăng đơn theo kho + đối tác | Trả tài khoản giao vận tương ứng `idKho` + `maDoiTac` (đối tác) trong đơn vị hiện tại — phục vụ chọn phương thức giao hàng | EF | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **OrderLogistic / DeliveryConnect** gọi `TimTheoIdPrivate/{id}` (GET) và `TimTheoIdPrivate_V2` (POST, `GiaoHangTaiKhoanFilter_Private`) → lấy thông tin tài khoản giao hàng theo Id để dựng/đăng đơn giao vận. Theo doc-comment entity: `Id` của bảng này được dùng THAY cho WarehouseId khi lấy chi tiết kết nối ở DeliveryConnect (GiaoHangTaiKhoan.cs:8-10).

## Quan hệ
- **Gọi sang:** chỉ `GiaoHangTaiKhoanService` (EF nội bộ WarehouseProduct).
- **Dữ liệu (entities):** `GiaoHangTaiKhoan` (IdDonVi, IdChiNhanh, IdKho, Ma, Ten, GiaoHangDoiTacMa/Ten, GiaoHangHinhThucGiaoHangMa/Ten, IsMacDinh). 1 chi nhánh có nhiều kho; mỗi bản ghi gắn 1 kho + 1 đối tác + 1 hình thức giao hàng.
- **Ripple / đồng bộ:** không publish event. Ảnh hưởng gián tiếp xuống giao vận: Id bản ghi là khóa tra cứu cấu hình kết nối đối tác ở DeliveryConnect; đổi/xóa tài khoản → ảnh hưởng đăng đơn giao vận của kho đó.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller `partial` + `[SBAuthorize()]` ở class-level (GiaoHangTaiKhoanController.cs:13-14). FE `giao-hang-tai-khoan.service.ts` còn gọi `GetKhoTheoQuanKho_Dropdown` (GET) và `GetListUserQuanKhoByKhoId` (POST) **không có trong file controller này** → khả năng nằm ở partial khác hoặc là endpoint FE stale; cần xác nhận partial bổ sung trước khi kết luận thiếu/thừa.
- `GiaoHangTaiKhoan.cs:8-10` — doc-comment xác nhận hợp đồng cross-service: `Id` thay `WarehouseId` ở DeliveryConnect; đừng coi đây là tài khoản giao vận độc lập — nó là lớp ánh xạ kho ↔ giao vận.
- `GetThongTinTaiKhoanByKho` nhận `maDoiTac` (string) → dữ liệu lệ thuộc mã đối tác giao vận đúng convention; sai mã → trả rỗng.
- Validate độ dài field nằm ở data-annotation entity (Ma 200 / Ten 255 / DoiTacTen 400...) — vượt giới hạn → ModelState invalid, trả `MESS_Model_UnValidate`.
