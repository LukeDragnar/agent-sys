---
controller: DonViTienIchController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Đơn vị tiện ích — danh sách/tìm kiếm đơn vị tiện ích (api/QuanTriArea/DonViTienIch); chỉ HT-AD/HT-KT"
muc_dich: "Tra cứu danh sách đơn vị tiện ích theo điều kiện (tiện ích nào đang gắn cho đơn vị nào). Controller mỏng 1 endpoint, ủy quyền IDonViTienIchService."
alias: ["đơn vị tiện ích", "DonViTienIch", "tiện ích của đơn vị"]
auth: public
state_machine: []
deprecated: false
entities: [DonViTienIch]
stored_procs: []
depends: ["IDonViTienIchService"]
cross_module_in: ["FE Quản trị tiện ích gọi TimTheoDieuKien để xem đơn vị nào đang dùng tiện ích"]
cross_module_out: []
---

# DonViTienIchController — Quản lý đơn vị tiện ích

Controller mỏng vùng quản trị (`Areas/SanboxArea`, `[TienIch(..., TI_HeThong, true)]`, route `api/QuanTriArea/DonViTienIch/...`). Chỉ 1 endpoint tìm theo điều kiện, ủy quyền `IDonViTienIchService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Grid/Lọc | POST `api/QuanTriArea/DonViTienIch/TimTheoDieuKien` | `DonViTienIch_TimTheoDieuKien` | public (HT-AD/HT-KT) | — | `TimTheoDieuKien(DonViTienIchPayload)` → danh sách đơn vị-tiện ích lọc theo điều kiện. | EF `DonViTienIch` | mô tả permission ghi nhầm "Tìm kiếm chi nhánh" (copy-paste) — `DonViTienIchController.cs:21`. |

## Quan hệ
- **Gọi sang:** `IDonViTienIchService`.
- **Dữ liệu (entities):** `DonViTienIch`. DTO: `DonViTienIchPayload`.
- **Ripple / đồng bộ:** không có.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonViTienIchController.cs:21` — text mô tả của `[SBAuthorize]` ghi "Tìm kiếm chi nhánh" (sai, đáng lẽ "đơn vị tiện ích") — copy-paste, không ảnh hưởng hành vi.
