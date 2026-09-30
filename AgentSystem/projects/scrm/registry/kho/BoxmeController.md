---
controller: BoxmeController
module: kho
service: OrderLogistic
man_hinh_ui: "không có UI — controller bị comment toàn bộ (không build, không endpoint)"
muc_dich: "ĐVVC Boxme — file controller tồn tại nhưng TOÀN BỘ class bị comment (dead code). Không expose endpoint nào. Giữ làm scaffold tham chiếu (GetThongTinKetNoiDonVi proxy sang DeliveryUnitConnectService) nếu sau này bật lại Boxme."
alias: ["boxme", "đối tác boxme", "giao hàng boxme"]
auth: internal
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends:
  - "(nếu bật lại) IDeliveryUnitConnectService.GetThongTinKetNoiDonVi → proxy delivery_connect"
cross_module_in: []
cross_module_out: []
---

# BoxmeController — ĐVVC Boxme (DEAD — toàn bộ comment)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — (không có endpoint hoạt động) | — | internal | class bị comment | Không route nào — toàn bộ `[ApiController]/[Route]/class` đều nằm trong block comment | — | `deprecated: true` — file chỉ còn namespace + class comment (`BoxmeController.cs:13-45`) |

## Quan hệ
- **Gọi sang:** (chỉ trong code comment) `IDeliveryUnitConnectService.GetThongTinKetNoiDonVi` với `ShippingUnitCode = EnumShippingUnitCode.Boxme` — KHÔNG active.
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- **DEAD CODE**: toàn bộ class `BoxmeController` bị comment (`//`), không build thành controller, không có endpoint nào hoạt động. `BoxmeController.cs` chỉ còn `using` + `namespace` + block comment.
- Nếu cần bật lại Boxme: uncomment, controller dự kiến có 1 endpoint POST `GetThongTinKetNoiDonVi` proxy qua `DeliveryUnitConnectService` (giống `DeliveryUnitConnectController.GetThongTinKetNoiDonVi` nhưng fix cứng ShippingUnitCode = Boxme).
- `deprecated: true` → AI bỏ qua nhanh khi quét luồng đăng đơn/kết nối; Boxme hiện không khả dụng qua controller riêng.
