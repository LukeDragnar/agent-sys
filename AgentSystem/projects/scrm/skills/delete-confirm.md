# Recipe — Xóa + confirm

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Xóa bản ghi với modal xác nhận. Mặc định **soft delete** (`IsDelete=true`); queue/temp table dùng **hard delete** (`removeInDatabase:true`).

## FE — icon Xóa trong bảng (trigger)
Action **Xóa** ở cột "Thao tác" = **icon trash ĐỎ clickable + tooltip**. **KHÔNG** dùng `<a nzDanger>`.
```html
<i nz-icon nzType="delete" nzTheme="outline" (click)="deleteRow(...)" nz-tooltip nzTooltipTitle="Xóa"
   style="color: red; font-size: 16px; cursor: pointer;"></i>
```
**Why:** `nzDanger` chỉ áp màu đỏ cho `nz-button`, KHÔNG cho thẻ `<a>` → `<a nzDanger><i nz-icon nzType="delete"></i></a>` ra **icon xanh** (sai). Đa số bảng list dùng `<i nz-icon nzType="delete" style="color:red;...">`.
- KHÔNG tự đổi style nút có sẵn khi user chưa yêu cầu (đã bị nhắc "nhầm đấy đừng thay đổi"). Chỉ chỉnh khi user bảo "cho giống style chung". Bám style màn đang sửa.

## FE — confirm rồi xóa
```typescript
onXoa(id: any): void {
  this._modalService.confirm({
    nzTitle: this.tsb.t('tlt-xac-nhan-xoa', 'Xác nhận xóa'),
    nzContent: this.tsb.t('msg-ban-co-chac-chan-muon-xoa', 'Bạn có chắc chắn muốn xóa?'),
    nzOkText: this.tsb.t('btn-xoa', 'Xóa'), nzOkDanger: true,
    nzCancelText: this.tsb.t('btn-huy', 'Hủy'),     // nzOnCancel ĐỂ TRỐNG (tự đóng)
    nzOnOk: () => {
      this._spinner.show();
      this._service.XoaTheoId(id).then(rs => {
        this._spinner.hide();
        if (rs.success) { this._notification.success(MESSAGE.SUCCESS, MESSAGE.DELETE_SUCCESS); this.gridLoadData(); }
        else this._notification.error(MESSAGE.ERROR, rs.message);
      }).catch(() => { this._spinner.hide(); this._notification.error(MESSAGE.ERROR, MESSAGE.SYSTEM_ERROR); });
    },
  });
}
```

## BE — XoaTheoIdAsync (soft delete)
```csharp
public async Task<IMethodResult<object>> XoaTheoIdAsync(Guid id, UserLogin userLogin)
{
    var entity = await _repos.GetByIdAsync(id);
    if (entity == null || entity.IdDonVi != userLogin.DonViId)
        return MethodResult<object>.ResultWithError(Contants.MESS_NotFound, Contants.CODE_NotFound);
    await _repos.DeleteAsync(entity);            // soft delete: set IsDelete=true, NgayXoa, NguoiXoaUserId
    return MethodResult<object>.ResultWithData(true);
}
// Queue/temp: await _queueRepos.DeleteAsync(rec, removeInDatabase: true);   // hard delete
```
`DeleteAsync(entity, removeInDatabase=false)` ở `EfRepository.cs` — false = soft, true = xóa DB thật.

## Bẫy thường gặp
- **`nzOnCancel` để trống** — không gọi lại API với cờ false (BE coi như submit).
- **Soft vs hard:** master data soft delete (có lịch sử khôi phục); **queue table dùng `removeInDatabase:true`** tránh tích rác.
- Xóa kèm child: xóa child trước (không có FK constraint nhưng dữ liệu logic), bọc transaction nếu nhiều bảng.
- Check tenant (`IdDonVi == userLogin.DonViId`) trước khi xóa.

## File source mẫu
- FE: `pages/work/dm-trang-thai/dm-trang-thai.component.ts:91`
- BE: `WorkFlow/WorkFlow.Service/Generated/CongViecService.cs:2797` · `EfRepository.cs` (DeleteAsync + removeInDatabase)
