---
name: reference-ecommerce-connect-deleteasync-persists-dirty
description: Trong project EcommerceConnect, reposXxx.DeleteAsync(entity) tự persist cả dirty fields (Description, InProcess, ...) cùng với IsDeleted=true. KHÔNG cần UpdateAsync riêng trước DeleteAsync.
metadata:
  type: reference
---

# EcommerceConnect — `DeleteAsync` persist dirty fields cùng soft-delete

## Rule

Trong project `ecommerce_connect`, pattern soft-delete chuẩn cho `DataReceive` / temp tables là:

```csharp
dataReceive.Description = "Lý do soft-delete";
dataReceive.MarkDirty(nameof(dataReceive.Description));
await reposDataReceive.DeleteAsync(dataReceive);
```

**1 hit DB.** `DeleteAsync` tự set `IsDeleted=true` + persist mọi field đã `MarkDirty` trước đó.

## KHÔNG làm

```csharp
// ❌ THỪA — 2 hit DB
await reposDataReceive.UpdateAsync(dataReceive);
await reposDataReceive.DeleteAsync(dataReceive);
```

## Why

Repository `EfRepository<T>.DeleteAsync` trong project này không phải hard-delete — nó implement soft-delete bằng cách set `IsDeleted=true` rồi gọi UpdateAsync internally, persist toàn bộ dirty fields đã MarkDirty.

## How to apply

- Khi soft-delete kèm message/state update → MarkDirty các field cần persist + DeleteAsync.
- Pattern existing trong code: [OrderTiktokService.cs:181-183](D:/PROJECT/Git/_SourceForCode/ecommerce_connect/EcommerceConnect.Service/Generated/MySystem/OrderTiktokService.cs#L181), [OrderReturnTiktokService.cs:104-107](D:/PROJECT/Git/_SourceForCode/ecommerce_connect/EcommerceConnect.Service/Generated/MySystem/OrderReturnTiktokService.cs#L104), [PushMechanismService.cs:84-86](D:/PROJECT/Git/_SourceForCode/ecommerce_connect/EcommerceConnect.Service/Generated/Shopee/PushMechanismService.cs#L84).
- Khi QA code: KHÔNG flag pattern `MarkDirty → DeleteAsync` là "Description không persist" — đó là false positive.

## Khác biệt với SCRM

SCRM (`OrderLogistic`) có thể có behavior khác — chưa verify. Memory này chỉ áp dụng `ecommerce_connect`.

## Bối cảnh

Phát hiện 2026-06-08 khi QA luồng OrderReturnShopee, user (thiennq) chỉ ra rằng pattern hiện tại không cần UpdateAsync riêng.
