---
name: Đếm bản ghi bảng lớn — dùng sys.partitions
description: Bảng SQL Server lớn KHÔNG dùng COUNT(*) (chậm vài giây đến vài chục giây), thay bằng SUM(sys.partitions.rows) — chạy vài ms.
type: feedback
---

Khi cần đếm bản ghi của bảng mà dữ liệu **chắc chắn sẽ rất lớn** (log, webhook, history, audit trail, message queue persistence…), **KHÔNG** dùng `COUNT(*)` / `COUNT(1)` — kể cả có `WITH (NOLOCK)` — vì SQL Server vẫn scan index → vài giây đến vài chục giây với bảng cỡ chục triệu+ rows.

**Why:** User chia sẻ rule team 2026-05-27 — đã gặp case bảng `dbo.FBDataWebhook` query COUNT chạy lâu, blocking job. Đổi sang đọc metadata `sys.partitions` → vài mili-giây (read-only system view, không scan data).

**How to apply:**

❌ KHÔNG dùng (chậm với bảng lớn):
```sql
SELECT @count = COUNT(1)
FROM dbo.FBDataWebhook WITH (NOLOCK)
```

✅ Dùng (vài ms, đọc metadata):
```sql
SELECT @count = SUM(p.rows)
FROM sys.partitions p
WHERE p.object_id = OBJECT_ID('dbo.FBDataWebhook')
  AND p.index_id IN (0, 1);
```

**Giải thích `index_id IN (0, 1)`:**
- `0` = heap (bảng không có clustered index)
- `1` = clustered index
- Mỗi row của bảng count đúng 1 lần (không cộng dồn ở non-clustered index `index_id >= 2`)

**Khi áp dụng:**
- ✅ Job/SP cần count nhanh để quyết định throttle, paging, health check, monitoring dashboard
- ✅ Kiểm tra "bảng có data không" (`if (@count > 0)`) — chấp nhận con số xấp xỉ
- ✅ Báo cáo tổng row trong admin/dev tool
- ❌ KHÔNG dùng khi cần count chính xác với filter (`WHERE Status = X`) — `sys.partitions` chỉ trả tổng toàn bảng
- ❌ KHÔNG dùng khi cần count realtime exact (transaction in-flight chưa commit có thể đã/chưa được phản ánh tùy isolation)

**Cross-DB / cross-schema:**
```sql
-- Bảng ở DB khác
SELECT @count = SUM(p.rows)
FROM OtherDb.sys.partitions p
WHERE p.object_id = OBJECT_ID('OtherDb.dbo.TableName')
  AND p.index_id IN (0, 1);

-- Schema khác dbo
SELECT @count = SUM(p.rows)
FROM sys.partitions p
WHERE p.object_id = OBJECT_ID('schema_name.TableName')
  AND p.index_id IN (0, 1);
```

**Áp dụng cho:** scrm, ecommerce_connect, ps_delivery, delivery_*, mọi project SQL Server có bảng dự kiến lớn.
