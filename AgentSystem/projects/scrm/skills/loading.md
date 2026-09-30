# Recipe — Loading (spinner chung Sandbox)

> **Trạng thái: ✅ Verify từ source SCRM.**

## Khi nào dùng
MỌI thao tác async có loading (fetch list, tải chi tiết, submit form, gửi tin...). **Bắt buộc dùng spinner CHUNG** — KHÔNG tự xài `<nz-spin>` / spinner lẻ cho fetch/section-loading (để đồng bộ branding Sandbox toàn hệ thống).

## Service chung
`SpinnerManagerService` — `src/app/shared/services/spinner-manager.service.ts` (bọc ngx-spinner branded Sandbox). Method: `show()` / `hide()`.

## FE — pattern chuẩn
```typescript
constructor(private _spinner: SpinnerManagerService, /* ... */) {}

taiDuLieu(): void {
  this._spinner.show();                      // bắt đầu async
  this._service.layDanhSach(filter)
    .then((rs: any) => {
      this._spinner.hide();                  // hide ở CẢ then...
      if (rs?.success) this.data = rs.data;
    })
    .catch(() => {
      this._spinner.hide();                  // ...LẪN catch (tránh treo spinner)
    });
}
```

## Quy tắc
- `show()` ngay trước gọi async; `hide()` ở **cả `then` lẫn `catch`** (nếu không, lỗi mạng → spinner treo mãi).
- **KHÔNG** `<nz-spin>` cho fetch/section-loading → thay bằng `SpinnerManagerService`.
- `[nzLoading]` trên **nút** (submit/refresh) = trạng thái nút, **được phép giữ** (không phải overlay loading).
- `nz-spin nzSimple nzSize="small"` cho indicator **theo từng item** (ô upload, 1 dòng, 1 vùng nhỏ) — **được phép**: đó là chỉ báo cục bộ, đổi sang overlay chung thì upload 1 ảnh cũng khoá cả màn.
- Mẫu tham chiếu: `ticket-ho-tro.component.ts` (`loadTicketMoi`), `ticket-detail-drawer.component.ts` (`taiChat`/`guiChat`), `delete-confirm.md` (đã dùng `_spinner.show/hide`).

## 🔴 Service ĐẾM REF — bẫy lệch cặp (nguồn lỗi thật, 30/07/2026)
`SpinnerManagerService` giữ `requestCount`: `show()` tăng, `hide()` giảm, chỉ ẩn overlay khi về 0.
⇒ **thiếu 1 `hide()` là overlay treo VĨNH VIỄN, khoá cả trang** — không phải chỉ "spinner hơi lâu".

Hai bẫy đã cắn thật:

**1. Hàm A `show()` rồi gọi hàm B mà B cũng `show()` → cần ĐỦ 2 `hide()`.**
Đúng với cờ boolean (`loading = false` ghi đè, last-write-wins) nhưng **SAI với ref-count**.
```typescript
// ❌ SAI — 2 show / 1 hide → treo
this.spinner.show();
Promise.all(calls).then(() => {
  this.loadThongKe();          // hàm này lại show() lần nữa
}).catch(() => { this.spinner.hide(); });

// ✅ ĐÚNG — hide() khớp cặp show() của CHÍNH hàm này, đặt trước khi gọi load con
this.spinner.show();
Promise.all(calls).then(() => {
  this.spinner.hide();
  this.loadThongKe();          // load con tự show/hide phần của nó
}).catch(() => { this.spinner.hide(); });
```
Đừng tin comment kiểu *"loading tự tắt trong đó"* — comment đó viết cho thời còn dùng cờ boolean.

**2. Nhánh `.catch()` viết INLINE dễ bị bỏ sót khi sửa hàng loạt.**
`}).catch(() => { this.loading = false; });` không khớp mẫu nhiều dòng → sed/replace bỏ qua, và lỗi **chỉ lộ khi API fail**. Xem `rules/code.md` › "Sửa hàng loạt".

**Component có nhiều nhánh set cờ → gom vào 1 helper, lấy CỜ làm nguồn duy nhất** (gọi trùng không đếm thêm ⇒ không thể lệch cặp):
```typescript
private datLoading(dangTai: boolean): void {
  if (dangTai === this.loading) return;   // đã ở trạng thái đó → không đếm thêm
  this.loading = dangTai;
  if (dangTai) this.spinner.show(); else this.spinner.hide();
}
```

> ⚠️ Recipe này ĐÃ tồn tại từ trước mà lỗi vẫn lặp lại nhiều lần — kể cả ở chính `ticket-ho-tro`, màn được trích làm "mẫu tham chiếu" (4 chỗ `nz-spin`). Nghĩa là **rule không tự chạy**: khi đụng bất kỳ màn nào có loading, phải MỞ file này ra đối chiếu, và `review-agent` phải soi (đã có dòng checklist ở `qa-code.md` §12).
