---
controller: HomeDisplayConfigController
module: website
service: Website
man_hinh_ui: "Website > Cấu hình hiển thị trang chủ — list các 'dòng' (component) hiển thị trang chủ theo STT/loại, nút Thêm mới + Sửa/Xóa per-row + popup cấu hình (loại component, ngành hàng, chọn sản phẩm thủ công)"
muc_dich: "CRUD cấu hình các dòng/component hiển thị trên trang chủ website/app: loại component (EnumComponentDong), thứ tự, ngành hàng (DM_NganhHang), chọn sản phẩm thủ công hay tự động, trạng thái sử dụng; cấp dữ liệu trang chủ qua endpoint private cho website."
alias: ["cấu hình trang chủ", "home display config", "component dòng", "cấu hình hiển thị", "hiển thị trang chủ"]
auth: public
state_machine: []
deprecated: false
entities: [HomeDisplayConfig, DM_NganhHang, LinkWebsite]
stored_procs: []
depends: [Website, WarehouseProduct (SanPham/LayTatCaSpCha_Private)]
cross_module_in: [Website (DanhSachCauHinhHienThiTrangChu_Private — render trang chủ)]
cross_module_out: [WarehouseProduct (gọi SanPham/LayTatCaSpCha_Private để lấy sản phẩm theo ngành hàng), CommonService cache HomeDisplayConfigDv]
---

# HomeDisplayConfigController — Cấu hình hiển thị trang chủ

> Controller `partial` (`public partial class HomeDisplayConfigController`) — có thể còn endpoint ở file partial khác. `[TienIchAttribute("HomeDisplayConfigController", "Quản lý domain", MaTienIchSCRM.TI_Website)]` (label TienIch "Quản lý domain" không khớp chức năng thực — xem lưu ý). Tất cả endpoint dùng `[SBAuthorize]` rỗng (chỉ cần đăng nhập, không gắn permission key riêng). Lọc theo `IdDonVi`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (list) | POST `api/HomeDisplayConfig/TimTheoDieuKien` | `[SBAuthorize]` rỗng | public | luôn | List config theo đơn vị, sort Stt; filter IsSuDung/IsChonThuCong/Type; gán TypeName (enum), tên+slug ngành hàng nếu Type=SanPham; KeyWord lọc in-memory theo TenDmNganhHang/TypeName | EF | total đếm TRƯỚC khi áp filter IsSuDung/Type → tổng phân trang sai khi có filter (HomeDisplayConfigService.cs:98-103) |
| 2 | Thêm mới | "Thêm mới" → popup | POST `api/HomeDisplayConfig/ThemMoi` | `[SBAuthorize]` rỗng | public | luôn | Set default IsSuDung=false; nếu Type=SanPham và có LstSanPham → serialize vào ProductList; insert trong transaction + execution strategy | EF | validation (ValidationThemMoi/slug) bị comment toàn bộ → KHÔNG validate input (HomeDisplayConfigService.cs:143-151) |
| 3 | Sửa / mở chi tiết | "Sửa" (icon edit, per-row) | GET `api/HomeDisplayConfig/TimTheoId/{id:Guid}` + POST `CapNhatThongTin` | `[SBAuthorize]` rỗng | public | per-row | TimTheoId trả DTO + deserialize LstSanPham từ ProductList; CapNhatThongTin chỉ validate Id + (nếu IsChonThuCong=true) gọi ValidateSanPhamList (stt ≥0, không trùng stt, NgayBatDau ≤ NgayHetHan) → cập nhật trong transaction | EF | TimTheoId không null-check record (NRE nếu id không tồn tại/sai đơn vị, HomeDisplayConfigService.cs:373-376); validation CapNhat (ValidationCapNhatThongTin) cũng bị comment |
| 4 | Xóa | "Xóa" (icon delete, per-row) | POST `api/HomeDisplayConfig/XoaTheoId/{id:Guid}` | `[SBAuthorize]` rỗng | public | per-row | Check tồn tại + đúng IdDonVi → DeleteAsync trong transaction | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Website (render trang chủ)** gọi service `DanhSachCauHinhHienThiTrangChu_Private(IdDonVi)` → lấy LinkWebsite của đơn vị, đọc HomeDisplayConfig (qua cache `CommonService.LayHomeConfigDisplay`), với mỗi dòng Type=SanPham gọi sang **WarehouseProduct** `api/SanPham/LayTatCaSpCha_Private` (top 4 sản phẩm cha theo ngành hàng hoặc theo list id cấu hình thủ công) để fill `SanPhamChas`. Lưu ý: method này nằm ở service nhưng KHÔNG thấy endpoint public ở controller này → expose qua controller partial khác hoặc caller nội bộ.

## Quy tắc nghiệp vụ (gating)
- `IsChonThuCong == true` → bắt buộc qua `ValidateSanPhamList`: list không rỗng + có ít nhất 1 IdSanPham; mọi Stt ≥ 0; Stt không trùng; mỗi sp NgayBatDau ≤ NgayHetHan. Sai → trả lỗi, không lưu. (Chỉ áp ở `CapNhatThongTin`; `ThemMoi` KHÔNG gọi validate này.)
- `Type == EnumComponentDong.SanPham` → serialize LstSanPham vào cột `ProductList` (JSON). Type khác → todo, chưa set field riêng.

## Quan hệ
- **Gọi sang:** WarehouseProduct (`SanPham/LayTatCaSpCha_Private`) qua `ClientHelper`; `CommonService` (cache home config + ngành hàng).
- **Dữ liệu (entities):** `HomeDisplayConfig` (Stt, Type, ProductList[json], IsChonThuCong, IsSuDung, IdDmNganhHang, IdDonVi, IdChiNhanh), `DM_NganhHang` (Ten, Slug), `LinkWebsite` (IdChiNhanhKhoDefault).
- **Ripple / đồng bộ:** đổi cấu hình → ảnh hưởng trang chủ website/app; cache redis `HomeDisplayConfigDv` (CommonService quản, có Luu/Xoa redis nhưng CRUD ở đây KHÔNG tự xóa cache → có thể stale, xem lưu ý).

## Lỗi đã biết / lưu ý kỹ thuật
- `HomeDisplayConfigService.cs:98-103` — `total` được tính trước khi áp các filter `IsSuDung`/`IsChonThuCong`/`Type`; KeyWord lại lọc in-memory sau phân trang → tổng số bản ghi và dữ liệu trang không nhất quán khi có điều kiện lọc.
- `HomeDisplayConfigService.cs:143-151, 243-248` — toàn bộ validation Thêm/Cập nhật bị comment; input gần như không được kiểm tra trừ ValidateSanPhamList khi IsChonThuCong=true.
- `HomeDisplayConfigService.cs:366-389` (`TimTheoId`) — gọi `homeDisplayConfig.Id` ngay sau FirstOrDefaultAsync không null-check → NullReferenceException nếu id không tồn tại hoặc khác đơn vị.
- CRUD KHÔNG gọi `CommonService.XoaRedisHomeConfigDisplay` → cache trang chủ có thể cũ sau khi sửa cấu hình; cần clear cache thủ công.
- `[TienIchAttribute(... "Quản lý domain" ...)]` label không khớp chức năng (cấu hình trang chủ, không phải domain).
- Auth toàn bộ `[SBAuthorize]` rỗng → chỉ cần đăng nhập, không phân quyền chi tiết.
