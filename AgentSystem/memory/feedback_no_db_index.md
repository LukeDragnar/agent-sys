---
name: KHÔNG tạo index trên DB
description: Migration / script DB không được tạo INDEX (kể cả UNIQUE INDEX). Chỉ PRIMARY KEY trên table mới.
type: feedback
---

KHÔNG tạo `CREATE INDEX` hay `CREATE UNIQUE INDEX` trong migration / script DB.

**Why:** User có rule riêng — index do DBA / team DB quyết định và thêm sau khi đánh giá production load, không phải developer tự thêm trong migration code. Confirmed by user 2026-05-12: "tôi có rule k tạo index mà".

**How to apply:**

- ✅ `PRIMARY KEY` trên table mới — OK (mọi table cần PK, không phải "index" theo nghĩa rule này)
- ❌ `CREATE INDEX IX_*` — không tạo, kể cả khi nhìn thấy query pattern có lợi
- ❌ `CREATE UNIQUE INDEX UX_*` — không tạo, dù để enforce idempotency
- ❌ `INCLUDE` columns trong index — không tạo
- ❌ Filtered indexes (`WHERE ...`) — không tạo

Khi cần enforce uniqueness hoặc tăng tốc query:
1. **Idempotency / uniqueness** → dựa vào C# code best-effort check (SELECT-then-INSERT trong try-catch). Race condition rare, log + skip nếu xảy ra. KHÔNG dùng UNIQUE INDEX để hard-enforce.
2. **Query performance** → DBA review và thêm index sau. Đề xuất hint trong PR description, không tự thêm.

Nếu thấy migration của tool gen ra index → xóa trước khi commit/đẩy SQL.

**Áp dụng cho:** ecommerce_connect, SCRM, ps_delivery, delivery_*, mọi project trong workspace này.
