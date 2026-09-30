---
controller: DepositOrderDeliveryBatchController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > màn 'Đợt trả cọc' (/deposit-receipt/deposit-order-delivery-batch-manager). Lưới đợt + nút: Tạo đợt (popup edit), Sửa, Xóa, Gán danh sách trả cọc, Xóa danh sách, Cấu hình mẫu SMS/Zalo (popup sms-edit), Khóa đợt (SetSended), Xem thống kê."
muc_dich: "Quản lý 'đợt trả cọc' = tệp phiếu cọc đủ điều kiện được gom theo (sản phẩm × số lượng × ngày × chi nhánh) để chuẩn bị trả cọc/gửi SMS hàng loạt. Gán phiếu cọc vào đợt qua stored proc, gắn mẫu SMS/Zalo, khóa đợt khi chốt."
alias: ["đợt trả cọc", "dot tra coc", "tệp trả cọc", "gán đợt trả cọc", "khóa đợt trả cọc", "đợt giao", "deposit delivery batch", "danh sách trả cọc"]
auth: public
state_machine: [IsSended=false(nháp/sửa-được), IsSended=true(đã-khóa/SetSended)]
deprecated: false
entities: [DepositOrderDeliveryBatch, DepositOrderDeliveryBatchProduct, DepositReceipt, DepositSmsSend, HistoryModifyDepositOrderDeliveryBatch(MongoDB)]
stored_procs: [PRC_SCRM_Deposit_GetAndUpdateIdBatch, PRC_SCRM_Deposit_GetStatsDepositReceiptByBatch]
depends: [CommonService/GetSanPhamByListId, CommonService/DeleteVoucherCodeByIdNguonTao, CommonService/GenVoucherCodeByBatch, SqlSugar/Dapper (SP), EFCore.BulkExtensions (BulkUpdate), MongoDB (audit)]
cross_module_in: []
cross_module_out: [Marketing/Voucher (DeleteVoucherCodeByIdNguonTao + GenVoucherCodeByBatch khi tạo/xóa voucher cho đợt)]
---

# DepositOrderDeliveryBatchController — Đợt trả cọc (gom phiếu cọc → gán mẫu SMS → khóa đợt)

> Đợt trả cọc = "rổ" cấu hình theo danh sách `DepositOrderDeliveryBatchProduct` (mỗi dòng = sản phẩm × số lượng × khoảng ngày cọc × chi nhánh nhận). 2 trạng thái: `IsSended=false` (nháp — sửa/gán/xóa/cấu hình SMS được) và `IsSended=true` (đã khóa qua SetSended — KHÓA CỨNG mọi thao tác ghi). Việc gán phiếu cọc vào đợt KHÔNG làm bằng LINQ mà gọi stored proc `PRC_SCRM_Deposit_GetAndUpdateIdBatch` (tìm + set IdBatch trực tiếp trong DB theo chunk 1000). Mọi sửa đổi audit vào MongoDB `HistoryModifyDepositOrderDeliveryBatch`. Controller hằng `[SBAuthorize(key,..., "DV-AD-TONG, DV-QL-KHO-TONG")]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo đợt | Nút "Tạo đợt" (popup edit) | `POST api/DepositOrderDeliveryBatch/Create` | `DepositOrderDeliveryBatchController_Create` | public | màn đợt trả cọc | Tạo đợt + danh sách SP, mã tự sinh prefix "BAT". Validate: không trùng SP, SL>0. Khởi tạo `IsSended=false`, `IsUseJsonParam=false`. | **EF** | — |
| 2 | Sửa đợt | Nút "Sửa" | `POST api/DepositOrderDeliveryBatch/Update` | `DepositOrderDeliveryBatchController_Update` | public | chỉ khi IsSended=false | Thay TOÀN BỘ danh sách SP (xóa cũ → insert mới). Cấm nếu `IsSended=true`. Log MongoDB. | **EF** + MongoDB | — |
| 3 | Lấy chi tiết | (mở popup edit) | `POST api/DepositOrderDeliveryBatch/GetById/{id:Guid}` | `DepositOrderDeliveryBatchController_GetById` | public | dòng đợt | Trả đợt + SP; enrich mã/tên/giá/ảnh SP từ CommonService; deserialize JsonParam nếu có. | **EF** | — |
| 4 | Xóa đợt | Nút "Xóa" | `POST api/DepositOrderDeliveryBatch/Delete/{id:Guid}` | `DepositOrderDeliveryBatchController_Delete` | public | IsSended=false & chưa gán phiếu & chưa có SMS | Hard delete đợt + SP. Cấm nếu `IsSended=true`, hoặc còn phiếu cọc gán đợt, hoặc đã có lịch sử DepositSmsSend. | **EF** | Mess lỗi "Không tìm thấy dữ liệu cần xóa" dùng cả cho path không-phải-xóa; xóa đợt trước SP trong transaction → nếu lỗi giữa chừng có thể orphan product. |
| 5 | Tìm kiếm / lưới | Lưới đợt + lọc (ngày, keyword, đã khóa) | `POST api/DepositOrderDeliveryBatch/SearchWithConditions` | `DepositOrderDeliveryBatchController_SearchWithConditions` | public | màn đợt | LINQ lọc theo NgayTao, keyword (tên/mã), cờ IsSended; group theo đợt join SP; paging; enrich SP từ CommonService. | **EF** (không SP) | — |
| 6 | Gán danh sách trả cọc | Nút "Gán đợt trả cọc" | `POST api/DepositOrderDeliveryBatch/GanDotTraCoc/{id:Guid}` | `DepositOrderDeliveryBatchController_GanDotTraCoc` | public | IsSended=false | (1) gọi XoaDanhSachTraCoc clear link cũ; (2) theo từng SP, tính delta SL còn thiếu; (3) gọi SP `PRC_SCRM_Deposit_GetAndUpdateIdBatch` theo chunk 1000 → tìm & set IdBatch cho phiếu đủ điều kiện (lọc theo SP/ngày cọc/chi nhánh nhận/TypeObject). Log `GanKhachHangChoDotTraCoc`. | **SP** GetAndUpdateIdBatch | SP-call không bọc transaction → fail giữa chừng để lại trạng thái dở; SP trả 0 → break im lặng. |
| 7 | Xóa danh sách trả cọc | Nút "Xóa danh sách" | `POST api/DepositOrderDeliveryBatch/XoaDanhSachTraCoc/{id:Guid}` | `DepositOrderDeliveryBatchController_XoaDanhSachTraCoc` | public | IsSended=false | Nullify `IdDepositOrderDeliveryBatch` + `DepositOrderDeliveryBatchDate` trên phiếu gán đợt (BulkUpdate). Cấm nếu IsSended=true. Log `GoKhachHangKhoiDotTraCoc`. | **EF** BulkUpdate | `.Take(1000)` cứng → đợt >1000 phiếu chỉ gỡ 1000 đầu, phần dư còn dính link. |
| 8 | Thống kê đợt | (xem thống kê SL theo SP) | `POST api/DepositOrderDeliveryBatch/GetStatsBatch/{id:Guid}` | `DepositOrderDeliveryBatchController_GetStatsBatch` | public | dòng đợt | Gọi SP `PRC_SCRM_Deposit_GetStatsDepositReceiptByBatch` đếm phiếu theo SP; cap số đếm theo SL cấu hình của đợt. | **SP** GetStatsDepositReceiptByBatch | — |
| 9 | Cấu hình mẫu SMS/Zalo | Popup "Cấu hình SMS" (sms-edit) | `POST api/DepositOrderDeliveryBatch/UpdateSMSInfo` | `DepositOrderDeliveryBatchController_UpdateSMSInfo` | public | IsSended=false | Lưu `IdSmsPattern`, `IdZaloPattern`, `IsUseJsonParam`, `JsonParam`. Validate JsonParam: ≤2000 ký tự, không trùng key, key/value không rỗng. Cấm nếu IsSended=true. | **EF** | — |
| 10 | Khóa đợt | Nút "Khóa" (SetSended) | `POST api/DepositOrderDeliveryBatch/SetSended/{id:Guid}` | `DepositOrderDeliveryBatchController_SetSended` | public | đợt đã cấu hình mẫu SMS/Zalo | Set `IsSended=true` (chốt cuối, khóa mọi sửa). Yêu cầu đã có ≥1 mẫu SMS hoặc Zalo. | **EF** | KHÔNG kiểm tra đợt đã có phiếu gán hay chưa → có thể khóa đợt rỗng. |
| 11 | Autocomplete đợt | (combobox chọn đợt) | `POST api/DepositOrderDeliveryBatch/DepositReceiptBatchAutoComplete` | `DepositOrderDeliveryBatchController_DepositReceiptBatchAutoComplete` | public | nơi cần chọn đợt | Tìm đợt theo keyword (tên/mã), lọc theo đơn vị, sort NgayTao DESC; trả paging (Id/Code/Name). | **EF** | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Khóa cứng khi IsSended=true:** chặn Update / XoaDanhSachTraCoc / Delete / UpdateSMSInfo / GanDotTraCoc / (TaoMaVoucher / ClearVoucherCode nội bộ).
- **Delete đợt:** chỉ khi IsSended=false AND không còn phiếu gán đợt AND chưa có lịch sử DepositSmsSend.
- **GanDotTraCoc:** điều kiện chọn phiếu nằm trong SP (theo FromDepositDate/ToDepositDate/ListIdReceivingBranch/TypeObject/IdProduct); chunk 1000.
- **SetSended:** bắt buộc ≥1 mẫu SMS/Zalo; (thiếu) chưa ràng buộc phải có phiếu trong đợt.
- **JsonParam (UpdateSMSInfo):** ≤2000 ký tự, key/value không rỗng, không trùng key.
- **Multi-tenant:** mọi query lọc `IdDonVi == userLogin.DonViId`.

## Quan hệ
- **Gọi sang:** CommonService (SP, voucher gen/delete), SqlSugar/Dapper (2 stored proc), EFCore.BulkExtensions.
- **Dữ liệu (entities):** `DepositOrderDeliveryBatch` + `DepositOrderDeliveryBatchProduct`; cập nhật `DepositReceipt.IdDepositOrderDeliveryBatch`; đọc `DepositSmsSend` (chặn xóa); audit MongoDB.
- **Ripple / đồng bộ:** KHÔNG publish RabbitMQ ở controller này. Voucher cho đợt gọi đồng bộ sang Marketing (DeleteVoucherCodeByIdNguonTao + GenVoucherCodeByBatch). Đợt là input để DepositSmsSend tạo lịch sử & gửi SMS.

## Luồng xuyên module
- Tạo đợt → Gán danh sách (SP set IdBatch trên phiếu cọc) → Cấu hình mẫu SMS/Zalo → SetSended (khóa) → DepositSmsSend.InsertFromBatch + SendSmsByBatch gửi tin cho KH trong đợt.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/DepositOrderDeliveryBatchController.cs` — role `DV-AD-TONG, DV-QL-KHO-TONG`.
- BE `Deposit/Deposit.Service/Generated/DepositOrderDeliveryBatchService.cs`
  - L159-160 / L175-176: `LockBatch`/`UnLockBatch` — code đổi cờ bị comment, gọi UpdateAsync nhưng KHÔNG đổi field nào → **no-op** (cài đặt dở).
  - L192: `XoaDanhSachTraCoc` `.Take(1000)` cứng → đợt >1000 phiếu gỡ thiếu.
  - L284: `TaoMaVoucher` giới hạn vòng lặp 50 (×1000) → >50k phiếu dừng sớm.
  - L376-407: `GanDotTraCoc` gọi SP không bọc transaction; SP trả 0 → break im lặng.
  - L987-1003: `SetSended` không kiểm tra số phiếu đã gán → khóa được đợt rỗng.
  - L492 / L993: mess "Không tìm thấy dữ liệu cần xóa" dùng cho cả luồng không-xóa.
  - L514-515: Delete đợt trước SP → có thể orphan product nếu lỗi giữa chừng.
- FE `scrm_frontend/src/app/pages/deposit-receipt/deposit-order-delivery-batch-manager/` (manager + popup batch-edit + batch-sms-edit); route `deposit-receipt/deposit-order-delivery-batch-manager`.
