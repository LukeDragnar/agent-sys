---
controller: NotificationConfigController
module: he-thong
service: Notification
man_hinh_ui: "Hệ thống > Cấu hình thông báo (tab Tác nghiệp / Trạng thái đơn hàng / Trạng thái lịch hẹn): bảng cấu hình ai-nhận-noti theo sự kiện; nút Lưu / Xóa theo loại"
muc_dich: "Cấu hình QUY TẮC tự động gửi thông báo theo sự kiện nghiệp vụ (kết quả tác nghiệp CSKH, đổi trạng thái đơn hàng/giao hàng, trạng thái lịch hẹn): chọn mẫu thông báo + đối tượng nhận (sale/mkt/thu ngân/admin...) cho mỗi đơn vị."
alias: ["NotificationConfig", "NotiConfig", "cấu hình thông báo", "thiết lập gửi thông báo tự động"]
auth: mixed
state_machine: []
deprecated: false
entities: [NotificationConfig]   # SQL (NotificationDataContext)
stored_procs: []
depends: ["NotificationDataContext (SQL)", "IClientHelper (gọi service cấu hình tác nghiệp / trạng thái đơn hàng / lịch hẹn để build danh sách sự kiện)", "ServiceSettings"]
cross_module_in: ["FE Hệ thống/Cấu hình thông báo gọi Save*/TimTheoDonVi/Delete", "Service nghiệp vụ (CSKH/Order/Booking) → GetDataUsedBy* (private) để biết sự kiện này gửi noti cho ai + mẫu nào"]
cross_module_out: ["Quy tắc này điều khiển nội dung/đối tượng khi các service nghiệp vụ bắn noti qua NotificationsController.Create"]
---

# NotificationConfigController — Cấu hình quy tắc gửi thông báo tự động

Màn cấu hình "khi sự kiện X xảy ra thì gửi noti mẫu Y cho những ai". 3 loại cấu hình: **Tác nghiệp** (kết quả thao tác CSKH → tác nghiệp tiếp theo), **Trạng thái đơn hàng/giao hàng**, **Trạng thái lịch hẹn**. Mỗi loại có cặp Save (FE lưu) + GetDataUsedBy* (service nghiệp vụ đọc lúc runtime). Lưu **SQL** `NotificationConfig`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình theo đơn vị | (load tab) | POST `api/NotificationConfig/TimTheoDonVi` | `NotiConfig_TimTheoDonVi` (HT-AD, DV-AD-TONG, HT-CSKH) | public | Theo `LoaiCauHinhThongBao` (mặc định HeThong) | Lấy NotificationConfig của đơn vị theo loại; với Tác nghiệp còn merge thêm danh sách thiết lập tác nghiệp từ service ngoài để hiển thị đủ sự kiện. | EF `NotificationConfig` (nolock) + IClientHelper | — |
| 2 | Lưu cấu hình Tác nghiệp | Lưu (tab Tác nghiệp) | POST `api/NotificationConfig/SaveTacNghiep` | `NotiConfig_SaveTacNghiep` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Upsert list cấu hình tác nghiệp (IdTacNghiepCan/IdKetQua/IdTacNghiepTiepTheo + cờ IsSendTo*). | EF | — |
| 3 | Lưu cấu hình Trạng thái đơn hàng | Lưu (tab ĐH) | POST `api/NotificationConfig/SaveTrangThaiDonHang` | `NotiConfig_SaveTrangThaiDonHang` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Upsert list cấu hình theo `GiaoHangTrangThaiMa` + đối tượng nhận. | EF | — |
| 4 | Lưu cấu hình Trạng thái lịch hẹn | Lưu (tab Lịch hẹn) | POST `api/NotificationConfig/SaveTrangThaiLichHen` | — | public (login) | — | Upsert list theo `TrangThaiLichHen`. | EF | Quyền để trống `[SBAuthorize]` (key bị comment) — lỏng hơn 3 nút Save kia |
| 5 | Xóa cấu hình theo loại | Xóa | GET `api/NotificationConfig/DeleteByIdUnit/{loaiCauHinhThongBao:int}` | `NotiConfig_DeleteByIdUnit` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Xóa toàn bộ NotificationConfig của đơn vị theo loại. | EF | HTTP GET cho hành động xóa-hàng-loạt |
| 6 | Custom (test) | (không UI) | GET `api/NotificationConfig/TimTheoDonViCustom` | — | anonymous | — | `TimTheoDonViCustom()` — trả cấu hình không qua đơn vị token. | EF | `[AllowAnonymous]` — không lọc theo user |

## Endpoint nội bộ (private / service-to-service)
- [internal] **POST `GetDataUsedByTacNghiep`** `[PrivateSBAuthorize]` — CSKH gọi khi user hoàn tất 1 tác nghiệp: trả cấu hình noti (mẫu + đối tượng nhận) khớp IdTacNghiepCan/IdKetQua.
- [internal] **POST `GetDataUsedByTrangThaiGiaoHang`** — Order/Logistic gọi khi đổi trạng thái giao hàng → cấu hình noti tương ứng `GiaoHangTrangThaiMa`.
- [internal] **POST `GetDataUsedByTrangThaiLichHen`** — Booking/Lịch hẹn gọi khi đổi trạng thái lịch hẹn.

## Quy tắc nghiệp vụ (loại cấu hình)
`LoaiCauHinhThongBao` (enum `EnumLoaiCauHinhThongBao`) phân nhánh chính:
- **HeThong** (default), **TacNghiep**, **TrangThaiDonHang/GiaoHang**, **TrangThaiLichHen**.
- Đối tượng nhận = tập cờ boolean trên cùng 1 entity: `IsSendToSale / IsSendToTruongPhongSale / IsSendToTruongNhomSale / IsSendToMkt / IsSendToTruongPhongMkt / IsSendToTruongNhomMkt / IsSendToThuNgan / IsSendToAdminChiNhanh / IsSendToAdminTong` + `SendToListIdUser` (danh sách user chỉ định).
- Mẫu noti: `WebPatternCode` / `WebPatternCodeNext` + thời điểm gửi `WebSendLaterMinute` / `WebSendBeforMinute` (gửi trước/sau X phút).

## Quan hệ
- **Gọi sang:** NotificationDataContext (SQL), IClientHelper (service cấu hình tác nghiệp/đơn hàng/lịch hẹn để build danh sách sự kiện), ServiceSettings.
- **Dữ liệu (entities):** `NotificationConfig` (1 dòng = 1 quy tắc: loại + sự kiện + mẫu + tập cờ đối tượng nhận).
- **Ripple / đồng bộ:** không tự gửi noti; chỉ là **bảng tra cứu quy tắc** mà service nghiệp vụ đọc lúc runtime để quyết định gửi gì/cho ai → rồi gọi `NotificationsController.Create`.

## Lỗi đã biết / lưu ý kỹ thuật
- `NotificationConfigController.cs:59-66` `SaveTrangThaiLichHen` — key quyền bị comment, chỉ còn `[SBAuthorize]` rỗng → mọi user login lưu được, lỏng hơn 3 nút Save kia (cần HT-AD/DV-AD-TONG/HT-CSKH).
- `NotificationConfigController.cs:96-103` `TimTheoDonViCustom` `[AllowAnonymous]` — không lọc theo đơn vị/token; nên giới hạn hoặc gỡ nếu là endpoint test.
- `NotificationConfigController.cs:67-73` `DeleteByIdUnit` dùng HTTP GET cho xóa hàng loạt theo loại (không idempotent-safe, dễ trigger ngoài ý muốn).
- `NotificationConfigService.cs:60-105` `UpdateAsync` MarkDirty thủ công ~30 field — thêm field mới mà quên MarkDirty thì update sẽ bỏ sót field đó (lỗi âm thầm).
