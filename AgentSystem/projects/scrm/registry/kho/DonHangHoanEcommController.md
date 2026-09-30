---
controller: DonHangHoanEcommController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI (webhook BE-to-BE: ecommerce_connect → SCRM)"
muc_dich: "Cửa nhận webhook đơn hàng HOÀN (return/refund) từ TMĐT (Shopee/TikTok/Lazada) do ecommerce_connect đẩy sang: tạo mới / cập-nhật-trạng-thái / cập-nhật-chi-tiết đơn hoàn vào DonHangHoan. Đây là nguồn populate dữ liệu cho màn Đơn hàng hoàn (DonHangHoanController)."
alias: ["webhook đơn hoàn", "nhận đơn hoàn TMĐT", "return webhook", "đồng bộ đơn hoàn shopee", "RETURN_CREATE", "RETURN_UPDATE"]
auth: anonymous
state_machine: []
deprecated: false
entities: [DonHangHoan, DonHangHoanSanPham]
stored_procs: []
depends:
  - "IDonHangHoanService (OrderLogistic.Service/Generated/Ecommerce/DonHangHoanService.cs) — NhanDuLieuDonHangHoanTMDT route theo DataType"
cross_module_in:
  - "ecommerce_connect (POST NhanDuLieuDonHangHoanTMDT — webhook ECOMM→SCRM với DataType RETURN_CREATE/RETURN_UPDATE/RETURN_UPDATE_DETAIL)"
cross_module_out:
  - "DonHangHoanController (data sau khi insert/update phục vụ màn Đơn hàng hoàn — list, tạo HBBTL)"
---

# DonHangHoanEcommController — Webhook đơn hàng hoàn TMĐT

> Controller `partial`, namespace `OrderLogistic.API.Controllers.Ecommerce`. Chỉ có 1 endpoint webhook. Đây là **cạnh VÀO** populate `DonHangHoan` mà node `DonHangHoanController.md` mô tả là "ngoài controller" — chính là controller này.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận dữ liệu đơn hoàn TMĐT | (không UI) | POST `api/DonHangHoanEcomm/NhanDuLieuDonHangHoanTMDT` | (không) — `[SBAuthorize]` BỊ COMMENT | anonymous | webhook ECOMM→SCRM | Route theo `DataType`: RETURN_CREATE→tạo đơn hoàn mới + items; RETURN_UPDATE→chỉ update status fields (Items=null OK); RETURN_UPDATE_DETAIL→update status + replace items. Trả `ResSendReturnDataToCrm` | EF | Endpoint MỞ HOÀN TOÀN — `[SBAuthorize]` comment (DonHangHoanEcommController.cs:29) vì ECOMM gửi `Authorization: Bearer` mà SBAuthorize chỉ đọc cookie/SystemAuthorization → tránh 401 |

## Endpoint nội bộ (private / service-to-service)
- [anonymous] **ecommerce_connect → `NhanDuLieuDonHangHoanTMDT`**: webhook BE-to-BE. Đây là nguồn duy nhất tạo/đồng bộ entity `DonHangHoan` từ phía hệ thống. Sau khi insert/update, dữ liệu phục vụ màn Đơn hàng hoàn (list, map SP, tạo HBBTL) ở `DonHangHoanController`.

## Quan hệ
- **Gọi sang:** `IDonHangHoanService.NhanDuLieuDonHangHoanTMDT` (cùng service dùng cho màn Đơn hàng hoàn — `DonHangHoanService.cs:105`).
- **Dữ liệu (entities):** `DonHangHoan` (header đơn hoàn: TrangThai, LoaiHoan, MaDonHoanEcomm, EcommerceId, logistics fields), `DonHangHoanSanPham` (line items).
- **Ripple / đồng bộ:** chỉ persist đơn hoàn; KHÔNG đụng tồn kho/kế toán. Ripple tồn/công nợ chỉ xảy ra sau này khi user tạo HBBTL (PhieuBanTraLai) từ màn Đơn hàng hoàn.

## Luồng xuyên module (return webhook)
```
[sàn Shopee/TikTok/Lazada — yêu cầu trả/hoàn] → ecommerce_connect
  → POST NhanDuLieuDonHangHoanTMDT (DataType RETURN_CREATE) → insert DonHangHoan + DonHangHoanSanPham
  → (RETURN_UPDATE) → update status; (RETURN_UPDATE_DETAIL) → update status + replace items
  → dữ liệu hiển thị ở DonHangHoanController (list 4 tab) → user tạo HBBTL khi hàng về kho
```

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangHoanEcommController.cs:29` `[SBAuthorize]` BỊ COMMENT → webhook không xác thực app-level (giống `NhanDuLieuDonHangTMDT` đơn gốc); dựa mạng nội bộ. Hardening tương lai: ECOMM gửi header `SystemAuthorization` (service token).
- Logic route 3 DataType nằm ở `DonHangHoanService.cs:163/184/193`; RETURN_UPDATE cho phép `Items=null` (chỉ cập status) — đừng nhầm là thiếu data.
- Side-effect tồn/kế toán KHÔNG ở đây — đơn hoàn chỉ là dữ liệu cho tới khi tạo HBBTL bên Accounting.
