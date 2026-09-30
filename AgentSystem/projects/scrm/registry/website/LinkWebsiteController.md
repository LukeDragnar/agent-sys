---
controller: LinkWebsiteController
module: website
service: Website
man_hinh_ui: "Admin SCRM > Quản lý website/link website — màn cấu hình website bán hàng của đơn vị: list + thêm/sửa/xóa link website, cấu hình kho / thanh toán / liên hệ / marketing theo từng tab"
muc_dich: "API quản lý cấu hình LinkWebsite (website bán hàng) của đơn vị: CRUD link, kiểm tra trùng full-domain, và cập nhật từng nhóm cấu hình (kho, thanh toán, liên hệ, marketing). Cung cấp endpoint private cho service khác lấy đơn hàng/link theo đơn vị."
alias: ["link website", "cấu hình website", "quản lý website bán hàng", "website đơn vị", "LinkWebsite"]
auth: mixed
state_machine: []
deprecated: false
entities: [LinkWebsite]
stored_procs: []
depends: [Website/ILinkWebsiteService, Authen (CurrentUser/DonViId)]
cross_module_in: [Order/ThanhToan (GetOrderByMaThanhToan — private), nội bộ Website (GetLinkWebsiteByUnit_Private)]
cross_module_out: []
---

# LinkWebsiteController — Quản lý link website (cấu hình website bán hàng đơn vị)

> API controller thật (`[ApiController]`, route `api/LinkWebsite`, kế thừa `BaseCoreController`), `partial` — có thể còn endpoint ở file partial khác. `[TienIch("LinkWebsiteController", ..., MaTienIchSCRM.TI_Website)]`. Mọi action ghi/lọc đều scope theo `CurrentUser.DonViId`. Quyền theo permission key `LinkWebsite_*`; 2 endpoint `[PrivateSBAuthorize]` cho service-to-service → `auth: mixed`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | grid list link website | POST `api/LinkWebsite/TimTheoDieuKien` | `LinkWebsite_TimTheoDieuKien` | internal | luôn | List link website theo filter (scope đơn vị user) | EF | - |
| 2 | Lấy link theo user đăng nhập | (load cấu hình website của user) | GET `api/LinkWebsite/GetLinkWebsiteByUserLogin` | `LinkWebsite_GetLinkWebsiteByUserLogin` | internal | - | Trả link website gắn với user hiện tại | EF | - |
| 3 | Thêm mới | "Thêm mới" | POST `api/LinkWebsite/ThemMoi` | `LinkWebsite_ThemMoi` | internal | - | Tạo link website mới cho đơn vị | EF | quyền chỉ `DV-AD-TONG` |
| 4 | Tìm theo id | mở chi tiết/sửa | GET `api/LinkWebsite/TimTheoId/{id}` | `LinkWebsite_TimTheoId` | internal | per-row | Trả LinkWebsiteDto **chỉ khi `IdDonVi == CurrentUser.DonViId`**, ngược lại báo "Không tìm thấy nhà cung cấp!" | EF | message lỗi ghi "nhà cung cấp" (copy nhầm) cho entity LinkWebsite |
| 5 | Cập nhật thông tin | "Lưu" | POST `api/LinkWebsite/CapNhatThongTin` | `LinkWebsite_CapNhatThongTin` | internal | per-row | Cập nhật link website | EF | quyền `DV-AD-TONG` |
| 6 | Xóa | "Xóa" | POST `api/LinkWebsite/Xoa/{id}` | `LinkWebsite_Xoa` | internal | per-row | Check tồn tại + `IdDonVi==DonViId` rồi xóa; sai đơn vị → MESS_NotFound | EF | - |
| 7 | Kiểm tra trùng full domain | (lúc nhập domain) | POST `api/LinkWebsite/CheckTonTaiFullDomain` | `LinkWebsite_CheckTonTaiFullDomain` | internal | - | Trả full domain đã tồn tại hay chưa (validate trước khi lưu) | EF | - |
| 8 | Cập nhật cấu hình KHO | tab "Kho" | POST `ThemMoiKhoAsync` · `CapNhatThongTinKho` | `LinkWebsite_ThemMoiKhoAsync` / `LinkWebsite_CapNhatThongTinKho` | internal | - | Lưu/cập nhật cấu hình kho (chi nhánh kho default, IsCheckKho) cho link website | EF | quyền `DV-AD-TONG` |
| 9 | Cập nhật cấu hình THANH TOÁN | tab "Thanh toán" | POST `ThemMoiThanhToanAsync` · `CapNhatThanhToan` | `LinkWebsite_ThemMoiThanhToanAsync` / `LinkWebsite_CapNhatThanhToan` | internal | - | Lưu/cập nhật cấu hình thanh toán (TK ngân hàng hệ thống, hình thức chuyển khoản) | EF | quyền `DV-AD-TONG` |
| 10 | Cập nhật cấu hình LIÊN HỆ | tab "Liên hệ" | POST `ThemMoiLienHeAsync` · `CapNhatLienHeAsync` | `LinkWebsite_ThemMoiLienHeAsync` / `LinkWebsite_CapNhatLienHeAsync` | internal | - | Lưu/cập nhật thông tin liên hệ hiển thị trên website | EF | quyền `DV-AD-TONG` |
| 11 | Cập nhật cấu hình MARKETING | tab "Marketing" | POST `ThemMoiMarketingAsync` · `CapNhatMarketingAsync` | `LinkWebsite_ThemMoiMarketingAsync` / `LinkWebsite_CapNhatMarketingAsync` | internal | - | Lưu/cập nhật cấu hình marketing | EF | quyền `DV-AD-TONG` |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **Order/Thanh toán** gọi `GetOrderByMaThanhToan/{code}/{idDonVi}` → lấy đơn hàng theo mã thanh toán + đơn vị (phục vụ callback/đối soát thanh toán website).
- [internal] Service khác gọi `GetLinkWebsiteByUnit_Private/{idDonVi}` → lấy cấu hình link website theo đơn vị (storefront `GetDataByDomain` và các service web resolve cấu hình theo đơn vị).

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService` (toàn bộ CRUD + cấu hình); dùng `CurrentUser.DonViId` để scope.
- **Dữ liệu (entities):** `LinkWebsite` (Domain/FullDomain, IdDonVi, IdChiNhanh, IdTheme, IdChiNhanhKhoDefault, IsCheckKho, IdTkNganHangHeThong, IsSuDungHinhThucChuyenKhoan, ThongTinLienHe, Footer*Content, Logo/Favicon...).
- **Ripple / đồng bộ:** cấu hình link website là nguồn cho **toàn bộ storefront** (4 controller MVC ở module này resolve đơn vị/theme/chi nhánh kho qua đây). Đổi domain/theme/cấu hình kho → ảnh hưởng trực tiếp website public của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoId` chặn cross-tenant đúng (so `IdDonVi == CurrentUser.DonViId`) nhưng trả message "Không tìm thấy **nhà cung cấp**!" (LinkWebsiteController.cs:54) — text sai entity, dễ gây nhầm khi debug.
- Controller `partial` → kiểm tra file partial khác nếu thiếu endpoint.
- `GetDataByDomain` (endpoint public theo domain) đã bị comment out (LinkWebsiteController.cs:73-79) — chức năng đó nay nằm trong `ILinkWebsiteService.GetDataByDomain` gọi nội bộ từ các MVC controller, không expose HTTP.
- Phân quyền không đồng nhất: action đọc mở cho nhiều role DV-*, nhưng mọi action ghi/cấu hình chỉ `DV-AD-TONG`.
