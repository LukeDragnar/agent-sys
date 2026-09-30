# Recipe — Modal create/edit (popup form)

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Mở popup thêm/sửa không chuyển trang; truyền data vào, nhận kết quả khi đóng, reload list sau lưu.

## FE — mở modal & truyền data
```typescript
const modal = this._modalService.create<EditModalComponent, ResultDto | null>({
  nzTitle: null, nzContent: EditModalComponent, nzWidth: '720px',
  nzFooter: null, nzMaskClosable: false,
  nzComponentParams: { congViecMauId: data.id || null, currentValues: data.values || {}, listTrangThai: this.listTrangThai },
});
modal.afterClose.subscribe((result: ResultDto | null) => {
  if (result === null) return;          // null = cancel → giữ nguyên
  this.gridLoadData();                  // reload list
});
```

## FE — component trong modal (nhận param, submit, đóng)
```typescript
@Input() congViecMauId: string | null = null;
@Input() currentValues: Record<string,string> = {};
constructor(private modalRef: NzModalRef) {}
ngOnInit() { this.values = { ...(this.currentValues || {}) }; }  // CLONE input để cancel thật
onSave()   { this.modalRef.close({ defaultValues: this.values }); }
onCancel() { this.modalRef.close(null); }
```

## Bẫy thường gặp
- **Clone input trong ngOnInit** (`{...currentValues}`) — không sửa trực tiếp object cha, nếu không cancel mất tác dụng.
- **Luôn gọi `modalRef.close()`** ở mọi nhánh (Save/Cancel/X) — không gọi thì `afterClose` không bắn.
- Trả về kiểu nhất quán (`null` hoặc object), tránh `undefined`.
- Key trong `nzComponentParams` phải trùng tên `@Input`.
- Sau lưu nhớ `gridLoadData()` để thấy data mới.

## File source mẫu
- Mở modal: `pages/work/danh-sach-quy-trinh/danh-sach-quy-trinh-edit/danh-sach-quy-trinh-edit.component.ts:381`
- Component modal: `pages/work/danh-sach-quy-trinh/step-default-values-modal/step-default-values-modal.component.ts`
