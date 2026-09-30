---
controller: DatLichNangCaoController
module: cua-hang
service: Booking
man_hinh_ui: "Trang đặt lịch nâng cao end-user (đặt theo lịch/ca/khu vực/vị trí với 2 view: 'Chọn nhanh' và 'Tùy chỉnh'), chọn ô trống → giữ chỗ (hold 2 phút) → thanh toán tạo Thẻ lịch + loạt lịch hẹn; có màn xem lịch theo Thẻ khách hàng."
muc_dich: "Luồng đặt lịch nâng cao (theo khung giờ/ca/vị trí, tính giá theo khung giờ): lấy điều kiện, dựng lịch trống, giữ chỗ Redis, thanh toán → tạo thẻ lịch + nhiều lịch hẹn + gửi SMS/Zalo link check-in; tra cứu lịch theo thẻ khách hàng."
alias: ["đặt lịch nâng cao", "đặt lịch theo ca", "đặt lịch theo khung giờ", "giữ chỗ đặt lịch", "hold lịch", "thẻ lịch khách hàng", "đặt lịch tính giờ", "đặt chỗ theo vị trí"]
auth: anonymous
state_machine: []
deprecated: false
entities: [DatLich, DatLichChiTiet, ChoNgoi, NgayLamViec, KhungGio, KhungGioNgayLamViec, TheLichKhachHang]
stored_procs: []
depends: [WarehouseProduct (DichVu tính giờ theo khu vực + giá theo khung giờ + TaoTheLichKhachHang), Customer (KhachHang_FindOrCreated), Redis (giữ chỗ hold + cấu hình SMS V2), ShortLink (link check-in), SMS/Zalo (gửi tin hệ thống), CauHinhService (DistanceTwoTimeFrame/DefaultStatusBooking), Core (DonVi/ChiNhanh enrich SMS)]
cross_module_in: [End-user web — toàn bộ endpoint (AllowAnonymous): Search/Hold/Payment; trang xem lịch theo thẻ (LayLichHenTheoIdTheKhachHang)]
cross_module_out: [WarehouseProduct — tạo Thẻ lịch khách hàng + đọc dịch vụ tính giờ/giá khung giờ; Customer — FindOrCreated khách; Redis — set/clear key giữ chỗ DatLichGiuCho_ThanhToan (TTL 2 phút); SMS/Zalo — gửi link check-in; tạo DatLich+DatLichChiTiet gắn IdTheKhachHang]
---

# DatLichNangCaoController — Đặt lịch nâng cao (theo ca/khu vực, giữ chỗ + thanh toán)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy điều kiện đặt lịch | (init form) | POST `api/DatLichNangCao/LayDieuKienDatLich` | (key `DatLich_LayDieuKienDatLich` bị comment) | anonymous | mở form | Validate DonVi+ChiNhánh; lấy NgayLamViec đang hoạt động + KhungGio; sinh `ListCaLichHenBase` theo `cauHinh.DistanceTwoTimeFrame` (mặc định nếu ≤0); trả ngày-trong-tuần + ca cơ bản | EF + CauHinhService | - |
| 2 | Tìm lịch trống — view Chọn nhanh | tab "Chọn nhanh" | POST `SearchThongTinDatLich_ViewChonNhanh` | (không key) | anonymous | tab chọn nhanh | `InitLichTheoCa` (ngày-trong-tuần → ca → vị trí → ngày chi tiết); với mỗi ca chọn vị trí FREE rẻ nhất; tính giá theo khung giờ + khu vực; bỏ ô bận (DB + redis hold) | EF + HTTP WarehouseProduct (giá khung giờ) + Redis | Code có `//TODO` chưa chặn response quá lớn (Service:349-354) → request rộng có thể trả data rất lớn/chậm |
| 3 | Tìm lịch trống — view Tùy chỉnh | tab "Tùy chỉnh" | POST `SearchThongTinDatLich_ViewTuyChinh` | (không key) | anonymous | tab tùy chỉnh | Như #2 nhưng trả ma trận ca × ngày × chỗ ngồi (`ListData` theo `ViewTuyChinh_Ca`) để user tự chọn từng ô | EF + HTTP + Redis | `//TODO` chặn response lớn cũng bị comment (Service:458-463) |
| 4 | Giữ chỗ — view Chọn nhanh | chọn ô → giữ chỗ | POST `HoldThongTinDatLich_ViewChonNhanh` | (không key) | anonymous | sau khi chọn | Validate IdHold; với mỗi ô `IsRegist`: check bận (DB + redis); nếu trống → set redis key `DatLichGiuCho_ThanhToan` TTL ~2 phút với IdHold; gộp ca liên tục; trả `SoTienTong` + `TimeExpiredHold` | EF + Redis | TTL giữ chỗ hardcode `2*60` giây — comment `//TODO: có cần cấu hình` (Service:596,2042) |
| 5 | Giữ chỗ — view Tùy chỉnh | chọn ô → giữ chỗ | POST `HoldThongTinDatLich_ViewTuyChinh` | (không key) | anonymous | sau khi chọn | Như #4 nhưng duyệt cấu trúc `ListNgayTheoCa` của view tùy chỉnh | EF + Redis | Như #4 |
| 6 | Bỏ giữ chỗ | thoát/hủy chọn | POST `ClearLich_HoldThongTinDatLich` | (không key) | anonymous | - | Với mỗi ô đã hold: nếu redis key == IdHold của mình → `RemoveAsync` (trả về void, không bọc ResponseResult) | Redis | Endpoint trả `Task` (void) không qua `ResponseResult` (Controller:92-96) → client không nhận body chuẩn |
| 7 | Thanh toán → tạo lịch | "Thanh toán" | POST `ThongTinDatLich_Payment` | (không key) | anonymous | sau giữ chỗ | Lọc bỏ ô bận; validate (DonVi/ChiNhánh, danh sách+ngày+giờ, redis hold còn hạn); tìm/tạo KH (Customer); tạo `TheLichKhachHang` (WarehouseProduct); tạo N `DatLich`+`DatLichChiTiet` gắn IdThe (transaction); tạo ShortLink; gửi SMS/Zalo link check-in | EF (transaction) + HTTP (Customer/WP) + Redis + ShortLink | Catch nuốt lỗi transaction: rollback nhưng comment "TODO: xóa những thứ đã tạo" (thẻ/KH/shortlink) chưa được dọn → rác khi insert lịch fail (Service:944-953) |
| 8 | Lấy lịch theo thẻ khách hàng | màn xem lịch theo thẻ | POST `LayLichHenTheoIdTheKhachHang` | (không key) | anonymous | - | Validate DonVi/ChiNhánh/IdTheKhachHang; list lịch theo thẻ (filter Keyword==Ma chính xác); enrich tên vị trí (ChoNgoi); sort theo Ngay/GioBatDau | EF | `SoTien` để `//TODO` chưa tính (Service:1720) |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint private. Phương thức `CheckInLichHen(idDatLich, idDonVi, idChiNhanh)` có trong service (quét QR check-in → chuyển trạng thái `DangPhucVu`, chỉ trong 24h, chỉ lịch có IdTheKhachHang) nhưng **chưa expose** qua controller này.

## Quy tắc nghiệp vụ (gating / tính giá / giữ chỗ)
1. **Sinh ca**: bước ca = `cauHinh.DistanceTwoTimeFrame` (≤0 → `Constants.DistanceTwoTimeFrameDefault`); ca kết thúc không vượt giờ làm việc; tự cắt nếu ngày = hôm nay (gioBatDau = giờ hiện tại).
2. **Giá theo khung giờ (ưu tiên có thứ tự)**: (a) cấu hình theo `IdKhuVuc` chỉ định + đúng `NgayTrongTuan` → (b) cùng khu vực nhưng NgayTrongTuan mặc định (null/<0) → (c) cấu hình khu vực mặc định `IdKhuVuc==null` + NgayTrongTuan chỉ định → (d) khu vực mặc định + NgayTrongTuan mặc định. Không có giá (GiaDichVu<0/null) → ô `IsWork=false`.
3. **Bận (IsBusy)** = có DatLichChiTiet trùng ChoNgoi+ngày+giao thoa giờ (DB, loại lịch phục vụ tại nhà), HOẶC redis key hold tồn tại với IdHold khác mình.
4. **Giữ chỗ**: set redis `DatLichGiuCho_ThanhToan(DonVi,ChiNhánh,Ngay,ViTri,GioBatDau,GioKetThuc)` = IdHold, TTL = `120 - (thời gian đã xử lý)` giây; thanh toán phải còn key hợp lệ trong 2 phút, hết hạn → "Mã giữ chỗ đã hết hạn...".
5. **Thanh toán**: chỉ xử lý ô `IsBusy != true`; bỏ ô thiếu giờ; trạng thái lịch mặc định = `cauHinh.DefaultStatusBooking` (fallback `ChuaXacNhan`); tất cả lịch tạo ra cùng 1 `IdTheKhachHang`.

## Quan hệ
- **Gọi sang:** WarehouseProduct (`CauHinhDonHangMatHangMacDinh_LayTatCaDichVuTinhGioTheoKhuVuc`, `TheDichVuKhachHang_TaoTheLichKhachHang`), Customer (`KhachHang_FindOrCreated`), ShortLink (`CreateCustomAsync`), Redis (giữ chỗ + cấu hình SMS V2), SMS/Zalo (SmsSend SaveSendMessageSystemV2 / SaveSendSmsSystem / SaveSendZaloSystem), Core (DonVi/ChiNhanh enrich param SMS).
- **Dữ liệu (entities):** `DatLich` + `DatLichChiTiet` (gắn `IdTheKhachHang`), `ChoNgoi` (vị trí), `NgayLamViec`/`KhungGio`/`KhungGioNgayLamViec` (lịch làm việc), `TheLichKhachHang` (thẻ — bên WarehouseProduct).
- **Ripple / đồng bộ:** Redis hold key (TTL 2'); tạo Thẻ lịch (WarehouseProduct); gửi SMS/Zalo link check-in (action `LINK_CHECK_IN`).

## Luồng xuyên module (đặt lịch nâng cao end-user)
```
LayDieuKienDatLich → Search...ViewChonNhanh/ViewTuyChinh (InitLichTheoCa: ngày→ca→vị trí→ngày chi tiết, giá khung giờ từ WarehouseProduct)
  → Hold...ViewChonNhanh/ViewTuyChinh (set redis hold TTL 2', tính SoTienTong)
  → ThongTinDatLich_Payment:
       FindOrCreated KhachHang (Customer)
       → Tạo TheLichKhachHang (WarehouseProduct)
       → Tạo N DatLich + DatLichChiTiet (transaction) gắn IdThe
       → Tạo ShortLink + gửi SMS/Zalo (link check-in)
  (ClearLich_HoldThongTinDatLich nếu hủy → xóa redis hold)
Sau đó khách quét QR → CheckInLichHen (service, chưa expose) → DangPhucVu
```

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller `[AllowAnonymous]` toàn bộ, không filter quyền** (Controller:19) — mọi endpoint nhận `idDonVi/idChiNhanh` từ body không token; ai biết id đều search/hold/payment được. Lưu ý lộ giá + tạo dữ liệu (lịch/thẻ/KH) mà không xác thực.
- **Payment rollback không dọn dữ liệu phụ** (Service:944-953) — khi insert lịch lỗi: rollback transaction lịch nhưng Thẻ lịch / Khách hàng / ShortLink đã tạo trước đó KHÔNG bị xóa (comment TODO) → rác + thẻ mồ côi.
- **TTL giữ chỗ hardcode 120s** (Service:596, 2042) — chưa lấy từ cấu hình (comment TODO); với thao tác lâu, `TimeExpiredHold` có thể âm.
- **Chưa chặn response quá lớn** (Service:349-354, 458-463) — logic giới hạn kích thước kết quả bị comment (`Marshal.SizeOf`) → tìm khoảng ngày/ca rộng → kết quả rất lớn, chậm (TODO trong code).
- **`ThongTinDatLich_Payment` không tính tiền theo DV thật khi hold** — nhiều chỗ `SoTien = 0` / `//TODO: Có cần lấy tiền từ tbl dịch vụ không?` (Service:666,702,2111,2148); số tiền lấy từ giá khung giờ ở bước search, ô bận set 0.
- **`SoBuoi` cố định = 1** (Service:881) — comment ghi 1 buổi có thể gồm 2 ca, cần check lại khi nhiều ca.
- **`ClearLich_HoldThongTinDatLich` trả void** (Controller:92-96) — không bọc `ResponseResult`, client không có body chuẩn để biết kết quả.
- **`CheckInLichHen` chưa expose** — logic check-in QR (chuyển `DangPhucVu`, chỉ trong 24h kể từ giờ hẹn, chỉ lịch có IdTheKhachHang) tồn tại trong service nhưng controller này không có endpoint gọi (Service:1631-1678); phần update vào TheLich còn để trống (comment).
- Code thật: BE controller `Booking/Booking.API/Controllers/DatLich/DatLichNangCaoController.cs`; service `Booking/Booking.Service/Generated/DatLich/DatLichNangCaoService.cs` (~2194 dòng); DTO `Models.Core.DTO.Booking.Client.DatLich.DatLichNangCaoDto`.
