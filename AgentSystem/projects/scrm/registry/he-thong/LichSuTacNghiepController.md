---
controller: LichSuTacNghiepController
module: he-thong
service: LogService
man_hinh_ui: "Mixed: ghi log (nội bộ) + màn lịch sử tác nghiệp Sale/Kho + tra cứu mã vận đơn (public)"
muc_dich: "Ghi log lịch sử tác nghiệp (publish RabbitMQ → ghi MongoDB ở background) và tra cứu lịch sử tác nghiệp Sale/Kho; có endpoint tra cứu mã vận đơn ẩn danh."
alias: ["lịch sử tác nghiệp", "log tương tác", "tra cứu vận đơn", "tra cứu tác nghiệp"]
auth: mixed
state_machine: []
deprecated: false
entities: [LichSuTacNghiep, EventLichSuTuongTac]
stored_procs: []
depends: [ILichSuTacNghiepService, IPublishEndpoint (RabbitMQ), IMongoDbClient]
cross_module_in: ["FE Sale/Kho gọi Log + *_TimTheoDieuKien", "OrderLogistic gọi GetLichSuTacNghiepGiaoHangTheoDsOrderLogistic / XoaLichSuTacNghiepTheoDsContact (PrivateSBAuthorize)", "Tra cứu vận đơn public gọi GetTraCuuThongTinVanDon"]
cross_module_out: ["Publish LichSuTacNghiep → EventLichSuTuongTacConsumer ghi MongoDB"]
---

# LichSuTacNghiepController — Lịch sử tác nghiệp + tra cứu vận đơn

Route `api/LichSuTacNghiep` (không có `[TienIch]`). Auth lẫn lộn: `[AuthenLog]` (log), `[SBAuthorize]` (tra cứu Sale/Kho), `[PrivateSBAuthorize]` (service-to-service), `[AllowAnonymous]` (tra cứu vận đơn) → **mixed**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu tác nghiệp Sale | Tìm | POST `api/LichSuTacNghiep/Sale_TimTheoDieuKien` | (SBAuthorize, không key) | internal | — | Set `Type=1` rồi TimTheoDieuKien + CurrentUser | Mongo | SBAuthorize không có key → chỉ check đăng nhập |
| 2 | Tra cứu tác nghiệp Kho | Tìm | POST `api/LichSuTacNghiep/Kho_TimTheoDieuKien` | (SBAuthorize, không key) | internal | — | Set `Type=2` rồi TimTheoDieuKien | Mongo | nt |
| 3 | Tra cứu Sale+Kho | Tìm | POST `api/LichSuTacNghiep/Sale_Kho_TimTheoDieuKien` | (SBAuthorize, không key) | internal | — | Suy Type theo input: có IdDonHangLogistic→Type=2; chỉ IdContact→Type=1,ActionId=25; còn lại Type=null | Mongo | Logic phân loại theo input, không theo quyền (đoạn check chi nhánh bị comment) |
| 4 | Tra cứu mã vận đơn | (public tra cứu) | POST `api/LichSuTacNghiep/GetTraCuuThongTinVanDon` | — | anonymous (`[AllowAnonymous]`) | — | Tra cứu thông tin vận đơn theo ThongTinTimKiemMaVanDon | Mongo | **Public** — không auth, cần rate-limit/captcha |

## Endpoint nội bộ (private / service-to-service)
- [internal] Ghi log: POST `api/LichSuTacNghiep/Log` (`[AuthenLog]`) → publish `LichSuTacNghiep` lên RabbitMQ, trả `"queued"`, consumer ghi MongoDB background.
- [internal] OrderLogistic gọi `XoaLichSuTacNghiepTheoDsContact` (`[PrivateSBAuthorize]`) → xóa lịch sử theo DS contact.
- [internal] OrderLogistic gọi `GetLichSuTacNghiepGiaoHangTheoDsOrderLogistic` (`[PrivateSBAuthorize]`) → lấy lịch sử giao hàng theo DS order logistic.

## Quan hệ
- **Gọi sang:** `ILichSuTacNghiepService`, `IPublishEndpoint` (RabbitMQ), `IMongoDbClient`.
- **Dữ liệu (entities):** `LichSuTacNghiep` (MongoDB), DTO tra cứu vận đơn.
- **Ripple / đồng bộ:** `Log` publish `LichSuTacNghiep` → `EventLichSuTuongTacConsumer` ghi MongoDB (background, eventual).

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuTacNghiepController.cs:128-134` — `GetTraCuuThongTinVanDon` `[AllowAnonymous]`: endpoint công khai không auth → rủi ro lạm dụng/scrape, nên thêm rate-limit/captcha.
- `LichSuTacNghiepController.cs:67,76,85` — 3 endpoint tra cứu dùng `[SBAuthorize]` không truyền key permission → chỉ kiểm tra đã đăng nhập, không phân quyền chi tiết.
- `LichSuTacNghiepController.cs:89-97` — đoạn phân loại Type theo chi nhánh/admin bị comment; hiện chỉ suy theo IdDonHangLogistic/IdContact.
- `LichSuTacNghiepController.cs:47-65` — `LogUsage` (ghi Elasticsearch) đã comment toàn bộ.
