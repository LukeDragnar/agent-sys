---
controller: HopDongTienIchController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Hợp đồng tiện ích / Yêu cầu mua tiện ích — danh sách + duyệt/hủy duyệt + chi tiết + xóa (api/QuanTriArea/HopDongTienIch); HT-AD/HT-KT"
muc_dich: "Quản lý hợp đồng (yêu cầu mua) tiện ích: tìm theo điều kiện, duyệt/hủy duyệt hợp đồng, xem chi tiết, xóa."
alias: ["hợp đồng tiện ích", "HopDongTienIch", "yêu cầu mua tiện ích", "duyệt tiện ích"]
auth: public
state_machine: [ChoDuyet, DaDuyet, HuyDuyet]
deprecated: false
entities: [HopDongTienIch]
stored_procs: []
depends: ["IHopDongTienIchService"]
cross_module_in: ["FE Quản trị tiện ích gọi TimTheoDieuKien + DuyetHopDong/HuyDuyetHopDong + ChiTietHopDong"]
cross_module_out: ["Duyệt hợp đồng tiện ích → kích hoạt tiện ích cho đơn vị (qua HopDongTienIchService)"]
---

# HopDongTienIchController — Quản lý hợp đồng tiện ích

Controller vùng quản trị (`partial`, namespace `Core.API.Controllers`, route `api/QuanTriArea/HopDongTienIch/...`), `[SBAuthorize]` mức controller. Quản lý yêu cầu mua / hợp đồng tiện ích kèm luồng duyệt. Ủy quyền `IHopDongTienIchService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách yêu cầu mua tiện ích | Grid/Lọc | POST `api/QuanTriArea/HopDongTienIch/TimTheoDieuKien` | `HopDongTienIch_TimTheoDieuKien` | public (HT-AD/HT-KT) | — | `TimTheoDieuKienAsync(HopDongTienIchFilterDto)`. | EF `HopDongTienIch` | — |
| 2 | Duyệt hợp đồng | Duyệt | POST `api/QuanTriArea/HopDongTienIch/DuyetHopDong/{ma}` | `HopDongTienIch_DuyetHopDong` | public (HT-AD/HT-KT) | hợp đồng đang chờ duyệt | `DuyetHopDongAsync(ma)` → duyệt theo mã hợp đồng (kích hoạt tiện ích). | EF `HopDongTienIch` | — |
| 3 | Hủy duyệt hợp đồng | Hủy duyệt | POST `api/QuanTriArea/HopDongTienIch/HuyDuyetHopDong/{id}` | `HopDongTienIch_HuyDuyetHopDong` | public (HT-AD/HT-KT) | hợp đồng đã duyệt | `HuyDuyetHopDong(id)`. | EF `HopDongTienIch` | — |
| 4 | Chi tiết hợp đồng | Mở chi tiết | POST `api/QuanTriArea/HopDongTienIch/ChiTietHopDong/{id}` | `HopDongTienIch_ChiTietHopDong` | public (HT-AD/HT-KT) | — | `ChiTietHopDongAsync(id)`. | EF `HopDongTienIch` | — |
| 5 | Xóa hợp đồng | Xóa | POST `api/QuanTriArea/HopDongTienIch/Xoa/{id}` | `HopDongTienIch_Xoa` | public (HT-AD/HT-KT) | — | Check `GetByIdAsync(id)` ≠ null (không có → `MESS_NotFound`); có → `DeleteAsync(id)`, trả `MESS_Delete_Success`. | EF `HopDongTienIch` | — |

## Quy tắc nghiệp vụ (gating / state)
- Trạng thái hợp đồng tiện ích: chờ duyệt → duyệt (`DuyetHopDong`) → có thể hủy duyệt (`HuyDuyetHopDong`). Duyệt theo **mã** (`{ma}`), các thao tác khác theo **id**.

## Quan hệ
- **Gọi sang:** `IHopDongTienIchService`.
- **Dữ liệu (entities):** `HopDongTienIch`. DTO: `HopDongTienIchFilterDto`.
- **Ripple / đồng bộ:** duyệt hợp đồng → kích hoạt tiện ích cho đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `DuyetHopDong` nhận `{ma}` (string) trong khi `HuyDuyet/ChiTiet/Xoa` nhận `{id}` (Guid) — không đồng nhất key thao tác trên cùng hợp đồng.
