# Recipe — Validate reactive form (Angular + ng-zorro)

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Form thêm/sửa dùng `FormGroup` + `Validators` + hiển thị lỗi ng-zorro.

## Khai báo + init form (ĐÚNG cách)
Dự án bật `useDefineForClassFields: true` → **KHÔNG** dùng field initializer (`this.fb` chưa gán). Khai báo type, init trong **constructor body** (hoặc ngOnInit):
```typescript
form: FormGroup;                                   // chỉ khai báo, KHÔNG = this.fb.group(...)
constructor(private fb: FormBuilder) {
  this.form = this.fb.group({
    ten: [null, [Validators.required]],
    soDienThoai: [null, [Validators.required, Validators.pattern(/^[0-9]{10}$/)]],
  });
}
```

## Template hiển thị lỗi (ng-zorro)
```html
<nz-form-item>
  <nz-form-label nzRequired>Tên</nz-form-label>
  <nz-form-control [nzErrorTip]="'Vui lòng nhập tên'">
    <input nz-input formControlName="ten" />
  </nz-form-control>
</nz-form-item>
```

## Validate trước submit
```typescript
onLuu() {
  if (this.form.invalid) {
    Object.values(this.form.controls).forEach(c => { c.markAsDirty(); c.updateValueAndValidity(); });
    return;                          // hoặc this.markFormGroupTouched(this.form)
  }
  this._service.luu(this.form.value).then(...);
}
```

## Bẫy thường gặp
- **`useDefineForClassFields`** → init form trong constructor body, không phải field initializer. Đây là lỗi phổ biến nhất (`this.fb` undefined).
- **Quên `markAsDirty`/`markAllAsTouched`** trước submit → lỗi không hiển thị dù form invalid.
- `Validators.required` trên `nz-select`: value phải là `null` khi rỗng (không phải `''`).
- Sau `patchValue` (load data) nên `updateValueAndValidity()` nếu cần re-validate.

## File source mẫu
- `pages/work/danh-sach-cong-viec/cong-viec-edit/cong-viec-edit.component.ts` (declare + init + onLuu) · `.html` (nz-form-item + nzErrorTip)
- Helper `markFormGroupTouched`: `base-edit.component.ts`
