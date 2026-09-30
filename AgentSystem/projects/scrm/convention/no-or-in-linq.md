---
name: scrm-no-or-in-linq
description: SCRM BE — không dùng OR trong WHERE LINQ vì không ăn index; tách thành 2 query riêng + Concat/Distinct in-memory
metadata:
  type: feedback
---

Khi cần match 2 cột khác nhau với cùng 1 giá trị (vd `IdPhieu == id || IdPhieuNhapCu == id`) trong query LINQ trên DB SCRM, **KHÔNG** viết OR trong cùng 1 `.Where`. Tách thành 2 query riêng, mỗi query filter trên 1 cột, rồi `Concat().Distinct()` in-memory.

**Why:** SQL Server với OR trên 2 cột khác nhau thường không ăn được index (index seek mỗi cột riêng → optimizer fallback scan). Tách 2 query → mỗi nhánh ăn đúng index lead với cột tương ứng, hai lần seek nhanh hơn một lần scan.

**How to apply:**
- Áp dụng cho mọi LINQ query trên `_repository.TableWithNolock()` khi WHERE có dạng `(x.ColA == val || x.ColB == val)` hoặc tương tự.
- Có 2 pattern thay thế, chọn theo context:

**Pattern A — Tách 2 query (khi mỗi nhánh có thể ăn index seek riêng):**
```csharp
var listA = await _repo.TableWithNolock().Where(x => x.IdDonVi == id && x.ColA == val && ...).Select(x => x.Field).ToListAsync();
var listB = await _repo.TableWithNolock().Where(x => x.IdDonVi == id && x.ColB == val && ...).Select(x => x.Field).ToListAsync();
var combined = listA.Concat(listB).Distinct().ToList();
```

**Pattern B — 1 query covering scan + filter LINQ (khi có index covering lead khác, ColA/ColB nằm trong INCLUDE):**
```csharp
var allRows = await _repo.TableWithNolock()
    .Where(x => x.IdDonVi == id && x.Imei != null)        // lead cột ăn index covering (vd IX_GetByIdDonViImei)
    .Select(x => new { x.Imei, x.ColA, x.ColB, ... })     // ColA/ColB phải trong INCLUDE để covering
    .ToListAsync();
var rows = allRows.Where(r => r.ColA == val || r.ColB == val).ToList();  // OR filter ở client
```
Pattern B hợp khi: có covering index lead cột scope (IdDonVi, IdSanPham...), không có index seek riêng cho ColA/ColB, và scope đủ nhỏ để client filter không tốn nhiều memory.

- Vẫn giữ filter `IdDonVi`/`IdChiNhanh` để ăn covering index ([[scrm-always-nolock]]).
- Liên quan: [[scrm-always-nolock]].

Case khám phá: bug 437.6 — `KiemTraImeiCuaPhieuDaDung` ban đầu dùng Pattern A, sau đó user yêu cầu chuyển sang Pattern B vì có index `IX_GetByIdDonViImei` covering chứa `IdPhieu`/`IdPhieuNhapCu` trong INCLUDE.
