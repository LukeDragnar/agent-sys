---
name: mongodb-query-pattern
description: MongoDB.Driver — Filter dùng Builders<T>.Filter (không lambda), Join dùng Aggregate().Lookup().Unwind().Project() (không LINQ from..join). Áp dụng mọi project .NET dùng MongoDB driver.
metadata:
  type: feedback
---

## Rule 1 — Filter: dùng `Builders<T>.Filter`, KHÔNG dùng lambda

**Why:** Lambda overload (`x => x.Field == value`) phải qua bước Expression-Tree → BsonDocument runtime ⇒ chậm hơn, dễ gặp surprise translation (string methods, DateTime methods, `Contains`...). `FilterDefinition<T>` build sẵn từ Builders → driver gửi thẳng xuống Mongo, query plan ổn định + tận dụng index tốt hơn.

**How to apply:** Mọi method nhận filter của `MongoDB.Driver` — `CountDocumentsAsync`, `Find`, `DeleteManyAsync`, `UpdateOneAsync`, `UpdateManyAsync`, `ReplaceOneAsync` — luôn truyền `FilterDefinition<T>`.

**✅ ĐÚNG:**
```csharp
var filter = Builders<UserNotification>.Filter.Eq(x => x.UserId, userid)
           & Builders<UserNotification>.Filter.Eq(x => x.IsDaXem, false);
var total = await collection.CountDocumentsAsync(filter);
```

**❌ TRÁNH:**
```csharp
var total = await collection.CountDocumentsAsync(
    b => b.UserId == userid && b.IsDaXem == false);
```

**Wrapper có 2 overload:** nếu repo dùng wrapper kiểu `IMongoDbClient.CountDocumentsAsync<T>(...)` có cả overload `Expression<Func<T,bool>>` lẫn `FilterDefinition<T>` (cùng signature, khác type tham số) — **luôn chọn `FilterDefinition<T>`**, dễ chọn nhầm.

**Compose filter:**
```csharp
var b = Builders<MessageChatBox>.Filter;
var filter = b.Gte(c => c.NgayTao, fromDate) & b.Lt(c => c.NgayTao, toDate);
if (!string.IsNullOrEmpty(pageId)) filter &= b.Eq(c => c.ParentId, pageId);
```

---

## Rule 2 — Join: dùng `Aggregate().Lookup()`, KHÔNG dùng LINQ `from..join`

**Why:** LINQ `from x in collection join y in ... select ...` trên Mongo có nguy cơ fallback sang client-side join (kéo cả 2 collection về memory). `Aggregate().Lookup()` build pipeline `$lookup` server-side, tận dụng index trên foreign field.

**How to apply:** Khi join 2 collection trong cùng query, dùng Aggregate pipeline.

**✅ ĐÚNG (INNER JOIN qua `.Unwind`):**
```csharp
var result = await collectionUser
    .Aggregate()
    .Match(x => x.UserId == userId)
    .SortByDescending(x => x.NgayTao)
    .Skip(offset)
    .Limit(limit)
    .Lookup<UserNotification, NotificationInfo, NotificationJoinResult>(
        foreignCollection: collection,
        localField:        x => x.NotificationId,
        foreignField:      x => x.Id,
        @as:               x => x.Notifications)
    .Unwind<NotificationJoinResult, NotificationJoinResult>(x => x.Notifications)   // INNER JOIN
    .Project(x => new NotificationInfo
    {
        Id      = x.NotificationId,
        Message = x.Notifications.Message,
        NgayTao = x.NgayTao,
        Sound   = x.Notifications.Sound,
        Data    = x.Notifications.Data,
    })
    .ToListAsync();
```

**❌ TRÁNH:**
```csharp
var query = from u in collectionUser
            join n in collection on u.NotificationId equals n.Id into noti
            from n in noti.DefaultIfEmpty()
            select new NotificationInfo { Id = u.NotificationId, Message = n.Message, ... };
```

**Thứ tự stage:** `Match → Sort → Skip → Limit → Lookup → Unwind → Project`. Đặt `Match`/`Sort`/`Skip`/`Limit` **TRƯỚC** `Lookup` để Mongo lookup ít document hơn (giảm I/O + ăn index).

**INNER vs LEFT JOIN:**
- INNER: `.Unwind<TResult, TResult>(x => x.Foreign)` — mặc định loại document không match.
- LEFT: bỏ `.Unwind` (giữ `IEnumerable<TForeign>` rồi xử lý null), hoặc dùng option `preserveNullAndEmptyArrays = true` qua `AggregateUnwindOptions`.

**Class trung gian cho Lookup:** phải có property `IEnumerable<TForeign> X` map đúng tên với `@as` parameter. Convention naming: suffix `JoinResult` (vd `NotificationJoinResult`).

---

## Rule 3 — Update / Replace cùng nguyên tắc

```csharp
// Update partial — dùng UpdateDefinition + Builders<T>.Update
var update = Builders<UserNotification>.Update
    .Set(b => b.IsDaXem, true)
    .Set(b => b.NgayXem, DateTime.Now);
await collection.UpdateOneAsync(filter, update);
```

KHÔNG dùng lambda cho filter param của `UpdateOneAsync`/`UpdateManyAsync`/`ReplaceOneAsync` — cùng lý do Rule 1.

`ReplaceOneAsync` + `IsUpsert = true` chỉ dùng khi thực sự cần insert-or-update; nếu chỉ update vài field → dùng `UpdateOneAsync` với `UpdateDefinition`.

---

## Rule 4 — Naming / constants

- Collection name: dùng constant trong static class collection (vd `MongoDBCollection.UserNotification`) hoặc `nameof(EntityClass)`. KHÔNG hardcode literal string lặp lại nhiều chỗ.
- Sort key: dùng typed `Builders<T>.Sort.Descending(x => x.Field)` — KHÔNG dùng string `"Field"` (typo-prone, refactor-unsafe).

---

## Reference

- Code mẫu đúng pattern (SCRM): `Notification/Notification.API/Services/NotificationService.cs` — method `GetsByUserId` (Aggregate + Lookup + Unwind + Project).
- Code mẫu wrapper Builders filter: các service `Log*` / `Webhook*` / `Accounting` — pattern `var filter = Builders<X>.Filter.Lt(...); await collection.CountDocumentsAsync(filter);`.

Related: [[scrm-always-nolock]] (EF Core counterpart cho SQL Server query rule).
