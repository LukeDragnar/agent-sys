---
controller: BangQRCodeController
module: cua-hang
service: PreOrder
man_hinh_ui: "Cửa hàng / PreOrder — cấu hình mã QR đặt trước theo chỗ ngồi/bàn (GetOrCreate khi gán QR cho bàn, list theo chi nhánh, xóa QR)"
muc_dich: "Quản lý mã QR PreOrder gắn với (đơn vị + chi nhánh + chỗ ngồi): tạo-hoặc-lấy mã QR cho 1 bàn, list theo chi nhánh, xóa mã."
alias: ["mã QR đặt trước", "QR pre-order", "QR bàn", "gán QR cho chỗ ngồi", "mã QR gọi món"]
auth: public
state_machine: []
deprecated: false
entities: [BangQRCode]
stored_procs: []
depends: [PreOrder, CommonCache (GetCoCauDonViTuCache — tên đơn vị/chi nhánh)]
cross_module_in: [PublicPreOrderController — public/GetSanPham & public/Save quét QR khách scan gọi GetByMaQR (qua service)]
cross_module_out: [PublicPreOrderController dùng BangQRCode để resolve đơn vị/chi nhánh/chỗ ngồi khi khách đặt trước]
---

# BangQRCodeController — Quản lý mã QR PreOrder

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo hoặc lấy mã QR cho 1 chỗ ngồi | "Tạo mã QR" / lấy QR bàn | POST `api/BangQRCode/GetOrCreate` | `BangQRCode_PreOrder_TaoMaQR` | public | gán QR cho bàn | Tìm BangQRCode theo (DonViId user + IdChiNhanh + IdChoNgoi); nếu có → trả bản cũ; nếu chưa → enrich tên ĐV/CN từ cache, sinh `MaQR` unique 8 ký tự, insert; trả `BangQRCodeResponseDto` | EF + cache CoCau | Idempotent theo (DV+CN+ChoNgoi): tránh tạo trùng QR cho cùng bàn |
| 2 | Xem danh sách mã QR theo chi nhánh | list QR | GET `api/BangQRCode/GetByChiNhanh?idChiNhanh=` | `BangQRCode_PreOrder_XemTheoChiNhanh` | public | màn list | List tất cả QR theo (DonViId user + idChiNhanh); project sang `BangQRCodeResponseDto` (không phân trang) | EF | - |
| 3 | Xóa mã QR | "Xóa" (confirm) | POST `api/BangQRCode/Delete/{id:Guid}` | `BangQRCode_PreOrder_XoaMaQR` | public | per-row | Tìm theo (Id + DonViId user); không thấy → lỗi "Không tìm thấy mã QR!"; xóa **cứng** (`DeleteAsync`) | EF | Xóa cứng — QR cũ khách đã in/dán sẽ chết (GetByMaQR trả null → public trả "đã bị xóa") |

## Quan hệ
- **Gọi sang:** `_cacheService.GetCoCauDonViTuCache(DonViId)` để lấy `TenDonVi` + `TenChiNhanh` khi tạo mới (tên chỗ ngồi/khu vực lấy từ request DTO).
- **Dữ liệu (entities):** `BangQRCode` (Id, IdDonVi, IdChiNhanh, IdChoNgoi, MaQR, TenDonVi, TenChiNhanh, TenChoNgoi, TenKhuVuc).
- **Ripple / đồng bộ:** `GetByMaQR(maQR)` là entry point cho luồng public — khách quét QR ở bàn → `PublicPreOrderController` resolve menu + lưu đơn đặt trước theo QR này.

## Lỗi đã biết / lưu ý kỹ thuật
- **Sinh MaQR**: 8 ký tự [A-Z0-9], retry kiểm tra trùng DB, quá 10 lần ném "Không thể sinh mã QR unique!" (`BangQRCodeService:138`). Không có prefix → mã thuần ngẫu nhiên.
- **Tất cả query dùng `TableWithNolock()`** → đọc dirty; với GetOrCreate có khe đua nếu 2 request đồng thời cùng (DV+CN+ChoNgoi) có thể tạo 2 QR (không có lock/unique constraint ở tầng app).
- **Xóa cứng** (không soft-delete) — khác `DonHangPre` (dùng `IsDelete`); QR đã in mất hiệu lực ngay.
- Code thật: BE `PreOrder/PreOrder.API/Controllers/BangQRCodeController.cs`, service `PreOrder/PreOrder.Service/Generated/BangQRCodeService.cs`, entity `PreOrder.DataContext/Entity/BangQRCode.cs`.
