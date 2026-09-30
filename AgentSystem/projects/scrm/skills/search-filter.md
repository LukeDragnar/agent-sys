# Recipe — Search / Filter

> **Trạng thái: ✅ Đã verify từ source SCRM thật** (quét 2026-06-19).

## Khi nào dùng
Màn hình list có form lọc nhiều điều kiện + tìm kiếm keyword. Thường đi kèm [paging.md](paging.md).

## BE — FilterDto + build query động

**FilterDto** (kế thừa `GridInfo` để có paging) — field nullable cho từng điều kiện:
```csharp
public class XxxFilterDto : GridInfo
{
    public int? TrangThai { get; set; }
    public List<int>? ListTrangThai { get; set; }
    public Guid? IdChiNhanh { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string? Keyword { get; set; }
}
```

**`TimTheoDieuKien`** — multi-tenant ĐẦU TIÊN, rồi build động:
```csharp
public async Task<IMethodResult<List<XxxDto>>> TimTheoDieuKien(XxxFilterDto filter, UserLogin user)
{
    var query = _repos.TableWithNolock().Where(x => x.IdDonVi == user.DonViId);   // tenant trước

    if (filter.TrangThai != null)
        query = query.Where(x => x.TrangThai == filter.TrangThai);
    if (filter.ListTrangThai?.Any() == true)
        query = query.Where(x => filter.ListTrangThai.Contains(x.TrangThai));
    if (filter.IdChiNhanh != null && filter.IdChiNhanh != Guid.Empty)             // FK: check Empty
        query = query.Where(x => x.IdChiNhanh == filter.IdChiNhanh);
    if (filter.TuNgay != null)
        query = query.Where(x => x.NgayLamViec >= filter.TuNgay.Value.Date);      // .Date tránh giờ/phút
    if (filter.DenNgay != null)
        query = query.Where(x => x.NgayLamViec <= filter.DenNgay.Value.Date);
    if (!string.IsNullOrEmpty(filter.Keyword)) {
        filter.Keyword = filter.Keyword.Trim();                                   // BẮT BUỘC Trim
        query = query.Where(x => x.Ma.Contains(filter.Keyword) || x.Ten.Contains(filter.Keyword));
    }

    var page = filter.Page < 1 ? 1 : filter.Page;
    var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
    var total = await query.CountAsync();
    var list = await query.OrderByDescending(x => x.NgayTao)
        .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    var data = await BuildDtoListAsync(list, user);
    return MethodResult<List<XxxDto>>.ResultWithData(data, "", total);
}
```
- Mẫu: `Branch/Branch.Service/Generated/PhieuYeuCauService.cs:801`

**Lọc nâng cao (động theo UI)** — FE gửi `tabFilter { matchMode, conditions:[{field,operator,value}] }` → BE serialize JSON → truyền vào **Stored Procedure** (không build LINQ động):
- Mẫu: `WorkFlow/WorkFlow.Service/Generated/CongViecService.cs:175` (param `@TabFilterJson`, `@MatchMode`)

## FE — form lọc + gọi search

```typescript
gridLoadData() {
  const filter = {
    ...this.getGridOption(),                                  // page, pageSize, sorts
    trangThai: this.searchModel.trangThai ?? undefined,       // ?? undefined (KHÔNG ?? null)
    idChiNhanh: this.searchModel.idChiNhanh ?? undefined,
    tuNgay: this.date?.[0] ?? undefined,
    denNgay: this.date?.[1] ?? undefined,
    keyword: this.searchModel.keyword ?? undefined,
  };
  this._spinner.show();
  this._service.TimTheoDieuKien(filter).subscribe(res => {
    this._spinner.hide();
    if (res.success) { this.gridData = res.data ?? []; this.totalRecord = res.totalRecord ?? 0; }
  });
}
onSearch()      { this.page = 1; this.gridLoadData(); }       // RESET page
onResetFilter() {
  this.searchModel = { trangThai: null, idChiNhanh: null };
  this.date = this.buildDefaultDateRange();
  this.page = 1; this.gridLoadData();
}
```
- Mẫu: `src/app/pages/major/phieu-yeu-cau/phieu-yeu-cau.component.ts:134`
- Lọc nâng cao (merge tab + local conditions, sort 'asc'/'desc'→1/-1): `pages/work/danh-sach-cong-viec/cong-viec-workspace/cong-viec-workspace.component.ts:601`

## Bẫy thường gặp
- **`?? undefined` (không `?? null`)** — gửi `null` xuống BE bị hiểu là "lọc theo null", không phải "bỏ lọc".
- **Reset `page = 1`** mỗi khi đổi filter/search/reset.
- **Trim keyword** ở BE trước khi `Contains`.
- **FK check `Guid.Empty`**: `if (id != null && id != Guid.Empty)` — `Guid.Empty` không phải null nhưng cũng = "không lọc".
- **`DateTime.Date`** khi so sánh khoảng ngày để bỏ phần giờ/phút/giây.
- **Multi-tenant** luôn nằm ở `.Where` đầu tiên, không phụ thuộc filter từ client (tránh rò data cross-tenant).
- List page thường **không debounce** — search trigger bằng nút/Enter, không auto khi gõ.

## File source mẫu
- BE: `Branch/Branch.Service/Generated/PhieuYeuCauService.cs:801` · nâng cao `WorkFlow/.../CongViecService.cs:175`
- FE: `pages/major/phieu-yeu-cau/phieu-yeu-cau.component.ts:134` · nâng cao `pages/work/danh-sach-cong-viec/cong-viec-workspace/cong-viec-workspace.component.ts:601`
