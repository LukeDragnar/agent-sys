# Luồng — Quản lý đặt cọc

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId); có role kho tổng (DV-AD-TONG, DV-QL-KHO-TONG).
- Có sản phẩm, chi nhánh nhận đã khai báo.
- Phiếu cọc có thể được tạo từ landing-page (bất đồng bộ) hoặc trong hệ thống.
- Để gửi tin theo đợt: đã cấu hình mẫu SMS/Zalo.

## Luồng dùng (phiếu cọc)
1. Mở **Đặt cọc → Quản lý đặt cọc**, lọc đa điều kiện (chi nhánh, SP, trạng thái thanh toán/giao/hoàn/gửi SMS, mốc ngày). Hệ thống **luôn loại phiếu `TypeDepositReceipt=2` (bị thay thế)**.
2. **Cập nhật thanh toán**: đổi trạng thái 0↔1, set ngày/mã/phương thức/số tiền; lần đầu chuyển "đã thanh toán" → publish SMS lần đầu; ghi lịch sử MongoDB.
3. **Thay sản phẩm** (gating: DaThanhToan AND chưa vào đợt AND chưa đợi đợt sau): phiếu cũ → `Type=2` (ẩn), tạo phiếu thay thế `Type=1`; publish SMS action REPLACE.
4. **Hoàn cọc — yêu cầu** (gating: DaThanhToan AND chưa hoàn AND chưa giao): set `IsDepositRefund=true` + thông tin ngân hàng (số TK validate, tên, chi nhánh, lý do) + ngày; publish SMS REFUND + hủy voucher (`MessageRemoveVoucherCode`).
5. **Hoàn cọc — xác nhận** (gating: đã yêu cầu hoàn AND chưa xác nhận): set `IsConfirmRefund=true`, số tiền hoàn, mốc, người xác nhận; **áp cho MỌI bản ghi cùng DepositCode**; publish SMS CONFIRM_REFUND.
6. **Xác nhận đợi đợt sau** (idempotent — chỉ lần đầu): set `IsConfirmWaitNext=true`; publish SMS WAIT.
7. **Xóa khỏi đợt trả cọc** (đã vào đợt AND chưa giao, ≤1.000 phiếu): clear `IdDepositOrderDeliveryBatch` + ngày.
8. **Xem lịch sử** (MongoDB) / **Xuất Excel** (38 cột, file `ReportDatCoc.xlsx`).

## Luồng cấu hình (admin — Đợt trả cọc)
1. **Tạo đợt** (`Create`): khai danh sách SP × SL × khoảng ngày cọc × chi nhánh nhận; mã tự sinh prefix "BAT"; khởi tạo `IsSended=false`.
2. **Gán danh sách trả cọc** (`GanDotTraCoc`, IsSended=false): clear link cũ → theo từng SP tính delta SL → gọi stored proc `PRC_SCRM_Deposit_GetAndUpdateIdBatch` (chunk 1000) set IdBatch cho phiếu đủ điều kiện.
3. **Cấu hình mẫu SMS/Zalo** (`UpdateSMSInfo`, IsSended=false): lưu IdSmsPattern/IdZaloPattern/JsonParam (JsonParam ≤2000 ký tự, key/value không rỗng, không trùng key).
4. **Khóa đợt** (`SetSended`): set `IsSended=true` — bắt buộc đã có ≥1 mẫu SMS/Zalo; khóa cứng mọi thao tác ghi.

## Các trường hợp / case biên
- **Phiếu chưa thanh toán** → không thay SP, không hoàn cọc được.
- **Phiếu đã giao** (`IsDelivered=true`, do Order publish `MessageUpdateStatusDeliveriedByVoucherCode`) → khóa hoàn cọc & thay SP.
- **Xác nhận đợi đợt sau lặp lại** → không đổi gì (idempotent).
- **Đợt đã khóa (IsSended=true)** → chặn Update/Gán/Xóa danh sách/UpdateSMSInfo/Delete.
- **Xóa danh sách trả cọc đợt > 1.000 phiếu** → chỉ gỡ 1.000 phiếu đầu (`.Take(1000)` cứng) → phần dư còn dính link (xem dev).
- **Khóa đợt rỗng** → vẫn khóa được vì SetSended không kiểm tra số phiếu đã gán (chỉ yêu cầu có mẫu SMS) (xem dev).
- **Xóa đợt** → chỉ khi IsSended=false AND không còn phiếu gán AND chưa có lịch sử DepositSmsSend.
- **Tiền cọc trung bình** → luôn hiển thị 0 do lỗi thứ tự toán tử (xem dev).

## Ví dụ cụ thể có số
Sản phẩm "Tủ lạnh Y", chi nhánh Kho Hà Nội.
- 30 khách đặt cọc, mỗi người cọc 2.000.000đ, đều đã thanh toán → 30 phiếu DaThanhToan.
- Quản lý kho **Tạo đợt** BAT-0001: SP Tủ lạnh Y × 30, chi nhánh Kho Hà Nội, khoảng ngày cọc tháng 6.
- **Gán danh sách trả cọc** → stored proc set IdBatch cho 30 phiếu đủ điều kiện.
- **Cấu hình mẫu SMS** "Hàng của quý khách đã về..." → **Khóa đợt** (đã có mẫu → OK).
- Một khách đổi ý xin hoàn: phiếu chưa vào đợt thì hoàn được; nhưng khách này đã trong đợt BAT-0001 → muốn hoàn phải **Xóa khỏi đợt trả cọc** trước (gating: chưa giao), rồi mới Hoàn cọc.
- Khách khác (chưa thanh toán) bấm hoàn cọc → bị chặn vì phải DaThanhToan.
- Báo cáo: "tiền cọc trung bình" hiển thị 0 (lỗi đã biết), nhưng tổng từng phiếu 30 × 2.000.000 = 60.000.000đ vẫn đúng.

## Hậu điều kiện
- Mỗi hành động phiếu cọc ghi `DepositReceipt` (EF) + audit MongoDB; publish message SMS/Zalo theo action; hoàn cọc publish hủy voucher.
- Đợt trả cọc ghi `DepositOrderDeliveryBatch` + `DepositOrderDeliveryBatchProduct`; gán phiếu cập nhật `DepositReceipt.IdDepositOrderDeliveryBatch` (qua stored proc / BulkUpdate); voucher gọi đồng bộ sang Marketing.
- Khóa đợt → là input để DepositSmsSend tạo lịch sử & gửi SMS cho khách trong đợt.
