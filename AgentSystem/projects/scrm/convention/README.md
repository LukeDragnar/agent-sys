# SCRM — Convention (quy tắc viết code riêng mảng, chi tiết)

> **Convention** = quy tắc viết code đúng, áp project-wide (proactive, "luôn/không bao giờ"). Khác `lessons/` (bug-sự-cố 1 lần) và `skills/` (recipe 1 tác vụ).
>
> **2 tầng convention:** `../patterns.md` = core/tổng quan (lean, luôn đọc khi code) ↔ folder này = **chi tiết per-chủ-đề** (load-on-demand, có ví dụ đúng/sai đầy đủ). `../qa-code.md` = checklist review mirror lại.

## Danh sách

- [canonical-partner-field-pair.md](canonical-partner-field-pair.md) — field lấy từ sàn lưu cặp canonical (chuẩn nội bộ) + raw (gốc đối tác, suffix `DoiTac`)
- [form-domain-agnostic.md](form-domain-agnostic.md) — form dùng-chung (HBBTL, thu/chi, nhập/xuất) không biết domain nguồn; caller compute flag generic, form patch blindly
- [ngzorro-disable-attribute.md](ngzorro-disable-attribute.md) — `nz-button`/`nz-input` dùng `[disabled]` native; component `nz-*` mới dùng `[nzDisabled]`
- [no-or-in-linq.md](no-or-in-linq.md) — KHÔNG dùng `OR` (`||`) trong LINQ Where của SCRM (perf/index)
- [tablewithnolock-autofilter-isdelete.md](tablewithnolock-autofilter-isdelete.md) — `TableWithNolock()` đã auto-filter `IsDelete` → đừng thêm `x.IsDelete==false` thủ công
- [no-query-isdelete-for-log.md](no-query-isdelete-for-log.md) — bảng log/lớn: lưu ý query `IsDelete`
- [interface-inline-service.md](interface-inline-service.md) — quy ước khai báo interface/service SCRM
- [mongo-filter-definition.md](mongo-filter-definition.md) — SCRM Mongo: dùng `FilterDefinition`/`Builders` đúng cách
- [them-config-object-long-opt-in.md](them-config-object-long-opt-in.md) — config mới trên DTO lớn (`CauHinhSettingDto`): gom object lồng tên rõ + cờ `IsApDung` opt-in (backward-compat, không cột DB); tránh magic number/nhân bản helper
- [ngzorro-select-list-ui.md](ngzorro-select-list-ui.md) — `nz-select` panel rộng (`nzDropdownMatchSelectWidth=false`) + label phân biệt trùng tên (`ten - domain`); bảng list nhất quán (STT/MÃ căn giữa, nút copy, thứ tự cột drill=list chính); thẻ tổng quan responsive `auto-fit minmax`

> Core BE/FE convention (nolock, IdDonVi, IMethodResult, MarkDirty, naming, BaseEntity, Transaction, Job-queue) → `../patterns.md`.
