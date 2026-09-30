---
controller: DomainDonViController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (toàn bộ controller bị comment, không build vào API)"
muc_dich: "(BẢN CŨ — ĐÃ KHÓA) Quản lý domain theo đơn vị: admin đơn vị thêm/xóa/cập nhật/lấy/tìm domain. TOÀN BỘ FILE bị comment → không có endpoint nào tồn tại trong runtime."
alias: ["domain đơn vị", "domain don vi", "domain theo đơn vị"]
auth: internal
state_machine: []
deprecated: true
entities: [DomainDonVi]
stored_procs: []
depends: ["IDomainDonViService (không còn được wire vì controller bị comment)"]
cross_module_in: []
cross_module_out: []
---

# DomainDonViController — Quản lý domain theo đơn vị (ĐÃ KHÓA / COMMENT TOÀN BỘ)

**Toàn bộ file `DomainDonViController.cs` bị comment** (mọi dòng có tiền tố `//` từ `namespace` tới hết class). Controller **không được biên dịch thành endpoint** → không có route nào hoạt động. Coi như **deprecated**.

Theo phần code đã comment, thiết kế ban đầu: route `api/DomainDonVi/...`, `[TienIch(..., MaTienIchSCRM.TI_Chung)]`, ủy thác sang `IDomainDonViService`, dành cho admin đơn vị (role gợi ý `DV-AD-TONG`) với các action: `ThemMoi`, `Xoa`, `LayTheoId`, `CapNhat`, `TimTheoDieuKien`, `LayTatCa`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (Tất cả action — bản cũ) | — | (không tồn tại — code bị comment) | `DomainDonVi_ThemMoi` / `_Xoa` / `_LayTheoId` / `_CapNhat` / `_TimTheoDieuKien` / `_LayTatCa` | — | KHÓA | Không có hành vi — toàn bộ controller bị comment, không build | — | Cả file bị comment-out |

## Lỗi đã biết / lưu ý kỹ thuật
- `DomainDonViController.cs:1-89` — **toàn bộ file bị comment** → controller không tồn tại trong runtime. Mọi quyền `DomainDonVi_*` (thiết kế cho role `DV-AD-TONG`) hiện không gắn với endpoint nào.
- Nếu khôi phục: route gốc `api/DomainDonVi/...` (KHÔNG có Area), `MaTienIchSCRM.TI_Chung` (không phải TI_HeThong) → cần xem lại module phân loại.
