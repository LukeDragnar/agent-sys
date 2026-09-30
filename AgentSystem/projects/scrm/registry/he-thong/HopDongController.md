---
controller: HopDongController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Hợp đồng — danh sách/CRUD hợp đồng (api/QuanTriArea/HopDong); HT-AD/HT-KT"
muc_dich: "CRUD hợp đồng (HopDong) kế thừa SBBaseController + 1 endpoint tìm theo điều kiện. Controller mỏng vùng quản trị."
alias: ["hợp đồng", "HopDong", "contract"]
auth: public
state_machine: []
deprecated: false
entities: [HopDong]
stored_procs: []
depends: ["IHopDongService"]
cross_module_in: ["FE Quản trị hợp đồng gọi CRUD + TimTheoDieuKien"]
cross_module_out: []
---

# HopDongController — Quản lý hợp đồng

Controller vùng quản trị (`Areas/SanboxArea`, `partial`) kế thừa `SBBaseController<HopDong, HopDongDto>` (route `api/QuanTriArea/HopDong/...`), `[SBAuthorize]` mức controller. CRUD chuẩn từ base + 1 endpoint `TimTheoDieuKien`. Ủy quyền `IHopDongService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | CRUD chuẩn (kế thừa SBBaseController) | Thêm/Sửa/Xóa/Xem | (kế thừa) GET `.../TimTheoId/{id}`, GET `.../LayTatCa`, POST `.../ThemMoi`, `.../CapNhatThongTin`, `.../Xoa/{id}` | — (`[SBAuthorize]` mức controller, không key riêng) | public | — | CRUD HopDong qua `IBaseService` (map Dto↔Entity). | EF `HopDong` | — |
| 2 | Tìm theo điều kiện | Grid/Lọc | POST `api/QuanTriArea/HopDong/TimTheoDieuKien` | `HopDong_TimTheoDieuKien` | public (HT-AD/HT-KT) | — | `TimTheoDieuKienAsync(HopDongFilterDto, CurrentUser)` → danh sách hợp đồng lọc theo điều kiện + ngữ cảnh user. | EF `HopDong` | mô tả role trong key ghi `"(HT-AD, HT-KT)"` (có ngoặc thừa) — `HopDongController.cs:32`. |

## Quan hệ
- **Gọi sang:** `IHopDongService` (TimTheoDieuKienAsync) + `IBaseService<HopDong>` (CRUD nền).
- **Dữ liệu (entities):** `HopDong`. DTO: `HopDongDto`, `HopDongFilterDto`.
- **Ripple / đồng bộ:** không có rõ rệt.

## Lỗi đã biết / lưu ý kỹ thuật
- `HopDongController.cs:32` — chuỗi role của `[SBAuthorize]` là `"(HT-AD, HT-KT)"` (kèm dấu ngoặc thừa) → có thể lệch khi parse role.
