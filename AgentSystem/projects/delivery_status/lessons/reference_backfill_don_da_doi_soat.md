---
name: reference-backfill-don-da-doi-soat
description: Khi backfill/resend trạng thái đơn giao vận về CRM (delivery_status → SCRM), đơn "đã đối soát" bị CRM KHOÁ — trả {"success":false,"message":"Đơn hàng đã đối soát."} và không đổi trạng thái. Resend cách mấy cũng vô ích. Chấp nhận để nguyên, đừng ép (ép sẽ phá số liệu đối soát).
metadata:
  type: reference
---

# Backfill trạng thái đơn — đơn "đã đối soát" thì CRM khoá, resend vô ích

## Ràng buộc
Khi bù trạng thái cũ (resend `DataReceive` cho job đẩy lại về CRM), đơn **đã đối soát** (kế toán chốt sổ đối soát với đối tác) bị CRM từ chối update:
```json
{"success":false,"data":false,"status":200,"message":"Đơn hàng đã đối soát.", ...}
```
→ Đơn giữ nguyên trạng thái cũ, **KHÔNG nhảy** về trạng thái mới (vd "chuyển hoàn"). Ép đổi sẽ **phá số liệu đối soát** → đúng nghiệp vụ là **để nguyên**.

## Hệ quả khi backfill (cần biết trước)
- Số đơn "kẹt" (`PropValue1 IS NULL`) **sẽ KHÔNG về 0** — phần dư = đơn đã đối soát. Đây là **residual chấp nhận được, KHÔNG phải bug**.
- Job xử lý gracefully: message "Đơn hàng đã đối soát" nằm trong list drop-không-retry của `ProcessSendDataReceiveToCrm` → row bị `IsDeleted=1`, không retry vô hạn.
- ⚠️ Sau khi job xử, `Description` của row resend bị **GHI ĐÈ bằng response CRM** → mất tag chống-trùng → chạy lại batch sẽ **resend lại** mấy đơn đối-soát này (vô hại nhưng phí + noise). Muốn sạch: khi lọc đơn cần bù, loại đơn có row fail `Description LIKE '%đã đối soát%'`.

## Áp dụng
- Trước khi bù data số lượng lớn: **đừng cố "quét cho về 0"** — xác định đơn đã đối soát để bỏ qua.
- Bối cảnh: 2026-07, backfill ~70 đơn VTP kẹt chuyển hoàn (xem [bug_vtp_502_timestamp_lech_rot_cong.md](bug_vtp_502_timestamp_lech_rot_cong.md)); các đơn dư đều "đã đối soát" → user chốt để nguyên.
