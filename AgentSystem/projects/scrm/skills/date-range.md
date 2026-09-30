# Recipe — Date / Date-range picker

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Chọn 1 ngày hoặc khoảng từ-đến (filter báo cáo). Dự án bọc sẵn component `app-date-picker` + `app-date-picker-range`.

## Template
```html
<!-- single -->
<app-date-picker [(ngModel)]="ngayChon" [nzFormat]="'dd/MM/yyyy'" nzSize="small"
                 [nzDisabledDate]="disableFutureDates" (change)="onDateChange($event)"></app-date-picker>

<!-- range (có sẵn quick-select: Hôm nay/Tháng này/7 ngày/30 ngày...) -->
<app-date-picker-range [(ngModel)]="date" [IsSmall]="true" [IsDateFuture]="false"
                       [DateFormat]="'dd/MM/YYYY'" (changeInit)="onSearchSyncInit()"></app-date-picker-range>
```
`[(ngModel)]` của range = `[tuNgay, denNgay]` (ISO string). `(changeInit)` bắn khi component load xong default range.

## TS — default range + map tuNgay/denNgay
```typescript
import { startDay, endDay, addDays } from 'src/app/shared/helper/ultility';
date: any[] = [startDay(), endDay()];                 // default: hôm nay 00:00 → 23:59
gridLoadData() {
  if (this.date != null) { this.searchModel.fromDate = this.date[0]; this.searchModel.toDate = this.date[1]; }
  else { this._notification.error(MESSAGE.ERROR, this.tsb.t('msg-chua-chon-ngay', 'Chưa chọn ngày.')); return; }
  this._service.danhSach(this.searchModel).then(rs => { ... });
}
// helper: startDay = moment(d).startOf('day').format(); endDay = moment(d).endOf('day').format();
```

## Bẫy thường gặp
- **Format moment dùng `DD/MM/YYYY`** (D/Y hoa) — `dd/yyyy` (thường) sai ý nghĩa. Component nội bộ đã chuẩn hóa, theo prop.
- BE so sánh dùng `.Date` để bỏ giờ/phút (xem [search-filter.md](search-filter.md)).
- Null khi chưa chọn: check `if (date && date[0] && date[1])` trước khi gửi.
- `app-date-picker-range` có thể nhớ range qua localStorage — tắt bằng `[IsUseLocalStorage]="false"` nếu không muốn.
- Timezone: dùng `moment()` (local) + `startOf/endOf('day')`, tránh `new Date()` thuần.

## File source mẫu
- Component bọc: `src/app/components/date-picker/`, `src/app/components/date-picker-range/`
- Helper: `src/app/shared/helper/ultility.ts` (startDay/endDay/addDays)
- Ví dụ: `pages/major/thuchi/so-thu-chi/so-thu-chi.component.ts`
