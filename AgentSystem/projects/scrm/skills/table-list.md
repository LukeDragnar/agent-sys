# Recipe — Bảng danh sách (nz-table + resize + thiết lập cột + lưu layout)

> **Trạng thái: ✅ Verify bằng cách QUÉT TOÀN BỘ màn đang dùng** (2026-07-30, ~90 màn dùng
> `app-table-resize`, ~60 màn dùng `app-table-setting`). Bản cũ chỉ chép từ 1 màn (`ticket-ho-tro`)
> nên **nêu sai `[nzScroll]`** và **thiếu hẳn mục module/DI** → ai làm theo cũng phải sửa 4–6 vòng.

## 🚦 CHECKLIST TRƯỚC KHI GÕ CODE — thiếu 1 dòng là lỗi ngay
| # | Việc | Thiếu thì bị gì |
|---|---|---|
| 1 | Module chứa component: `imports: [ComponentsModule, FormsModule, ...antdModule]` | `NG8001 'app-table-setting' is not a known element` |
| 2 | **`providers: [TableService]`** (module HOẶC `@Component`) | Runtime **`NullInjectorError: No provider for TableService!`** |
| 3 | `<app-table-resize [(ngModel)]="gridColumns">` bọc ngoài `nz-table` | Không có resize |
| 4 | `[nzScroll]="{ x: 'scroll' }"` trên `nz-table` | Bảng **tràn khỏi khung**, không cuộn ngang, và **resize vô hiệu** |
| 5 | `appThDirective appThResizer [index_resizer]="i"` trên **MỖI** `<th>` | Không có tay kéo |
| 6 | **`<div class="guide-line" #guideLine></div>`** trong `app-table-resize` | **Bẫy #1**: hover hiện mũi tên mà **kéo TRƠ** |
| 7 | Cột hành động hardcode `[nzRight]="true"` **NGOÀI** `gridColumns` | User tắt/kéo mất cột hành động |
| 8 | `loadGridColumn` guard mảng rỗng | **Bảng trắng** dù data đủ |
| 9 | `field` trong `gridColumns` **không trùng nhau** | "Lưu cấu hình cột" **chết** |
| 10 | `TableColumnKey` có hậu tố `_Vn`, đổi cột thì **bump** | User cũ bị resize sai cột |

---

## 1. MODULE + DI — mục hay bị bỏ nhất

```typescript
@NgModule({
  declarations: [ManHinhComponent],
  imports: [
    ComponentsModule,          // app-table-setting, app-table-resize, appThDirective/appThResizer
    FormsModule,               // [(ngModel)] của app-table-resize + app-table-setting
    ...antdModule,             // nz-table/nz-card/nz-tag/... (constants/import.ts)
    NzGridModule,              // KHÔNG nằm trong antdModule, cần thì khai lẻ
  ],
  providers: [TableService],   // 🔴 BẮT BUỘC
})
```

- 🔴 **`TableService` khai `@Injectable()` KHÔNG có `providedIn: 'root'`**, và `ComponentsModule`
  chỉ `declare`+`export` component chứ **không provide service** → mỗi module/component dùng bộ
  table **phải tự khai**. Mẫu: `ticket-ho-tro.module.ts:57`, `major.module.ts:377`,
  `landing-page.module.ts:315` (cấp module) · `dm-trang-thai.component.ts:18` (cấp component).
  Lỗi này **compiler KHÔNG bắt được** (`tsc`/`ngc` đều xanh), chỉ nổ lúc chạy.
- 🔴 **KHÔNG declare component dùng bộ table trong `SharedModule`**: `ComponentsModule` lại
  `import { SharedModule }` (`components.module.ts:40`) → SharedModule import ngược là
  **CIRCULAR IMPORT** (`NG6002 SharedModule does not appear to be an NgModule class`).
  → Tách module riêng cho component đó, module mới import CẢ HAI.
- ⚠️ **SharedModule chỉ EXPORT một phần** các Nz module nó dùng nội bộ → module mới **không**
  thừa hưởng `nz-card`/`nz-progress`/`nz-table`/`nzGutter`, phải tự khai `...antdModule`.
- ⚠️ Trong SharedModule thì **KHÔNG** spread được `...antdModule` (`constants/import.ts` lại
  import SharedModule → vòng tròn) — phải khai lẻ từng Nz module.
- Component vừa dùng cho route vừa mở bằng `NzModalService` → module phải nạp ở `AppModule`
  (**không lazy**), không thì `nzContent: Component` lỗi lúc chạy.

## 2. `[nzScroll]` — dùng `{ x: 'scroll' }`, ĐỪNG tự tính px

```html
<nz-table [nzScroll]="{ x: 'scroll' }" ...>
```

**Đây là convention của hệ thống: 391 chỗ dùng `{ x: 'scroll' }`.** Cơ chế: nz-table đặt
`table-layout: fixed` + gán `width` = giá trị `x`; `'scroll'` không phải width hợp lệ nên trình
duyệt bỏ qua → **bảng tự rộng bằng TỔNG width các cột**, còn `.ant-table-body` có `overflow-x`
nên tự hiện thanh cuộn ngang. Kéo giãn cột là bảng nới ra ngay, không cần code gì thêm.

🔴 **ĐỪNG** làm `[nzScroll]="{ x: scrollX }"` với getter cộng `thWidth` (bản recipe cũ ghi thế,
chép từ `ticket-ho-tro` — thiểu số). Hai cái sai:
1. `app-table-resize` ghi `thWidth` mới ngay trong `mousemove` rồi chỉ `_cdr.detectChanges()` cho
   **view của chính nó** → view cha (nơi đặt binding) không refresh kịp → bề rộng bảng đứng nguyên,
   `table-layout: fixed` bèn **co các cột khác lại nhường chỗ** cho cột đang kéo, thay vì cuộn ngang.
2. Getter trả object mới mỗi vòng CD → nz-table xử lý `ngOnChanges` liên tục vô ích.

`[nzScroll].y` + `appAutoScrollY`: **chỉ 2 màn dùng** → KHÔNG phải chuẩn. Chỉ cần khi muốn paging
dính đáy trong màn full-page (kèm `[isHidenOverflow]="true"`, xem §6).

## 3. HTML — khung chuẩn

```html
<app-table-resize [(ngModel)]="gridColumns">
  <nz-table appDragToScroll [nzData]="gridData" [nzScroll]="{ x: 'scroll' }" nzSize="small"
            [nzShowPagination]="false">
    <thead>
      <tr>
        <ng-container *ngFor="let col of gridColumns; let i = index">
          <th appThDirective appThResizer [index_resizer]="i"
              *ngIf="col.visible && hienCot(col.field)"
              [nzWidth]="col.thWidth" [nzColumnKey]="col.field" [nzLeft]="col.isNzLeft"
              class="text-center">          <!-- quy ước: MỌI header căn giữa -->
            {{ col.header }}
          </th>
        </ng-container>
        <!-- Cột hành động: hardcode NGOÀI ngFor + ghim phải -->
        <th nzWidth="90px" [nzRight]="true" class="text-center">THAO TÁC</th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let item of gridData; let i = index">
        <ng-container *ngFor="let col of gridColumns">
          <td *ngIf="col.visible && hienCot(col.field)" [nzLeft]="col.isNzLeft"
              [class.text-center]="col.field === 'stt'" [class.text-right]="col.field === 'soTien'">
            <ng-container [ngSwitch]="col.field">
              <ng-container *ngSwitchCase="'stt'">{{ stt(i) }}</ng-container>
              <ng-container *ngSwitchDefault>{{ item[col.field] || '—' }}</ng-container>
            </ng-container>
          </td>
        </ng-container>
        <td [nzRight]="true" class="text-center">
          <button nz-button nzType="link" nzSize="small"><i nz-icon nzType="eye"></i></button>
        </td>
      </tr>
    </tbody>
  </nz-table>

  <!-- 🔴 BẮT BUỘC cho resize (@ContentChild('guideLine')) — xem §5 -->
  <div class="guide-line" #guideLine></div>
</app-table-resize>
```

- `[index_resizer]="i"` = index trong **toàn mảng** `gridColumns` (kể cả cột đang ẩn) — vì thế
  `*ngFor` phải bọc `ng-container` rồi `*ngIf` ở `<th>`, KHÔNG lọc mảng trước khi ngFor.
- `appThDirective` + `appThResizer`: `appThResizer` cho tay kéo ở mép, thêm `appThDirective` thì
  mousedown trên cả `<th>` cũng kéo được. Thống kê: 232 chỗ chỉ `appThResizer`, 189 chỗ dùng cả hai.
  Khuyến nghị **dùng cả hai** (kéo dễ hơn).
- `appDragToScroll` (khai trong **SharedModule**): giữ chuột trên header rồi kéo để cuộn ngang.
  Tự bỏ qua khi bấm `button/input/.resizer` và nuốt 1 click sau khi kéo (không lỡ trigger sort).
- **Dòng con / dòng chi tiết cũng PHẢI duyệt CÙNG mảng `gridColumns`** — không thì user ẩn cột
  hay đổi thứ tự là dòng cha/con lệch nhau. Ô nào không có nghĩa với dòng con thì để **TRỐNG**,
  đừng nhét dữ liệu lệch cột cho đỡ trống.

## 4. TS — cột động + persist

```typescript
readonly TableColumnKey = 'TEN_MAN_LIST_V1';   // đổi cột/thứ tự -> BUMP _V2

/** Cột hành động KHÔNG khai ở đây (hardcode [nzRight] trong HTML). */
gridColumns: GridColumn[] = [
  new GridColumn({ field: 'stt',  header: 'STT', thWidth: '70px',  visible: true, isNzLeft: true }),
  new GridColumn({ field: 'ten',  header: 'TÊN', thWidth: '320px', visible: true }),
];

/** 🔴 Guard: app-table-setting có thể emit undefined lúc ngOnInit -> gán thẳng là BẢNG TRẮNG. */
loadGridColumn(cols: GridColumn[]): void {
  if (Array.isArray(cols) && cols.length > 0) { this.gridColumns = cols; }
}

/** Ẩn cột theo NGỮ CẢNH (khác visible do user tự tắt). */
hienCot(field: string): boolean {
  if (field === 'loai') { return !this.khoaLoai; }
  return true;
}

/** STT theo TRANG: sang trang 2 phải là 21, 22... Dùng chung cho dòng cha lẫn dòng con. */
stt(i: number): number { return (this.page - 1) * this.pageSize + i + 1; }
```

Bánh răng thiết lập cột — đặt ở cuối toolbar lọc:
```html
<app-table-setting [(ngModel)]="gridColumns" [gridColumnsDefault]="gridColumns"
  [isSaveGridColumns]="true" [TableColumnKey]="TableColumnKey"
  (loadGridColumn)="loadGridColumn($event)"></app-table-setting>
```

## 5. `guide-line` — bẫy số 1 của resize

`app-table-resize` đọc div này qua `@ContentChild('guideLine')` để vẽ vạch dọc lúc kéo.
**Thiếu là kéo TRƠ.** `assets/scss/main.scss:1083` khai **CẢ HAI** `#guide-line` **và** `.guide-line`
(global) nên:

| Ngữ cảnh | Dùng |
|---|---|
| Màn thường (full page) | `id="guide-line"` — ~80 màn dùng |
| Trong **modal / drawer / CDK overlay** | `class="guide-line"` — overlay render NGOÀI scoped-css nên selector `#id` scoped không áp tới; và `id` dễ trùng khi 2 bảng cùng trang |
| Component chạy CẢ HAI chế độ | **`class="guide-line"`** (an toàn cho cả hai) |

- Trong drawer, vạch có thể **KẸT** sau khi kéo → `nzWrapClassName="xxx"` + CSS global
  `.xxx .guide-line { display:none !important }`. Resize vẫn chạy (component đọc inline `style.left`),
  chỉ mất vạch preview.

## 6. Paging dính đáy (TÙY CHỌN — chỉ màn full-page)
Cần đủ **cả 3**: `app-table-resize [isHidenOverflow]="true"` + `appAutoScrollY #ash="autoScrollY"`
+ `[nzScroll]="{ x: 'scroll', y: ash.scrollY }"`. Thiếu 1 → paging trôi xuống dưới màn, hoặc bảng
cao cố định (màn nhỏ vỡ). **KHÔNG hardcode `y`** (mỗi khách 1 cỡ màn).
⚠️ `[isHidenOverflow]="true"` cần có `#tableContainerChild` (cdkVirtualScrollViewport); không có
virtual-scroll thì **đừng bật** `isHidenOverflow`.
Bảng trong **modal/popup** thì bỏ hẳn mục này — modal đã tự cuộn.

## 7. Icon + căn lề — quy ước
| Việc | Dùng | Đừng |
|---|---|---|
| **Tải về** (mũi tên đi XUỐNG) | `<span nz-icon nzType="download" nzTheme="outline">` | `sb-icon-export` (mũi tên đi RA = xuất/setting) |
| Xuất Excel / mở setting | `<span class="sb-icon-export">` | |
| Header | **luôn căn giữa** | |
| Data | số → phải · ngày/trạng thái/STT → giữa · chữ → trái | |
| Ô phải gọn 1 dòng (ngày, mã) | `white-space: nowrap` + `nzWidth` đủ rộng (ngày `dd/MM/yyyy HH:mm` cần **150px**) | |
| Ô có thể rất dài (tên, email) | `overflow:hidden; text-overflow:ellipsis; white-space:nowrap` + `[title]` để hover xem full | |

⚠️ **Cần flex trong ô (icon + chữ + badge) thì bọc `<div>` BÊN TRONG `<td>`**, KHÔNG đặt
`display:flex`/`max-width` lên chính `<td>`: biến ô bảng thành flex container làm hỏng table-layout
và sinh khung chữ nhật lạ quanh nội dung.
⚠️ Ô có icon xổ (chevron) thì **LUÔN render chỗ cho icon** (`flex: 0 0 16px`, rỗng khi không có),
không thì hàng có icon bị đẩy chữ sang phải → **cả cột lệch so le**.

## 7b. 🔴 MỘT MÀN CÓ NHIỀU BẢNG — thêm/sửa cột phải rà HẾT

Màn lớn thường có **>1 bảng**, mỗi bảng là **một bộ độc lập**: mảng cột riêng + `TableColumnKey` riêng + `TableSettingComponent` riêng. Thêm cột vào bảng này **không** ảnh hưởng bảng kia.

Ví dụ thật — `ticket-ho-tro.component.ts`:

| Bảng | Mảng cột | `TableColumnKey` | TableSetting |
|---|---|---|---|
| List chính | `gridColumns` | `TICKET_CUSTOMER_LIST` | `tblSetMain` |
| Drill Thống kê | `drillGridColumns` | `TICKET_DRILL_TK` | `tblSetDrill` |

**Việc cần làm khi thêm cột — 4 thứ, KHÔNG cái nào tự lan:**

1. **Thêm cột vào TỪNG mảng** (`grep "GridColumn\[\] = \["` trong file để đếm có mấy bảng).
2. **Thêm case render trong TỪNG khối `ngSwitchCase`** — mỗi bảng có khối riêng.
3. **Quy tắc ẩn/hiện phải xét lại cho từng bảng.** Vòng gate kiểu
   `gridColumns.forEach(c => { if (adminCols.includes(c.field)) c.visible = isSandboxTeam })`
   **chỉ chạy trên 1 mảng**. Bảng kia phải **lập luận riêng** rồi kết luận — cấm copy mù:
   copy nguyên `adminCols` sang có thể thừa (bảng đó vốn nằm trong tab đã gate),
   không xét thì có ngày **lộ cột nội bộ** cho khách.
4. **Cùng 1 field phải render CÙNG cách ở mọi bảng.** Bảng này in nhãn từ danh mục
   (*"Đề xuất tính năng"*), bảng kia in mã thô (`YeuCau`) → 2 bảng cùng data mà nói khác nhau.

⚠️ **Sau khi deploy: user đã lưu layout KHÔNG tự thấy cột mới.** `TableColumnKey` cố định,
`loadColDefaultToConfig` tự append nhưng **phải clear cấu hình cột thủ công**. Cột mới quan trọng
→ phải ghi vào ghi chú deploy, không thì "code có mà người dùng bảo không thấy".

> Sự cố 30/07/2026 (591-3): thêm 5 cột duyệt vào list chính, **quên `drillGridColumns`** → bảng
> "Thống kê → danh sách ticket" thiếu sạch 5 cột. Lỗi **im lặng**: không crash, không báo gì,
> chỉ thiếu dữ liệu ở một bảng ít người mở — user phát hiện chứ không phải review.

## 8. Bẫy còn lại
- **Khai cột hành động trong `gridColumns`** → bị `app-table-setting` cho tắt/kéo đi. Hardcode
  `<th/td [nzRight]="true">` NGOÀI ngFor.
- **Ghim cột quên 1 bên** (`th` có `[nzLeft]` mà `td` không) → lệch cột khi cuộn ngang.
- **`field` TRÙNG trong `gridColumns`** → "Lưu cấu hình cột" **chết** (lưu key theo `field`).
  Đụng mảng cột thì **grep toàn mảng** tìm key trùng, không chỉ đọc dòng vừa thêm.
  Xem `../lessons/grid-column-field-trung-save-config-die.md`.
- **Đổi cột/thứ tự mà không bump `TableColumnKey`** → user cũ đã lưu layout theo index → resize
  sai cột / cột mới không hiện.
- **Badge trạng thái để trong `.tenman-table`** → drawer chi tiết dùng lại không ăn màu. Để
  **top-level** trong SCSS.
- Sort = **dropdown `nz-select` ở toolbar**, KHÔNG `nzSortFn` trên `<th>` (khó với server-paging).
- Search **debounce 400ms** (`Subject` + `debounceTime`), hoặc nút kính lúp + `(keyup.enter)` nếu
  muốn user chủ động tìm.
- **`ngc`/`tsc` xanh KHÔNG có nghĩa là chạy được.** 2 lớp lỗi chỉ nổ lúc runtime:
  DI thiếu provider, và `guide-line` thiếu (resize trơ). Bật màn lên bấm thử: **kéo 1 cột** +
  **mở bánh răng tắt/bật 1 cột** + **F5 xem layout còn giữ**.

## 9. Lọc/toolbar đi kèm (khuôn `ticket-ho-tro`)
```html
<div class="xx-header">                <!-- tiêu đề trái, bộ lọc dạt phải -->
  <h5 class="m-b-0">Tiêu đề</h5>
  <div class="xx-loc">                 <!-- margin-left:auto; display:flex; gap:10px -->
    <nz-select nzSize="small" nzAllowClear nzPlaceHolder="Tất cả loại" ...></nz-select>
    <app-loc-khoang-ngay [presets]="['homNay','7ngay','30ngay','thangNay','6thang']"
      [macDinh]="'6thang'" [soNamToiDa]="3" (thayDoi)="onKhoangNgay($event)"></app-loc-khoang-ngay>
    <nz-input-group [nzPrefix]="pfx" nzSize="small"><input nz-input [(ngModel)]="keyword" /></nz-input-group>
    <ng-template #pfx><i nz-icon nzType="search" class="opacity-05"></i></ng-template>
    <button nz-button nzSize="small"><i nz-icon nzType="reload"></i></button>
    <app-table-setting ...></app-table-setting>
  </div>
</div>
```
- Control ngày dùng chung: `components/loc-khoang-ngay` (`LocKhoangNgayModule`) — có sẵn mốc nhanh
  Hôm nay/7 ngày/30 ngày/Tháng này/6 tháng/1 năm/3 năm, chặn ngày tương lai, tự chuẩn hoá
  đầu ngày/cuối ngày.
- ⚠️ Control **chỉ emit khi ĐỔI, không emit lúc init** → `ngOnInit` của màn phải **TỰ seed**
  `tuNgay/denNgay` khớp `[macDinh]` (mẫu `ticket-ho-tro.component.ts:542`), không thì ô ngày hiện
  "6 tháng" mà dữ liệu lại là toàn bộ.
- Vào từ 1 màn cụ thể mà muốn khoá bộ lọc → **gán sẵn giá trị vào ngModel của ô lọc** rồi
  `[nzDisabled]`, đừng chỉ lọc ngầm ở getter: ô select sẽ hiện placeholder "Tất cả" → user tưởng
  chưa lọc.

## 10. Request phân trang — BE
DTO kế thừa **`GridInfoNotRequire`** nếu màn không cho user tự sort, và FE gửi qua
`this.getGridOptionSearchInput(keyword, pageSize, page, [])` của `BaseListComponentV1`.
🔴 Kế thừa `GridInfo` mà FE không gửi `sorts` → **HTTP 400 `The Sorts field is required`**
(`GridInfo.PageInfo/Sorts` là non-nullable, project bật nullable nên ASP.NET coi là bắt buộc).

## File source mẫu
- Đủ bộ, có dòng cha/con + chạy cả page lẫn modal:
  `shared/components/popup-xuat-file/popup-xuat-file.component.{ts,html,scss}` + `.module.ts`
- Full-page list + paging dính đáy: `pages/ticket-ho-tro/ticket-ho-tro.component.*`
- Component chung: `app-table-resize` · `app-table-setting` · `appThDirective`/`appThResizer` ·
  `appDragToScroll` (SharedModule) · `appAutoScrollY` (`components/directives/`)
