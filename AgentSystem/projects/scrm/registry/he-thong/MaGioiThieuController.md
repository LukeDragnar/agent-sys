---
controller: MaGioiThieuController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Mã giới thiệu: 2 góc nhìn — Admin (HT-AD) duyệt/quản lý + NGT (người giới thiệu, HT-NGT) tự xem mã của mình; grid lọc theo điều kiện, tìm theo đơn vị / hợp đồng; nút Thêm mới / Xóa / Đổi trạng thái sử dụng / Duyệt thanh toán hoa hồng"
muc_dich: "Quản lý mã giới thiệu (referral) của người giới thiệu (NGT): cấp mã, tra cứu theo đơn vị/hợp đồng, đổi trạng thái sử dụng, duyệt thanh toán hoa hồng. Phân tách rạch ròi endpoint cho Admin và cho User (NGT)."
alias: ["mã giới thiệu", "referral", "MaGioiThieu", "người giới thiệu", "NGT", "hoa hồng giới thiệu"]
auth: mixed
state_machine: []
deprecated: false
entities: [MaGioiThieu]
stored_procs: []
depends: ["IMaGioiThieuService", "Core.DataContext (SQL)"]
cross_module_in: ["FE Hệ thống/Mã giới thiệu (Admin + NGT) gọi CRUD/tìm kiếm/duyệt", "Kênh public (anonymous) check mã giới thiệu khi đăng ký/nhập mã — TimTheoMaGioiThieu"]
cross_module_out: ["Duyệt thanh toán hoa hồng → cập nhật trạng thái thanh toán theo hợp đồng (ripple sang nghiệp vụ hợp đồng/kế toán hoa hồng)"]
---

# MaGioiThieuController — Quản lý mã giới thiệu (referral) & hoa hồng

CRUD + tra cứu mã giới thiệu, tách 2 luồng quyền: **Admin** (HT-AD: quản lý toàn bộ) và **User/NGT** (HT-NGT: chỉ xem mã của chính mình theo `CurrentUser`). Có endpoint **public** (`[AllowAnonymous]`) để kiểm tra mã giới thiệu khi nhập mã. Tier quyền kèm role: HT-KT (kế toán), HT-CSKH gần như được mở ở mọi action. Đăng ký TienIch `MaGioiThieuController` thuộc nhóm `TI_HeThong`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách mã của tôi (NGT) | Grid (góc nhìn NGT) | POST `api/MaGioiThieu/GetListMaGioiThieuByUserLogin` | `MaGioiThieu_GetListMaGioiThieuByUserLogin` (HT-NGT, HT-KT, HT-CSKH) | public | Người đăng nhập là NGT | Lấy danh sách mã giới thiệu của `CurrentUser`. | EF `MaGioiThieu` | — |
| 2 | Lọc (Admin) | Grid lọc Admin | POST `api/MaGioiThieu/TimTheoDieuKienAdmin` | `MaGioiThieu_TimTheoDieuKienAdmin` (HT-AD, HT-KT, HT-CSKH) | public | Vai trò Admin | `TimTheoDieuKienAdmin(filter, CurrentUser)` — lọc toàn hệ thống. | EF | Method tên `TimTheoDieuKienAsync` nhưng route `...Admin` (lệch naming) |
| 3 | Lọc (NGT) | Grid lọc NGT | POST `api/MaGioiThieu/TimTheoDieuKienUser` | `MaGioiThieu_TimTheoDieuKienUser` (HT-NGT, HT-KT, HT-CSKH) | public | Vai trò NGT | `TimTheoDieuKienUser(filter, CurrentUser)` — giới hạn theo user. | EF | — |
| 4 | Tìm theo đơn vị / hợp đồng | Tab/filter đơn vị, hợp đồng | POST `api/MaGioiThieu/UserTimKiemTheoDonVi` · `/UserTimKiemTheoHopDong` · `/AdminTimKiemTheoDonVi` · `/AdminTimKiemTheoHopDong` | `MaGioiThieu_UserTimKiemTheoDonVi` · `_UserTimKiemTheoHopDong` · `_AdminTimKiemTheoDonVi` · `_AdminTimKiemTheoHopDong` (User: HT-NGT…; Admin: HT-AD…) | public | Theo vai trò | Tìm đơn vị/hợp đồng đã nhập mã GT — 4 biến thể User vs Admin. | EF | 4 endpoint gần trùng chỉ khác phạm vi user/admin |
| 5 | Thêm mới | Thêm mã GT | POST `api/MaGioiThieu/ThemMoi` | `MaGioiThieu_ThemMoi` (HT-AD, HT-KT, HT-CSKH) | public | ModelState valid | Validate model → `Create(data, CurrentUser)`. | EF | Chỉ Admin/KT/CSKH tạo (NGT không tự tạo) |
| 6 | Xóa | Xóa | POST `api/MaGioiThieu/Xoa/{id:Guid}` | `MaGioiThieu_Xoa` (HT-AD, HT-KT, HT-CSKH) | public | — | `Delete(id, CurrentUser)`. | EF | — |
| 7 | Chi tiết theo Id | Mở chi tiết | POST `api/MaGioiThieu/TimTheoId/{id:Guid}` | `MaGioiThieu_TimTheoId` (HT-AD, HT-KT, HT-CSKH) | public | — | `TimTheoId(id, CurrentUser)`. | EF | Dùng POST cho thao tác đọc 1 bản ghi |
| 8 | Kiểm tra mã giới thiệu | Nhập mã (public) | POST `api/MaGioiThieu/TimTheoMaGioiThieu` | — (`[AllowAnonymous]`) | anonymous | — | `TimTheoMaGioiThieu(payload, CurrentUser)` — check mã có hợp lệ. | EF | **Anonymous** — public không cần login; CurrentUser có thể null |
| 9 | Đổi trạng thái sử dụng | Bật/tắt sử dụng mã | POST `api/MaGioiThieu/ChangeStatusOfUse/{id:Guid}` | `MaGioiThieu_ChangeStatusOfUse` (HT-AD, HT-KT, HT-CSKH) | public | — | `ChangeStatusOfUse(id, CurrentUser)` — toggle trạng thái sử dụng. | EF | — |
| 10 | Duyệt thanh toán hoa hồng | Duyệt (1 hợp đồng) | POST `api/MaGioiThieu/DuyetThanhToanHoaHong/{id:Guid}` | `MaGioiThieu_DuyetThanhToanHoaHong` (HT-AD, HT-KT) | public | Theo hợp đồng | `DuyetThanhToanHoaHong(id, CurrentUser)` — duyệt theo id hợp đồng. | EF | Chỉ HT-AD/HT-KT (siết hơn các action khác) |
| 11 | Duyệt hoa hồng hàng loạt | Duyệt nhiều | POST `api/MaGioiThieu/DuyetThanhToanHoaHongMulti` | `MaGioiThieu_DuyetThanhToanHoaHongMulti` (HT-AD, HT-KT) | public | Danh sách id hợp đồng | `DuyetThanhToanHoaHongMulti(payload, CurrentUser)`. | EF | — |

## Quan hệ
- **Gọi sang:** `IMaGioiThieuService` (toàn bộ CRUD + duyệt hoa hồng), Core.DataContext (SQL).
- **Dữ liệu (entities):** `MaGioiThieu` (mã GT, NGT chủ mã, trạng thái sử dụng, liên kết đơn vị/hợp đồng, hoa hồng).
- **Ripple / đồng bộ:** Duyệt thanh toán hoa hồng → cập nhật trạng thái thanh toán theo hợp đồng; mã GT được tham chiếu khi đăng ký đơn vị/hợp đồng (kênh anonymous check mã).

## Lỗi đã biết / lưu ý kỹ thuật
- `MaGioiThieuController.cs:103-108` `TimTheoMaGioiThieu` `[AllowAnonymous]` — public không login; truyền `CurrentUser` (có thể null) xuống service → service phải null-safe.
- `MaGioiThieuController.cs:33` method `TimTheoDieuKienAsync` map route `TimTheoDieuKienAdmin` → tên method không khớp route (dễ nhầm khi grep).
- `MaGioiThieuController.cs:96-101` `TimTheoId` dùng **POST** cho thao tác đọc 1 bản ghi (lệch REST convention so với GET ở các controller khác).
- Phân quyền không đối xứng: phần lớn action mở cho HT-AD/HT-KT/HT-CSKH; riêng Duyệt hoa hồng (#10, #11) chỉ HT-AD/HT-KT — đúng ý đồ siết duyệt tiền.
