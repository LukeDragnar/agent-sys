# Recipe — Dropdown/combo load + cache

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Load danh mục (chi nhánh, sản phẩm, tài khoản...) vào `nz-select`. Danh mục ít đổi → cache Redis ở BE.

## BE — trả danh mục (+ cache Redis)
```csharp
[HttpPost("GetComboByIds")]
public async Task<IActionResult> GetComboByIds(List<Guid> ids)
    => ResponseResult(await _service.GetComboByIds(ids, CurrentUser));

public async Task<IMethodResult<List<ComBoSpa>>> GetComboByIds(List<Guid> ids, UserLogin user)
{
    var combo = await _repos.TableWithNolock()
        .Where(cb => !cb.IsDelete && ids.Contains(cb.Id) && cb.IdDonVi == user.DonViId)
        .ToListAsync();
    return MethodResult<List<ComBoSpa>>.ResultWithData(combo);
}
```
Cache pattern (Redis→miss→DB→set) qua `ICommonCacheService` + `IRedisCacheService`, key trong `CommonKeyCache`. Sau khi sửa danh mục → `XoaCacheCombo()` / `RemoveAsync(key)` để invalidate.

## FE — nz-select bind
```html
<nz-select [(ngModel)]="searchModel.idChiNhanh" nzShowSearch nzAllowClear
           [nzPlaceHolder]="'plh-chi-nhanh' | tsb : 'Chi nhánh'" (ngModelChange)="onSearch()">
  <nz-option *ngFor="let item of listChiNhanh" [nzLabel]="item.tenChiNhanh" [nzValue]="item.id"></nz-option>
</nz-select>
```
```typescript
ngOnInit() { this.getChiNhanh(); }
getChiNhanh() {
  this._chiNhanhService.DonViLayListChiNhanh().then((res: ResponseData) => {
    if (res.success) this.listChiNhanh = res.data;
  });
}
```

## Bẫy thường gặp
- **Clear cache sau khi sửa danh mục** — không thì dropdown vẫn data cũ.
- `nzShowSearch` lọc theo `nzLabel` → phải set `[nzLabel]` đúng.
- List lớn: dùng `trackBy` trong `*ngFor` tránh re-render.
- Sau create danh mục → reload list dropdown (`getChiNhanh()`).

## File source mẫu
- BE: `WarehouseProduct/.../ComBoSpaController.cs:206` · `ComBoSpaService.cs:2047` · cache `Accounting/.../Cache/CommonCacheService.cs`
- FE: `pages/major/thuchi/so-thu-chi/so-thu-chi.component.ts:240` (load trong ngOnInit) · service `chinhanh.service.ts`
