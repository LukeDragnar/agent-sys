---
controller: ChuyenGiaController
module: website
service: Website
man_hinh_ui: "Website > Quản lý chuyên gia — list chuyên gia (bác sĩ/kỹ thuật viên) hiển thị trên website của đơn vị; nút Thêm mới + Sửa/Xóa per-row"
muc_dich: "Quản lý danh mục chuyên gia của đơn vị để hiển thị trên website (CRUD): tìm theo điều kiện, thêm/sửa/xóa; cung cấp endpoint private cho service khác lấy chuyên gia."
alias: ["chuyên gia", "quản lý chuyên gia", "bác sĩ", "chuyên gia website", "expert"]
auth: mixed
state_machine: []
deprecated: false
entities: [ChuyenGia]
stored_procs: []
depends: [Website (ChuyenGiaService)]
cross_module_in: [Storefront/Website private consumers (TimTheoDieuKienPrivate, TimTheoIdPrivate — [PrivateSBAuthorize])]
cross_module_out: []
---

# ChuyenGiaController (Website) — Quản lý chuyên gia hiển thị trên website

> Trùng tên với `ChuyenGiaController` ở module `he-thong/` (controller khác). Node này là bản thuộc **Website service** (`Website.API/Controllers/Api/ChuyenGiaController.cs`), route `api/ChuyenGia`, `partial class : BaseCoreController`, `[TienIchAttribute(..., MaTienIchSCRM.TI_Website)]`. Quyền gắn theo role nhóm `DV-*` (dịch vụ/spa).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (list chuyên gia) | POST `api/ChuyenGia/TimTheoDieuKien` | `ChuyenGia_TimTheoDieuKien` | public | luôn | Set `IdDonVi = CurrentUser.DonViId` → list chuyên gia theo đơn vị + filter | EF | - |
| 2 | Lấy chi tiết chuyên gia | "Sửa"/xem → popup | GET `api/ChuyenGia/TimTheoId/{id}` | `ChuyenGia_TimTheoId` | public | per-row | Trả chi tiết chuyên gia theo id | EF | - |
| 3 | Thêm mới chuyên gia | "Thêm mới" → popup | POST `api/ChuyenGia/ThemMoi` | `ChuyenGia_ThemMoi` | public | luôn | Tạo chuyên gia mới (kèm CurrentUser) | EF | - |
| 4 | Cập nhật chuyên gia | "Lưu" (popup sửa) | POST `api/ChuyenGia/CapNhatThongTin` | `ChuyenGia_CapNhatThongTin` | public | per-row | Cập nhật chuyên gia (kèm CurrentUser) | EF | - |
| 5 | Xóa chuyên gia | "Xóa" (icon delete, per-row) | POST `api/ChuyenGia/Xoa/{id}` | `ChuyenGia_Xoa` | public | per-row | Xóa chuyên gia theo id | EF | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **Storefront/Website** gọi `TimTheoDieuKienPrivate` (ChuyenGiaFilterReqDto) → list chuyên gia hiển thị công khai; `TimTheoIdPrivate/{id}` → chi tiết 1 chuyên gia. Không gắn permission key (chỉ token service-to-service).

## Quan hệ
- **Gọi sang:** nội bộ `ChuyenGiaService` (Website service).
- **Dữ liệu (entities):** `ChuyenGia` (lọc theo `IdDonVi` của user).
- **Ripple / đồng bộ:** chuyên gia hiển thị trên storefront qua endpoint private; đổi/xóa → ảnh hưởng nội dung trang website đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `partial class` → có thể còn endpoint ở file partial khác; khi trace cần grep cả các partial.
- Endpoint admin lọc theo `CurrentUser.DonViId`, nhưng `TimTheoDieuKienPrivate` gọi thẳng service KHÔNG ép `IdDonVi` từ token → caller private phải tự truyền `IdDonVi` trong filter, nếu thiếu có thể trả chéo đơn vị (`ChuyenGiaController.cs:70-74`).
- Hai bản `ChuyenGiaController` (he-thong vs website) — đừng nhầm permission key giữa 2 controller.
