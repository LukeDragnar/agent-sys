---
name: feedback_defer_persist_until_external_success
description: Luồng sync/create thủ công gọi service ngoài — KHÔNG Insert record local upfront, defer tới sau khi call ngoài thành công để tránh row mồ côi
metadata:
  type: feedback
---

Trong luồng **sync/create thủ công** có gọi service ngoài giữa chừng (vd ECOMM tạo đơn rồi push sang CRM), **KHÔNG Insert record local trước rồi mới gọi ngoài**. Phải: build payload in-memory → gọi service ngoài → **chỉ Insert khi call thành công**. Nếu build/push fail thì return sớm, record chưa từng được persist → không để lại row mồ côi `Status=null`.

**Why:** Trace bắt bug HIGH ở `OrderReturnShopeeService.SyncOneReturnToCrm` (nút "Lấy đơn hoàn mới"): code Insert `OrderReturnData` upfront → nếu `BuildReturnDataFull` null hoặc push CRM fail thì `return false` để lại row `Status=null`. Hậu quả: lần sau discovery idempotent (lọc theo "đã có trong OrderReturnData") **bỏ qua** sn đó, nút "Đồng bộ trạng thái" lại fail vì CRM chưa CREATE → **đơn kẹt vĩnh viễn, phải xóa tay**.

**How to apply:**
- Phân biệt với **webhook job** (vd `ProcessSendOrderReturnToCrm`): job CỐ Ý Insert-upfront + `catch` set `Status=Error` + SP retry cap `ResendNo` làm safety net → orphan được track & retry. Luồng thủ công KHÔNG có lưới retry đó nên phải defer.
- CREATE: tạo object in-memory (gán `Id=NewGuid`), sync field, push ngoài, **thành công mới `InsertAsync`** (Insert ghi đủ cột, khỏi MarkDirty). UPDATE (record đã có DB): sync field → push → `MarkDirty` + `UpdateAsync` sau khi thành công.
- Liên quan [[feedback_self_trace_before_done]] (chính trace phát hiện), [[reference_ecommerce_connect_deleteasync_persists_dirty]].
