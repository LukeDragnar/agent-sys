---
controller: ReturnsController
module: kho
service: OrderLogistic
man_hinh_ui: "Operations Center / Hoàn TMĐT - Xử lý — màn xử lý đơn hoàn sàn TMĐT (Shopee): danh sách đơn hoàn, chi tiết, đề xuất/chấp nhận/xác nhận hoàn, khiếu nại (dispute), upload bằng chứng, tracking reverse logistics."
muc_dich: "Proxy controller của SCRM cho ~15 Shopee Returns API: forward mỗi action sang ECOMM (/Shopee/Returns/<Action>) qua IReturnsService để seller xử lý đơn hoàn/refund/dispute sàn TMĐT."
alias: ["hoàn tmđt", "xử lý đơn hoàn shopee", "returns shopee", "refund sàn", "dispute hoàn", "đơn hoàn tmđt", "reverse logistics"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangHoan]
stored_procs: []
depends:
  - "IReturnsService (proxy sang ECOMM Shopee Returns; tự check IdDonVi == userLogin.DonViId)"
cross_module_in:
  - "FE Operations Center / Hoàn TMĐT (mọi action seller bấm)"
cross_module_out:
  - "ECOMM service (/Shopee/Returns/<Action>) → Shopee Open API"
---

# ReturnsController — Xử lý hoàn TMĐT (proxy Shopee Returns)

> Proxy thuần: mỗi endpoint forward 1:1 sang ECOMM → Shopee Open API qua `IReturnsService`. Chỉ support EcommerceId == 1 (Shopee); TikTok/Lazada extend sau. Auth từng method bằng `[SBAuthorize]`; service tự check `IdDonVi == userLogin.DonViId`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách đơn hoàn | "Tìm kiếm" → grid hoàn | GET `api/Returns/GetReturnList` | `[SBAuthorize]` (TienIch `TI_OrderLogistic`) | public | màn list hoàn | Nhận `ReqGetReturnListSCRM` → cross-check data sàn với SCRM đã sync → trả danh sách | proxy ECOMM Shopee | - |
| 2 | Chi tiết đơn hoàn | (mở chi tiết) | GET `api/Returns/GetReturnDetail?idDonHangHoan=` | `[SBAuthorize]` | public | per-đơn | Trả chi tiết 1 đơn hoàn theo `IdDonHangHoan` SCRM | proxy ECOMM | - |
| 3 | Giải pháp hoàn khả dụng | (mở popup xử lý) | GET `api/Returns/GetAvailableSolutions?idDonHangHoan=` | `[SBAuthorize]` | public | trước khi offer | Trả list giải pháp (return+refund vs refund-only) + min/max amount | proxy ECOMM | - |
| 4 | Xem ảnh bằng chứng đã upload | (tab bằng chứng) | GET `api/Returns/QueryProof?idDonHangHoan=` | `[SBAuthorize]` | public | đơn có dispute | Trả list ảnh seller đã upload làm bằng chứng dispute | proxy ECOMM | - |
| 5 | Tracking reverse logistics | (tab hành trình) | GET `api/Returns/GetReverseTrackingInfo?idDonHangHoan=` | `[SBAuthorize]` | public | đơn đã có vận đơn hoàn | Trả timeline reverse + post-return logistics | proxy ECOMM | - |
| 6 | Lý do khiếu nại + yêu cầu bằng chứng | (popup dispute) | GET `api/Returns/GetReturnDisputeReason?idDonHangHoan=` | `[SBAuthorize]` | public | trước khi dispute | Trả list dispute reasons + evidence requirements | proxy ECOMM | - |
| 7 | Danh sách hãng vận chuyển hoàn | (form seller arrange) | GET `api/Returns/GetShippingCarrier?idDonHangHoan=` | `[SBAuthorize]` | public | chỉ TW/BR + is_seller_arrange | Trả list reverse logistics carriers + template | proxy ECOMM | Chỉ áp cho market TW/BR + seller tự sắp xếp |
| 8 | Đề xuất giải pháp hoàn | "Đề xuất" / "Thương lượng" | POST `api/Returns/Offer` | `[SBAuthorize]` | public | đơn đang thương lượng | Nhận `ReqOfferSCRM` → đề xuất giải pháp hoàn (thương lượng) | proxy ECOMM | - |
| 9 | Chấp nhận đề xuất buyer | "Chấp nhận" | POST `api/Returns/AcceptOffer` | `[SBAuthorize]` | public | có offer buyer | Nhận `ReqReturnSCRM` → chấp nhận đề xuất của buyer | proxy ECOMM | - |
| 10 | Xác nhận hoàn | "Xác nhận hoàn" | POST `api/Returns/Confirm` | `[SBAuthorize]` | public | đã nhận hàng | Nhận `ReqReturnSCRM` → xác nhận đã nhận hàng, OK refund | proxy ECOMM | - |
| 11 | Khiếu nại đơn hoàn | "Khiếu nại" | POST `api/Returns/Dispute` | `[SBAuthorize]` | public | đủ điều kiện dispute | Nhận `ReqDisputeSCRM` → mở dispute | proxy ECOMM | - |
| 12 | Upload bằng chứng dispute | "Tải bằng chứng" | POST `api/Returns/UploadProof` | `[SBAuthorize]` | public | sau ConvertImage | Nhận `ReqUploadProofSCRM` (URL ảnh đã convert) → upload | proxy ECOMM | Cần ConvertImage trước để có URL |
| 13 | Convert ảnh (multipart) | (chọn file ảnh) | POST `api/Returns/ConvertImage` (FromForm: idDonHangHoan, file) | `[SBAuthorize]` | public | trước UploadProof/Dispute | File trống → trả lỗi "File ảnh trống."; else đọc bytes → `_returnsService.ConvertImage(...)` → trả URL Shopee | proxy ECOMM | Doc-comment: service hiện stub NotImplemented — cần implement multipart forward (ReturnsController.cs:167) |
| 14 | Hủy compensation dispute | "Hủy khiếu nại" | POST `api/Returns/CancelDispute` | `[SBAuthorize]` | public | có dispute đang mở | Nhận `ReqReturnSCRM` → huỷ compensation dispute | proxy ECOMM | - |
| 15 | Upload shipping proof | "Tải bằng chứng vận chuyển" | POST `api/Returns/UploadShippingProof` | `[SBAuthorize]` | public | chỉ TW/BR + is_seller_arrange | Nhận `ReqUploadShippingProofSCRM` → upload bằng chứng vận chuyển hoàn | proxy ECOMM | Chỉ áp cho TW/BR + seller arrange |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint private; toàn bộ là proxy public (SBAuthorize) forward sang ECOMM.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **EcommerceId gating**: chỉ xử lý EcommerceId == 1 (Shopee). Sàn khác (TikTok/Lazada) chưa support → service từ chối.
- **Phân quyền dữ liệu**: `IReturnsService` tự kiểm `IdDonVi` của đơn hoàn == `userLogin.DonViId` → seller chỉ thao tác đơn thuộc đơn vị mình.
- **Market gating (TW/BR)**: `GetShippingCarrier` / `UploadShippingProof` chỉ áp cho market TW/BR + `is_seller_arrange` (seller tự sắp xếp vận chuyển hoàn).

## Quan hệ
- **Gọi sang:** `IReturnsService` → ECOMM `/Shopee/Returns/<Action>` → Shopee Open API.
- **Dữ liệu (entities):** tham chiếu `DonHangHoan` (qua `IdDonHangHoan` SCRM); controller không thao tác EF trực tiếp.
- **Ripple / đồng bộ:** trạng thái hoàn/refund/dispute đồng bộ về SCRM qua ECOMM; dữ liệu hiển thị cross-check với data SCRM đã sync.

## Luồng xuyên module
- Seller bấm action (Operations Center) → ReturnsController → IReturnsService → ECOMM → Shopee Open API → Shopee phản hồi → đồng bộ trạng thái đơn hoàn về SCRM.
- Dispute: ConvertImage (lấy URL ảnh) → UploadProof/Dispute → Shopee.

## Lỗi đã biết / lưu ý kỹ thuật
- `ReturnsController.cs:167` `ConvertImage`: doc-comment ghi service hiện stub `NotImplemented` — chưa forward multipart; cần implement trước khi luồng upload bằng chứng dùng được.
- Chỉ support Shopee (EcommerceId == 1); extend TikTok/Lazada sau (ReturnsController.cs:14-15).
- `GetShippingCarrier`/`UploadShippingProof` chỉ dành cho TW/BR + seller arrange → market VN có thể không có dữ liệu.
- Controller là proxy thuần — mọi business logic + phân quyền dữ liệu nằm ở `IReturnsService`/ECOMM, không ở đây.
