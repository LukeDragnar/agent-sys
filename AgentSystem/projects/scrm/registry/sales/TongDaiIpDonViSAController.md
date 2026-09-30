---
controller: TongDaiIpDonViSAController
module: sales
service: Switchboard
man_hinh_ui: "Quản trị hệ thống (SanboxArea) > Giao dịch tổng đài IP đơn vị (super-admin duyệt/hủy thanh toán nạp tiền, sửa hạn giao dịch)"
muc_dich: "Khu vực super-admin (SanboxArea) quản lý giao dịch nạp tiền tổng đài IP của các đơn vị: xem lịch sử giao dịch, duyệt/hủy duyệt thanh toán nạp tiền, cập nhật ngày hết hạn chi tiết giao dịch."
alias: ["super admin tổng đài", "duyệt thanh toán tổng đài", "giao dịch tổng đài SA", "duyệt nạp tiền tổng đài"]
auth: internal
state_machine: [GiaoDich_ChoThanhToan, GiaoDich_DaThanhToan, GiaoDich_HuyThanhToan]
deprecated: false
entities: [GiaoDichTongDai, TongDaiIpDonVi]
stored_procs: []
depends: [ITongDaiIpDonViSAService]
cross_module_in: []
cross_module_out: ["TongDaiIpDonVi (duyệt/hủy giao dịch → đổi state thanh toán + cộng số dư tenant đơn vị)"]
---

# TongDaiIpDonViSA — Super-admin giao dịch tổng đài IP đơn vị

> Controller trong `SanboxArea` (`api/SanboxArea/...`), `TI_HeThong`, chỉ role hệ thống (HT-AD, HT-KT) → super-admin duyệt nạp tiền cho đơn vị.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lịch sử giao dịch tổng đài (theo đơn vị) | (load) | POST `GetLichSuThanhToanSA` | `TongDaiIpDonViSAController_GetLichSuThanhToanSA` (HT-AD, HT-KT) | internal | màn quản trị giao dịch | Trả lịch sử giao dịch nạp tiền theo `idDonVi`. | EF | — |
| 2 | Duyệt thanh toán nạp tiền | Duyệt | POST `DuyetThanhToanNapTienTongDaiSA` | `TongDaiIpDonViSAController_DuyetThanhToanNapTienTongDaiSA` (HT-AD, HT-KT) | internal | giao dịch chờ thanh toán | `idDonVi` rỗng → trả lỗi "Id đơn vị không được để trống"; hợp lệ → duyệt giao dịch → state **DaThanhToan** (cộng số dư đơn vị). | EF | — |
| 3 | Hủy duyệt thanh toán | Hủy duyệt | POST `HuyDuyetThanhToanToanTongDaiSA` | `TongDaiIpDonViSAController_HuyDuyetThanhToanToanTongDaiSA` (HT-AD, HT-KT) | internal | giao dịch đã duyệt | Hủy duyệt giao dịch → state **HuyThanhToan**. | EF | Không validate `idDonVi`/`idGiaoDich` rỗng như endpoint duyệt |
| 4 | Cập nhật ngày hết hạn giao dịch | Lưu | POST `UpdateThongTinGiaoDichDonViSA` | `TongDaiIpDonViSAController_UpdateThongTinGiaoDichDonViSA` (HT-AD, HT-KT) | internal | chi tiết giao dịch | Cập nhật ngày hết hạn chi tiết giao dịch. | EF | — |

## Quan hệ
- **Gọi sang:** `ITongDaiIpDonViSAService` (xử lý duyệt/hủy giao dịch + cập nhật hạn).
- **Dữ liệu (entities):** giao dịch nạp tiền tổng đài, tổng đài IP đơn vị.
- **Ripple / đồng bộ:** duyệt/hủy giao dịch SA gắn cùng state-machine với `TongDaiIpDonViController` (nạp tiền/thanh toán) — super-admin là người duyệt cuối cho luồng nạp tiền đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiIpDonViSAController.cs:54-57` — chỉ `DuyetThanhToan...` validate `idDonVi` rỗng; `HuyDuyetThanhToanToanTongDaiSA` (`:71`) KHÔNG validate `idDonVi`/`idGiaoDich` rỗng → có thể gọi với tham số null.
