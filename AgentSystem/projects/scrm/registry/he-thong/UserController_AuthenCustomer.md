---
controller: UserController
module: he-thong
service: AuthenCustomer
man_hinh_ui: "App mobile khách hàng — gần như toàn bộ màn của app khách: combo/thẻ dịch vụ đã mua, đặt & lịch sử lịch hẹn, chi nhánh, danh mục dịch vụ/sản phẩm, đơn hàng, thông tin cá nhân (điểm thưởng/ghi nợ/khuyến mãi/hợp đồng/ảnh)."
muc_dich: "Cổng dữ liệu chính cho app khách hàng: tra cứu danh mục (dịch vụ/sản phẩm/combo/thẻ/chi nhánh), đặt/hủy lịch hẹn, xem đơn hàng & lịch sử, và thông tin cá nhân — tất cả scope theo khách đang login."
alias: ["app khách hàng", "màn hình app member", "đặt lịch app", "combo đã mua", "thẻ dịch vụ", "đơn hàng app khách", "điểm thưởng app", "user customer api"]
auth: public
state_machine: []
deprecated: false
entities: [User]
stored_procs: []
depends: ["IUserService (AuthenCustomer.Service) — orchestrate cross-service", "Cross-service: Order (đơn hàng/lịch sử dùng DV/đơn tạm), Booking (lịch hẹn/khung giờ), Core/ChiNhanh (chi nhánh), Promotion (khuyến mãi), WarehouseProduct (sản phẩm), Customer (điểm thưởng/ghi nợ/hợp đồng/ảnh)"]
cross_module_in: ["App mobile khách (JWT khách) → toàn bộ endpoint *_Private", "Back-office (token nhân viên) → GetByIdKhach"]
cross_module_out: ["Tạo đơn nhập (TaoDonNhap) → Order service", "Tạo/hủy lịch hẹn (TaoLich/HuyLich) → Booking service"]
---

# UserController — Cổng dữ liệu app khách hàng (AuthenCustomer)

Controller **lớn (~35 endpoint)** — trung tâm dữ liệu cho app mobile khách hàng. Kế thừa `BaseController`, hầu hết endpoint `[SBAuthorizeCustomer]` (JWT khách) + validate `ModelState` + truyền `CurrentCustomer` xuống `IUserService` (mọi logic/cross-service ở UserService). Nhóm theo region: Combo, TheDichVu, LichHen, ChiNhanh, DichVu, SanPham, DonHang, ThongTinKhachHang. Hầu hết là READ scope theo khách; số ít WRITE: đặt/hủy lịch (`TaoLich`/`HuyLich`), tạo đơn (`TaoDonNhap`). Theo schema (>25 endpoint) → bảng giữ hành động UI tiêu biểu, phần còn lại gom ở "Endpoint nội bộ".

## Hành động (UI / công khai) — nhóm tiêu biểu
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Combo của tôi / theo chi nhánh | Tab "Combo" | POST `GetListComboDaMua`, `GetComboByChiNhanh_Private`, `GetDanhSachComboPrivate`, `GetDetailComboPrivate/{id}`, `GetDetailComboKhachHangByIdPrivate` | — | public (login) | Tab combo | Trả combo đã mua / theo chi nhánh / danh mục / chi tiết — scope CurrentCustomer. Một số map `payload → LayTatCaPrivateFilterDto` trước khi gọi service. | qua UserService (cross-service Order/Customer) | — |
| 2 | Thẻ dịch vụ | Tab "Thẻ dịch vụ" | POST `GetListTheDaMua`, `GetDanhSachTheDichVuPrivate`, `GetDetailTheDichVuPrivate/{id}`, `GetDetaiTheDichVuKhachHanglByIdPrivate` | — | public (login) | Tab thẻ | Danh sách thẻ đã mua / danh mục / chi tiết theo khách. | qua UserService | Tên endpoint có typo "Detai...Hangl" (giữ nguyên để FE không vỡ) |
| 3 | Lịch hẹn (xem/đặt/hủy) | "Đặt lịch" / "Lịch sử" / "Hủy" | POST `GetLichSuLichHen`, `GetChiTietLichSuLichHen`, `TaoLich`, `GetListKhungGioInDay`, `HuyLich` | — | public (login) | Màn đặt lịch | Xem lịch sử/chi tiết; `TaoLich` tạo lịch hẹn (→ Booking); `GetListKhungGioInDay` lấy khung giờ trống theo ngày; `HuyLich` đổi trạng thái hủy. | qua UserService → Booking | WRITE: ripple sang Booking |
| 4 | Chi nhánh | Chọn chi nhánh | GET `GetListChiNhanh`, `GetListTinhThanhChiNhanhPrivate`, `GetChiTietChiNhanhPrivate/{idDonVi}`; POST `GetListChiNhanhPhanLoai` | — | public (login) | Chọn chi nhánh | Danh sách chi nhánh / theo tỉnh thành / phân loại / chi tiết. | qua UserService → Core/ChiNhanh | `GetChiTietChiNhanhPrivate` không truyền CurrentCustomer (chỉ idDonVi) |
| 5 | Danh mục dịch vụ | Tab "Dịch vụ" | POST `GetDanhSachDichVuPrivate`, `GetDichVuByChiNhanh_Private`, `GetListDMDichVu`, `GetDetailDichVuPrivate/{id}` | — | public (login) | Tab dịch vụ | Danh mục/dịch vụ theo chi nhánh/chi tiết, scope khách. | qua UserService | — |
| 6 | Danh mục sản phẩm | Tab "Sản phẩm" | POST `GetListDMSanPham`, `GetDanhSachSanPhamPrivate`, `GetDetailSanPhamPrivate/{id}` | — | public (login) | Tab sản phẩm | Danh mục/danh sách/chi tiết sản phẩm. | qua UserService → WarehouseProduct | — |
| 7 | Đơn hàng & lịch sử dùng DV | Tab "Đơn hàng" | POST `GetDonHangByKhachHangPrivate`, `GetDetaiDonHanglByKhachHangPrivate`, `GetLichSuSuDungDichVuPrivate`, `TaoDonNhap`, `GetOrderByMaThanhToan/{code}/{idDonVi}`, `GetAllTempOrders` | — | public (login) | Tab đơn hàng | Đơn hàng/chi tiết/lịch sử dùng DV; `TaoDonNhap` tạo đơn (→ Order); `GetAllTempOrders` ép IdKhach/SĐT = CurrentCustomer (an toàn scope). | qua UserService → Order | WRITE TaoDonNhap; tên endpoint typo "Detai...Hangl" |
| 8 | Thông tin khách hàng | Tab "Cá nhân" | GET `GetInfo`; POST `GetLichSuDiemThuongByKhachHangPrivate`, `GetDanhSachGhiNoByKhachHangPrivate`, `GetDanhSachKhuyenMaiByKhachHangPrivate`, `GetDanhSachImageByKhachHangPrivate`, `GetDanhSachHopDongByKhachHangPrivate` | — | public (login) | Tab cá nhân | Thông tin khách + lịch sử điểm thưởng/ghi nợ/khuyến mãi/ảnh/hợp đồng, scope CurrentCustomer. | qua UserService → Customer/Promotion | — |

## Endpoint nội bộ (private / service-to-service)
- **POST `GetByIdKhach/{idKhachHang}`** — `[SBAuthorize]` (token **nhân viên**, không phải khách): back-office lấy tài khoản khách theo IdKhachHang. Là endpoint DUY NHẤT trong controller dùng `[SBAuthorize]` thay vì `[SBAuthorizeCustomer]`.

## Quy tắc nghiệp vụ (scope theo khách)
1. Mọi endpoint `[SBAuthorizeCustomer]` chạy `CurrentCustomer` (resolve từ JWT khách trong authorize filter); service tự lọc dữ liệu theo IdKhachHang/IdDonVi của khách.
2. Endpoint validate `ModelState` đầu method → sai → trả `MESS_Model_UnValidate`/`CODE_Model_UnValidate`.
3. Một số endpoint map `LayTatCaPrivateWithoutIdDonViFilterDto → LayTatCaPrivateFilterDto` (AutoMapper) để chèn IdDonVi server-side (chống client tự đặt IdDonVi).
4. `GetAllTempOrders` ghi đè `IdKhach`/`SoDienThoai` bằng CurrentCustomer → khách không xem được đơn của người khác.

## Quan hệ
- **Gọi sang:** `IUserService` (orchestrator) → cross-service Order / Booking / Core(ChiNhanh) / Promotion / WarehouseProduct / Customer.
- **Dữ liệu (entities):** `User` (tài khoản khách); dữ liệu nghiệp vụ (đơn/lịch/combo/thẻ...) lấy qua service ngoài.
- **Ripple / đồng bộ:** `TaoLich`/`HuyLich` → Booking; `TaoDonNhap` → Order.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserController.cs:223-227` — `GetChiTietChiNhanhPrivate(idDonVi)` không truyền `CurrentCustomer` xuống service → không scope theo khách (kiểm tra có lộ chi nhánh đơn vị khác không).
- `UserController.cs:104-112,343-352` — tên endpoint có typo (`GetDetaiTheDichVuKhachHanglByIdPrivate`, `GetDetaiDonHanglByKhachHangPrivate`) — giữ nguyên do FE đã bind; đừng "sửa chính tả" route.
- Toàn bộ logic/edge-case nằm ở `UserService`; controller chỉ validate ModelState + forward → trace sâu cần đọc UserService.
- `GetByIdKhach` dùng `[SBAuthorize]` (nhân viên) lẫn trong controller toàn `[SBAuthorizeCustomer]` (khách) — phân quyền 2 loại token khác nhau trong cùng controller, dễ nhầm khi bảo trì.
