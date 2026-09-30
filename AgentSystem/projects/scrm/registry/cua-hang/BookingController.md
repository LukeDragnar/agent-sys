---
controller: BookingController
module: cua-hang
service: Booking
man_hinh_ui: "nội bộ — không có UI quản trị; endpoint phục vụ trang public (mở từ short-link/QR) khi khách bấm link đặt lịch / đánh giá đơn hàng / xem thẻ khách hàng"
muc_dich: "Giải mã 1 short-link (hashCode) thành thông tin hiển thị tương ứng: lịch hẹn, đánh giá đơn hàng (offline/online), hoặc thẻ khách hàng — gom dữ liệu từ nhiều service (Customer/WarehouseProduct/Promotion/Core/Order) để render trang public."
alias: ["thông tin đặt lịch theo link", "mở link đặt lịch", "trang đánh giá đơn hàng", "QR thẻ khách hàng", "GetThongTinDatLich", "short link booking"]
auth: public
state_machine: []
deprecated: false
entities: [DatLich, DatLichChiTiet, ChoNgoi, ShortLink]
stored_procs: []
depends: [ShortLinkService (nội bộ Booking), DatLichService (TimTheoIdAsync_Private_20250402), WarehouseProductService (thẻ KH / combo / thẻ tiền), CustomerService (KhachHang_TimTheoId_Private + KhachHangDanhGia), Promotion (KhuyenMaiKhachHang), Core (ChiNhanh/DonVi), OrderReport (lịch sử dùng DV)]
cross_module_in: [FE trang public mở qua short-link/QR gọi GetThongTinDatLich/{hashCode}]
cross_module_out: [Đọc-only nhiều service qua _clientHelper; KHÔNG ghi dữ liệu]
---

# BookingController — Lấy thông tin theo short-link đặt lịch / đánh giá

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin theo short-link | (mở trang public từ link/QR — không có nút trong admin) | GET `api/Booking/GetThongTinDatLich/{hashCode}` | — (không `[SBAuthorize]`, controller-level `[SBAuthorize]` đã comment) | public | luôn, khi khách mở link | Giải `hashCode` → ShortLink. Theo `LoaiDoiTuong` rẽ nhánh: **LichHen** → lấy lịch hẹn (`TimTheoIdAsync_Private_20250402`) + combo + thẻ tiền + khuyến mãi + thông tin KH + lịch sử DV, gắn HashKey; **MaDanhGia** → gọi Customer `GetThongTinDonHangByMa` lấy đơn để đánh giá; **DonHangOffline/Online** → gọi Customer `GetThongTinDonHangIdDonHang` (POST payload); **TheKhachHang** → lấy thẻ KH (Anonymous) + nếu là Thẻ-lịch thì list 3 ngày lịch hẹn sắp tới + QR check-in. Lỗi bất kỳ → trả `ResultWithError`, exception bọc try/catch trả message. | EF (DatLich/DatLichChiTiet/ChoNgoi) + nhiều HTTP cross-service | Xem mục lỗi |

> Controller chỉ có **1 endpoint duy nhất**. Toàn bộ độ phức tạp nằm ở service `GetThongTinDatLich` (rẽ 4 nhánh theo `EnumLoaiDoiTuong`).

## Quy tắc nghiệp vụ (rẽ nhánh theo `ShortLink.LoaiDoiTuong`)
Thứ tự xử lý trong `BookingService.GetThongTinDatLich`:
1. `GetShortLinkByHashCode(hashCode)` lỗi/null → trả error "Lấy thông tin link xảy ra lỗi." Gán `resData.IdDonVi`, load tên đơn vị (`getDataDonVi`).
2. **LichHen** (`LoaiDoiTuong == LichHen` và `IdLichHen != Empty`): lấy lịch hẹn bản `_20250402`; song song (`Task.WhenAll`) load chi nhánh + combo + thẻ tiền (private) + khuyến mãi + thông tin KH + lịch sử dùng DV; map `DatLichDetail_20250402Dto`; gắn `HashKeyDatLich` (AES, hết hạn +15 phút). Return.
3. **MaDanhGia** (`LoaiDoiTuong == MaDanhGia` và có `maDanhGia`): GET Customer `GetThongTinDonHangByMa/{maDanhGia}` → nếu `Data==null` trả error; nếu có `IdKhachHang` → load combo/thẻ/KM/KH/lịch sử; load chi nhánh; `Type=0`; gán mức độ hài lòng/ý kiến parent. Return.
4. **DonHangOffline / DonHangOnline**: validate `IdChiNhanh`, `IdDonHangOffline`/`IdDonHangOnline`, `IdDonVi` không rỗng (thiếu → error tương ứng). Dựng `KhachHangDanhGiaIdDonHangDto` (KieuLoaiDonHang = Order/OrderLogistic) → POST Customer `GetThongTinDonHangIdDonHang/{maDanhGia}`; tương tự nhánh 3.
5. **TheKhachHang** (`shortLink.LoaiDoiTuong == TheKhachhHang`): `LayThongTin_ShortLink_TheKhachHang` — gọi WarehouseProduct `TheDichVuKhachHang_LayTheKhachHangTheoIdThe_Anonymous`; nếu `LoaiThe == TheLich` → query `DatLich` (nolock) theo `IdDonVi + IdTheKhachHang`, ngày >= hôm nay (loại slot đã qua giờ), lấy 3 ngày gần nhất, build list lịch + tên chỗ ngồi (join `DatLichChiTiet`×`ChoNgoi`), mỗi lịch gắn `HashKey` + thông tin chi nhánh; lấy thông tin KH (`KhachHang_TimTheoId_Private`), ẩn SĐT.

## Quan hệ
- **Gọi sang (đọc-only, qua `_clientHelper` + service nội bộ):**
  - Booking nội bộ: `ShortLinkService.GetShortLinkByHashCode`, `DatLichService.TimTheoIdAsync_Private_20250402`.
  - Core: `ChiNhanh/TimTheoId`, `DonVi/GetDonViById`.
  - Customer: `KhachHangDanhGia/GetThongTinDonHangByMa`, `KhachHangDanhGia/GetThongTinDonHangIdDonHang`, `KhachHang/TimTheoId_Private`, `KhachHang_TimTheoId_Private`.
  - WarehouseProduct: `TheDichVuKhachHang_LayTheKhachHangTheoIdThe_Anonymous`, `ComBoSpaKhachHang/GetListComBoByIdKhachHang_Private`, `TheDichVuKhachHang/LayTatCaTheTien_Private`.
  - Promotion: `KhuyenMaiKhachHang/TimKiemTheoIdKhachHang_LichHen`.
  - OrderReport: `OrderReport/HistoriesUsingServiceCustomer`.
- **Dữ liệu (entities):** `DatLich`, `DatLichChiTiet`, `ChoNgoi` (đọc trực tiếp EF cho nhánh Thẻ-lịch); còn lại lấy qua HTTP DTO.
- **Ripple / đồng bộ:** không ghi gì — thuần read/aggregate cho trang public.

## Lỗi đã biết / lưu ý kỹ thuật
- **Endpoint public không gắn quyền** — `BookingController.cs:13` chỉ `[Route]`/`[ApiController]`; `[SBAuthorize]` ở controller-level bị comment (`:12`) và method `GetThongTinDatLich` không có attribute auth nào → ai có `hashCode` đều gọi được. Đây là thiết kế cho trang public (short-link), nhưng response trả khá nhiều dữ liệu KH (combo, thẻ tiền, khuyến mãi, lịch sử DV) → cân nhắc lộ dữ liệu nếu hashCode đoán/lộ được.
- **Lấy thẻ KH bằng biến thể `_Anonymous`** (`Service:263`) — chủ ý cho nhánh public; xác nhận biến thể này đã kiểm soát đúng `IdDonVi`.
- **Message lỗi mơ hồ** — nhánh LichHen khi `lichHenRes.Success == false` trả đúng chữ `"sai"` (`Service:122`); khó debug, nên đổi message rõ hơn.
- **Phụ thuộc bản method có hậu tố ngày** — dùng `TimTheoIdAsync_Private_20250402` (`Service:118`), bản cũ `TimTheoIdAsync_Private` đã comment (`Service:117`) → lưu ý khi maintain, có 2 phiên bản lịch hẹn.
- **Nhánh DonHangOffline/Online không `return` sớm** — sau khi xử lý (`Service:186-239`) code chạy tiếp xuống check `TheKhachhHang` rồi mới `return resData` (`Service:248`); với 2 loại đơn hàng thì điều kiện thẻ KH false nên vẫn trả đúng `resData`, nhưng luồng phụ thuộc thứ tự if — dễ vỡ nếu thêm nhánh.
- **Mọi sub-call bọc try/catch nuốt lỗi trả result rỗng** (`getDataChiNhanh`/`GetListComBo...`/`GetThongTinKhachHang`...) — 1 service phụ lỗi sẽ không báo, chỉ thiếu data; khó phát hiện sự cố cross-service.
- Code thật: controller `Booking.API/Controllers/BookingController.cs`; service `Booking.Service/Generated/BookingService.cs` (method chính `GetThongTinDatLich` :89, nhánh thẻ KH `LayThongTin_ShortLink_TheKhachHang` :257).
