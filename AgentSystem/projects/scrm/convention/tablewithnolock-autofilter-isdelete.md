---
name: tablewithnolock-autofilter-isdelete
description: SCRM — TableWithNolock()/mọi EF query tự lọc IsDelete==false qua global query filter; KHÔNG thêm IsDelete==false thủ công
metadata:
  type: feedback
---

Trong SCRM, **KHÔNG thêm `.Where(x => x.IsDelete == false)` vào EF query** — nó THỪA.

**Why:** Mỗi DbContext (WarehouseProduct/OrderLogistic/Order...) trong `OnModelCreating` có vòng `foreach (entityType in Model.GetEntityTypes()) { ... HasQueryFilter(e => EF.Property<bool>(e,"IsDelete") == false) }` → **global query filter `IsDelete == false` áp cho TẤT CẢ entity**. `TableWithNolock()` = `Entities.AsNoTracking().WithHint(...,"NOLOCK")` → vẫn dính filter này. Bản `TableWithNolockFullData()` mới `IgnoreQueryFilters()` để "lấy cả data đã xóa".

**How to apply:**
- EF query (`_repos.TableWithNolock()...` hoặc `_dbContext.Xxx...`, kể cả sub-query `.Any(l => ...)`): **bỏ** `&& x.IsDelete == false`.
- GIỮ `IsDelete = 0` / `IsDelete = 1` trong **raw SQL** (ExecuteSqlRaw / stored proc / OPENJSON) — raw SQL KHÔNG có global filter.
- Cần đọc cả data đã xóa (vd phiếu cutover IsDelete=1): dùng raw SQL hoặc `TableWithNolockFullData()`/`IgnoreQueryFilters()`, KHÔNG dùng TableWithNolock thường.

Liên quan: [[no-query-isdelete-for-log]], [[no-or-in-linq]].
