---
controller: CauHinhController
module: ke-toan
service: Invoice
man_hinh_ui: "Cấu hình HĐĐT > tab Thiết lập chung: bật/tắt xuất HĐ tự động, chọn trạng thái đơn được phép xuất HĐ, thông tin khách hàng mặc định; một phần endpoint là service-to-service (Order/OrderLogistic gọi sang lấy cấu hình)"
muc_dich: "Lưu/đọc cấu hình HĐĐT cấp đơn vị: quyền-được-phát-hành theo loại đơn (Order/OrderLogistic), danh sách trạng thái đơn cho phép xuất HĐ, thông tin KH mặc định; cung cấp cấu hình cho service khác"
alias: ["cấu hình hóa đơn", "thiết lập HĐĐT", "cấu hình xuất hóa đơn", "CauHinh Invoice"]
auth: mixed   # SBAuthorize (UI) + PrivateSBAuthorize (internal Order/OrderLogistic)
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends:
  - ICauHinhService          # đọc/ghi CauHinh (JsonSetting / JsonSetting_SuperAdmin)
cross_module_in:
  - Order (PrivateGetConfig, GetCauHinhInitPrivate, GetListStatusAllowPublishInvoicePrivate, GetCustomerInfoDefault)
  - OrderLogistic (tương tự — lấy cấu hình HĐ khi quyết định phát hành tự động)
cross_module_out:
  - Order/OrderLogistic — cấu hình "xuất HĐ tự động sau X phút" được đồng bộ sang (CapNhatOrder/CapNhatOrderLogistic publish message)
---

# CauHinhController — Cấu hình HĐĐT cấp đơn vị

> Cấu hình lưu dạng **JSON** trong `CauHinh.JsonSetting` (1 bản ghi / đơn vị) — không phải cột riêng từng setting.
> Trộn 2 nhóm endpoint: **UI** (`SBAuthorize`, dùng `CurrentUser`) và **internal** (`PrivateSBAuthorize`, Order/OrderLogistic truyền `idDonVi` trực tiếp). Cùng logic nhưng nguồn `DonViId` khác (token vs tham số).
> `IsAllowPublishInvoice` là gate nghiệp vụ quan trọng: quyết định nút "Xuất hóa đơn" có hiện ở màn đơn hàng không.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình HĐĐT | (load tab cấu hình) | `GET Get` | không key (`[SBAuthorize]`) | public | tab cấu hình | `_service.GetAsync(CurrentUser)` — đọc JsonSetting đơn vị | EF `CauHinh` | — |
| 2 | Lưu cấu hình init | Nút "Lưu" tab thiết lập | `POST SaveCauHinhInit` | CauHinh_Invoice_LuuCauHinhKhachHang (DV-AD-TONG) | mixed | — | Validate; `_service.SaveCauHinhInit(inSetting, user)` — ghi JsonSetting | EF | chỉ DV-AD-TONG mới lưu |
| 3 | Lấy cấu hình init | (load form thiết lập) | `GET GetCauHinhInit` | không key | public | — | `_service.GetCauHinhInit(CurrentUser)` | EF | — |
| 4 | Kiểm tra được phép xuất HĐ | (ẩn/hiện nút Xuất HĐ ở màn đơn) | `POST IsAllowPublishInvoice/{type:int}` | không key | public | màn Order/OrderLogistic | Gate: có tiện ích Misa/Viettel? không → false. Admin-CN hoặc kế toán → true. Ngược lại đọc cấu hình: type=Order && IsShowAtDonHangOffline, hoặc type=OrderLogistic && IsShowAtDonHangOnline | EF | logic nhiều nhánh — xem mục Quy tắc |
| 5 | Lấy danh sách trạng thái cho phép xuất HĐ | (config trạng thái + filter màn đơn) | `POST GetListStatusAllowPublishInvoice` | không key | public | — | `_service.GetListStatusAllowPublishInvoice(CurrentUser.DonViId)` — đọc `ListTrangThaiDuocPhepXuatHoaDon` từ JsonSetting | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Order/OrderLogistic → `POST PrivateGetConfig`** (`PrivateSBAuthorize`): lấy cấu hình đầy đủ (giống Get nhưng cho service khác).
- [internal] **`POST GetCustomerInfoDefault/{id:Guid}`**: lấy thông tin KH mặc định theo idDonVi (điền sẵn người mua khi phát hành).
- [internal] **`POST GetCauHinhInitPrivate/{idDonVi:Guid}`**: như GetCauHinhInit nhưng truyền idDonVi (đóng gói thành `new UserLogin{DonViId=idDonVi}`).
- [internal] **`POST GetListStatusAllowPublishInvoicePrivate/{idDonVi:Guid}`**: danh sách trạng thái cho phép xuất HĐ theo idDonVi (Order dùng để biết khi nào auto-publish).

## Quy tắc nghiệp vụ (gating — `IsAllowPublishInvoice`)
1. **Không có tiện ích nào (Misa & Viettel đều off)** → `false` (chặn).
2. **Có tiện ích + là Admin chi nhánh HOẶC có role Kế toán** → `true` (bỏ qua cấu hình đơn vị).
3. **Có tiện ích nhưng không phải admin/kế toán** → đọc cấu hình đơn vị (`GetAsync(DonViId)`):
   - cấu hình null → `false`
   - `typeOrder == Order(offline) && IsShowAtDonHangOffline == true` → `true`
   - `typeOrder == OrderLogistic(online) && IsShowAtDonHangOnline == true` → `true`
   - còn lại → `false`
- **`GetListStatusAllowPublishInvoice`:** trả `ListTrangThaiDuocPhepXuatHoaDon` (rỗng nếu chưa cấu hình) — Order/OrderLogistic chỉ auto-publish khi đơn ở 1 trong các trạng thái này.

## Quan hệ
- **Gọi sang:** `ICauHinhService` → `IRepository<CauHinh>` (JsonSetting). Một số luồng publish message (`CapNhatOrder`/`CapNhatOrderLogistic`) đẩy cấu hình "xuất HĐ tự động" sang Order/OrderLogistic.
- **Dữ liệu (entities):** `CauHinh` (IdDonVi, JsonSetting, JsonSetting_SuperAdmin) — 1 bản ghi/đơn vị, mọi setting nén trong JSON.
- **Ripple / đồng bộ:** đổi cấu hình auto-publish → Order/OrderLogistic nhận message để biết "sau X phút tự xuất HĐ" / "trạng thái nào được xuất".

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `CauHinhService.cs:311-352` (`IsAllowPublishInvoice`) — có `return MethodResult<bool>.ResultWithData(false)` cuối hàm là **dead code** (mọi nhánh đã return ở trên).
- Cấu hình là JSON 1 cột → đổi schema setting không cần migration nhưng khó query/validate field lẻ.
- Endpoint UI (#1,#3,#4,#5) đều `[SBAuthorize]` trống (không key) — chỉ cần đăng nhập; chỉ `SaveCauHinhInit` mới gắn key + role DV-AD-TONG.
- Cặp endpoint UI/internal đôi (`GetCauHinhInit` vs `GetCauHinhInitPrivate`, `GetListStatus...` vs `...Private`) chỉ khác nguồn `DonViId` (token vs param) → logic trùng lặp, dễ lệch nếu sửa 1 bên.
