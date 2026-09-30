# Recipe: Confirm-on-dirty (chặn đóng khi có thay đổi chưa lưu) — drawer/form ng-zorro

**Khi nào dùng:** drawer/modal sửa dữ liệu → user lỡ đóng/click mask khi đang sửa dở → mất dữ liệu. Cần popup "Bạn có thay đổi chưa lưu, đóng không?".

Rút từ `ticket-detail-drawer` + `sandbox-issue-form` (session 2026-07-20).

## 1. Snapshot chữ ký lúc mở + so lúc đóng
Chụp "chữ ký" toàn bộ field edit ngay sau khi load xong (cuối `ngOnInit`/sau khi patch form):
```ts
private _snapshotForm = '';
chuKyForm(): string {
  return JSON.stringify({
    tieuDe: this.editTieuDe, phanHoi: this.editPhanHoi, /* ...các field edit... */
    tuNgay: this.editTuNgay?.getTime() ?? null,           // Date → getTime (so được)
    fileIds: (this.files || []).map(f => f.id).sort(),     // list → sort cho ổn định
  });
}
// cuối ngOnInit / sau patch:
this._snapshotForm = this.chuKyForm();

coThayDoi(): boolean { return this.chuKyForm() !== this._snapshotForm; }
```
> ⚠️ Chụp snapshot **SAU khi patch dữ liệu vào form**, không phải trước — nếu không sẽ luôn báo dirty.

## 2a. Template drawer (`<nz-drawer [nzVisible]>`): chặn ở `onClose()`
```ts
onClose() {
  if (this.coThayDoi()) {
    this._modal.confirm({
      nzTitle: 'Có thay đổi chưa lưu',
      nzContent: 'Đóng và bỏ thay đổi?',
      nzOnOk: () => this.close.emit(),   // OK → đóng thật
    });
    return;
  }
  this.close.emit();
}
```

## 2b. Service drawer (`_drawer.create(...)`): dùng `nzOnCancel`
```ts
this._drawer.create({
  nzMaskClosable: true,                 // cho đóng khi click mask (vẫn qua confirm)
  nzContent: XxxComponent,
  nzOnCancel: () => new Promise<boolean>(resolve => {
    const cmp = drawerRef.getContentComponent();
    if (!cmp.coThayDoi()) return resolve(true);        // sạch → đóng
    this._modal.confirm({
      nzTitle: 'Có thay đổi chưa lưu', nzContent: 'Đóng và bỏ thay đổi?',
      nzOnOk: () => resolve(true),      // đóng
      nzOnCancel: () => resolve(false), // ở lại
    });
  }),
});
```
> `nzOnCancel` resolve(false) = ở lại, resolve(true) = đóng. Áp cho cả nút X, ESC, click mask.

## Bẫy
- Không `nzMaskClosable: true` → click mask không đóng được (khó chịu). Bật nó, để confirm lo phần "chưa lưu".
- Field date so bằng object `!==` luôn khác → phải `.getTime()`. List file so phải `.sort()` trước.
- Snapshot chụp sai thời điểm (trước patch) → false-positive "có thay đổi".

Liên quan: `modal-form.md`, `save-luu.md`, `validate-form.md`.
