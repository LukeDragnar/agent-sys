---
controller: NhomNhanVienController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > (nội bộ) — lấy danh sách nhóm nhân viên theo 1 đơn vị bất kỳ cho màn quản trị (route api/QuanTriArea/NhomNhanVien)"
muc_dich: "Biến thể QUẢN TRỊ của NhomNhanVien: cho tài khoản quản trị hệ thống (HT-AD, HT-CSKH) lấy tất cả nhóm theo id đơn vị bất kỳ (không giới hạn đơn vị của CurrentUser như bản Controllers)."
alias: ["nhóm nhân viên quản trị", "NhomNhanVien QuanTriArea"]
auth: public
state_machine: []
deprecated: false
entities: [NhomNhanVien]
stored_procs: []
depends: ["INhomNhanVienService", "BaseController"]
cross_module_in: ["Màn quản trị hệ thống gọi LayTatCaNhomTheoIdDonVi để xem nhóm của 1 đơn vị bất kỳ"]
cross_module_out: []
---

# NhomNhanVienController (QuanTriArea) — Nhóm nhân viên (quản trị)

Biến thể QuanTriArea, kế thừa `BaseController` (KHÔNG phải SBBaseController). Chỉ 1 endpoint: lấy nhóm theo id đơn vị truyền vào — dành cho admin hệ thống xem nhóm của đơn vị khác. Controller-level `[TienIch(TI_HeThong)]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy nhóm theo đơn vị | (load lưới quản trị) | GET `api/QuanTriArea/NhomNhanVien/LayTatCaNhomTheoIdDonVi/{id:Guid}` | `NhomNhanVienController_LayTatCaTheoIdDonVi` (HT-AD, HT-CSKH) | public | — | `LayTatCaNhomTheoIdDonVi(id)` trả tất cả nhóm của đơn vị có id truyền vào. | EF | — |

## Quan hệ
- **Gọi sang:** INhomNhanVienService (dùng chung service với bản Controllers).
- **Dữ liệu (entities):** `NhomNhanVien`.
- **Ripple / đồng bộ:** chỉ đọc, không ripple.

## Lỗi đã biết / lưu ý kỹ thuật
- Khác bản `Controllers/NhomNhanVienController` (CRUD đầy đủ, giới hạn theo đơn vị CurrentUser): bản này nhận id đơn vị tùy ý → chỉ nên cấp cho admin hệ thống (HT-AD/HT-CSKH).
