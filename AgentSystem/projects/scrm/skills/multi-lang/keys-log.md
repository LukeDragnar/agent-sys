---
name: Multi-lang Keys Log - SCRM
description: All translation keys created for SCRM
type: reference
---

# Multi-lang Keys Log - SCRM

Danh sách tất cả translation keys đã tạo cho SCRM Frontend.

## Purpose

- Track all keys ever created (avoid duplicates)
- Quick lookup: key exists?
- Audit: when was key created, by whom

## Format

```markdown
## {YYYY-MM-DD} - {Mode/Module}

- `key-name`: Vietnamese text
- `another-key`: Another Vietnamese text
- ...
```

## Current Log

### 2026-04-24 - New File (date-picker-option component)

- `date-picker-choose-date`: Chọn ngày
- `date-picker-choose-month`: Chọn tháng
- `date-picker-choose-year`: Chọn năm
- `date-format-month`: Tháng
- `date-format-year`: Năm

### 2026-04-24 - New File (date-picker-range component)

- `date-range-from-to-date`: Từ ngày -> Đến ngày
- `date-range-from-date`: Từ ngày
- `date-range-to-date`: Đến ngày
- `date-range-from-hour`: Từ giờ: 
- `date-range-to-hour`: Đến giờ: 
- `date-range-today`: Hôm nay
- `date-range-this-month`: Tháng hiện tại
- `date-range-yesterday`: Hôm qua
- `date-range-7-days-ago`: 7 ngày trước
- `date-range-30-days-ago`: 30 ngày trước
- `date-range-last-month`: Tháng trước
- `date-range-tomorrow`: Ngày mai
- `date-range-next-3-days`: 3 ngày tới
- `date-range-next-7-days`: 7 ngày tới
- `date-range-next-30-days`: 30 ngày tới

### 2026-04-24 - Batch 4 (bnt-delete component - UPDATED)

- `button-delete`: Xóa (delete button with proper functional component)

### 2026-04-24 - Batch 1 (chat component)

- `chat-error-scroll`: Lỗi cuộn:
- `chat-view-post`: Xem bài viết

### 2026-04-24 - New File (date-picker component) — NO WRAPPABLE TEXT

*Analyzed: date-picker.component.ts + date-picker.component.html*  
*Result: Zero Vietnamese texts found. Component uses parameterized `@Input()` for all user-facing text.*  
*Decision: No modifications needed. Best-practice pattern for reusable components.*

---

### 2026-04-24 - New File (cache-filter-save-form component)

- `error-missing-data`: Cần truyền đủ dữ liệu!
- `error-name-required`: Tên không được bỏ trống!

### 2026-04-24 - New File (search-and-filter component)

- `button-filter`: Bộ lọc
- `button-search`: Tìm kiếm
- `error-update-filter`: Xảy ra lỗi khi cập nhật bộ lọc
- `msg-delete-filter-success`: Xóa bộ lọc thành công!
- `msg-filter-empty`: Bộ lọc không được để trống!
- `msg-page-code-empty`: Mã page không được để trống!
- `msg-update-filter-success`: Cập nhật bộ lọc thành công!
- `title-save-filter`: Lưu bộ lọc hiện tại

### 2026-04-24 - Batch 2 (sb-tooltip component)

- `tooltip-default-formula`: Tiền hàng - Giảm giá + Thưởng

### 2026-04-24 - Batch 1 (btn-save component)

- `button-save`: Lưu

### 2026-04-24 - Batch 3 (date-picker-range-type2 component)

- `action-close`: Đóng
- `action-select`: Chọn
- `date-range-30-days-ago`: 30 ngày trước
- `date-range-3-days-ahead`: 3 ngày tới
- `date-range-7-days-ago`: 7 ngày trước
- `date-range-7-days-ahead`: 7 ngày tới
- `date-range-30-days-ahead`: 30 ngày tới
- `date-range-from-date`: Từ ngày ->
- `date-range-label-from`: Từ ngày:
- `date-range-label-to`: Đến ngày:
- `date-range-last-month`: Tháng trước
- `date-range-last-week`: Tuần trước
- `date-range-placeholder`: Từ ngày -> Đến ngày
- `date-range-this-month`: Tháng hiện tại
- `date-range-this-week`: Tuần hiện tại
- `date-range-to-date`: -> Đến ngày
- `date-range-today`: Hôm nay
- `date-range-tomorrow`: Ngày mai
- `date-range-yesterday`: Hôm qua

### 2026-04-24 - Batch 3 (switchboard + table components) — 6 files

**omicall-view-dial component:**
- `phone-required`: Vui lòng nhập số điện thoại
- `phone-invalid`: Số điện thoại không hợp lệ
- `warning-title`: Cảnh báo
- `error-title`: Lỗi
- `contact-create-failed`: Tạo contact thất bại
- `success-title`: Thành công
- `contact-create-success`: Tạo contact thành công
- `customer-not-found`: Không tìm thấy thông tin khách hàng
- `contact-not-found`: Không tìm thấy thông tin contact
- `contact-detail-title`: Chi tiết contact
- `copy-success`: Copy thành công
- `copy-failed`: Copy thất bại
- `undetermined`: Không xác định
- `action-copy`: Copy
- `contact-info-tooltip`: Thông tin contact
- `note-input-placeholder`: Nhập ghi chú
- `phone-input-placeholder`: Nhập số điện thoại
- `incoming-call`: Có cuộc gọi tới

**th-directive component:**
- `component-works`: th-directive works!

**table-setting component:**
- `error-generic`: Lỗi
- `customize-table-display-title`: Tùy chỉnh hiển thị thông tin bảng
- `clear-column-config-success`: Xóa cấu hình cột trong bộ nhớ tạm của trình duyệt thành công!
- `apply-config-all-users-success`: Cấu hình cột đã được áp dụng cho tất cả người dùng!
- `apply-config-all-users-error`: Một lỗi đã xảy ra khi áp dụng cấu hình cột cho tất cả người dùng!
- `store-range-label`: Cửa hàng(Từ 1-10):
- `hide-statistics`: Ẩn tổng thống kê
- `legend-title`: Chú thích
- `edit-columns-action`: Chỉnh sửa cột
- `apply-config-all-users-action`: Áp dụng cấu hình cột cho toàn bộ user
- `clear-column-config-action`: Xóa cấu hình cột trong bộ nhớ tạm của trình duyệt

### 2026-04-24 - Group 6 (drop-down-list components) — 4 components

**drop-down-list-mix-datepicker-kieungay component:**
- Already complete from previous batch - no new keys

**drop-down-list-nguon-kh component:**
- Error message already wrapped - no new keys

**drop-down-list-nhan-vien-kho component:**
- Error message already wrapped - no new keys

**drop-down-list-nhom-kh component:**
- `error-customer-group-data-fail`: Lấy dữ liệu nhóm khách hàng thất bại

### 2026-05-13 - don-hang-hoan (FE landing-page) — 4 files

**don-hang-hoan component (cha) — HTML + TS:**
- `don-hang-hoan-title`: Quản lý Đơn Hàng Hoàn
- `don-hang-hoan-plh-keyword`: Mã đơn hoàn, mã đơn gốc, mã sản phẩm...
- `don-hang-hoan-plh-ve-tu-dau`: Về từ đâu
- `don-hang-hoan-plh-san-tmdt`: Sàn TMDT
- `don-hang-hoan-plh-chon-gian-hang`: -- Chọn gian hàng --
- `don-hang-hoan-plh-trang-thai`: Trạng thái
- `don-hang-hoan-plh-ma-don-goc`: Mã đơn gốc
- `don-hang-hoan-plh-tt-hoan-tmdt`: TT hoàn TMDT
- `don-hang-hoan-plh-tt-van-chuyen-hoan`: TT vận chuyển hoàn
- `don-hang-hoan-plh-tt-van-chuyen-nguoc`: TT vận chuyển ngược
- `don-hang-hoan-plh-tt-hoan-tu-kho`: TT hoàn từ kho
- `don-hang-hoan-plh-loai-hoan`: Loại hoàn
- `don-hang-hoan-plh-ly-do-hoan`: Lý do hoàn
- `lbl-stt`: STT
- `lbl-thao-tac`: Thao tác
- `action-chi-tiet`: Chi tiết
- `opt-tat-ca`: -- Tất cả --
- `ecommerce-shopee`: Shopee
- `ecommerce-tiktok`: TikTok
- `ecommerce-lazada`: Lazada
- `ecommerce-khac`: Khác
- `status-don-hang-moi`: Mới
- `status-don-hang-da-xac-nhan`: Đã xác nhận
- `status-don-hang-dang-xu-ly`: Đang xử lý
- `status-don-hang-hoan-tat`: Hoàn tất
- `lbl-ma-don-hoan`: Mã đơn hoàn
- `lbl-ma-don-goc`: Mã đơn gốc
- `lbl-san-tmdt`: Sàn TMDT
- `lbl-trang-thai`: Trạng thái
- `lbl-tt-hoan-tmdt`: TT hoàn TMDT
- `lbl-tt-van-chuyen-hoan`: TT vận chuyển hoàn
- `lbl-tt-van-chuyen-nguoc`: TT vận chuyển ngược
- `lbl-tt-hoan-tu-kho`: TT hoàn từ kho
- `lbl-tien-hoan`: Tiền hoàn
- `lbl-ly-do-hoan`: Lý do hoàn
- `lbl-ngay-tao`: Ngày tạo
- `msg-loi-tai-ds-don-hoan`: Lỗi tải danh sách đơn hoàn
- `msg-loi-lay-ds-don-hoan`: Lỗi lấy danh sách đơn hoàn
- `title-chi-tiet-don-hang-hoan`: Chi tiết Đơn Hàng Hoàn

**don-hang-hoan-detail component (modal) — HTML + TS:**
- `section-thong-tin-chung`: Thông tin chung
- `lbl-ma-don-hoan-tmdt`: Mã đơn hoàn TMDT
- `lbl-loai-hoan`: Loại hoàn
- `lbl-ma-van-don-hoan`: Mã vận đơn hoàn
- `section-trang-thai`: Trạng thái
- `lbl-trang-thai-crm`: Trạng thái CRM
- `lbl-tt-bang-chung-ban`: TT bằng chứng bán
- `lbl-tt-van-chuyen-nguoc-normal-return`: TT vận chuyển ngược (Normal Return)
- `lbl-tt-hoan-tu-kho-itrr`: TT hoàn từ kho (ITRR)
- `section-thong-tin-hoan`: Thông tin hoàn
- `lbl-giai-phap-hoan`: Giải pháp hoàn
- `section-san-pham-hoan`: Sản phẩm hoàn
- `lbl-ten-san-pham`: Tên sản phẩm
- `lbl-ma-san-pham`: Mã sản phẩm
- `lbl-so-luong`: Số lượng
- `lbl-don-gia`: Đơn giá
- `section-chi-tiet-yeu-cau-hoan`: Chi tiết yêu cầu hoàn
- `msg-khong-tim-thay-don-hoan`: Không tìm thấy đơn hoàn
- `msg-loi-lay-chi-tiet-don-hoan`: Lỗi lấy chi tiết đơn hoàn

**Reused keys (already in DB from prior batches):**
- `error-title`: Lỗi
- `undetermined`: Không xác định

Total: 58 new keys + 2 reused. SQL: `sql/2026-05-13.sql`.

---

## Examples (for reference)

```markdown
### 2026-04-24 - Baseline (Order module)

- `order-status-pending`: Chờ duyệt
- `order-status-approved`: Đã duyệt
- `order-status-rejected`: Bị từ chối
- `msg-create-success`: Tạo thành công
- `msg-update-error`: Cập nhật thất bại
- `error-validation-required`: Trường bắt buộc
- `action-edit`: Sửa
- `action-delete`: Xóa
- `customer-name-placeholder`: Nhập tên khách hàng
- `branch-placeholder`: Chọn chi nhánh

### 2026-04-25 - New File (Sale module)

- `sale-title`: Quản lý bán hàng
- `sale-search-placeholder`: Tìm kiếm đơn hàng
- `status-active`: Hoạt động
- `status-inactive`: Không hoạt động
```

---

## Dedup Check

Before agent creates new key:
1. Search in this log
2. If found → reuse (don't create duplicate)
3. If not found → create new (add to log after execution)

---

## Maintenance

- Update after EVERY execution
- Append new entries (don't remove)
- Keep alphabetical within each date section
- Link to SQL file created: `trace/{date}-keys.sql`
