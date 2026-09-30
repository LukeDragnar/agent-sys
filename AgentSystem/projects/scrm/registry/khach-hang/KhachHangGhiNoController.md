---
controller: KhachHangGhiNoController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang-detail > tab 'Công nợ' (cong-no): danh sách ghi nợ/hóa đơn nợ + tổng nợ/điểm + popup 'Thu nợ thủ công' (no-thu-cong)"
muc_dich: "Quản lý công nợ khách hàng: ghi nợ, trả nợ (thu nợ), thu nợ thủ công, số dư ban đầu, bù trừ công nợ, và xử lý công nợ theo luồng kế toán"
alias: [GhiNo, "Ghi nợ", "Công nợ KH", "Thu nợ khách hàng", CongNoKhachHang, "Debt"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangGhiNo, KhachHangGhiNoDetail, CustomerRollbackTransaction, KhachHang]
stored_procs: []
depends: [IKhachHangGhiNoService, IKhachHangGhiNoThuCongService, BaseDanhMucController, "Accounting (BuTruCongNo, luồng kế toán)", "Marketing (SyncCustomerToMarketingCampaign)"]
cross_module_in: ["Accounting/Order (private XuLyKhachHangTheoLuongKeToan, CheckHoaDonCoDuocRollback, ThemMoiSoDuBanDauKhachHang, XoaSoDuBanDauKhachHang)"]
cross_module_out: ["KhachHang (cập nhật SoTienNoDonHang / SoTienNoDonHangLogistic)", "Marketing (SyncCustomerToMarketingCampaign sau khi đổi công nợ)"]
---

# KhachHangGhiNo — Công nợ / Ghi nợ khách hàng

> Controller **có logic tiền** (ĐỦ/thorough). Phân biệt rõ **ghi nợ** (KieuLoai = No) và **trả nợ/thu nợ** (KieuLoai = Tra); mọi thao tác tiền đều có ràng buộc số tiền + đồng bộ sang `KhachHang` và Marketing.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy DS ghi nợ theo KH | Lưới tab "Công nợ" | `POST GetAllGhiNoByKhachHang` | KhachHangGhiNo_LayTheoDonVi | public | — | `GetAllGhiNoByKhachHang(filterDto)` → list ghi nợ theo filter/KH | EF | — |
| 2 | Tổng chi tiêu KH | Card tổng chi tiêu | `GET GetTongChiTieuByKh?idKhachHang` | KhachHangGhiNo_GetTongChiTieuByKh | public | — | `GetTongChiTieuByKh(idKhachHang)` | EF | — |
| 3 | Số nợ + điểm KH | Card "Số nợ / điểm" | `GET GetDiemVaSoNoKh?idKhachHang` | KhachHangGhiNo_GetDiemVaSoNoKh (nhiều role) | public | — | `GetDiemVaSoNoKh(idKhachHang)` | EF | — |
| 4 | DS hóa đơn KH đang nợ | Lưới hóa đơn nợ | `GET GetListHoaDonNoByKH?idKhachHang` | KhachHangGhiNo_GetListHoaDonNoByKH | public | — | `GetListHoaDonNoByKH(idKhachHang)` | EF | — |
| 5 | DS đơn hàng nợ (kèm tiền) | (chọn HĐ để trả nợ) | `POST LayDanhSachDonHangNoTheoKhachHang?idKhachHang` | — (`[SBAuthorize]`) | public | — | `LayDanhSachDonHangNoTheoKhachHang(idKhachHang, CurrentUser)` | EF | idKhachHang truyền qua query string dù verb POST |
| 6 | **Thu nợ / trả nợ** (theo HĐ) | Nút "Thu nợ" / lưu trả nợ | `POST ThemMoiAsync` | KhachHangGhiNo_ThemMoiAsync | public | — | Ghi nhận **trả nợ** (KieuLoai=Tra); **chặn nếu số tiền trả > tổng nợ**; phân bổ tiền trả lần lượt vào các khoản nợ (nhỏ trước) | EF (+ CustomerRollbackTransaction) | Tên `ThemMoiAsync` gây hiểu nhầm là "thêm nợ" nhưng thực chất là luồng **trả nợ** |
| 7 | Sinh mã khoản nợ | (mở popup thu nợ thủ công) | `GET GetMaKhoanNo` | KhachHangGhiNo_GetMaKhoanNo | public | — | Sinh mã khoản nợ duy nhất trong đơn vị | EF | — |
| 8 | **Thu nợ thủ công** (thêm nợ) | Popup "Thu nợ thủ công" (no-thu-cong) | `POST InsertNoThuCong` | KhachHangGhiNo_InsertNoThuCong | public | — | **Thêm khoản nợ thủ công** (KieuLoai=No); cộng số nợ vào `KhachHang.SoTienNoDonHang` (Order) hoặc `SoTienNoDonHangLogistic` (OrderLogistic); transaction + sync Marketing | EF (transaction) | — |
| 9 | Xóa ghi nợ | Nút "Xóa" (1 dòng nợ) | `POST XoaGhiNo/{id}` | KhachHangGhiNo_XoaGhiNo | public | — | `XoaGhiNo(id)` — xóa 1 bản ghi nợ | EF | — |

## Endpoint nội bộ (private / service-to-service)

- [internal] **Lấy ghi nợ private:** `POST GetDanhSachGhiNoByKhachHangPrivate` (`[PrivateSBAuthorize]`, validate ModelState) — service khác lấy DS ghi nợ của KH.
- [internal] **Rollback hóa đơn:** `POST CheckHoaDonCoDuocRollback` (`[PrivateSBAuthorize]`) — Order kiểm tra HĐ có được phép rollback theo công nợ.
- [internal] **Luồng kế toán:** `POST XuLyKhachHangTheoLuongKeToan` (`[PrivateSBAuthorize]`) — Accounting đẩy `List<DataDonHangKhKeToan>` để cập nhật công nợ KH theo chứng từ kế toán.
- [public] **Bù trừ công nợ (kế toán):** `GET GetCongNoPhaiThuAsync?maDoiTuong&ngayButru&taiKhoan` (`[SBAuthorize]`) + `POST GhiNhanBuTruCongNoKhachHangAsync` (body `GhiNhanCongNoNccAndKh`) — lấy công nợ phải thu theo ngày bút trừ rồi ghi nhận bù trừ (KH ⇄ NCC).
- [internal] **Số dư ban đầu:** `POST ThemMoiSoDuBanDauKhachHang` (body `ReqThemMoiSoDuBanDauKhachHang`) + `POST XoaSoDuBanDauKhachHang` (`[PrivateSBAuthorize]`) — khởi tạo/xóa số dư công nợ ban đầu của KH (thường từ luồng kế toán / import).

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)

> Blueprint cho trace-agent mô phỏng số tiền. Hai loại bản ghi: **No** (ghi nợ, làm tăng nợ) và **Tra** (trả nợ, làm giảm nợ). `tongNo = Σ(SoTien khi KieuLoai=No) − Σ(SoTien khi KieuLoai=Tra)`.

- **Thu nợ thủ công (`InsertNoThuCong`)** — thêm khoản **nợ mới**:
  - `IdKhachHang` rỗng → lỗi "Id khách hàng là bắt buộc."
  - `SoTien` null hoặc ≤ 0 → lỗi "Số tiền nợ không hợp lệ."
  - Mã khoản nợ: nếu client không gửi → sinh mã ngẫu nhiên loop tới khi không trùng trong đơn vị; nếu gửi mà đã tồn tại → lỗi "Mã khoản nợ đã tồn tại trong hệ thống."
  - KH không thuộc đơn vị user → lỗi "Không tìm thấy thông tin khách hàng!"
  - Ghi `KhachHangGhiNo` với `KieuLoai = No`, đồng thời cộng dồn nợ vào KH theo `KieuLoaiDonHang`: `Order` → `SoTienNoDonHang += SoTien`; `OrderLogistic` → `SoTienNoDonHangLogistic += SoTien`. Toàn bộ trong 1 transaction; lỗi → rollback. Thành công → `SyncCustomerToMarketingCampaign`.
- **Trả nợ (`ThemMoiAsync`)** — ghi nhận **trả nợ** (KieuLoai=Tra):
  - Qua `ValidateInsert` trước.
  - **Ngưỡng chặn:** nếu `SoTien (trả) > tongNo` → lỗi **"Số tiền trả vượt quá tổng nợ."** (không cho trả quá nợ).
  - Phân bổ tiền trả: gom các khoản nợ còn dư (theo `MaKhoanNo`, lọc khoản có `No − Tra > 0`), **sắp xếp số nợ tăng dần** (`OrderBy SoTienNo` = trả khoản nhỏ trước), trừ dần `SoTien` cho từng khoản tới khi hết tiền hoặc hết khoản nợ.
  - Tạo `CustomerRollbackTransaction` (IdTransaction) để rollback nếu lỗi giữa chừng.
- **Số dư ban đầu (`ThemMoiSoDuBanDauKhachHang` / `XoaSoDuBanDauKhachHang`)**: khởi tạo/xóa công nợ mở đầu cho KH (private, từ kế toán/import).

## Quan hệ
- **Gọi sang:** `IKhachHangGhiNoService` (nghiệp vụ chính: list/trả nợ/bù trừ/số dư) · `IKhachHangGhiNoThuCongService` (thu nợ thủ công).
- **Dữ liệu (entities):** `KhachHangGhiNo` (gốc, có cờ `KieuLoai` No/Tra) · `KhachHangGhiNoDetail` · `CustomerRollbackTransaction` (chống lỗi nửa chừng) · cập nhật `KhachHang.SoTienNoDonHang/SoTienNoDonHangLogistic`.
- **Ripple / đồng bộ:**
  - Mỗi thay đổi công nợ → cập nhật field tổng nợ trên `KhachHang`.
  - `SyncCustomerToMarketingCampaign` → đẩy thông tin KH sang Marketing sau khi đổi nợ.
  - Bù trừ công nợ liên quan **NhaCungCap** (KH đồng thời là NCC) và **Accounting** (ngày bút trừ).

## Luồng xuyên module
1. **Kế toán đẩy công nợ:** Accounting → `XuLyKhachHangTheoLuongKeToan(List<DataDonHangKhKeToan>)` → cập nhật ghi nợ + tổng nợ KH. Ripple: tab Công nợ FE và Marketing campaign cập nhật.
2. **Rollback đơn hàng:** Order gọi `CheckHoaDonCoDuocRollback` để biết HĐ có được rollback theo trạng thái công nợ trước khi hủy.
3. **Bù trừ KH ⇄ NCC:** `GetCongNoPhaiThuAsync` (lấy công nợ phải thu theo ngày bút trừ) → `GhiNhanBuTruCongNoKhachHangAsync` ghi nhận bù trừ giữa công nợ KH và NCC.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `KhachHangGhiNoController.cs:87-94` (`ThemMoiAsync`) — tên method nghe như "thêm nợ" nhưng là luồng **trả nợ** (service set KieuLoai=Tra, chặn trả > tổng nợ) → dễ hiểu nhầm khi triage.
- `KhachHangGhiNoService.cs:238` — ràng buộc cứng **"Số tiền trả vượt quá tổng nợ."**; trace cần mô phỏng case trả đúng/thiếu/thừa.
- `KhachHangGhiNoService.cs:285,294-306` — thuật toán phân bổ tiền trả **ưu tiên khoản nợ nhỏ nhất trước** (OrderBy SoTienNo); đổi thứ tự sort sẽ đổi khoản nào được tất toán.
- `KhachHangGhiNoThuCongService.cs:53-54` — `SoTien <= 0` bị chặn; `:89-98` cộng nợ vào field KH khác nhau theo `KieuLoaiDonHang` (Order vs OrderLogistic) → sai loại đơn sẽ ghi nhầm cột tổng nợ.
- `KhachHangGhiNoController.cs:108-110` (`LayDanhSachDonHangNoTheoKhachHang`) — POST nhưng nhận `idKhachHang` qua query, không qua body.
- `KhachHangGhiNoService.cs:664` — có một bản `InsertNoThuCong` **bị comment** trong service chính; bản đang dùng nằm ở `KhachHangGhiNoThuCongService.cs:30`.
