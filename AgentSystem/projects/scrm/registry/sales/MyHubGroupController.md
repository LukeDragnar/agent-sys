---
controller: MyHubGroupController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài MyHub (sim số) > Tổng đài đơn vị (tổng đài active, tìm theo điều kiện, kích hoạt tổng đài)"
muc_dich: "Quản lý tổng đài đơn vị cho MyHub (sim số): lấy tổng đài đang kích hoạt, tìm danh sách tổng đài theo điều kiện (auto-scope đơn vị), và đổi tổng đài đang kích hoạt."
alias: ["group MyHub", "tổng đài đơn vị MyHub", "kích hoạt tổng đài MyHub"]
auth: public
state_machine: []
deprecated: false
entities: [Group, TongDaiDonVi]
stored_procs: []
depends: [IGroupService]
cross_module_in: []
cross_module_out: []
---

# MyHubGroup — Tổng đài đơn vị MyHub (sim số)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tổng đài đang kích hoạt | (load) | GET `GetTongDaiActive` | `MyHubGroup_GetTongDaiActive` (nhiều role DV) | public | mở màn tổng đài | Trả tổng đài đang kích hoạt của đơn vị. | EF | — |
| 2 | Tìm danh sách tổng đài theo điều kiện | Tìm | POST `TimTheoDieuKien` | `MyHubGroup_TimTheoDieuKien` (HT-AD, HT-KT, DV-AD-TONG) | public | màn danh sách | Nếu **không** super/kế-toán/CSKH → ép `IdDonVi=DonViId`; trả danh sách. | EF | — |
| 3 | Đổi tổng đài đang kích hoạt | Kích hoạt | GET `ChangeActiveTongDai/{id}` | `MyHubGroup_ChangeActiveTongDai` (HT-AD, HT-KT, DV-AD-TONG) | public | dòng tổng đài | Đặt tổng đài id là active cho đơn vị. | EF | — |

## Quan hệ
- **Gọi sang:** `IGroupService` (tổng đài active + tìm + đổi active).
- **Dữ liệu (entities):** group/tổng đài đơn vị MyHub.
- **Ripple / đồng bộ:** đổi tổng đài active ảnh hưởng tổng đài đang dùng của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `MyHubGroupController.cs:33-40` — `TimTheoDieuKien` auto-scope đơn vị khi không phải super/HT-KT/CSKH.
