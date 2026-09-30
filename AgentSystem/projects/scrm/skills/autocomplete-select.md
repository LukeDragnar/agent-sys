# Recipe — Autocomplete / search-select

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Gõ để tìm + chọn 1 item từ danh sách lớn (khách hàng, sản phẩm, nhân viên) — search server-side + load thêm khi scroll.

## FE — nz-select server search (debounce)
```html
<nz-select [(ngModel)]="_myObject" [nzServerSearch]="true" nzShowSearch nzAllowClear
           [nzMode]="isMultiChoose ? 'multiple' : 'default'"
           (nzOnSearch)="onSearchSPKeyword($event)" (nzScrollToBottom)="onScrollSanPham()"
           (ngModelChange)="onSelectionChange($event)">
  <nz-option *ngFor="let item of dataChild" [nzLabel]="item?.ten" [nzValue]="item?.id"></nz-option>
</nz-select>
```
```typescript
onSearchSPKeyword(value: string) {
  this.keySearchSanPham = value; this.pageSP = 1; this.dataChild = [];   // clear data cũ
  this.onDebounce(this.getSanPhamCon, value, 300, 20);                   // debounce 300ms
}
onScrollSanPham() {                                                      // load page tiếp
  this.pageSP++;
  this.onDebounceScrollLoadPageCustom(this.getSanPhamCon, this.keySearchSanPham, 300, 20, this.pageSP);
}
getSanPhamCon(filter) {
  this._dropdownListService.DropdownSanPhamChild({ ...filter, listIdExisted: this.listNotGet })
    .then((res: ResponseData) => { if (res.success) this.dataChild = [...this.dataChild, ...res.data]; });  // append
}
```
> `onDebounce` / `onDebounceScrollLoadPageCustom` có sẵn trong `baseComponent.ts`.

## BE — endpoint search
```csharp
public async Task<IMethodResult<List<DropdownCommon>>> DropdownSanPhamChildAutoComplete(
    SanPhamSearchAutoCompletePayload payload, Guid idDonVi, UserLogin? user)
{
    var query = _repos.TableWithNolock().Where(x => x.IdDonVi == idDonVi);
    if (!string.IsNullOrEmpty(payload.Keyword)) {
        payload.Keyword = payload.Keyword.Trim().ToLower();
        query = query.Where(a => (a.Ten != null && a.Ten.ToLower().Contains(payload.Keyword))
                              || (a.Ma  != null && a.Ma.ToLower().Contains(payload.Keyword)));
    }
    var data = await query.OrderBy(x => x.Ten)
        .Skip((payload.PageInfo.Page - 1) * payload.PageInfo.PageSize).Take(payload.PageInfo.PageSize)
        .Select(sp => new DropdownCommon { Id = sp.Id, Ten = sp.Ten, Ma = sp.Ma }).ToListAsync();
    return MethodResult<List<DropdownCommon>>.ResultWithData(data);
}
```

## Bẫy thường gặp
- **Debounce 300ms** tránh gọi API mỗi ký tự; gõ keyword mới → clear `dataChild` + reset page.
- **Scroll load thêm:** `(nzScrollToBottom)` → page++ cùng keyword, **append** không ghi đè.
- Trả về object qua `(ngModelChange)` (không chỉ id) nếu cha cần data.
- Loại item đã chọn bằng `ListIdExisted` ở BE.

## File source mẫu
- FE: `components/drop-down-list-sanpham/sanphamcon-autocomplete/` (.html + .ts) · base `constants/baseComponent.ts` (onDebounce)
- BE: `WarehouseProduct/.../Common/CommonController.cs` · `CommonSanPhamService.cs`
