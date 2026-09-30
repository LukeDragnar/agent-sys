---
controller: NhomNganhNgheController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI riêng (cung cấp dữ liệu nhóm ngành nghề cho dropdown/lọc ngành nghề)"
muc_dich: "Tra cứu nhóm ngành nghề (phân cấp trên Ngành nghề). Hiện chỉ phơi 1 endpoint tìm theo điều kiện."
alias: ["nhóm ngành nghề", "phân loại ngành nghề", "NhomNganhNghe"]
auth: public
state_machine: []
deprecated: false
entities: [NhomNganhNghe]
stored_procs: []
depends: ["INhomNganhNgheService", "SBBaseController<NhomNganhNghe, NhomNganhNgheDto>"]
cross_module_in: ["FE/form ngành nghề gọi TimTheoDieuKien để lấy nhóm ngành nghề"]
cross_module_out: []
---

# NhomNganhNgheController — Nhóm ngành nghề

Kế thừa `SBBaseController` (có sẵn CRUD chuẩn) nhưng controller này CHỈ khai báo thêm 1 endpoint `TimTheoDieuKien`; các CRUD khác để mặc định kế thừa. Không có `[SBAuthorize]`/`[TienIch]` ở controller-level → endpoint mở (chỉ qua middleware mặc định).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới/dropdown) | POST `api/NhomNganhNghe/TimTheoDieuKien` | — | public | — | Lọc nhóm ngành nghề theo `NhomNganhNgheFilterDto`. | EF | — |
| 2 | CRUD kế thừa | (kế thừa) | `TimTheoId/{id}`, `LayTatCa`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` | — | public | — | CRUD chuẩn từ `SBBaseController`, không override/không gắn key. | EF | Endpoint kế thừa không gắn key → mở cho mọi request, không gate role |

## Quan hệ
- **Gọi sang:** INhomNganhNgheService (EF).
- **Dữ liệu (entities):** `NhomNganhNghe` (phân cấp trên Ngành nghề).
- **Ripple / đồng bộ:** không bắn event; danh mục tham chiếu.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller-level KHÔNG có `[SBAuthorize]`/`[TienIch]`, các method (kể cả CRUD kế thừa) không gắn permission key → không phân quyền theo role.
