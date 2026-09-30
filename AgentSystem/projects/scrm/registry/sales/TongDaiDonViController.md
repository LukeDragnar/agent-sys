---
controller: TongDaiDonViController
module: sales
service: Switchboard
man_hinh_ui: "Quản lý tổng đài (cấp hệ thống) > Tổng đài đơn vị (tạo/cập nhật/kết nối tổng đài, nạp tiền & duyệt số dư, đăng ký webhook)"
muc_dich: "Quản lý tổng đài cấp đơn vị (chủ yếu role hệ thống HT-AD/HT-KT): tạo/cập nhật/kết nối tổng đài cho đơn vị, đăng ký webhook, get số dư, nạp tiền + duyệt/xóa lệnh nạp tiền, xem lịch sử nạp tiền."
alias: ["tổng đài đơn vị", "nạp tiền tổng đài đơn vị", "duyệt số dư tổng đài", "đăng ký webhook tổng đài"]
auth: public
state_machine: []
deprecated: false
entities: [TongDaiDonVi, UnitConnective, LichSuNapTien]
stored_procs: []
depends: [ITongDaiDonViService, "Partner tổng đài (external — kết nối/webhook/balance)"]
cross_module_in: []
cross_module_out: ["Partner tổng đài (đăng ký webhook, get balance, nạp tiền)"]
---

# TongDaiDonVi — Quản lý tổng đài cấp đơn vị

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Chi tiết tổng đài đơn vị | (load) | POST `TongDaiDonViChiTiet` | `TongDaiDonVi_TongDaiDonViChiTiet` (HT-AD, HT-KT, DV-AD-TONG) | public | mở chi tiết | Trả chi tiết tổng đài đơn vị. | EF | — |
| 2 | Danh sách tổng đài đơn vị | (load) | POST `TongDaiDonViDanhSach` | `TongDaiDonVi_TongDaiDonViDanhSach` (HT-AD, HT-KT, DV-AD-TONG, DV-SALE, DV-QL-CG) | public | màn danh sách | Nếu **không** super/admin-HT/kế-toán/CSKH → ép `CrmUnitId = DonViId` (scope đơn vị); trả danh sách. | EF | — |
| 3 | Tạo / cập nhật / kết nối tổng đài | Lưu | POST `TongDaiDonViTaoMoi`, POST `TongDaiDonViCapNhat`, GET `TongDaiDonViKetNoi` | `_TongDaiDonViTaoMoi` / `_TongDaiDonViCapNhat` / `_TongDaiDonViKetNoi` (HT-AD, HT-KT) | public | màn tổng đài | Tạo mới / cập nhật / kết nối tổng đài đã tồn tại theo `switchBoardCode`+email. | EF + partner | — |
| 4 | Đăng ký webhook | Đăng ký webhook | POST `TongDaiDangKyWebhook` | `TongDaiDonVi_TongDaiDangKyWebhook` (HT-AD, HT-KT) | public | tổng đài đã kết nối | Đăng ký webhook nhận data từ partner tổng đài. | partner | — |
| 5 | Get số dư | (load) | POST `TongDaiGetBalance` | `TongDaiDonVi_TongDaiGetBalance` (HT-AD, HT-KT) | public | màn số dư | Trả số dư tổng đài đơn vị (gọi partner). | partner | — |
| 6 | Nạp tiền + lịch sử nạp tiền | Nạp tiền | POST `TongDaiNapTienDonVi`, POST `GetHistoryNapTienDonVi`, POST `GetAllDonViNapTienHistory` | `_TongDaiNapTienDonVi` / `_GetHistoryNapTienDonVi` / `_GetAllDonViNapTienHistory` (HT-AD, HT-KT) | public | tab nạp tiền | Nạp tiền; lịch sử nạp tiền theo đơn vị (false) hoặc toàn bộ đơn vị (true). | EF | — |
| 7 | Duyệt số dư / xóa lệnh nạp tiền | Duyệt | POST `TongDaiDuyetSoDuDonVi`, POST `TongDaiXoaLenhNapTien` | `_TongDaiDuyetSoDuDonVi` / `_TongDaiXoaLenhNapTien` (HT-AD, HT-KT) | public | lệnh nạp tiền chờ | Duyệt số dư (cập nhật balance) / xóa lệnh nạp tiền. | EF | — |

## Quan hệ
- **Gọi sang:** `ITongDaiDonViService` → partner tổng đài (kết nối, webhook, get balance, nạp tiền).
- **Dữ liệu (entities):** tổng đài đơn vị, lịch sử nạp tiền, số dư.
- **Ripple / đồng bộ:** kết nối/webhook/nạp tiền đẩy sang partner tổng đài (external).

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiDonViController.cs:43-48` — `TongDaiDonViDanhSach` auto-scope đơn vị khi user không phải super/HT-AD/HT-KT/CSKH (`CrmUnitId = DonViId`) → DV-SALE/DV-QL-CG chỉ thấy tổng đài đơn vị mình.
- `GetHistoryNapTienDonVi` dùng chung service với cờ bool: `false` (theo đơn vị) vs `true` (all đơn vị) — phân biệt qua endpoint, không qua tham số UI.
