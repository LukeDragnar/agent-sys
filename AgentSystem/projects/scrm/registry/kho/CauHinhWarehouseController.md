---
controller: CauHinhController
module: kho
service: WarehouseProduct
man_hinh_ui: "Cấu hình kho (settings màn Quản lý kho/sản phẩm) — các tab: cấu hình chung (ẩn giá nhập, kiểm tra tồn qua API), cấu hình QLSP, giới hạn tồn kho, đơn đặt hàng (kiểm duyệt + người duyệt); + màn SuperAdmin (HT-AD/HT-CSKH) bật/tắt search bằng SQL/ElasticSearch."
muc_dich: "Đọc/lưu cấu hình service kho theo đơn vị (lưu JSON trong 1 record CauHinh per IdDonVi): ẩn giá nhập, kiểm tra tồn qua API, giới hạn tồn kho, QLSP, kho tổng, đơn đặt hàng (kiểm duyệt + chi nhánh được phép tạo) + cấu hình SuperAdmin (search SQL/ES)."
alias: ["cấu hình kho", "cài đặt kho", "ẩn giá nhập", "giới hạn tồn kho", "cấu hình QLSP", "cấu hình quản lý sản phẩm", "kiểm tra tồn qua api", "cấu hình đơn đặt hàng", "người duyệt đơn đặt hàng", "cấu hình superadmin warehouse", "search bằng SQL", "kho tổng"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [WarehouseProduct, Core/DonVi, Redis]
cross_module_in: [OrderLogistic]
cross_module_out: [WarehouseProduct (KhoChiTiet/SanPham search routing), OrderLogistic (kho tổng / đơn đặt hàng)]
---

# CauHinhController — Cấu hình service kho (WarehouseProduct)

> Lưu trữ: 1 record `CauHinh` per `IdDonVi`; setting để trong 2 cột JSON — `JsonSetting` (cấu hình đơn vị) và `JsonSetting_SuperAdmin` (cấu hình SuperAdmin). Đọc qua Redis cache (key `CK_CauHinhWarehouseProductSetting_{idDonVi}`, TTL 1 ngày); mọi Save đều clear cache. User đơn vị không có record riêng → fallback đọc record `IdDonVi = Guid.Empty`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình chung (init) | "Lưu" tab cấu hình chung (ẩn giá nhập, kiểm tra tồn qua API...) | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_Warehouse_LuuCauHinhKhachHang` | public | - | Merge `CauHinhSettingInitDto` vào JsonSetting đơn vị. Validate: nếu `IsKiemTraTonKhoQuaApi=true` thì `ApiUrlCheckTonKho` bắt buộc, rỗng → lỗi "Hãy điền api url". Ghi LichSuTacDong (ADD/UPDATE), clear cache | EF (CauHinh.JsonSetting) | Permission key tên `...KhachHang` nhưng thực chất là cấu hình chung kho — đặt tên lệch nghĩa |
| 2 | Lấy cấu hình chung (init) | (load tab cấu hình chung) | GET `api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` (mở) | public | - | Map JsonSetting → `CauHinhSettingInitDto` (theo IdDonVi user, fallback Guid.Empty) | EF | - |
| 3 | Lấy toàn bộ cấu hình | (load màn cấu hình) | GET `api/CauHinh/Get` | `[SBAuthorize]` (mở) | public | - | Trả `CauHinhSettingDto` đầy đủ của đơn vị (qua cache) | EF | Permission key bị comment, chỉ còn `[SBAuthorize]` rỗng |
| 4 | Lưu cấu hình QLSP | "Lưu" tab Quản lý sản phẩm | POST `api/CauHinh/SaveCauHinhQLSPInit` | `[SBAuthorize()]` (mở) | public | - | Merge `CauHinhSettingQLSPInitDto` vào JsonSetting đơn vị; log + clear cache | EF | Permission key `CauHinh_Warehouse_LuuCauHinhQLSP` bị comment → quyền rỗng |
| 5 | Lấy cấu hình QLSP | (load tab QLSP) | GET `api/CauHinh/GetCauHinhQLSPInit` | `[SBAuthorize()]` (mở) | public | - | Map JsonSetting → `CauHinhSettingQLSPInitDto` | EF | - |
| 6 | Lưu cấu hình giới hạn tồn kho | "Lưu" tab Giới hạn tồn kho | POST `api/CauHinh/SaveCauHinhGioiHanTonKho` | `CauHinh_Warehouse_SaveCauHinhGioiHanTonKho` | public | - | Merge `CauHinhSettingGioiHanTonKhoDto` vào JsonSetting; log + clear cache | EF | - |
| 7 | Lấy cấu hình giới hạn tồn kho | (load tab giới hạn tồn) | GET `api/CauHinh/GetCauHinhGioiHanTonKho` | `[SBAuthorize()]` (mở) | public | - | Map JsonSetting → `CauHinhSettingGioiHanTonKhoDto` | EF | - |
| 8 | Lưu cấu hình đơn đặt hàng | "Lưu" tab Đơn đặt hàng (kiểm duyệt + người duyệt) | POST `api/CauHinh/SaveCauHinhDonDatHang` | `[SBAuthorize()]` (mở) | public | - | Set `CanKiemDuyet` + `DanhSachNguoiDungDonDatHang` vào JsonSetting; clear cache (KHÔNG dùng Save chung — ghi field riêng) | EF | Không ghi LichSuTacDong (khác các Save còn lại) |
| 9 | Lấy cấu hình đơn đặt hàng | (load tab đơn đặt hàng) | GET `api/CauHinh/GetCauHinhDonDatHang` | `[SBAuthorize]` (mở) | public | - | Trả `CanKiemDuyet` + danh sách người dùng đơn đặt hàng | EF | - |
| 10 | Lấy người được duyệt theo chi nhánh | (dropdown người duyệt khi tạo đơn) | GET `api/CauHinh/LayDanhSachNguoiDuyetTheoChiNhanh?idChiNhanh=` | `[SBAuthorize]` (mở) | public | - | Lọc người có `CoQuyenDuyet`; `idChiNhanh` null/Empty → tất cả chi nhánh, ngược lại lọc theo `IdChiNhanh` (xem Quy tắc) | EF | - |
| 11 | Lấy chi nhánh được phép tạo đơn | (load màn tạo đơn đặt hàng) | GET `api/CauHinh/LayDanhSachChiNhanhDuocPhepTao` | `[SBAuthorize]` (mở) | public | - | Trả `KhongGioiHan` + `DanhSachIdChiNhanh` theo role + cấu hình (xem Quy tắc) | EF | - |
| 12 | Lưu cấu hình SuperAdmin (theo đơn vị, từ màn đơn vị) | "Lưu" màn cấu hình SuperAdmin per đơn vị | POST `api/CauHinh/SaveCauHinhSuperAdminManCauHinhDonVi` | `CauHinh_Warehouse_SaveCauHinhSuperAdminManCauHinhDonVi` | public (`HT-AD,HT-CSKH`) | - | Merge `...ManCauHinhDonViDto` vào JsonSetting_SuperAdmin. Validate `KhoangThoiGianDuocBaoCaoXemSoLuongBanSanPham > 0` | EF (JsonSetting_SuperAdmin) | - |
| 13 | Lấy cấu hình SuperAdmin (màn đơn vị) | (load màn cấu hình SuperAdmin đơn vị) | POST `api/CauHinh/GetCauHinhSuperAdminManCauHinhDonVi` | `[SBAuthorize()]` (mở) | public | - | Map JsonSetting_SuperAdmin → `...ManCauHinhDonViDto` (null → DTO rỗng) | EF | - |
| 14 | Lưu cấu hình SuperAdmin (toàn hệ thống) | "Lưu" màn SuperAdmin (apply 1 đơn vị / nhiều / tất cả) | POST `api/CauHinh/Save_SuperAdmin` | `CauHinh_Warehouse_LuuCauHinhWareHouseSuperAdmin` | public (`HT-AD,HT-CSKH`) | chỉ HT-AD/HT-CSKH | Apply `CauHinhSettingSuperAdminDto` cho `ListIdDonVi` (hoặc tất cả nếu `IsApplyForAll`); insert nếu chưa có / spread-merge nếu đã có; transaction + clear cache mỗi đơn vị | EF (batch) | `IsApplyForAll=true` gọi Core `DonVi/GetAllIdDonVi` rồi update toàn bộ — thao tác diện rộng |
| 15 | Lấy cấu hình SuperAdmin theo đơn vị | (màn SuperAdmin chọn đơn vị) | POST `api/CauHinh/GetCauHinhSuperAdmin` | `[SBAuthorize]` (mở) | public | - | Trả `CauHinhSettingSuperAdminDto` theo `payload.IdDonVi` (null → default) | EF | Permission key bị comment, chỉ còn `[SBAuthorize]` rỗng |

## Endpoint nội bộ (private / service-to-service)
- [internal] `OrderLogistic` gọi POST `api/CauHinh/CapNhatSuDungKhoTongWarehouse` (`[PrivateSBAuthorize]`) khi bật/tắt kho tổng → set `IsSuDungKhoTong`, `IdChiNhanhTong`, `ListUserChiNhanhTong`, `ChoPhepKhoTongTaoDonChiNhanh` vào JsonSetting đơn vị; clear cache. (KHÔNG ghi LichSuTacDong.)
- [internal] GET `api/CauHinh/GetAllCauHinhSuperAdmin` (`[PrivateSBAuthorize]`) — trả tất cả record có `JsonSetting_SuperAdmin` (IdDonVi + JsonSetting_SuperAdmin) cho service khác đọc routing search.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
1. **Kiểm tra tồn qua API** (`SaveCauHinhInit`): `IsKiemTraTonKhoQuaApi == true` → bắt buộc có `ApiUrlCheckTonKho`; rỗng → chặn lưu "Hãy điền api url". `false` → bỏ qua check URL. (Field: `IsKiemTraTonKhoQuaApi`, `ApiUrlCheckTonKho`.)
2. **Người được duyệt theo chi nhánh** (`LayDanhSachNguoiDuyetTheoChiNhanh`): lọc `CoQuyenDuyet == true`; nếu `idChiNhanh == null || Guid.Empty` → trả tất cả người có quyền duyệt ở mọi chi nhánh; ngược lại chỉ những người có `IdChiNhanh == idChiNhanh`. (Field: `DanhSachNguoiDungDonDatHang[].CoQuyenDuyet`, `.IdChiNhanh`.)
3. **Chi nhánh được phép tạo đơn** (`LayDanhSachChiNhanhDuocPhepTao`) — thứ tự rẽ nhánh:
   - **Nhánh A (không giới hạn):** user là `IsSuperUser || IsCSKH || IsAdmin || IsAdminNhieuChiNhanh()` → `KhongGioiHan = true` (thấy tất cả chi nhánh). Bỏ qua cấu hình.
   - **Nhánh B (user thường, chưa cấu hình danh sách):** `DanhSachNguoiDungDonDatHang` rỗng → `KhongGioiHan = false`, `DanhSachIdChiNhanh` = chỉ chi nhánh user thuộc về (`userLogin.IdChiNhanh`, rỗng nếu `Guid.Empty`).
   - **Nhánh C (có cấu hình, user KHÔNG trong danh sách):** → `KhongGioiHan = false`, `DanhSachIdChiNhanh = []` (không được tạo ở chi nhánh nào).
   - **Nhánh D (có cấu hình, user trong danh sách):** → `DanhSachIdChiNhanh` = các `IdChiNhanh` mà bản ghi của user có `CoQuyenTao == true`.
4. **Apply SuperAdmin diện rộng** (`Save_SuperAdmin`): `IsApplyForAll == true` → gọi Core `GetAllIdDonVi`, apply cho TẤT CẢ đơn vị; `false` → chỉ `ListIdDonVi` (rỗng + không ApplyForAll → lỗi "IdDonVi không được để trống"). Mỗi đơn vị: chưa có record → insert mới; đã có → `ObjectHelper.CombineObjects` spread-merge giữ cấu hình cũ. (Field: `IsApplyForAll`, `ListIdDonVi`, `CauHinhSettingSuperAdminDto`.)
5. **Ngưỡng báo cáo SuperAdmin** (`SaveCauHinhSuperAdminManCauHinhDonVi`): `KhoangThoiGianDuocBaoCaoXemSoLuongBanSanPham <= 0` → chặn lưu. Phải `> 0`.
6. **Cờ routing search (SuperAdmin)**: `Allows_Search_SanPham_By_SQL`, `Allows_Return_SanPham_Results_ElasticSearch_From_SQL`, `Allows_Search_KhoChiTiet_By_SQL`, `Allows_Use_KhoChiTietSync` — default insert: 3 cờ SQL = false, `Allows_Use_KhoChiTietSync = true`. Các service search SanPham/KhoChiTiet đọc cờ này để chọn nguồn SQL vs ElasticSearch.

## Quan hệ
- **Gọi sang:** Core `api/DonVi/GetAllIdDonVi` (chỉ khi `Save_SuperAdmin` với `IsApplyForAll=true`).
- **Dữ liệu (entities):** `CauHinh` (1 record/IdDonVi; 2 cột JSON `JsonSetting` + `JsonSetting_SuperAdmin`). Không có entity con — toàn bộ setting nằm trong JSON.
- **Ripple / đồng bộ:** clear Redis `CK_CauHinhWarehouseProductSetting_{idDonVi}` sau mọi Save. Cờ SuperAdmin ảnh hưởng routing search SanPham/KhoChiTiet (SQL vs ElasticSearch) ở các service khác. Kho tổng / đơn đặt hàng ảnh hưởng OrderLogistic.

## Lỗi đã biết / lưu ý kỹ thuật
- **Quyền không đồng nhất**: nhiều endpoint cấu hình dùng `[SBAuthorize()]`/`[SBAuthorize]` rỗng (Get, GetCauHinhInit, SaveCauHinhQLSPInit, GetCauHinhQLSPInit, GetCauHinhGioiHanTonKho, SaveCauHinhDonDatHang, GetCauHinhSuperAdmin, GetCauHinhSuperAdminManCauHinhDonVi) — chỉ cần đăng nhập là gọi/lưu được. Nhiều permission key bị comment (`CauHinh_Warehouse_LayCauHinhKho`, `CauHinh_Warehouse_LuuCauHinhQLSP`, `CauHinh_Warehouse_GetCauHinhSuperAdmin`). Cần xác nhận có chủ ý — đặc biệt `SaveCauHinhQLSPInit`/`SaveCauHinhDonDatHang` là ghi cấu hình mà không có permission key riêng. (Controller-level `[SBAuthorize("CauHinhController")]` cũng đang comment — CauHinhController.cs:15.)
- **Tên permission key lệch nghĩa**: `SaveCauHinhInit` dùng key `CauHinh_Warehouse_LuuCauHinhKhachHang` ("khách hàng") nhưng lưu cấu hình chung kho (CauHinhController.cs:26).
- **`SaveCauHinhDonDatHang` ghi field riêng, không qua `Save<T>` chung** (CauHinhService.cs:735) → KHÔNG ghi `LichSuTacDong`; `CapNhatSuDungKhoTongWarehouse` cũng không log. Các Save khác đều log ADD/UPDATE. Lưu ý khi truy vết lịch sử thay đổi cấu hình.
- **Logic IsSuperUser/HT-AD dùng IdDonVi=Guid.Empty đã bị comment toàn bộ** trong `GetAsync`/`GetCauHinhInit`/`GetCauHinhQLSPInit`/`Save<T>` (CauHinhService.cs:85-88, 286-289, 308-312, 330-334) → hiện super/HT-AD vẫn đọc/ghi theo `userLogin.DonViId` của họ, KHÔNG ép về Guid.Empty như docstring interface mô tả. Docstring (CauHinhService.cs:24-26) lệch với hành vi thực.
- **Save_SuperAdmin nuốt lỗi cache-key & exception**: trong transaction bắt exception set `isSucces=false` rồi rollback; ngoài ra cả method bọc try-catch trả message lỗi. Khi `IsApplyForAll=true` update diện rộng toàn bộ đơn vị — thao tác nặng, cân nhắc khi trace.
