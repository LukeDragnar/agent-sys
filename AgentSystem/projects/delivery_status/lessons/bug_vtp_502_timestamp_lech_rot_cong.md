---
name: bug-vtp-502-timestamp-lech-rot-cong
description: VTP đóng dấu thời gian lệch thứ tự nghiệp vụ — mã hoàn 502 (Đang hoàn) mang OrderStatusDate SỚM hơn mã 300/500 chặng hoàn xử lý liền kề → cổng "OrderStatusDate >= orderDb.OrderStatusDate" trong VtpService giết nhầm 502 → đơn kẹt "Đã lấy hàng/Đang giao" thay vì "Đang hoàn". Fix: ép 502/504 luôn áp dụng bất kể timestamp + khoá luồng hoàn.
metadata:
  type: reference
---

# VTP — mã Chuyển hoàn 502 rớt cổng vì đối tác đóng dấu timestamp lệch

## Sự cố (2026-07)
Đơn VTP (vd `LB001000667`) thực tế đã "Chuyển hoàn bưu cục gốc" (VTP 502 → nội bộ **40 Đang hoàn**) nhưng CRM vẫn hiện "Đã lấy hàng(21)"/"Đang giao(30)", **không nhảy chuyển hoàn**. Diện rộng (~70 đơn VTP).

## Root cause (chốt bằng data product, KHÔNG phải suy đoán)
`VtpService.SendDataReceiveToCrm` có cổng update:
```
dataReceive.OrderStatusDate >= orderDb.OrderStatusDate     // chỉ nhận mốc mới-hơn-hoặc-bằng
```
Job `ProcessSendDataReceiveToCrm` xử lý bản ghi theo `CreatedDate` (thứ tự nhận). **VTP đóng dấu `OrderStatusDate` LỆCH thứ tự nghiệp vụ**: mã 300 "Đóng bảng kê đi" chặng hoàn (mốc 18:59:**26**) lại MUỘN hơn mã 502 "Chuyển hoàn" (mốc 18:59:**25**) — dù 502 mới là trạng thái chốt. 300 xử lý trước → DB lưu mốc 26; 502 tới sau bị `25 >= 26` = false → **bị loại** ("Đơn hàng đã update trạng thái mới hơn..."). Vì 502 rớt → `PropValue1` không set "502" → khoá-luồng-hoàn không chạy → 300/500 chặng hoàn ghi đè lùi về "Đã lấy hàng/Đang giao".

## Bẫy CHUNG — áp cho MỌI đối tác
Đừng tin `OrderStatusDate` của đối tác monotonic theo nghiệp vụ. **Mã milestone "hoàn/kết thúc" thường có SỐ NHỎ hơn mã đi/tồn** (502 < 505/506/508) → tiebreaker `>` hoặc cổng `>=` dễ giết nhầm khi đối tác đóng dấu lệch/cùng giây. Gặp partner khác báo "không nhảy trạng thái X" → soi ngay cổng so-timestamp này.

## Fix (commit product-2.22)
`VtpService`: thêm `lstStatusForceApply = {501,502,504}` — ép 3 mã milestone (Đã giao / Đang hoàn / Đã hoàn) **LUÔN áp dụng bất kể timestamp** (mở rộng đúng cách 501 đã được đặc-cách sẵn), nhưng **GIỮ** `!lstStatusTheEnd.Contains(orderDb.OrderStatusCode)` để không hồi sinh đơn đã terminal. Cộng khoá luồng hoàn: đơn `PropValue1=="502"` → bỏ ghi trạng thái di chuyển giữa đường chặng hoàn (300/500/505/506/508), chỉ nhận trạng thái kết thúc (501/503/504/201/-15) hoặc chính 502.
- File: `DeliveryStatus.Service/Generated/Partner/VtpService.cs`.
- Mẫu ĐÚNG tham chiếu (cùng repo): `VnPostService.cs` — khoá state "18" (`allowLogicUpdateStatus`) + tiebreaker cùng-ngày dùng `!=` thay vì `>`.

## Backfill đơn cũ đã kẹt
Deploy fix chỉ ăn webhook về SAU. Đơn cũ đã kẹt (502 đã bị purge khỏi `DataReceive`) → **resend từ `BakDataReceive` về `DataReceive`** (reset `IsDeleted=0/InProcess=0/ResendNo=0`, đổi `Id=NEWID()`) cho job đã vá xử lại. Nhận diện đơn kẹt: `PropValue1 IS NULL` + trạng thái non-terminal + có 502/504 trong `BakDataReceive`. Ràng buộc: xem [reference_backfill_don_da_doi_soat.md](reference_backfill_don_da_doi_soat.md).
