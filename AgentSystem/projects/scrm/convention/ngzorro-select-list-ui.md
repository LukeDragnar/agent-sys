# Convention: ng-zorro select rộng + bảng danh sách nhất quán + thẻ tổng quan responsive

> Quy tắc UI lặp lại nhiều màn SCRM. Rút từ màn Ticket hỗ trợ (session 2026-07-20).

## 1. `nz-select`: panel rộng + phân biệt item trùng tên
Khi option dài / có thể **trùng tên** (nhiều đơn vị cùng tên khác domain):
```html
<nz-select
  [nzDropdownMatchSelectWidth]="false"          <!-- panel KHÔNG bó theo box -->
  nzDropdownClassName="custom-nz-select-dropdown" <!-- panel rộng ~500px -->
  class="tkx-donvi-tab">                          <!-- box fixed-width -->
  <nz-option [nzLabel]="dv.tenCongTy + (dv.tenMien ? ' - ' + dv.tenMien : '')" ...>
```
```scss
.tkx-donvi-tab { width: 190px; flex: 0 0 190px; }        // box cố định, không co giãn lộn xộn
::ng-deep .custom-nz-select-dropdown { min-width: 500px !important; }
```
- **Label phân biệt trùng tên:** thêm domain sau tên — `tenDonVi - tenMien` (dấu `-` thường, đúng style ~10 màn khác). User dạng `ten (username)` dùng ngoặc.
- Placeholder gợi nghĩa "tất cả": `"Tất cả sale phụ trách"` / `"Tất cả đơn vị yêu cầu"`.

## 2. Hàng thẻ tổng quan (summary cards) — responsive
Đầy hàng khi ít thẻ, tự giảm khi màn nhỏ (KHÔNG hardcode `repeat(4,1fr)`):
```scss
.tkx-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));  // auto-fit = fill + wrap
  gap: 12px;
}
```
- `auto-fit` + `minmax`: 3 thẻ vẫn dàn full dòng; màn hẹp tự bớt cột. Số thẻ động → không cần tính JS.

## 3. Bảng danh sách — nhất quán giữa list chính và drill
- **Căn giữa STT** (và **MÃ** ở bảng drill): `[style.text-align]="col.field === 'stt' ? 'center' : null"` cho cả `th` và `td`.
- **Nút copy** cho ô tiêu đề (icon cạnh text, `copyText(item.tieuDe)`).
- **Thứ tự cột bảng drill = list chính** — user chuyển qua lại 2 bảng, thứ tự lệch gây rối. Khi thêm cột (trạng thái/domain/sale phụ trách) phải đồng bộ 2 nơi.
- Có cột domain rồi thì **bỏ tên đơn vị nhồi dưới tiêu đề** (tránh trùng thông tin).

## Bẫy
- Quên `[nzDropdownMatchSelectWidth]="false"` → panel bó theo box hẹp → label dài bị cắt.
- `repeat(N, 1fr)` cứng → 3 thẻ vẫn chiếm N cột, thẻ giãn xấu; màn nhỏ tràn ngang.
- Sửa cột 1 bảng quên bảng kia → drill ≠ list → khó đối chiếu.

Liên quan: `../skills/table-list.md`, `../skills/autocomplete-select.md`, `ngzorro-disable-attribute.md`.
