---
name: feedback-scrm-mongo-filter-definition
description: SCRM Mongo query — dùng Builders<T>.Filter.Eq (FilterDefinition) thay vì Expression<Func<T, bool>> để driver gen BsonDocument tối ưu.
metadata:
  type: feedback
---

# SCRM — Mongo query dùng `FilterDefinition` builder, không dùng Expression LINQ

Mọi Mongo query trong SCRM phải dùng `Builders<T>.Filter.Eq/And/Or/...` để build `FilterDefinition<T>`, không dùng `Expression<Func<T, bool>>`.

```csharp
// ❌ KHÔNG
var count = await collection.CountDocumentsAsync(x => x.UserId == userid && x.IsDaXem == false);
var docs = await _mongoClient.FindByConditionAsync<X>(
    x => x.IdDonVi == idDonVi && x.IdPhieu == idPhieu,
    MongoDBCollection.X);

// ✅ ĐÚNG
var fb = Builders<UserNotification>.Filter;
var filter = fb.And(
    fb.Eq(x => x.UserId, userid),
    fb.Eq(x => x.IsDaXem, false)
);
var count = await collection.CountDocumentsAsync(filter);

var fb2 = Builders<X>.Filter;
var filter2 = fb2.And(
    fb2.Eq(x => x.IdDonVi, idDonVi),
    fb2.Eq(x => x.IdPhieu, idPhieu)
);
var docs = await _mongoClient.FindByConditionAsync(filter2, MongoDBCollection.X);
```

**Why**: Khi dùng `Expression<Func<T, bool>>`, MongoDB.Driver phải walk expression tree → translate sang BsonDocument runtime. Có rủi ro:
1. **Translation pitfall**: vài operator (vd `.ToString()` của Guid, null-coalesce) bị giữ là method call trong filter thay vì substitute giá trị → query sai (đã gặp ở `LayChiTietBySession`, xem [[scrm_no_query_isdelete_for_log]]).
2. **Overhead runtime**: expression tree → BsonDocument tốn CPU mỗi lần query (không cache).
3. **Driver có thể sinh `$where` JavaScript** (không ăn index) trong vài case phức tạp.

`Builders<T>.Filter.Eq` build BsonDocument trực tiếp — bypass expression-tree translator, gen ra `{field: value}` chuẩn ăn index.

**How to apply**:
- File mới: viết FilterDefinition từ đầu. `using MongoDB.Driver;` để có `Builders<T>`.
- File cũ: khi đụng tới query Mongo bằng Expression → refactor sang FilterDefinition cùng commit.
- Có overload `IMongoDbClient` cho cả 2 pattern — dùng overload nhận `FilterDefinition<T>` (xem `MongoDbClient.cs:55-67`).
- Reference apply: `PhieuHoanThanhChinhSuaService.cs:LayChiTietBySession`, `PhieuHoanThanhChinhSuaService.Imei.cs:LayTinhTrangImei`.

Liên quan: [[scrm_no_query_isdelete_for_log]] (cùng nguyên tắc tối ưu Mongo audit log query).
