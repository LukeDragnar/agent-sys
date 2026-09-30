---
controller: HotlineController
module: sales
service: Switchboard
man_hinh_ui: "Quản trị hệ thống (SanboxArea) > Hotline tổng đài (lịch sử kích hoạt hotline, gia hạn hotline)"
muc_dich: "Quản lý hotline tổng đài ở khu vực quản trị (SanboxArea, role HT-KT/HT-AD): xem lịch sử kích hoạt hotline và gia hạn hotline (auto-scope đơn vị nếu không phải super/kế-toán). (Có bản trùng tên ở folder Controllers — rỗng)."
alias: ["hotline tổng đài", "gia hạn hotline", "lịch sử kích hoạt hotline"]
auth: public
state_machine: []
deprecated: false
entities: [Hotline, HistoryActiveHotline]
stored_procs: []
depends: [IHotlineService]
cross_module_in: []
cross_module_out: []
---

# Hotline (SanboxArea) — Hotline tổng đài (quản trị)

> ⚠️ Có 2 HotlineController. Bản ACTIVE này ở `Areas\SandBoxArea\HotlineController.cs` (route `api/SanboxArea/Hotline`, `TI_HeThong`). Bản `Controllers\HotlineController.cs` (route `api/Hotline`, MyHub v1) rỗng → node `HotlineController_Switchboard.md`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lịch sử kích hoạt hotline | (load) | POST `GetHistoryActiveHotline` | `Hotline_SanboxArea_GetHistoryActiveHotline` (HT-KT, HT-AD) | public | màn hotline | Nếu **không** super/kế-toán → ép `IdDonVi = CurrentUser.DonViId` (scope đơn vị); trả lịch sử kích hoạt hotline. | EF | — |
| 2 | Gia hạn hotline | Gia hạn | POST `ExtendTimeHotline` | `Hotline_SanboxArea_ExtendTimeHotline` (HT-KT, HT-AD) | public | hotline đang dùng | Gia hạn thời gian hotline cho `CurrentUser`. | EF | — |

## Quan hệ
- **Gọi sang:** `IHotlineService` (lịch sử + gia hạn hotline).
- **Dữ liệu (entities):** hotline, lịch sử kích hoạt hotline.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas\SandBoxArea\HotlineController.cs:23-34` — `GetHotlineByIdUserPartner` đã comment.
- `GetHistoryActiveHotline` auto-scope đơn vị khi không phải super/HT-KT.
