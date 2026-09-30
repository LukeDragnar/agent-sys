---
name: SCRM Backend Code Conventions
description: Conventions và patterns khi viết code SCRM Backend (Entity, Service, Controller)
type: feedback
---

# SCRM Backend — Code Conventions & Patterns

## 1. Database Query — Bắt buộc

✅ **LUÔN dùng `.TableWithNolock()`** — tránh deadlock
```csharp
var data = await _repo.TableWithNolock()
    .Where(x => x.IdDonVi == userLogin.DonViId && ...)
    .ToListAsync();
```

✅ **LUÔN filter `IdDonVi`** — multi-tenant safety
```csharp
.Where(x => x.IdDonVi == userLogin.DonViId)
```

✅ **Filter `IdChiNhanh` nếu user không phải admin**
```csharp
if (!userLogin.IsAdmin && !userLogin.IsAdminNhieuChiNhanh())
{
    gridInfo.IdChiNhanh = userLogin.IdChiNhanh;
}
query = query.Where(x => 
    gridInfo.IdChiNhanh == null || x.IdChiNhanh == gridInfo.IdChiNhanh
);
```

✅ **Update entity dùng `.MarkDirty()` pattern**
```csharp
entity.FieldName = newValue;
entity.MarkDirty(nameof(entity.FieldName));
await _repo.UpdateAsync(entity);
```

## 2. Service Method Signatures

✅ **Return type: `IMethodResult<T>`**
```csharp
public async Task<IMethodResult<Guid>> SaveEntity(ReqSaveDto request, UserLogin userLogin)
public async Task<IMethodResult<object>> TimTheoDieuKien(ReqFilterDto gridInfo, UserLogin userLogin)
public async Task<IMethodResult<DetailDto>> TimTheoId(Guid id, UserLogin userLogin)
```

✅ **Method naming:**
- Query: `TimTheoDieuKien`, `TimTheoId`, `TimTheoXyz`
- Create/Update: `Save` (chung cho cả 2)
- Delete: `Xoa`
- Logic methods: `CreateJobXyz`, `ProcessXyz`, `HandleXyz`

✅ **Mọi public method phải có `UserLogin userLogin` parameter** (except special cases)

## 3. Logging — SetInfoBase Pattern

✅ **LUÔN call `_logDataService.SetInfoBase()` ở đầu method**
```csharp
public async Task<IMethodResult<Guid>> Save(ReqSaveDto request, UserLogin userLogin)
{
    _logDataService.SetInfoBase(nameof(Save), request.EcommerceCode, null, userLogin);
    
    try
    {
        // logic
    }
    catch (Exception ex)
    {
        _logDataService._messError += LogDataService.FormatLogDataException(ex);
        return MethodResult<Guid>.IsError($"Lỗi: {_logDataService._idTrans}");
    }
    finally
    {
        _logDataService._messInfo += LogDataService.FormatLogData(nameof(ReqSaveDto), request);
        await _logDataService.WriteLog(_logDataService._messInfo, _logDataService._messError);
    }
}
```

## 4. Job/Queue Pattern

✅ **Job entity phải có fields:**
```csharp
public Guid IdDonVi, IdChiNhanh;  // tenant
public string? Status;             // PENDING, PROCESSING, SUCCESS, FAILED
public string? Message;            // error detail
public bool IsProcessing;          // lock flag
public int ResendNo;               // retry count (optional)
```

✅ **Processing logic:**
```csharp
// 1. Query pending jobs
var jobs = await _repo.TableWithNolock()
    .Where(x => x.IdDonVi == tenantId && x.Status == "PENDING" && !x.IsProcessing)
    .Take(batchSize)
    .ToListAsync();

// 2. Lock jobs
foreach (var job in jobs)
{
    job.IsProcessing = true;
    job.MarkDirty(nameof(job.IsProcessing));
}
await _repo.UpdateAsync(jobs);

// 3. Process
var updateList = new List<JobEntity>();
foreach (var job in jobs)
{
    try
    {
        // do work
        job.Status = "SUCCESS";
        job.Message = "";
    }
    catch (Exception ex)
    {
        job.Status = "FAILED";
        job.Message = ex.Message;
    }
    finally
    {
        job.IsProcessing = false;
        job.MarkDirty(nameof(job.Status));
        job.MarkDirty(nameof(job.Message));
        job.MarkDirty(nameof(job.IsProcessing));
        updateList.Add(job);
    }
}

// 4. Batch update once
if (updateList.Any())
{
    await _repo.UpdateAsync(updateList);
}
```

## 5. Entity Base Class

✅ **Mọi entity kế thừa từ `BaseEntity`**
```csharp
public class MyEntity : BaseEntity
{
    public Guid IdDonVi { get; set; }
    public Guid? IdChiNhanh { get; set; }
    // fields...
}
// BaseEntity tự cung cấp: Id, IsDeleted, NgayTao, NgayCapNhat, NguoiTao, NguoiCapNhat
```

## 6. RabbitMQ Publish Pattern

✅ **Message class phải có:**
```csharp
public class MyMessage
{
    public Guid IdDonVi { get; set; }
    public Guid IdChiNhanh { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    // payload...
}

// Publish:
await _publishEndpoint.Publish<MyMessage>(model);
```

## 7. Stored Procedure Check Duplicate

✅ **Pattern để check trùng lặp (SP)**
```csharp
// Database.SqlQuery<bool>(
//     $"EXEC PRC_SCRM_Module_Entity_GetByKey @p0",
//     entityId
// ).FirstOrDefault()

// Or use LINQ if possible
var existed = await _repo.TableWithNolock()
    .FirstOrDefaultAsync(x => 
        x.IdDonVi == userLogin.DonViId &&
        x.UniqueField == request.UniqueField
    );

if (existed != null)
{
    return MethodResult<Guid>.IsError("Record đã tồn tại");
}
```

## 8. Response DTO Pattern

✅ **Naming:** `Res{Action}{Entity}` hoặc `{Entity}Dto`
```csharp
public class ResSaveEntityDto
{
    public Guid Id { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }
}

// Or list response
public class ResListEntityDto
{
    public List<EntityDto> Data { get; set; }
    public int Total { get; set; }
}
```

✅ **Request DTO naming:** `Req{Action}{Entity}` hoặc `{Entity}FilterDto`
```csharp
public class ReqSaveEntityDto
{
    public Guid? Id { get; set; }  // null = create, có giá trị = update
    public string? Name { get; set; }
    // ...
}

public class ReqFilterEntityDto
{
    public string? Keyword { get; set; }
    public Guid? IdChiNhanh { get; set; }
    public GridInfo PageInfo { get; set; }
    public List<SortInfo>? Sorts { get; set; }
}
```

## 9. Validation & Error Handling

✅ **Validate trước logic**
```csharp
if (string.IsNullOrEmpty(request.Name))
    return MethodResult<Guid>.IsError("Tên không được để trống");

if (!request.IdDonVi.HasValue || request.IdDonVi == Guid.Empty)
    return MethodResult<Guid>.IsError("DonVi không hợp lệ");
```

✅ **Error messages tiếng Việt, có context**
```csharp
return MethodResult<Guid>.IsError("Không tìm thấy đơn hàng");
return MethodResult<Guid>.IsError($"Lỗi tạo phiếu ({ex.Message})");
```

## 10. Transaction Pattern

✅ **Dùng EF Transaction để ensure atomicity**
```csharp
using (var transaction = await _dbContext.Database.BeginTransactionAsync())
{
    try
    {
        await _repoMain.InsertAsync(mainEntity);
        await _repoDetail.InsertAsync(detailEntities);
        
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        
        // Publish after commit (optional)
        await _publishEndpoint.Publish(message);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## Checklist khi viết Service method

- ☐ Có `UserLogin userLogin` parameter
- ☐ Validate input (null check, format check)
- ☐ Filter `IdDonVi` trong WHERE
- ☐ Dùng `.TableWithNolock()`
- ☐ Dùng `.MarkDirty()` khi update
- ☐ Call `SetInfoBase()` ở đầu method
- ☐ Xử lý exception + log error
- ☐ Return `IMethodResult<T>` (Success/Error)
- ☐ Tiếng Việt trong error message
- ☐ Job phải lock + batch update nếu là async job
- ☐ Publish message sau khi transaction commit (nếu có)
