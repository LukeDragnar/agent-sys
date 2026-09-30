---
name: DI checklist khi thêm service / entity mới
description: Mỗi lần thêm Service hoặc Entity mới trong .NET project (ecommerce_connect, scrm BE, etc.) phải check 3-4 chỗ DI registration; quên là code build OK nhưng runtime crash.
type: feedback
---

Khi thêm Service mới hoặc Entity mới trong .NET project, **BẮT BUỘC** check DI ở các chỗ sau trước khi báo xong:

**Why:** User feedback (2026-05-12): "bạn rất hay thiếu DI service hoặc entity đó để ó nhé". Code compile OK nhưng runtime fail khi resolve service hoặc query entity chưa map DbSet. Build pass != feature works.

**How to apply:**

### 1. Entity mới (Entity inherit `EntityBase`)
- ☐ Thêm `public virtual DbSet<{Entity}> {Entity} { get; set; }` vào DbContext (vd: `DeliveryConnectDataContext.cs` cho ecommerce_connect)
- ☐ Tạo migration SQL nếu chưa có

### 2. Service mới (Interface `I*Service` + class)
- ☐ `services.AddScoped<I*Service, *Service>();` trong file DI extension (ecommerce_connect: `DependencyInjectionService.cs`; scrm: tương tự)
- ☐ Đặt vào đúng `#region` (MySystem / Crm / Shopee / Tiktok / Lazada / ...)

### 3. Hosted service / Job
- ☐ `services.AddHostedService<Job*>();` trong `JobExtention.cs` (production block) + cả debug block nếu cần `IsDebug=true`

### 4. IRepository<T>
- ✅ Generic `AddScoped(typeof(IRepository<>), typeof(EfRepository<>))` đã register sẵn — không cần thêm. Chỉ cần entity có DbSet là resolved.

### Workflow tự verify
1. Sau khi viết code: grep `AddScoped` cho service mới — phải tìm thấy
2. Grep `DbSet<{Entity}>` — phải tìm thấy
3. Build solution: `dotnet build` — 0 errors
4. Nếu refactor constructor (đổi DI dependencies), check call sites của constructor và registration line phải khớp

### Common miss patterns (đã từng gặp)
- Tạo `IOrderReturnTiktokService` nhưng quên `AddScoped` → runtime resolve fail
- Tạo entity `OrderReturnTempTiktok` nhưng quên DbSet → EF Core không track, repository không query được
- Refactor constructor swap `IProductConnectService` → `IRepository<ProductConnect>` — generic IRepository auto-resolved nên không cần update DI, nhưng phải verify

### Khi nào áp dụng
- Mỗi task có "create service" hoặc "create entity" trong scope
- Trước khi build verify
- Trước khi báo "xong" với user
