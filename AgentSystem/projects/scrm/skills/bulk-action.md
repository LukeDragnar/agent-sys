# Recipe — Bulk action (chọn nhiều dòng)

> **Trạng thái: 🟡 FE verify từ source; BE là pattern khung** (quét 2026-06-19). Khi áp dụng BE, đối chiếu service thật + tuân [qa-code.md](../qa-code.md) (`TableWithNolock`, `ResultWithData`, `MarkDirty`).

## Khi nào dùng
Chọn nhiều dòng (checkbox) xử lý hàng loạt: xóa nhiều, đổi trạng thái nhiều. FE gửi `List<Guid>` → BE xử lý bulk.

## FE — chọn nhiều (checkbox)
```typescript
setOfCheckedId = new Set<string>();
checked = false; indeterminate = false;
onAllChecked(c: boolean) { this.gridData.forEach(i => this.updateCheckedSet(i.id, c)); this.refreshCheckedStatus(); }
onItemChecked(id: string, c: boolean) { this.updateCheckedSet(id, c); this.refreshCheckedStatus(); }
updateCheckedSet(id: string, c: boolean) { c ? this.setOfCheckedId.add(id) : this.setOfCheckedId.delete(id); }
refreshCheckedStatus() {
  this.checked = this.gridData.every(({ id }) => this.setOfCheckedId.has(id));
  this.indeterminate = this.gridData.some(({ id }) => this.setOfCheckedId.has(id)) && !this.checked;
}
onXoaNhieu() {
  if (this.setOfCheckedId.size === 0) { this._notification.warning(MESSAGE.WARNING, 'Chọn ít nhất 1 dòng'); return; }
  this._modalService.confirm({
    nzTitle: 'Xác nhận xóa', nzContent: `Xóa ${this.setOfCheckedId.size} bản ghi đã chọn?`, nzOkDanger: true,
    nzOnOk: () => {
      const ids = Array.from(this.setOfCheckedId);
      this._spinner.show();
      this._service.XoaNhieu(ids).then(res => {
        this._spinner.hide();
        if (res?.success) { this._notification.success(MESSAGE.SUCCESS, MESSAGE.DELETE_SUCCESS);
          this.setOfCheckedId.clear(); this.gridLoadData(); }
        else this._notification.error(MESSAGE.ERROR, res?.message);
      });
    },
  });
}
```
```html
<th nzWidth="40px" nzLeft [nzChecked]="checked" [nzIndeterminate]="indeterminate" (nzCheckedChange)="onAllChecked($event)"></th>
<td nzLeft [nzChecked]="setOfCheckedId.has(item.id)" (nzCheckedChange)="onItemChecked(item.id, $event)"></td>
```

## BE — xử lý List<Guid> (pattern, theo qa-code)
```csharp
public async Task<IMethodResult<object>> XoaNhieu(List<Guid> ids, UserLogin user)
{
    if (ids == null || ids.Count == 0) return MethodResult<object>.ResultWithError("Danh sách trống");
    if (ids.Count > 1000)             return MethodResult<object>.ResultWithError("Tối đa 1000 bản ghi/lần");
    var list = await _repos.TableWithNolock()
        .Where(x => x.IdDonVi == user.DonViId && ids.Contains(x.Id)).ToListAsync();
    // validate điều kiện TẤT CẢ trước; bọc transaction (CreateExecutionStrategy) nếu nhiều bảng
    await _repos.DeleteAsync(list);                       // soft delete bulk
    return MethodResult<object>.ResultWithData(true);
}
```

## Bẫy thường gặp
- **Giới hạn số lượng** (UI cap + BE validate ~1000) tránh timeout/memory.
- **Validate tất cả trước khi chạy** — 1 dòng fail thì return, không xử lý nửa vời (hoặc skip + báo rõ).
- **Bọc transaction** khi đụng nhiều bảng (xem [patterns.md](../patterns.md) Transaction).
- **Clear `setOfCheckedId` khi search/reload** — tránh xóa nhầm selection cũ.
- Confirm + spinner + disable nút khi đang chạy.

## File source mẫu
- FE: `pages/WarehouseProduct/quan-ly-don-dat-hang/quan-ly-don-dat-hang.component.ts` (.ts + .html, checkbox selection)
- BE: đối chiếu service bulk thật (`InsertAsync(list)`/`DeleteAsync(list)`) trước khi dùng.
