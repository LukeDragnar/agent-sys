---
controller: TheDichVuKhachHangController
module: khach-hang
service: WarehouseProduct
man_hinh_ui: "WarehouseProduct/thedichvu-khachhang (Thẻ dịch vụ của khách — list + thao tác hàng loạt) · sub: thedichvu-khachhang-edit (thêm/sửa) · -napthe (nạp tiền trực tiếp) · -giahan (gia hạn) · -lichsu (lịch sử dùng thẻ). Còn nhúng làm tab 'Thẻ dịch vụ' trong Customer/quanly-khachhang-detail (KH360)"
muc_dich: "Quản lý thẻ dịch vụ cấp cho khách (thẻ số lần / thẻ số tiền): cấp/sửa/xóa, khóa-mở-thu hồi, gia hạn, nạp tiền trực tiếp; cung cấp số dư thẻ (buổi/tiền) cho Order trừ khi thanh toán"
alias: [TheDichVuKhachHang, "Thẻ dịch vụ khách hàng", "Thẻ khách", "Thẻ buổi", "Thẻ tiền", "Voucher dịch vụ", TheKhachHang]
auth: mixed
state_machine: [SuDung, Khoa, ThuHoi, HetHan, HoanThanh]
deprecated: false
entities: [TheDichVuKhachHang, TheDichVuKhachHangLichSuGiaoDich, KhachHangLichSuTuongTac]
stored_procs: []
depends:
  - ITheDichVuKhachHangService
  - TheDichVu (thẻ gốc — định nghĩa số lần/giá trị/thời hạn)
  - Order (DonHangPtThanhToan/CreateWithNoOrder — ghi dòng tiền khi nạp)
  - KhachHangLichSuTuongTac (log tương tác KH)
  - BaseController (userLogin scope DonVi/ChiNhanh)
cross_module_in:
  - Order (LayTatCaTheLan/LayTatCaTheTien/LayTatCaThe + ChiTietTheoIds — thu ngân/đơn online lấy số dư thẻ để trừ buổi/tiền)
  - Customer (KH360 detail tab 'Thẻ dịch vụ' gọi LayTheKhachHangPhanTrang/LayTatCaThePt)
  - "GetListTheDVByIdKhachHang_Private (cross-service lấy thẻ theo IdKhachHang)"
cross_module_out:
  - Order (NapTheTrucTiep → POST OrderService/DonHangPtThanhToan/CreateWithNoOrder, transaction)
  - KhachHangLichSuTuongTac (mỗi thao tác lock/unlock/thu hồi/gia hạn/nạp ghi 1 log tương tác)
  - TheDichVuKhachHangLichSuGiaoDich (lịch sử giao dịch nạp/trừ thẻ)
---

# TheDichVuKhachHang — Thẻ dịch vụ của khách hàng

> Thẻ cấp cho khách theo định nghĩa `TheDichVu`. 2 loại chính: **Thẻ số lần** (`LoaiThe=1`, trừ buổi `SoLanConLai`) và **Thẻ số tiền** (`LoaiThe=2`, trừ tiền `SoTienConLai`, có thể kèm tiền khuyến mãi). Controller-level `[SBAuthorize()]` + nhiều endpoint `[PrivateSBAuthorize]`/`[AllowAnonymous]` → auth `mixed`.
> Mọi truy vấn scope theo `userLogin.DonViId`; admin chi nhánh (không phải admin tổng) bị siết thêm `IdChiNhanh`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | List thẻ KH (phân trang) | Lưới thẻ dịch vụ KH | `POST LayTheKhachHangPhanTrang` | TheDichVuKhachHang_LayTheKhachHangPhanTrang (rộng: AD/QLDV/CN/NV/KT/SALE/MKT…) | public | tab Thẻ dịch vụ (KH360) / màn list | ModelState invalid → `MESS_Model_UnValidate`; trả danh sách thẻ theo IdKhachHang | EF | — |
| 2 | Tìm thẻ theo điều kiện | Bộ lọc list | `POST TimTheoDieuKien` | TheDichVuKhachHang_TimTheoDieuKien (DV-AD-*, DV-QLDV) | public | màn list | Lọc theo `TrangThai` (SuDung/Khoa/ThuHoi/HoanThanh/HetHan) + ảo `HetHanSau1Tuan`/`HetHanSau1Thang` (NgayHetHan trong 7 ngày/1 tháng & còn SuDung) | EF | Trạng thái "hết hạn sau" tính theo `DateTime.Now`, không phải state lưu DB |
| 3 | Lấy chi tiết thẻ | Mở thẻ | `GET TimTheoId/{id}` · `GET ChiTietTheoId/{id}` | TheDichVuKhachHang_TimTheoId / _ChiTietTheoId (ChiTiet chỉ AD-TONG/CN/MCN) | public | xem/sửa | NotFound → "Không tìm thấy thẻ!" | EF | 2 endpoint chi tiết quyền lệch nhau |
| 4 | Thêm mới thẻ cho KH | Nút "Thêm mới" (edit) | `POST ThemMoi` | TheDichVuKhachHang_ThemMoi (DV-AD-*, DV-QLDV) | public | — | Validate KH/ngày cấp/thẻ gốc; số đã-dùng-khởi-tạo (buổi/tiền/KM) không được > tổng; khởi tạo `SoLanConLai`/`SoTienConLai` theo `LoaiThe`, `TrangThai=SuDung` | EF | — |
| 5 | Cập nhật thẻ | Nút "Lưu" (edit) | `POST CapNhat` | TheDichVuKhachHang_CapNhat (DV-AD-*, DV-QLDV) | public | sửa | Validate KH/ngày hết hạn/mã thẻ; mã trùng → "Mã thẻ đã tồn tại!"; tự set `HetHan` nếu NgayHetHan<nay, ngược lại `SuDung` | EF | — |
| 6 | Xóa thẻ | Nút "Xóa" | `GET Xoa/{id}` | TheDichVuKhachHang_Xoa (DV-AD-*, DV-QLDV) | public | — | NotFound → "Không tìm thấy thẻ cần xóa"; xóa + ghi log tương tác | EF | Dùng **HTTP GET** cho hành động xóa (không idempotent) |
| 7 | Khóa thẻ | Menu "Khóa thẻ" | `GET Lock/{id}` | TheDichVuKhachHang_Lock | public | thẻ đang SuDung | Đã Khoa → "Voucher này đã được khóa"; đã ThuHoi → "...đã bị thu hồi"; set `TrangThai=Khoa`, log `KhoaThe` | EF | — |
| 8 | Mở khóa thẻ | Menu "Mở khóa thẻ" | `GET UnLock/{id}` | TheDichVuKhachHang_UnLock | public | thẻ đang Khoa | Không Khoa → "Voucher này đang không bị khóa"; mở → nếu NgayHetHan<nay set `HetHan`, else `SuDung`; log `MoThe` | EF | — |
| 9 | Thu hồi thẻ | Menu "Thu hồi thẻ" | `GET ThuHoi/{id}` | TheDichVuKhachHang_ThuHoi | public | — | Đã ThuHoi → lỗi; set `TrangThai=ThuHoi`, log `ThuHoiThe` (one-way, không có un-thu-hồi) | EF | — |
| 10 | Thao tác hàng loạt | Menu "Thu hồi/Khóa/Mở khóa/Xóa thẻ" (chọn nhiều) | `POST ThuHoiMulti` · `LockMulti` · `UnLockMulti` · `XoaMulti` | TheDichVuKhachHang_ThuHoiMulti / _LockMulti / _UnLockMulti / _XoaMulti | public | tick nhiều dòng | Count Ids ≠ count tìm thấy → "Không tìm thấy thẻ dịch vụ"; áp trạng thái cho từng thẻ + log từng thẻ. LockMulti **bỏ qua** thẻ đã ThuHoi | EF | — |
| 11 | Gia hạn (hàng loạt) | Nút "Gia hạn" | `POST GiaHanMulti` | TheDichVuKhachHang_GiaHanMulti | public | tick nhiều dòng | Set `NgayHetHan` = 23:59:59 ngày chọn; nếu ngày<nay → thẻ SuDung chuyển `HetHan`, ngược lại HetHan→SuDung; log `GiaHanThe` (Old/NewValue) chỉ khi đổi ngày | EF | — |
| 12 | Nạp tiền trực tiếp | Nút "Nạp thẻ" (napthe) | `POST NapTheTrucTiep` | TheDichVuKhachHang_NapTheTrucTiep | public | thẻ tiền, SuDung | Thẻ ThuHoi/Khoa/HetHan → chặn; `SoTienConLai += SoTienNap+SoTienThuong`; **transaction**: update thẻ + insert lịch sử giao dịch + POST Order `DonHangPtThanhToan/CreateWithNoOrder`; thanh toán fail → rollback | EF + HTTP Order | Phụ thuộc Order online; lỗi gọi → rollback toàn bộ |
| 13 | Tất cả thẻ theo loại | (load số dư cho thu ngân) | `GET LayTatCaTheLan/{id}` · `GET LayTatCaTheTien/{id}` · `GET LayTatCaThe/{id}` · `POST LayTatCaThePt` | TheDichVuKhachHang_LayTatCaTheLan / _LayTatCaTheTien / _LayTatCaThe / _LayTatCaThePt (gồm DV-KT cho thẻ tiền) | public | thanh toán đơn | Trả thẻ còn dùng được của KH (buổi/tiền) | EF | `LayTatCaTheLan` khai báo `[HttpPost]` nhưng chỉ nhận `{id}` route (không body) |
| 14 | Lịch sử dùng thẻ | Tab "Lịch sử" (lichsu) | (sang controller) `POST api/TheDichVuKhachHangLichSu/TimTheoDieuKien` | (xem TheDichVuKhachHangLichSuController) | public | tab lịch sử | FE service gọi sang controller lịch sử riêng để xem trừ buổi/tiền theo thời gian | EF | Lịch sử dùng thẻ **ở controller khác** — node này chỉ trỏ tới |

## Endpoint nội bộ (private / service-to-service)

- `[internal]` **Customer/cross-service lấy thẻ theo KH:** `GET|POST GetListTheDVByIdKhachHang_Private` (`PrivateSBAuthorize`) — phiên bản GET fix `PageSize=100`.
- `[internal]` **Order — đơn online lấy chi tiết nhiều thẻ:** `POST ChiTietTheoIds` (`PrivateSBAuthorize`, region "Đơn hàng online thanh toán") — lấy số dư các thẻ để trừ khi thanh toán.
- `[internal]` **Lấy thẻ tiền (private):** `GET LayTatCaTheTien_Private/{id}` (`PrivateSBAuthorize`; mã quyền gốc bị comment).
- `[internal]` **Chi tiết theo id (cross-service):** `POST GetDetailByIdPrivate` (`PrivateSBAuthorize`).
- `[internal]` **Import KH:** `POST CreateFromImportKH` (`PrivateSBAuthorize`) — tạo hàng loạt thẻ khi import khách hàng.
- `[internal]` **Tạo thẻ tiền ảo:** `POST TaoTheTienAoKhachHang` (`PrivateSBAuthorize`).
- `[anonymous]` **Thẻ lịch (đặt lịch nâng cao):** `POST TaoTheLichKhachHang` · `GET LayTheKhachHangTheoIdThe_Anonymous` (`AllowAnonymous`) — luồng đặt lịch khách tự thao tác.
- `[disabled]` `TinhToanTheCuaKhach` — toàn bộ method + route đã **comment** (không tồn tại runtime).

## Quy tắc nghiệp vụ (gating / trạng thái thẻ)

State `TrangThai` (enum `EnumTheDichVu_TrangThai`): `SuDung` (sẵn sàng) · `Khoa` · `ThuHoi` · `HetHan` · `HoanThanh`. Entity comment cũ ghi 0=thu hồi,1=khóa,2=sẵn sàng (giá trị số map qua enum, không hardcode trong controller).

1. **Khóa:** chỉ thẻ chưa Khoa & chưa ThuHoi → `Khoa`. ThuHoi là trạng thái cuối, không khóa được.
2. **Mở khóa:** chỉ từ `Khoa`. Mở ra → re-evaluate hạn: `NgayHetHan < hôm nay` ⇒ `HetHan`, ngược lại `SuDung`.
3. **Thu hồi:** từ trạng thái bất kỳ (trừ đã ThuHoi) → `ThuHoi`. **Một chiều** — không có nghiệp vụ phục hồi.
4. **Gia hạn:** đổi `NgayHetHan`; ngày mới ở quá khứ ⇒ SuDung→HetHan; ngày mới tương lai ⇒ HetHan→SuDung. Thẻ Khoa/ThuHoi giữ nguyên trạng thái, chỉ đổi ngày.
5. **Nạp tiền trực tiếp:** chỉ thẻ **không** ở ThuHoi/Khoa/HetHan; cộng `SoTienNap + SoTienThuong` vào `SoTienConLai`; bắt buộc đồng bộ dòng tiền sang Order (nếu fail → rollback, thẻ không đổi).
6. **Khởi tạo (ThemMoi):** số đã-dùng-ban-đầu (buổi/tiền/tiền KM) phải ≤ tổng tương ứng của thẻ gốc; `SoLanConLai`/`SoTienConLai` tính theo `LoaiThe` (số lần vs số tiền).

## Quan hệ
- **Gọi sang:** `ITheDichVuKhachHangService` (toàn bộ nghiệp vụ) · Order `DonHangPtThanhToan/CreateWithNoOrder` (chỉ trong NapTheTrucTiep).
- **Dữ liệu (entities):** `TheDichVuKhachHang` (gốc, số dư buổi/tiền + trạng thái) · `TheDichVuKhachHangLichSuGiaoDich` (lịch sử nạp/giao dịch) · `KhachHangLichSuTuongTac` (log tương tác KH cho mọi thao tác).
- **Ripple / đồng bộ:**
  - Mọi thao tác khóa/mở/thu hồi/gia hạn/nạp → ghi 1 `KhachHangLichSuTuongTac` (hiện trong dòng thời gian KH360).
  - Nạp tiền → ghi `TheDichVuKhachHangLichSuGiaoDich` + tạo dòng tiền bên Order (transaction xuyên service).
  - Trừ buổi/tiền thực tế xảy ra ở **Order** (thu ngân/đơn online) gọi `LayTatCaThe*`/`ChiTietTheoIds`; trừ ghi vào `TheDichVuKhachHangLichSu` (controller lịch sử riêng).

## Luồng xuyên module
1. **Trừ thẻ khi thanh toán (Order):** Order thu ngân/đơn online → gọi `LayTatCaTheLan`/`LayTatCaTheTien`/`ChiTietTheoIds` lấy số dư → trừ `SoLanConLai`/`SoTienConLai` → ghi `TheDichVuKhachHangLichSu`. (Logic trừ nằm ở phía Order, không ở controller này.)
2. **Nạp tiền trực tiếp:** user nạp ở napthe → cộng số dư + insert lịch sử giao dịch + POST Order tạo dòng tiền (`CreateWithNoOrder`) trong 1 transaction → fail bất kỳ bước → rollback toàn bộ.
3. **Import khách hàng:** Customer import KH → cross-service `CreateFromImportKH` tạo thẻ theo file; rollback bằng `RollbackFromImportKH` nếu import lỗi.
4. **KH360 / cross-service đọc thẻ:** Customer detail (tab Thẻ dịch vụ) hoặc service khác gọi `GetListTheDVByIdKhachHang_Private` để hiển thị danh sách thẻ của khách.

## Lỗi đã biết / lưu ý kỹ thuật
- `TheDichVuKhachHangController.cs:57` (`Xoa`) & `:115/:124/:132` (`Lock/UnLock/ThuHoi`) — hành động thay đổi state/xóa dùng **HTTP GET** (không idempotent, dễ trigger qua link/prefetch).
- `TheDichVuKhachHangController.cs:88-94` (`LayTatCaTheLan`) — gắn `[HttpPost("LayTatCaTheLan/{id:Guid}")]` nhưng tham số chỉ là `{id}` route, không có body → POST thừa.
- `TheDichVuKhachHangController.cs:96-103` (`LayTatCaTheTien_Private`) — mã quyền `[SBAuthorize(...)]` bị **comment**, để trống chỉ còn `[PrivateSBAuthorize]`.
- `TheDichVuKhachHangController.cs:242-248` (`TinhToanTheCuaKhach`) — method + route **comment toàn bộ** (không runtime).
- `TheDichVuKhachHangService.cs:1271-1276` (`NapTheTrucTiep`) — chặn nạp khi thẻ ThuHoi/Khoa/HetHan; thành công phụ thuộc **Order online** (`CreateWithNoOrder`), thanh toán fail → rollback (số dư không tăng).
- `TheDichVuKhachHangService.cs:728-742` (`TimTheoDieuKien`) — trạng thái lọc `HetHanSau1Tuan`/`HetHanSau1Thang` là **ảo theo `DateTime.Now`** (không phải state DB), chỉ áp cho thẻ còn `SuDung`.
- `TheDichVuKhachHangService.cs:1099-1101` (`LockMulti`) — vòng lặp **bỏ qua** thẻ đang ThuHoi (không báo lỗi, lock im lặng phần còn lại).
- Lịch sử **dùng** thẻ (trừ buổi/tiền) nằm ở `TheDichVuKhachHangLichSuController` riêng — FE service `LichSuDungThe()` trỏ sang `api/TheDichVuKhachHangLichSu/TimTheoDieuKien`.
