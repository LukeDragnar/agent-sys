---
controller: DonHangController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > màn Đơn hàng TMĐT (Shopee/TikTok/Lazada): nút 'Lấy đơn từ sàn', 'In đơn', 'Đăng đơn (PostOrder)', tab 'Đơn lỗi đồng bộ' (tạo lại / xóa), xuất hóa đơn. Endpoint NhanDuLieuDonHangTMDT là webhook BE-to-BE (không có UI)."
muc_dich: "Cầu nối đơn hàng TMĐT giữa ecommerce_connect và OrderLogistic: nhận webhook tạo/cập-nhật/cập-nhật-thanh-toán đơn sàn (NhanDuLieuDonHangTMDT) để persist DonHangLogistic; và proxy các thao tác chủ động từ UI sang sàn (kéo đơn, in đơn, đăng đơn, lấy hóa đơn) + quản lý đơn lỗi đồng bộ."
alias: ["đơn hàng sàn", "đơn TMĐT", "đơn shopee", "đơn tiktok", "lấy đơn từ sàn", "đăng đơn lên sàn", "in đơn sàn", "đơn lỗi đồng bộ", "post order", "webhook đơn TMĐT"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonHangLogistic, DonHangLogisticSanPham, DonHangLogisticCombo, DonHangLogisticChiTietTMDT, DonHangThongTinThue, DonHangDangDon]
stored_procs: []
depends:
  - "IDonHangService (OrderLogistic.Service/Generated/Ecommerce/DonHangService.cs)"
  - "IEcommerceConnectService (proxy GetOrderFromEcomm / PrintOrder / PostOrder / GetOrderInvoice sang ecommerce_connect)"
  - "ICoreService (DonVi_GetDonViById — validate đơn vị khi nhận webhook)"
  - "IWarehouseProductService, IDonHangKeToanService, ESDocument index (qua TaoDonHangEcommerce)"
cross_module_in:
  - "ecommerce_connect (POST NhanDuLieuDonHangTMDT — webhook ECOMM→SCRM gửi DataType CREATE/UPDATE/UPDATEPAYMENT để tạo/đồng bộ DonHangLogistic)"
cross_module_out:
  - "ecommerce_connect (CRM_GetOrderFromEcomm→GetOrderFromEcomm; CRM_GetOrderPrintFromEcomm→PrintOrder; PostOrder→PostOrder sàn; CRM_GetOrderInvoice→lấy HĐ sàn)"
  - "ElasticSearch (ESIndexDonHangLogistic sau khi đánh dấu DonHangIsInDon=true ở in đơn)"
  - "WarehouseProduct / Accounting / Notification (ripple khi tạo đơn ecommerce qua TaoDonHangEcommerce trong NhanDuLieuDonHangTMDT)"
---

# DonHangController (OrderLogistic) — Đơn hàng TMĐT (Ecommerce)

> Trùng tên controller `DonHangController` với module khác → node này dùng suffix `_OrderLogistic`. Controller `partial` (`public partial class DonHangController`), namespace `OrderLogistic.API.Controllers.Ecommerce`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận dữ liệu đơn TMĐT (webhook) | (không UI) | POST `api/DonHang/NhanDuLieuDonHangTMDT` | (không) — `[SBAuthorize]` BỊ COMMENT | anonymous | webhook ECOMM→SCRM | Route theo `DataType`: CREATE→TaoDonHangEcommerce (insert DonHangLogistic + line + combo + thuế, ripple kho/KT/ES); UPDATE→CapNhatDonHangEcommerce (cập trạng thái); UPDATEPAYMENT→CapNhatGiaoDichDonHangEcommerce (cập phí giao dịch). Validate IdDonVi/IdChiNhanh + đơn vị tồn tại trước | EF | Endpoint MỞ HOÀN TOÀN — `[SBAuthorize]` comment (DonHangController.cs:34); chỉ dựa hạ tầng mạng nội bộ. Sàn khác CREATE/UPDATE/UPDATEPAYMENT → "Loại hành động không hợp lệ" |
| 2 | Lấy đơn từ sàn | "Lấy đơn từ sàn" | POST `api/DonHang/CRM_GetOrderFromEcomm` | `[SBAuthorize]` (mở — chỉ cần login) | public | chọn sàn/gian hàng/khoảng tg | Proxy sang ecommerce_connect `GetOrderFromEcomm` (đồng bộ thủ công đơn còn thiếu). Insert đơn nằm bên ECOMM (qua webhook #1) | proxy ECOMM | - |
| 3 | In đơn sàn | "In đơn" | POST `api/DonHang/CRM_GetOrderPrintFromEcomm` | `[SBAuthorize]` | public | tick ≥1 đơn | Lọc đơn đúng `EcommerceCode` (Shopee/TikTok) → proxy `PrintOrder`; đơn khác sàn trả fail kèm message. Đơn in thành công set `DonHangIsInDon=true` + re-index ES | EF + proxy ECOMM | DonHangService.cs:2854 chỉ map SHOPEE/TIKTOK cho in đơn; sàn khác coi là fail "không phải đơn của {EcommerceCode}" |
| 4 | Đăng đơn lên sàn | "Đăng đơn" | POST `api/DonHang/PostOrder` | `[SBAuthorize]` | public | đơn chưa post | Validate EcommerceCode + PartnerOrderCode; map FROM_TIKTOK→"TIKTOK"/FROM_SHOPEE→"SHOPEE" → proxy `PostOrder` (đẩy đơn/giao vận lên sàn) | proxy ECOMM | DonHangService.cs:2947 chỉ map 2 sàn TikTok/Shopee |
| 5 | Lấy tất cả đơn vị kết nối | (nội bộ dropdown) | POST `api/DonHang/CRM_GetAllListUnitConnect` | `[SBAuthorize]` | public | - | Proxy lấy danh sách UnitConnect (gian hàng kết nối) | proxy ECOMM | - |
| 6 | Lấy hóa đơn sàn | "Xuất/Lấy HĐ" | POST `api/DonHang/CRM_GetOrderInvoice` | `[SBAuthorize]` | public | - | Proxy lấy thông tin hóa đơn của đơn sàn | proxy ECOMM | - |
| 7 | Đơn lỗi đồng bộ: xem | tab "Đơn lỗi" | POST `api/DonHang/CRM_GetOrderCreateError` | `[SBAuthorize]` | public | - | List đơn tạo lỗi (sync error) để xử lý lại | EF | - |
| 8 | Đơn lỗi: tạo lại | "Tạo lại" | POST `api/DonHang/CRM_ReCreateOrderToCrm` | `[SBAuthorize]` | public | có đơn lỗi | Tạo lại đơn vào CRM từ payload lỗi | EF/proxy | - |
| 9 | Đơn lỗi: xóa | "Xóa" | POST `api/DonHang/CRM_DeleteOrderSyncError` | `[SBAuthorize]` | public | có đơn lỗi | Xóa bản ghi đơn lỗi đồng bộ | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [anonymous] **ecommerce_connect → `NhanDuLieuDonHangTMDT`**: webhook BE-to-BE. ECOMM gửi `Authorization: Bearer` nhưng `[SBAuthorize]` chỉ đọc cookie/SystemAuthorization → đã comment để tránh 401 (giống NhanDuLieuDonHangHoanTMDT). Đây là CỬA tạo đơn TMĐT thật, mọi ripple kho/KT bắt nguồn từ đây.

## Quan hệ
- **Gọi sang:** `IEcommerceConnectService` cho toàn bộ thao tác chủ động (kéo/in/đăng đơn/HĐ); `ICoreService.DonVi_GetDonViById` validate đơn vị; `TaoDonHangEcommerce` (trong service) đụng WarehouseProduct/Accounting/ES khi tạo đơn.
- **Dữ liệu (entities):** `DonHangLogistic` (header đơn: MaDon, MaDonEcomm, EcommerceId, IdDonVi, DonHangIsInDon), `DonHangLogisticSanPham`/`Combo`/`ChiTietTMDT`/`ThongTinThue` (line + thuế), `DonHangDangDon`.
- **Ripple / đồng bộ:** webhook CREATE → insert DonHangLogistic + ripple kho/KT/notification/ES (logic trong TaoDonHangEcommerce); in đơn → set DonHangIsInDon + re-index ES; đăng/in/HĐ proxy ngược sang sàn qua ecommerce_connect.

## Luồng xuyên module (đơn TMĐT end-to-end)
```
[sàn Shopee/TikTok] → ecommerce_connect (webhook/poll)
  → POST NhanDuLieuDonHangTMDT (DataType CREATE) → TaoDonHangEcommerce → insert DonHangLogistic (+ ripple kho/KT/ES)
  → DataType UPDATE/UPDATEPAYMENT → cập trạng thái / phí giao dịch đơn
  → user UI: CRM_GetOrderFromEcomm (kéo đơn thiếu) / CRM_GetOrderPrintFromEcomm (in → DonHangIsInDon) / PostOrder (đẩy đơn lên sàn) / CRM_GetOrderInvoice (lấy HĐ)
  → đơn tạo lỗi → tab Đơn lỗi: CRM_GetOrderCreateError → CRM_ReCreateOrderToCrm / CRM_DeleteOrderSyncError
```

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangController.cs:34` `[SBAuthorize]` của `NhanDuLieuDonHangTMDT` BỊ COMMENT → endpoint webhook không xác thực ở app-level; bảo mật dựa hoàn toàn vào mạng nội bộ. Hardening tương lai: dùng header `SystemAuthorization` (service token).
- Map sàn cứng SHOPEE/TIKTOK ở in đơn (`DonHangService.cs:2854`) và đăng đơn (`:2947`); LAZADA và sàn khác không nằm trong nhánh map → in đơn coi là fail, post order không set EcommerceCode.
- Side-effect tạo đơn (insert + ripple) nằm trong `TaoDonHangEcommerce` của service, KHÔNG ở controller — triage "đơn không vào" phải xem service + log `NhanDuLieuDonHangTMDT`.
- Các endpoint UI dùng `[SBAuthorize]` mở (chỉ cần login), không phân quyền key per-action; kiểm soát qua tiện ích `TI_OrderLogistic`.
