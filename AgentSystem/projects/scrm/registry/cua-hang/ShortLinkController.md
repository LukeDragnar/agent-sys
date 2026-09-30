---
controller: ShortLinkController
module: cua-hang
service: Booking
man_hinh_ui: "nội bộ — không có UI trực tiếp; sinh/đọc short link (rút gọn) cho link đánh giá, lịch hẹn, đơn hàng. Gọi từ job marketing campaign + flow gửi SMS/đánh giá. Có 1 endpoint thống kê (TongSoKhachNhanLinkDanhGia) phục vụ report."
muc_dich: "Tạo và quản lý short link (mã hash rút gọn) cho các đối tượng: mã đánh giá, lịch hẹn, thẻ khách hàng, đơn hàng offline/online; bulk-gen short link cho job marketing campaign; đếm số khách nhận link đánh giá."
alias: ["short link", "link rút gọn", "link đánh giá", "gen short link", "shortlink campaign", "đếm khách nhận link đánh giá"]
auth: mixed
state_machine: []
deprecated: false
entities: [ShortLink]
stored_procs: [PRC_SCRM_CHECK_SHORTLINK, PRC_Booking_Count_SoLuong_TinDanhGia]
depends: [Booking (ShortLink), Core (DonVi — tên miền đánh giá), SMS (AccountSmsAgency — domain mặc định), Redis (set GEN_SHORTLINK_CAMPAIGN chống trùng), SqlSugar/Dapper (SP check trùng + count)]
cross_module_in: [Job MarketingCampaign — GenShortLinkForJobMarketingCampaign (PrivateSBAuthorize); flow tạo link đánh giá/lịch hẹn — CreateCustomAsync/GetShortLinkByHashCode (PrivateSBAuthorize)]
cross_module_out: [Insert ShortLink (link đánh giá đơn hàng/lịch hẹn) → dùng trong SMS/đánh giá khách; domain link tùy cấu hình đơn vị (tên miền riêng) hoặc domain mặc định]
---

# ShortLinkController — Short link đặt lịch / đánh giá / đơn hàng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đếm khách nhận link đánh giá | thống kê/report | POST `api/ShortLink/TongSoKhachNhanLinkDanhGia` | `[SBAuthorize]` (chỉ login) | public | màn report đánh giá | ModelState invalid → lỗi model; ok → chạy SP `PRC_Booking_Count_SoLuong_TinDanhGia` (@IdDonVi từ user, @IdChiNhanh/@TuNgay/@DenNgay từ payload) → trả tổng (int) | SP `PRC_Booking_Count_SoLuong_TinDanhGia` | - |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`CreateCustomAsync`** (POST, body `ShortLinkInput`) — tạo (hoặc trả lại nếu đã tồn tại) short link cho 1 đối tượng theo `LoaiDoiTuong` (MaDanhGia / LichHen / TheKhachHang / DonHangOffline / DonHangOnline). Validate input (null/`IdDonVi=Empty`); idempotent: nếu đã có link cùng đối tượng+đơn vị → trả lại; lấy `DonVi` (Core) + cấu hình SMS (domain mặc định hay tên miền riêng); sinh `HashCode` 8 ký tự, retry tối đa 10 lần chống trùng; build `TxtShortLink` theo domain; insert.
- [internal — `[PrivateSBAuthorize]`] **`GetShortLinkByHashCode/{hashCode}`** (GET) — tra ShortLink theo hashCode; không có → lỗi "Không tồn tại ShortLink!".
- [internal — `[PrivateSBAuthorize]`] **`GenShortLinkForJobMarketingCampaign`** (POST, body `List<OrderInfoToGenShortLink>`) — bulk-gen short link cho danh sách đơn hàng (offline/online) của 1 job campaign. ModelState invalid → lỗi; lấy tên miền đánh giá + cấu hình SMS theo list đơn vị; loại đơn hàng đã có link (trả lại link cũ); sinh batch hash (loop ≤40), dùng SP `PRC_SCRM_CHECK_SHORTLINK` + Redis set `GEN_SHORTLINK_CAMPAIGN` chống trùng; `BulkInsert` (retry 2 lần đổi Id nếu lỗi).

## Quy tắc nghiệp vụ (gen hash / domain)
- **Idempotent theo đối tượng**: `CreateCustomAsync` tra trước theo `LoaiDoiTuong` + đơn vị; đã tồn tại → trả link cũ (không tạo mới).
- **Chọn domain link**: nếu cấu hình SMS đơn vị `IsUseDefault=true` → domain mặc định `DanhGiaClientUrl`; ngược lại dùng `TenMienDanhGia` của đơn vị (Core), fallback về domain mặc định nếu rỗng. Format: `{domain}/v/{hashCode}`.
- **Chống trùng hash**: `CreateCustomAsync` retry ≤10 lần (đơn lẻ); `GenShortLinkForJobMarketingCampaign` loop ≤40 lần, đối chiếu SP `PRC_SCRM_CHECK_SHORTLINK` + Redis set. Hết loop mà số gen != số cần → "Xảy ra lỗi!".

## Quan hệ
- **Gọi sang:** Core `GET /api/DonVi/GetDonViById/{id}`, `POST /api/DonVi/GetTenMienDanhGiaByListId`; SMS `GET /api/AccountSmsAgency/TimTheoIdDonViAsync_Private/{id}`, `POST /api/AccountSmsAgency/GetCauHinhDefaultTheoListId`; SqlSugar SP `PRC_SCRM_CHECK_SHORTLINK`, `PRC_Booking_Count_SoLuong_TinDanhGia`; Redis set `GEN_SHORTLINK_CAMPAIGN`.
- **Dữ liệu (entities):** `ShortLink` (HashCode, TxtShortLink, LoaiDoiTuong, MaDanhGia, IdLichHen, ValueDoiTuong, IdDonHangOffline, IdDonHangOnline, IdDonVi, IdChiNhanh).
- **Ripple / đồng bộ:** link sinh ra được dùng trong SMS / luồng đánh giá khách hàng; không có consumer ngược.

## Lỗi đã biết / lưu ý kỹ thuật
- **`CreateCustomAsync` retry range vô nghĩa** — `ShortLinkService.cs:191` `rnd.Next(8, 9)` luôn trả 8 (upper bound exclusive) → độ dài hash luôn 8, biến `ran` thừa.
- **Cấu hình SMS không bắt buộc** — `ShortLinkService.cs:164-167` đã comment check "Không tồn tại cấu hình sms"; thiếu cấu hình SMS vẫn tạo link với `IsUseDefault=false` → fallback domain đơn vị/mặc định (có thể không như ý nếu thiếu setting).
- **`GenShortLinkForJobMarketingCampaign` rò Redis set khi lỗi giữa chừng** — nếu loop kết thúc với `listShortLink.Count != payload.Count` (Service:328) trả lỗi sớm, các mã đã add vào Redis set `GEN_SHORTLINK_CAMPAIGN` KHÔNG được `SetRemove` (chỉ remove ở nhánh thành công Service:379-386) → rác tồn trong set.
- **`TongSoKhachNhanLinkDanhGia` đọc `ds.Tables[0].Rows[0][0]` không guard** — `ShortLinkService.cs:421`: nếu SP trả rỗng → IndexOutOfRange/NRE.
- **`BulkInserShortLink` nuốt exception** — `ShortLinkService.cs:390-409`: lỗi bulk insert chỉ đổi Id + giảm `soLanBulkInsert`, không log/không ném; nếu cả 2 lần fail thì im lặng mất dữ liệu insert.
- Code thật: BE controller `Booking/Booking.API/Controllers/ShortLinkController.cs`, service `Booking/Booking.Service/Generated/ShortLinkService.cs`.
