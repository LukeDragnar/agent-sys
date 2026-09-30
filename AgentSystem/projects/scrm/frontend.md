# SCRM Frontend — Angular 15 + ng-zorro

## Cấu trúc thư mục
```
src/app/
├── pages/           ← Màn hình chính (lazy-loaded modules)
│   ├── work/        ← WorkFlow screens
│   │   ├── cong-viec/         ← List công việc
│   │   ├── cong-viec-temp/    ← Template công việc
│   │   ├── du-an/             ← Dự án
│   │   └── quy-trinh-temp/    ← Quy trình mẫu
│   ├── Customer/
│   ├── Order/
│   └── ...
├── modules/         ← Shared UI modules (dropdown, grid, v.v.)
├── services/        ← API services theo domain
│   ├── work/        ← WorkFlow services
│   ├── customer/
│   └── ...
├── models/          ← Interfaces/types
├── constants/       ← baseComponent, config, gridOption
├── shared/          ← Helper, pipes, directives
├── layout/          ← App shell (sidebar, header)
└── guard/           ← Auth guards
```

## Routing
```typescript
// Lazy load theo module
{ path: 'work', loadChildren: () => import('./pages/work/work.module') }
// File: pages/work/work-routing.module.ts
```

## Environment — API URLs
```typescript
environment.SERVICE_API_WORK      // WorkFlow service
environment.SERVICE_API_ORDER     // Order service
environment.SERVICE_API_CUSTOMER  // Customer service
// Mỗi microservice có 1 URL riêng
```

## Template pattern (list screen)
```html
<!-- nz-table standard -->
<nz-table #table
  [nzData]="listData"
  [nzTotal]="totalRecord"
  [(nzPageIndex)]="page"
  [(nzPageSize)]="pageSize"
  [nzLoading]="isLoading"
  nzBordered
  nzSize="small"
  (nzPageIndexChange)="getData()"
  (nzPageSizeChange)="getData()">
  <thead>
    <tr>
      <th nzWidth="50px">STT</th>
      <th>Tiêu đề</th>
      <th nzWidth="150px">Trạng thái</th>
      <th nzWidth="120px">Thao tác</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let item of table.data; let i = index">
      <td>{{ (page-1)*pageSize + i + 1 }}</td>
      <td>{{ item.tieuDe }}</td>
      <td><nz-tag [nzColor]="item.mauSac">{{ item.tenTrangThai }}</nz-tag></td>
      <td>
        <a (click)="openDetail(item)">Sửa</a>
        <nz-divider nzType="vertical"></nz-divider>
        <a nzDanger (click)="delete(item)">Xóa</a>
      </td>
    </tr>
  </tbody>
</nz-table>

<!-- Search form -->
<nz-card>
  <div nz-row [nzGutter]="16">
    <div nz-col [nzSpan]="6">
      <input nz-input placeholder="Tìm kiếm..." [(ngModel)]="filter.keyword" />
    </div>
    <div nz-col [nzSpan]="6">
      <nz-select [(ngModel)]="filter.trangThai" nzPlaceHolder="Trạng thái" nzAllowClear style="width:100%">
        <nz-option [nzValue]="0" nzLabel="Chưa làm"></nz-option>
        <nz-option [nzValue]="1" nzLabel="Đang làm"></nz-option>
        <nz-option [nzValue]="2" nzLabel="Hoàn thành"></nz-option>
      </nz-select>
    </div>
    <div nz-col [nzSpan]="4">
      <button nz-button nzType="primary" (click)="getData()">Tìm kiếm</button>
    </div>
  </div>
</nz-card>
```

## Modal create/edit pattern
```typescript
openModal(item?: any) {
  const modal = this._modalService.create({
    nzTitle: item ? 'Cập nhật công việc' : 'Thêm công việc',
    nzContent: CongViecFormComponent,
    nzWidth: 800,
    nzComponentParams: { data: item },
    nzFooter: null  // form tự handle buttons
  });
  modal.afterClose.subscribe(result => {
    if (result) this.getData();
  });
}
```

## Notification pattern
```typescript
// Success
this._notification.success('Thành công', 'Lưu công việc thành công');
// Error
this._notification.error('Lỗi', res?.message || 'Có lỗi xảy ra');
// Warning
this._notification.warning('Cảnh báo', 'Vui lòng điền đầy đủ thông tin');
```

## Form pattern
```html
<form nz-form [formGroup]="form" nzLayout="vertical">
  <nz-form-item>
    <nz-form-label nzRequired>Tiêu đề</nz-form-label>
    <nz-form-control nzErrorTip="Vui lòng nhập tiêu đề">
      <input nz-input formControlName="tieuDe" placeholder="Nhập tiêu đề..." />
    </nz-form-control>
  </nz-form-item>
  <nz-form-item>
    <nz-form-label>Mức độ ưu tiên</nz-form-label>
    <nz-form-control>
      <nz-select formControlName="idMucDoUuTien" nzPlaceHolder="Chọn mức độ" nzAllowClear style="width:100%">
        <nz-option *ngFor="let m of listMucDo" [nzValue]="m.id" [nzLabel]="m.tenMucDo"></nz-option>
      </nz-select>
    </nz-form-control>
  </nz-form-item>
</form>
```

## Màn hình WorkFlow hiện có
| Màn hình | Path | Mô tả |
|---|---|---|
| CongViec (List) | `pages/work/cong-viec` | Danh sách công việc, filter theo dự án |
| CongViecTemp | `pages/work/cong-viec-temp` | Template công việc |
| DuAn | `pages/work/du-an` | Quản lý dự án |
| QuyTrinhTemp | `pages/work/quy-trinh-temp` | Template quy trình |
| CauTrucBang | `pages/work/cau-truc-bang` | Cấu trúc bảng tùy chỉnh |
