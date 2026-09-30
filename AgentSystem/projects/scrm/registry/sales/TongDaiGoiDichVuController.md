---
controller: TongDaiGoiDichVuController
module: sales
service: Switchboard
man_hinh_ui: "Quản lý tổng đài (cấp hệ thống) > Gói dịch vụ tổng đài (danh sách, gia hạn, nâng cấp gói)"
muc_dich: "Quản lý gói dịch vụ tổng đài (cấp hệ thống HT-AD/HT-KT): lấy danh sách gói dịch vụ, gia hạn gói, nâng cấp gói dịch vụ tổng đài cho đơn vị."
alias: ["gói dịch vụ tổng đài", "gia hạn gói tổng đài", "nâng cấp gói tổng đài", "service package"]
auth: public
state_machine: []
deprecated: false
entities: [TongDaiGoiDichVu, ServicePackage]
stored_procs: []
depends: [ITongDaiGoiDichVuService, "Partner tổng đài (external)"]
cross_module_in: []
cross_module_out: ["Partner tổng đài (gia hạn/nâng cấp gói)"]
---

# TongDaiGoiDichVu — Gói dịch vụ tổng đài

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách gói dịch vụ | (load) | POST `TongDaiGoiDichVuDanhSach` | `TongDaiGoiDichVu_TongDaiGoiDichVuDanhSach` (HT-AD, HT-KT) | public | màn gói dịch vụ | Trả danh sách gói dịch vụ tổng đài. | EF | — |
| 2 | Gia hạn gói dịch vụ | Gia hạn | POST `TongDaiGiaHanGoiDichVu` | `TongDaiGoiDichVu_TongDaiGiaHanGoiDichVu` (HT-AD, HT-KT) | public | gói đang dùng | Gia hạn gói dịch vụ tổng đài. | EF + partner | — |
| 3 | Nâng cấp gói dịch vụ | Nâng cấp | POST `TongDaiNangCapGoiDichVu` | `TongDaiGoiDichVu_TongDaiNangCapGoiDichVu` (HT-AD, HT-KT) | public | gói đang dùng | Nâng cấp gói dịch vụ tổng đài. | EF + partner | — |

## Quan hệ
- **Gọi sang:** `ITongDaiGoiDichVuService` → partner tổng đài (gia hạn/nâng cấp gói).
- **Dữ liệu (entities):** gói dịch vụ tổng đài / service package.
- **Ripple / đồng bộ:** gia hạn/nâng cấp đẩy sang partner tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller gọn, chỉ HT-AD/HT-KT thao tác. Không có endpoint comment hay khe hở phân quyền.
