---
controller: KhachHangCareHistoryController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang › quanly-khachhang-detail › tab 'Lịch sử chăm sóc' (lichsu-chamsoc) — thêm/sửa/xóa & xem lịch sử chăm sóc KH, hẹn ngày chăm sóc tiếp theo"
muc_dich: "CRUD lịch sử chăm sóc khách hàng (kế thừa BaseDanhMucController) + job gửi SMS/Zalo nhắc lịch chăm sóc theo NgayChamSocTiepTheo."
alias: ["Lịch sử chăm sóc", "Chăm sóc KH", "Care history", "Nhắc lịch chăm sóc", "KhachHangCareHistory"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangCareHistory]
stored_procs: []
depends:
  - IKhachHangCareHistoryService
  - BaseDanhMucController          # TimTheoId / LayTatCa / ThemMoi / CapNhatThongTin kế thừa
  - "SMS service (api/AccountSms*, api/NotificationConfig — gửi SMS/Zalo nhắc lịch)"
  - "Core service (api/Common/GetOrgById — thông tin đơn vị)"
cross_module_in: []
cross_module_out:
  - "SMS/Zalo service (gửi tin nhắn nhắc lịch chăm sóc qua job GuiSmsNhacLichChamSoc)"
---

# KhachHangCareHistoryController — Lịch sử chăm sóc khách hàng

> Kế thừa `BaseDanhMucController<KhachHangCareHistory, KhachHangCareHistoryDto>` (có sẵn `TimTheoId/LayTatCa/ThemMoi/CapNhatThongTin` ở `[SBAuthorize]` login-only). Controller bổ sung CRUD có map DTO + role + 1 **job nhắc lịch SMS** (`TestJob` → `GuiSmsNhacLichChamSoc`). Phần "Quy tắc nghiệp vụ" mô tả gating của job SMS để trace.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới lịch chăm sóc | Nút "Thêm mới" (lichsu-chamsoc) | `POST CreateItem` | KhachHangCareHistory_CreateItem (AD-TONG/AD-CN/AD-MCN/QLDV/NV) | public | — | ModelState invalid → lỗi; map `Dto→Entity`; `ThemMoiAsync(model, CurrentUser)`. | EF (`KhachHangCareHistory`) | — |
| 2 | Chi tiết theo id KH | Mở chi tiết/edit | `GET GetByIdKhachHangAsync/{id:Guid}` | KhachHangCareHistory_GetByIdKhachHangAsync (AD-TONG/AD-CN/AD-MCN/QLDV) | public | — | `GetByIdKhachHangAsync(id)`; có data → map `Entity→Dto`, không → trả nguyên item. | EF | Tên gợi "theo id KH" nhưng tham số `id` là khóa bản ghi chăm sóc (cần xác nhận semantics) |
| 3 | Tìm lịch chăm sóc theo điều kiện | Lưới tab "Lịch sử chăm sóc" | `POST TimTheoDieuKien` | KhachHangCareHistory_TimTheoDieuKien (AD-TONG/QLDV/AD-CN/NV/AD-MCN/KT/KT-TONG/HT-AD/SALE*/MKT*) | public | — | `TimTheoDieuKienAsync(gridInfo)`. **Không check ModelState.** | EF | — |
| 4 | Cập nhật lịch chăm sóc | Nút "Lưu" (sửa) | `POST CapNhatAsync` | KhachHangCareHistory_CapNhatAsync (AD-TONG/AD-CN/AD-MCN/QLDV/NV) | public | — | `CapNhatAsync(model)` (nhận entity trực tiếp). | EF | — |
| 5 | Xóa lịch chăm sóc | Nút "Xóa" | `POST Xoa/{id:Guid}` | KhachHangCareHistory_Xoa (AD-TONG/AD-CN/AD-MCN/QLDV/NV) | public | — | `DeleteAsync(id)`. | EF | — |
| 6 | Chạy job nhắc lịch (test/trigger) | (không UI — endpoint test/cron) | `GET TestJob` | (không attribute) | anonymous | — | `GuiSmsNhacLichChamSoc()` → trả `Ok(...)` raw (không qua ResponseResult). | EF + SMS/Core | **KHÔNG có auth** — endpoint kích hoạt gửi SMS hàng loạt public, ai cũng gọi được |
| 7 | (Kế thừa Base) TimTheoId / LayTatCa / ThemMoi / CapNhatThongTin | — | `GET TimTheoId/{id}` · `GET LayTatCa` · `POST ThemMoi` · `POST CapNhatThongTin` | `[SBAuthorize]` (login-only) | public | — | Hành vi mặc định BaseDanhMucController; FE dùng `CreateItem`/`CapNhatAsync` (#1/#4) thay cho Base. | EF | Endpoint Base không có permission key riêng |

## Quy tắc nghiệp vụ (gating — job gửi SMS/Zalo nhắc lịch `GuiSmsNhacLichChamSoc`)
> Blueprint để trace job nhắc lịch (`KhachHangCareHistoryService.cs:177+`):

1. **Có cấu hình thông báo loại NLCS?** Lấy `NotificationConfig` theo `ActionCode = "NLCS"`. Rỗng → không gửi. Distinct theo `IdDonVi`.
2. **Đơn vị có tài khoản SMS?** `api/AccountSms/TimTheoListId` cho list `IdDonVi`; không có data → trả lỗi "Không tìm thấy cấu hình đơn vị gửi sms.".
3. **Mỗi đơn vị, tài khoản SMS đang bật?** `IsUse == false && IsUseZalo == false` → `continue` (bỏ qua đơn vị).
4. **Số ngày nhắc trước:** `valueMinute = SmsSendBeforMinute` (làm tròn lên `Math.Ceiling`). `numberOfDays = ceil(valueMinute / (24*60))`. Nếu `valueMinute == 0` → nhánh nhắc đúng ngày (`NgayChamSocTiepTheo.Date == DateTime.Now.Date`, xem code `:311`).
5. **Lọc bản ghi cần nhắc:** `KhachHangCareHistory` của đơn vị, `NgayChamSocTiepTheo.HasValue` **AND** `NgayChamSocTiepTheo.Date == DateTime.Now.AddDays(numberOfDays).Date` **AND** `IsProcessing == false`. Không có bản ghi nào → `continue`.
6. **Gửi:** lấy SĐT KH (`lstSdtKh`) theo `IdKhachHang` của các bản ghi → bơm param SMS (`NGAY_CHAM_SOC`, `date` = `NgayChamSocTiepTheo` định dạng dd/MM/yyyy) → gửi SMS/Zalo.

> ⚠️ Phần gọi `NotificationConfig/TimTheoDonViCustom` cũ đã **bị comment** (`:179-187`, "Tạm comment tối ưu sang proc") — thay bằng `GetDsNotiConfigTheoDieuKien(ActionCode=NLCS)`.

## Quan hệ
- **Gọi sang:** `IKhachHangCareHistoryService`; SMS service (`AccountSms`, `AccountSmsAgency`, `NotificationConfig`); Core (`Common/GetOrgById`).
- **Dữ liệu (entities):** `KhachHangCareHistory` (EF) — field then chốt cho job: `NgayChamSocTiepTheo`, `IsProcessing`, `IdKhachHang`, `IdDonVi`.
- **Ripple / đồng bộ:** job nhắc lịch → gửi SMS/Zalo qua SMS service (ripple ra ngoài Customer).

## Luồng xuyên module
1. **Nhắc lịch chăm sóc (cron/job):** trigger `GuiSmsNhacLichChamSoc` → đọc cấu hình NLCS + tài khoản SMS đơn vị → lọc `KhachHangCareHistory` tới hạn (theo `NgayChamSocTiepTheo` & `SmsSendBeforMinute`) → gọi SMS/Zalo service gửi tin nhắc KH.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhachHangCareHistoryController.cs:87-91` (`TestJob`) — **không có attribute auth nào** (không `[SBAuthorize]`/`[PrivateSBAuthorize]`) → public; endpoint này gọi `GuiSmsNhacLichChamSoc` gửi SMS hàng loạt → rủi ro bị lạm dụng/spam nếu route lộ ra ngoài.
- `KhachHangCareHistoryController.cs:64-69` (`TimTheoDieuKien`) — bỏ check `ModelState.IsValid`.
- `KhachHangCareHistoryService.cs:179-187` — nhánh lấy cấu hình SMS cũ đã comment ("Tạm comment tối ưu sang proc", tamdc 20251202) → nếu proc mới chưa cover hết case có thể sót đơn vị.
- `KhachHangCareHistoryService.cs:236` — điều kiện ngày nhắc dùng `DateTime.Now.AddDays(numberOfDays).Date` so sánh **đúng 1 ngày** (==), không phải khoảng → lệch ngày (lỗi/job miss 1 nhịp) thì KH đó không được nhắc lại.
