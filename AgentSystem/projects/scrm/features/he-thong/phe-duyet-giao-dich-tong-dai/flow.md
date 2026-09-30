# Luồng — Phê duyệt giao dịch tổng đài (super-admin)

## Tiền điều kiện
- Người dùng đăng nhập với role hệ thống **HT-AD** hoặc **HT-KT** (controller trong `SanboxArea`, `TI_HeThong`).
- Đơn vị đã phát sinh **giao dịch nạp tiền tổng đài** (GiaoDichTongDai) ở trạng thái **chờ thanh toán** (`GiaoDich_ChoThanhToan`) từ luồng nạp tiền của đơn vị.
- Tổng đài IP đơn vị (TongDaiIpDonVi) tồn tại để cộng số dư khi duyệt.

## Luồng dùng (duyệt — super-admin)
1. Super-admin mở **Quản trị hệ thống → Giao dịch tổng đài IP đơn vị**.
2. Chọn đơn vị → gọi `GetLichSuThanhToanSA` lấy **lịch sử giao dịch** nạp tiền theo `idDonVi`.
3. Với giao dịch đang **chờ thanh toán**, bấm **Duyệt** → `DuyetThanhToanNapTienTongDaiSA`:
   - `idDonVi` rỗng → "Id đơn vị không được để trống".
   - Hợp lệ → duyệt giao dịch → state **DaThanhToan** → **cộng số dư** tổng đài đơn vị.
4. Nếu cần hủy: bấm **Hủy duyệt** → `HuyDuyetThanhToanToanTongDaiSA` → state **HuyThanhToan**.
5. Mở chi tiết → **Cập nhật ngày hết hạn** → `UpdateThongTinGiaoDichDonViSA`.

## State-machine giao dịch
`GiaoDich_ChoThanhToan` → (Duyệt) → `GiaoDich_DaThanhToan` → (Hủy duyệt) → `GiaoDich_HuyThanhToan`.
- Duyệt áp dụng cho giao dịch **chờ thanh toán**.
- Hủy duyệt áp dụng cho giao dịch **đã duyệt**.

## Các trường hợp / case biên
- **Duyệt thiếu `idDonVi`** → trả lỗi "Id đơn vị không được để trống" (chặn đúng).
- **Hủy duyệt với tham số rỗng** (`idDonVi` / `idGiaoDich` null) → **KHÔNG được validate** như endpoint duyệt → có thể gọi với tham số null mà không báo lỗi rõ (known-issue — xem dev.md).
- **Duyệt giao dịch không ở trạng thái chờ thanh toán** / **hủy duyệt giao dịch chưa duyệt** → cần đúng trạng thái; thao tác ngược trạng thái dễ gây lệch số dư, cần kiểm tra phía service.
- **Đối chiếu số dư sau hủy duyệt:** hủy duyệt một giao dịch đã cộng số dư → cần trừ lại số dư tương ứng để khớp.

## Ví dụ cụ thể có số
Đơn vị Y nạp 500.000đ vào tổng đài IP, tạo giao dịch G1 trạng thái **chờ thanh toán**.
- Kế toán hệ thống (HT-KT) mở màn, chọn đơn vị Y → thấy G1 = 500.000đ, chờ thanh toán.
- Bấm **Duyệt** G1 → G1 chuyển **đã thanh toán** → số dư tổng đài đơn vị Y +500.000đ (ví dụ từ 0 → 500.000đ).
- Phát hiện duyệt nhầm (đơn vị chưa chuyển khoản) → bấm **Hủy duyệt** G1 → G1 chuyển **hủy thanh toán**; cần đối chiếu để số dư về lại 0.
- Đơn vị xin gia hạn → mở G1, **Cập nhật ngày hết hạn** từ 30/06 → 31/07.

## Hậu điều kiện
- **Duyệt thành công:** giao dịch ở state `DaThanhToan`, số dư tổng đài đơn vị tăng tương ứng.
- **Hủy duyệt:** giao dịch ở state `HuyThanhToan` (cần đảm bảo số dư được điều chỉnh khớp).
- **Cập nhật hạn:** ngày hết hạn chi tiết giao dịch được lưu lại.
- Xem lịch sử (`GetLichSuThanhToanSA`) là read-only, không đổi dữ liệu.
