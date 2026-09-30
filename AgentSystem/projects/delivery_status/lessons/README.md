# delivery_status — Lessons (CHỈ bug-sự-cố thật)

> **Lessons** = bài học rút ra từ **sự cố đã xảy ra** (reactive "lần đó bị X vì Y"). Khác convention/pattern (quy tắc "luôn/không bao giờ" → `../patterns.md`).

| Lesson | Hook |
|---|---|
| [bug_vtp_502_timestamp_lech_rot_cong.md](bug_vtp_502_timestamp_lech_rot_cong.md) | VTP đóng dấu timestamp lệch → mã hoàn 502 (số nhỏ) rớt cổng `OrderStatusDate >=` → đơn không nhảy "Đang hoàn". Fix: ép 502/504 áp dụng bất kể timestamp (product-2.22). |
| [reference_backfill_don_da_doi_soat.md](reference_backfill_don_da_doi_soat.md) | Đơn **đã đối soát** thì CRM khoá — resend cách mấy cũng không đổi trạng thái được. Residual chấp nhận được, đừng ép. |
