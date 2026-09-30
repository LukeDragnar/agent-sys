# Recipe — Quartz job (chạy định kỳ)

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Tác vụ chạy định kỳ trên server không phụ thuộc API request (xóa data cũ, đồng bộ, gửi thông báo, batch).

## Viết job (IJob)
```csharp
[DisallowConcurrentExecution]   // tránh chạy chồng
public class JobClearDonHangPreExpired : IJob
{
    private readonly IRepository<DonHangPre> _repos;
    public JobClearDonHangPreExpired(IRepository<DonHangPre> repos) { _repos = repos; }
    public async Task Execute(IJobExecutionContext context)
    {
        try {
            var cutoff = DateTime.Now.AddDays(-30);
            var expired = await _repos.TableWithNolock()
                .Where(x => x.TrangThai == 0 && x.NgayTao < cutoff).ToListAsync();
            foreach (var pre in expired) await _repos.DeleteAsync(pre);
        } catch (Exception ex) { /* log */ }
    }
}
```

## Đăng ký AddJob + AddTrigger
Gom tất cả job vào **MỘT** `AddQuartz` block, đặt **sau** `if (IsDebug) return`:
```csharp
public static void AddJobConfig(this IServiceCollection services, IConfiguration cfg)
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var IsDebug = env == "StagingDebug" || env == "ProductionDebug";
    if (IsDebug) return;                                     // TRƯỚC AddQuartz

    services.AddQuartz(q => {
        q.UseMicrosoftDependencyInjectionJobFactory();
        var key1 = new JobKey("JobClearDonHangPreExpired");
        q.AddJob<JobClearDonHangPreExpired>(o => o.WithIdentity(key1));
        q.AddTrigger(o => o.ForJob(key1).WithIdentity("JobClear-trigger")
            .WithCronSchedule("0 0 2 * * ?"));               // 2:00 AM mỗi ngày
        // job 2: .WithSimpleSchedule(x => x.WithIntervalInSeconds(15).RepeatForever())
    });
    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
}
```

## 🔧 DEBUG job ở máy dev — "đưa job cần chạy lên TRƯỚC cửa chặn"
> Đúc kết 2026-07-30 (CustomerQueue xuất file nền). Khuôn gốc: `Contact/Contact.API/Service/JobExtention.cs:18`
> — `//debug khong chay job, job nao chay thi dua len truoc if nay`.

**Cơ chế của hệ thống: KHÔNG có cờ bật/tắt job.** Muốn debug job nào thì **đăng ký job đó trước
cửa chặn `IsDebug`**, job khác để sau. 🔴 **ĐỪNG tự nghĩ ra cờ** (`RUN_JOBS_IN_DEBUG`,
`"RunJobsInDebug": true`...) — hệ thống không có, thêm vào là rác + lệch convention.

```csharp
services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    // TODO-TRUOC-KHI-DAY: chuyển dòng dưới xuống SAU cửa chặn IsDebug.
    DangKyJobDangDebug(q);              // ← job đang debug: LÊN TRƯỚC

    //debug khong chay job, job nao chay thi dua len truoc dong nay
    if (IsDebug) return;                // ← return trong lambda: chỉ bỏ phần đăng ký còn lại

    DangKyJobConLai(q);                 // ← job khác: để SAU
});
services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```
- Đặt `if (IsDebug) return` **trong** `AddQuartz` (thay vì trước) là cách duy nhất để chạy MỘT job
  mà vẫn **gom 1 `AddQuartz` block**. Vẫn không phạm rule "không gọi AddQuartz nhiều lần".
- **Tách mỗi job thành 1 hàm `DangKyJobXxx(IServiceCollectionQuartzConfigurator q)`** → "dời job qua
  cửa chặn" chỉ là **dời 1 dòng**, không phải bê cả khối `AddJob`+`AddTrigger` (bê khối là loại sửa
  dễ để lại rác lúc trả về).
- Dán nhãn `TODO-TRUOC-KHI-DAY` ở dòng đó → trước khi đẩy `rg "TODO-TRUOC-KHI-DAY"` là ra hết.

### Debug thực tế
- Job **cùng process** với `*.API` → chỉ cần F5, **không** attach gì thêm.
- `[DisallowConcurrentExecution]` + cờ `static bool IsRunning` → **ngồi trên breakpoint thoải mái**,
  các tick sau bị bỏ qua chứ không chồng lên nhau.
- 🔴 **Exception bị `catch` + log ở nhiều tầng → KHÔNG nổi lên debugger.** Muốn thấy lỗi thật:
  *Debug → Windows → Exception Settings* → tick **Common Language Runtime Exceptions**.
  Không thì cứ tưởng "job không chạy" trong khi nó chạy rồi ném lỗi rồi ăn lỗi.
- Soi **DB nhanh hơn breakpoint**: đọc cột trạng thái/checkpoint của bảng job (`TrangThai`,
  `IsProcessing`, `RetryCount`, `NgayBatDau`, `ThongBaoLoi`) để biết nó tắc ở bước nào.

### 🔴 Job "chạy mà không làm gì" — kiểm SP/DDL đã deploy chưa TRƯỚC KHI debug tiếp
Job claim việc qua **stored procedure**: code mới trong repo có thể **kỳ vọng SP phiên bản mới**
(thêm cột trả về, thêm bước) mà DB đang là **bản cũ**. Khi đó:
- SP cũ không trả cột mới → map thành `default(0)` → không khớp `enum` nào → danh sách việc **rỗng**
  → job tick đều mỗi N giây rồi `return`, **im như không có gì**.
- Không hề có exception, không log lỗi → mất cả buổi tưởng lỗi code.

**Kiểm bằng 1 query trước khi đặt breakpoint** (read-only, an toàn):
```sql
SELECT LEN(m.definition) AS Dai,
       CASE WHEN m.definition LIKE '%dau_hieu_ban_moi%' THEN 1 ELSE 0 END AS DaDeploy
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name = 'PRC_Ten_SP';
```
Script SP để `memory/local/sql/` + gửi rời (**không** commit `.sql` vào repo code). Viết
`CREATE OR ALTER` để chạy đè an toàn.

### ⚠️ Debug job = claim việc THẬT trên staging
`*Debug` dùng **chung DB + hàng đợi với staging** → máy dev sẽ **tranh việc với pod staging đang
chạy**. Tester đang dùng staging thì kết quả có thể do máy dev sinh ra (và ghi file lên storage
staging). Bật job lúc debug là **quyết định có ý thức**, không phải mặc định — đó chính là lý do
tồn tại cửa chặn `if (IsDebug) return`.

## Bẫy thường gặp
- **AddJob phải kèm AddTrigger** — thiếu trigger thì job không chạy.
- **Gom 1 `AddQuartz` block** — không gọi `AddQuartz()` nhiều lần riêng lẻ.
- **JobKey + trigger identity duy nhất** — không trùng.
- **`if (IsDebug) return` mặc định đặt TRƯỚC** `AddQuartz` — không thì job chạy cả lúc debug.
  (Chỉ đưa vào trong khi đang cần debug đúng 1 job — xem mục DEBUG ở trên.)
- `AddQuartzHostedService` gọi 1 lần sau block.
- **Quartz in-memory, KHÔNG clustering** → scale N replica thì cả N cùng tick. Chỉ đúng khi mọi
  việc được **claim ATOMIC dưới DB** (applock / `UPDLOCK,READPAST` / `UPDATE` có điều kiện).
  Job mới mà claim bằng "select rồi update" là **xử lý trùng** khi scale.

## File source mẫu
- Job: `PreOrder/PreOrder.API/Jobs/JobClearDonHangPreExpired.cs`
- Đăng ký: `PreOrder/PreOrder.API/Service/JobExtention.cs` · gọi trong `Program.cs`
