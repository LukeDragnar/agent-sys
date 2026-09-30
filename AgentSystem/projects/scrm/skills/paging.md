# Recipe — Phân trang (Paging)

> **Trạng thái: ✅ Đã verify từ source SCRM thật** (quét 2026-06-19).

## Khi nào dùng
Màn hình list/table có phân trang server-side. Page **bắt đầu từ 1** (không phải 0).

## BE — trả dữ liệu phân trang

**1. FilterDto kế thừa `GridInfo`** (chứa `PageInfo { Page, PageSize }` + `Sorts`):
- `Libs/Models.Core/GridInfo.cs`, `Libs/Models.Core/PageInfo.cs`
```csharp
public class XxxFilterDto : GridInfo   // GridInfo có PageInfo.Page (từ 1), PageInfo.PageSize
{
    public string? Keyword { get; set; }
    public Guid? IdChiNhanh { get; set; }
}
```

**2. Service: Count TRƯỚC Skip/Take, trả qua `TotalRecord`:**
```csharp
var total = await query.CountAsync();                  // tổng SAU filter, TRƯỚC paging
var data = await query.OrderByDescending(x => x.NgayTao)
    .Skip((page - 1) * pageSize).Take(pageSize)        // công thức: (Page-1)*PageSize
    .ToListAsync();
return MethodResult<List<XxxDto>>.ResultWithData(data, "", total);  // total là param thứ 3
```
- `IMethodResult<T>.TotalRecord` là field tổng (≠ số dòng trang hiện tại) — `Libs/SB.Core/Base/IMethodResult.cs`
- `ResultWithData(data, message, totalRecord)` — `Libs/SB.Core/Base/BaseWebApiResponse.cs:21`
- **Validate:** `page = filter.Page < 1 ? 1 : filter.Page;` · `pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;`
- Mẫu thật: `Branch/Branch.Service/Generated/PhieuYeuCauService.cs:801`

## FE — bind phân trang (ng-zorro)

**BaseListComponentV1** có sẵn `page=1`, `pageSize`, `totalRecord`, `pageSizeOption=[10,20,50,100,1000]` — `src/app/constants/baseComponentV1.ts`

**HTML:**
```html
<nz-pagination
    [nzPageIndex]="page" [nzTotal]="totalRecord" [nzPageSize]="pageSize"
    nzShowSizeChanger [nzPageSizeOptions]="pageSizeOption"
    (nzPageIndexChange)="changePageIndex($event)"
    (nzPageSizeChange)="changePageSize($event)">
</nz-pagination>
```

**TS** — gom `getGridOption()` (page/pageSize/sorts) + searchModel rồi gọi API:
```typescript
gridLoadData() {
  const filter = { ...this.getGridOption(), ...this.searchModel };
  this.spinner.show();
  this._service.danhSach(filter).then(rs => {
    this.spinner.hide();
    if (rs.success) { this.gridData = rs.data; this.totalRecord = rs.totalRecord; }
  });
}
changePageIndex(e: number) { this.page = e; this.gridLoadData(); }
changePageSize(e: number)  { this.pageSize = e; this.gridLoadData(); }
```
- Handler chuẩn ở `src/app/constants/baseComponent.ts:170`
- Mẫu thật: `src/app/pages/major/thuchi/so-thu-chi/so-thu-chi.component.ts`

## Bẫy thường gặp
- **Reset `page = 1` khi search/filter** — nếu đang ở trang 5 mà lọc lại thì vẫn fetch trang 5 (trống). `onSearch()` phải set `this.page = 1` trước `gridLoadData()`.
- **Tính total trước Skip/Take** — Count sau khi đã Skip sẽ ra sai.
- **Aggregate (SUM) chỉ tính ở trang 1** để tránh lặp: `if (this.page == 1) { ... }`.
- Bảng **rất lớn**: cân nhắc đếm bằng `sys.partitions` thay `COUNT(*)` — xem [qa-code.md](../qa-code.md) mục 1.

## File source mẫu
- BE: `Branch/Branch.Service/Generated/PhieuYeuCauService.cs:801`
- FE: `src/app/pages/major/thuchi/so-thu-chi/so-thu-chi.component.ts` · base: `constants/baseComponent.ts:170`
