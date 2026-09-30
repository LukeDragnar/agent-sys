---
controller: LichSuTacDongController
module: he-thong
service: LogService
man_hinh_ui: "Lịch sử > Lịch sử tác động (hoạt động) — màn tra cứu + xóa"
muc_dich: "Tra cứu lịch sử tác động (audit hoạt động) theo bộ lọc; cung cấp dropdown tác động/bảng; cho phép xóa 1 dòng lịch sử."
alias: ["lịch sử tác động", "lịch sử hoạt động", "audit log"]
auth: internal
state_machine: []
deprecated: false
entities: [LichSuTacDong]
stored_procs: []
depends: [ILichSuTacDongService]
cross_module_in: ["FE màn lịch sử tác động gọi Dropdown*, TimTheoDieuKien, Xoa"]
cross_module_out: []
---

# LichSuTacDongController — Lịch sử tác động (hoạt động)

TienIch: `LichSuTacDongController` "Quản lý lịch sử hoạt động" (TI_Chung). Route `api/LichSuTacDong`. Mọi action có `[SBAuthorize]` key riêng → internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dropdown tác động | (combobox) | GET `api/LichSuTacDong/DropdownActivity` | LichSuTacDong_DropdownActivity | internal | — | List loại tác động | EF | — |
| 2 | Dropdown bảng | (combobox) | GET `api/LichSuTacDong/DropdownTable` | LichSuTacDong_DropdownTable | internal | — | List tableId | EF | — |
| 3 | Tìm theo điều kiện | Tìm | POST `api/LichSuTacDong/TimTheoDieuKien` | LichSuTacDong_TimTheoDieuKien | internal | — | Lọc lịch sử tác động + CurrentUser | EF | — |
| 4 | Xóa 1 dòng | Xóa | POST `api/LichSuTacDong/Xoa/{id}` | LichSuTacDong_Xoa | internal | — | Xóa 1 bản ghi lịch sử theo id + CurrentUser | EF | — |

## Quan hệ
- **Gọi sang:** `ILichSuTacDongService` (Dropdown*, TimTheoDieuKien, XoaLichSuTacDong).
- **Dữ liệu (entities):** `LichSuTacDong`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- (không phát hiện)
