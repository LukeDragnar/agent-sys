# Luồng — Lịch sử đăng ký tiện ích

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị. Endpoint `LichSuGiaoDich` chỉ yêu cầu `[SBAuthorize()]` (login), lọc theo `CurrentUser` → chỉ thấy giao dịch đơn vị mình.
- Đã có ít nhất một giao dịch mua tiện ích (sinh từ màn Quản lý / Đăng ký tiện ích).

## Luồng dùng (tra cứu)
1. Mở **Cài đặt → Lịch sử mua tiện ích** (`/caidat/lich-su-mua-tien-ich`).
2. `ngOnInit` build cột bảng (Mã giao dịch, Ngày giao dịch, Giá trị, Danh sách tiện ích, Trạng thái) rồi `gridLoadData()`.
3. Gọi `LichSuGiaoDich` (POST) với `{...gridOption, keyword, tuNgay, denNgay}` → `IHopDongTienIchService.LichSuGiaoDichAsync(gridInfo, CurrentUser)`.
4. Bảng hiển thị có phân trang (`totalRecord`), sắp xếp theo cột.
5. Người dùng lọc theo mã giao dịch / khoảng ngày → bấm **Tìm kiếm** → load lại.

## Trạng thái hiển thị (mapping màu ở FE)
- `trangThai == 0` → nền vàng (chờ — khớp `HopDongTienIch_TrangThai.ChoDuyet`).
- `trangThai == 1` → nền xanh lá.
- `trangThai == 3` → nền tím.
- `trangThai == 5` → nền đỏ.
- Text hiển thị lấy từ `trangThaiText` (BE trả). Ý nghĩa cụ thể của 1/3/5 (đã duyệt / hủy duyệt / hủy…): **cần product/registry xác nhận** — registry state machine HopDongTienIch chỉ liệt kê [ChoDuyet, DaDuyet, HuyDuyet], chưa map số.

## Case biên
- **Không có giao dịch nào** → bảng rỗng, `totalRecord=0`.
- **Tìm theo mã không khớp** → bảng rỗng.
- **Lọc ngày Từ > Đến** → tùy backend; thường trả rỗng (cần xác nhận có validate không).
- **Một lần mua giỏ trộn free + trả phí** → sinh **2 hợp đồng (2 mã giao dịch)** → 2 dòng trong lịch sử cho cùng thao tác.
- **Giao dịch giá trị 0** (chỉ tiện ích miễn phí) → cột Giá trị = 0đ, trạng thái thường là đã duyệt (auto).

## Ví dụ cụ thể có số
Đơn vị X có 3 giao dịch:
- `HD000121` — 12/06/2026 — 0đ — [Chat đa kênh] — trạng thái xanh (đã duyệt, miễn phí auto).
- `HD000123` — 18/06/2026 — 1.000.000đ — [Tổng đài IP x2] — trạng thái vàng (chờ duyệt).
- `HD000124` — 19/06/2026 — 300.000đ — [Báo cáo nâng cao] — trạng thái đỏ (hủy — cần xác nhận nghĩa code 5).
→ Admin tìm mã `HD000123` → ra đúng 1 dòng; lọc "Từ 18/06 đến 18/06" → ra 1 dòng (HD000123).

## Hậu điều kiện
- Read-only: thao tác xem/lọc **không thay đổi dữ liệu**.
- Dữ liệu hiển thị phản ánh trạng thái mới nhất của hợp đồng tiện ích (thay đổi khi quản trị hệ thống duyệt/hủy duyệt).
