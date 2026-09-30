# Luồng — Quản lý tài chính (đối tác trả góp & domain)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Cấu hình trả góp: chỉ cần login; non-admin bắt buộc có chi nhánh; cần có nhà cung cấp (NCC) để gắn tài khoản; danh mục `DMDoiTacTraGop` (PTTT=8) đã có đối tác.
- Đổi domain (đơn vị): quyền DV-AD-TONG. Duyệt/từ chối: admin hệ thống HT-AD/HT-KT.

## Luồng dùng (đơn vị)
**A. Cấu hình đối tác trả góp:**
1. Mở `caidat/cauhinh-doitac-tragop`.
2. **Map nhanh**: `GetDSDoiTacTraGopCheckedByDonVi` (đối tác + trạng thái checked) → tích/bỏ tích → `MapCauHinhDoiTac` (insert/delete theo `ListIdChecked`) → xóa cache `DoiTacTraGop_ByDonVi_{donVi}`.
3. **Cấu hình đầy đủ**: `GetDanhSachCauHinhDoiTacTraGop` (lưới) → Thêm `ThemMoiCauHinhTraGopTaoNcc` (validate → insert, IdLoaiQuy=QuyTraGop) / Sửa `UpdateTraGopById` (validate isUpdate → update, **IdLoaiQuy=ViDienTu**) / Xóa `DeleteDonViTraGopById`.
4. Form thanh toán đọc `GetDSDoiTacTraGop` (ưu tiên chi nhánh user, fallback chi nhánh null).

**B. Yêu cầu đổi domain:**
1. Mở màn Domain đơn vị → `TimKiemTheoDieuKienDV` (lịch sử của đơn vị).
2. `ThemMoi` → tạo yêu cầu (trạng thái Chờ duyệt).
3. `CapNhatTrangThaiHuyBo/{id}` (GET) → hủy khi chưa duyệt.

## Luồng cấu hình (admin hệ thống — QuanTriArea, ngoài cẩm nang khách)
`LichSuThayDoiDomainController` bản SanboxArea (`api/QuanTriArea/LichSuThayDoiDomain`, HT-AD/HT-KT):
1. `TimKiemTheoDieuKienAdmin` (lịch sử toàn hệ thống).
2. `CapNhatTrangThaiDuyet/{id}` (GET) → Duyệt + áp domain mới cho đơn vị.
3. `CapNhatTrangThaiTuChoi` (POST, kèm lý do) → Từ chối, giữ domain cũ.

## Case biên
- **Validate cấu hình trả góp** (`ValidateCauHinhTraGop`):
  - Không phải admin tổng → `IdChiNhanh` bắt buộc (null/Empty → "Id chi nhánh là bắt buộc").
  - `IdNhaCungCap`, `IdDoiTacTraGop`, `MaTaiKhoan`, `TenTaiKhoan` bắt buộc.
  - Trùng (IdDonVi, IdChiNhanh, IdDoiTacTraGop, MaTaiKhoan) → "Đối tượng đã tồn tại trong hệ thống" (update loại trừ chính nó).
- **Map nhanh vs cấu hình đầy đủ:** `MapCauHinhDoiTac` insert chỉ IdDonVi + IdDoiTacTraGop (thiếu chi nhánh/tài khoản/NCC) → khác bản ghi cấu hình đầy đủ.
- **IdLoaiQuy lệch:** Thêm = QuyTraGop, Sửa = ViDienTu → cần product xác nhận đúng ý đồ.
- **Xem/Xóa nhận Guid qua body** (`GetDonViTraGopById`, `DeleteDonViTraGopById`) → FE phải gửi raw Guid trong body.
- **Hủy domain (GET)** `CapNhatTrangThaiHuyBo` → side-effect qua GET, chỉ hợp lệ khi chưa duyệt.
- **Duyệt domain (GET)** `CapNhatTrangThaiDuyet` → đổi state + áp domain qua GET (cần chống double-submit).
- **Domain: phía đơn vị KHÔNG có duyệt/từ chối** — đúng tách quyền (2 controller khác nhau).

## Ví dụ cụ thể có số
Đơn vị W có 2 chi nhánh, nhận trả góp qua 2 đối tác (Home Credit, FE Credit).
- Admin W map nhanh: tích Home Credit + FE Credit → `MapCauHinhDoiTac` insert 2 bản ghi (IdDonVi+IdDoiTacTraGop), xóa cache.
- Admin W thêm cấu hình đầy đủ cho Home Credit tại CN1: NCC = "Cty Home Credit", MaTaiKhoan = "HC-CN1", TenTaiKhoan = "Quỹ trả góp HC" → validate OK → insert (IdLoaiQuy=QuyTraGop).
- Thêm lại y hệt (CN1, Home Credit, HC-CN1) → trùng → "Đối tượng đã tồn tại trong hệ thống".
- Admin W mở màn Domain, tạo yêu cầu đổi domain từ `w.scrm.vn` → `wshop.vn` → trạng thái Chờ duyệt. Đổi ý → `CapNhatTrangThaiHuyBo` → Hủy bỏ.
- Lần khác tạo lại yêu cầu `wshop.vn` → admin hệ thống `CapNhatTrangThaiDuyet` → áp domain mới; lịch sử ghi 1 bản Duyệt.

## Hậu điều kiện
- Cấu hình trả góp: ghi DB `DonViDoiTacTraGop`, xóa cache → form thanh toán đọc cấu hình mới.
- Yêu cầu domain tạo/hủy: ghi `LichSuThayDoiDomain`; Duyệt → áp domain mới cho đơn vị; Từ chối/Hủy → giữ domain cũ. Không bắn message/event ở luồng trả góp.
