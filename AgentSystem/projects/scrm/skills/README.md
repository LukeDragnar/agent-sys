# SCRM — Hướng dẫn theo tác vụ (Recipes)

**Mục đích:** Mỗi file = công thức làm-theo cho **1 tác vụ tái sử dụng**. Khác với [`../patterns.md`](../patterns.md) (rule nền áp cho mọi code).

**Cơ chế load-on-demand:** Agent **KHÔNG** đọc hết folder này mỗi task. Chỉ đọc đúng file khi task chạm tới tác vụ đó. Giữ context nhẹ.

> **Trạng thái:** ✅ verify từ source thật · 🟡 verify một phần (đối chiếu thêm khi dùng) · 🔲 chưa điền

---

## Nhóm gốc

| Tác vụ | File | Trạng thái |
|--------|------|-----------|
| Phân trang BE ↔ FE | [paging.md](paging.md) | ✅ |
| Xuất Excel | [export-excel.md](export-excel.md) | ✅ |
| Upload / Import file | [upload-file.md](upload-file.md) | ✅ |
| Search / Filter | [search-filter.md](search-filter.md) | ✅ |
| Loading (spinner chung Sandbox) | [loading.md](loading.md) | ✅ |
| Bảng list (paging đáy + ghim cột + cột động) | [table-list.md](table-list.md) | ✅ |

## A — CRUD & Form

| Tác vụ | File | Trạng thái |
|--------|------|-----------|
| Tạo Entity mới (EF Core, kiểu cột chuỗi) | [tao-entity.md](tao-entity.md) | ✅ |
| Modal create/edit (popup) | [modal-form.md](modal-form.md) | ✅ |
| Lưu (create + update chung) | [save-luu.md](save-luu.md) | ✅ |
| Xóa + confirm | [delete-confirm.md](delete-confirm.md) | ✅ |
| Validate reactive form | [validate-form.md](validate-form.md) | ✅ |
| Confirm khi đóng form/drawer có thay đổi chưa lưu | [confirm-on-dirty-drawer-form.md](confirm-on-dirty-drawer-form.md) | ✅ |

## B — Hiển thị & chọn dữ liệu

| Tác vụ | File | Trạng thái |
|--------|------|-----------|
| Dropdown/combo load + cache | [dropdown-cache.md](dropdown-cache.md) | ✅ |
| Tree / TreeSelect | [tree-select.md](tree-select.md) | ✅ |
| Date / Date-range picker | [date-range.md](date-range.md) | ✅ |
| Autocomplete / search-select | [autocomplete-select.md](autocomplete-select.md) | ✅ |
| Chống gọi API trùng + race (đổi filter / chuyển tab) | [chong-goi-api-trung-va-race.md](chong-goi-api-trung-va-race.md) | ✅ |

## C — In ấn & xuất

| Tác vụ | File | Trạng thái |
|--------|------|-----------|
| In phiếu / tem / barcode / QR | [print-barcode.md](print-barcode.md) | ✅ |
| Export PDF | [export-pdf.md](export-pdf.md) | ✅ |
| Bulk action (chọn nhiều dòng) | [bulk-action.md](bulk-action.md) | 🟡 (BE pattern khung) |

## D — Hệ thống & tích hợp

| Tác vụ | File | Trạng thái |
|--------|------|-----------|
| Phân quyền màn hình (Page/MaTinhNang) | [permission.md](permission.md) | ✅ |
| MassTransit publish/consume | [masstransit.md](masstransit.md) | ✅ |
| Quartz job | [quartz-job.md](quartz-job.md) | ✅ |
| Gửi SMS / Zalo / Notification | [send-notification.md](send-notification.md) | ✅ |
| Socket realtime (SignalR) | [socket-realtime.md](socket-realtime.md) | ✅ |
| Config StagingDebug (FE local ↔ staging DB/auth) | [stagingdebug-config.md](stagingdebug-config.md) | ✅ |

---

## Quy tắc bảo trì
- **Thêm recipe mới:** tạo `{ten-tac-vu}.md` + thêm 1 dòng vào bảng trên.
- **Đừng tách quá vụn:** gom theo nhóm tác vụ, mỗi recipe đủ "1 màn hình".
- **Mỗi recipe nên có:** (1) khi nào dùng · (2) code BE · (3) code FE · (4) bẫy thường gặp · (5) file source mẫu thật.
- Recipe được trích từ source SCRM thật (quét 2026-06-19) — khi code thay đổi nhiều, quét lại để cập nhật file:line.
