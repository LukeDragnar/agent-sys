# Dev — Nhận xét & đánh giá dịch vụ

> Tài liệu kỹ thuật. Source: BE `D:\PROJECT\Git\_SourceForCode\Group2\scrm`, FE `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`.

## Màn hình & route (FE — module Customer)
Routing: `src/app/pages/Customer/customer-routing.module.ts`

| Màn hình | Route | Component |
|---|---|---|
| Khách chấm điểm (Phản hồi dịch vụ) | `danh-gia-khach-hang` *(mở qua link, có param `maDanhGia`)* | `DanhgiaKhachhangComponent` (`danhgia-khachhang/`) |
| Danh sách phản hồi (shop xem/xử lý) | `phan-hoi-dich-vu` | `PhanHoiDichVuComponent` (`phan-hoi-dich-vu/`) |
| Quản lý danh mục đánh giá — theo đơn vị | `danh-gia-chi-tiet-unit` | `DanhgiaChitietUnitComponent` (`danhgia-chitiet-unit/`, có `-init` + `-edit`) |
| Quản lý danh mục đánh giá — mẫu SA (superadmin/ngành nghề) | `danh-gia-chi-tiet-sa` | `DanhgiaChitietSAComponent` (`danhgia-chitiet-sa/`) |
| Đánh giá thành công | (component) | `danhgia-thanhcong/` |

FE service: `src/app/services/customer/khachHang-danhGia.service.ts` (`KhachHangDanhGiaService extends BaseService`, base = `SERVICE_API_CUSTOMER + api/KhachHangDanhGia`; các API SA gọi `api/SanboxArea/KhachHangDanhGia/...`).

## Controller (BE — Customer service)
- `Customer/Customer.API/Controllers/KhachHangDanhGiaController.cs` — `[Route("api/[controller]")]`, `[SBAuthorize]`, kế thừa `BaseDanhMucController<KhachHangDanhGia, KhachHangDanhGiaDto>`.
  - `POST InsertDanhGia` — sinh ShortLink đánh giá (loại `DonHangOffline`), trả `TxtShortLink`.
  - `GET GetThongTinDonHangByMa/{maDanhGia}` — `[AllowAnonymous]`, khách mở link xem đơn.
  - `POST GetThongTinDonHangIdDonHang` — `[AllowAnonymous]`, tạo bản ghi đánh giá nếu chưa có + trả chi tiết đơn.
  - `POST SaveDanhGiaKhachHang` — `[AllowAnonymous]`, khách lưu đánh giá.
  - `GET IsHaveEvaluated/{maDanhGia}` — `[AllowAnonymous]`.
  - `POST GetListPhanHoiDichVu` — **trace** — quyền `KhachHangDanhGia_GetListPhanHoiDichVu` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV).
  - `POST ExportExcelPhanHoiDichVu` — quyền `KhachHangDanhGia_ExportExcelPhanHoiDichVu`.
  - `POST UpdatePhanHoiDichVu` — chỉ `[SBAuthorize()]` (đăng nhập; permission cụ thể đang comment).
  - Region "For Unit": `TimTheoDieuKienDsDetailForUnit` (**trace** — quyền `..._TimTheoDieuKienDsDetailForUnit`, DV-AD-TONG), `InsertKhDanhGiaDetailForUnit`, `UpdateKhDanhGiaDetailForUnit`, `GetKhDanhGiaChiTietForUnit/{id}`, `DeleteKhDanhGiaChiTietForUnit/{id}`, `InitDataForUnit`, `GetDropDownListTheoKieuLoai` (`[AllowAnonymous]`).
- `Customer/Customer.API/Areas/SandBoxArea/KhachHangDanhGiaController.cs` — `[Route("api/SanboxArea/[controller]")]` — biến thể **ForSA** (quản trị danh mục mẫu theo ngành nghề, quyền `HT-AD`).

## Entities (Customer.DataContext/Entity)
- `KhachHangDanhGia.cs` (bản ghi đánh giá cha): `IdDonVi, IdChiNhanh, KieuLoaiDonHang (EnumTypeDonHang), IdDonHang, MaHoaDon, MaDanhGia, MucDoHaiLong, ShortLinkDanhGia, IdKhachHang, TenChiNhanh, PhanHoiYKien, IsProcessed`.
- `KhachHangDanhGiaDetail.cs` (chi tiết theo dịch vụ–nhân viên): `IdDanhGia, IdDichVu, TenDichVu, MucDoHaiLongChiTiet, IdNhanVien, TenNhanVien, PhanHoiMucDoChiTiet, IdDonVi, IdChiNhanh`.
- `KhachHangDanhGiaChildren.cs` (**danh mục nhận xét nhanh** — bị đặt tên hơi gây nhầm): `IdNganhNghe, IdDonVi, KieuLoai (EnumMucDoHaiLong), TenChiTiet`. `IdDonVi == Guid.Empty` = bản ghi mẫu SA/ngành nghề; khác Empty = của đơn vị cụ thể.
- DTO: `Libs/Models.Core/DTO/Customer.Client/KhachHangDanhGia/KhachHangDanhGiaDto.cs` — chứa `ModelDanhGiaFilterDto : GridInfo` (KeyWord, MucDoHaiLong, TuNgay, DenNgay, IdChiNhanh, IsProcessed), `PhanHoiDichVuUpdateReq`, v.v. Enum `EnumMucDoHaiLong` (Customer.Service/Generated/Enums/EnumKhachHang.cs): **1=RatHaiLong, 2=HaiLong, 3=KhongHaiLong** (chỉ 3 giá trị).

## Luồng code (Service)
File: `Customer/Customer.Service/Generated/KhachHangDanhGiaService.cs` (`KhachHangDanhGiaService : BaseService<KhachHangDanhGia>`).

- **`GetThongTinDonHangIdDonHang(payload)`**: tìm `KhachHangDanhGia` theo (IdDonVi, IdDonHang, KieuLoaiDonHang); nếu chưa có → sinh `MaDanhGia` duy nhất (loop `GenerateUniqueCode` + check tồn tại), gọi `GetThongTinDonHang` lấy mã hóa đơn/id khách (Order hoặc OrderLogistic theo `KieuLoaiDonHang`), `GetChiTietChiNhanhPrivate` lấy tên chi nhánh, insert. Nếu là đơn Order → gọi `OrderService /api/DonHang/GetChiTietDonTheoId/{id}` ghép detail + dropdown nhận xét.
- **`SaveDanhGiaKhachHang(model)`**: resolve đơn qua `ShortLink/GetShortLinkByHashCode/{maDanhGia}` (3 loại đối tượng: MaDanhGia / DonHangOffline / DonHangOnline); set `MucDoHaiLong + PhanHoiYKien`; build list detail (insert mới nếu chưa có, update theo (IdDichVu, IdNhanVien) nếu đã có) — tất cả trong `ExecutionStrategy + transaction`. Sau commit: ghi `KhachHangLichSuTuongTac` (KieuLoai = DanhGia) qua `_serviceLogTuongTacKhachHang`, và `Publish<GuiThongBaoInfo>` (code `MauThongBaoCode.KhachHangDanhGiaDv`, link `/customer/phan-hoi-dich-vu?maDanhGia=...`).
- **`GetListPhanHoiDichVu(model)`** *(trace)*: ép `IdChiNhanh` theo user nếu không phải admin/CSKH/superuser; query `KhachHangDanhGia` theo đơn vị + filter + `MucDoHaiLong != null`; load detail + số điện thoại khách; build object ẩn danh (anonymous type), filter keyword in-memory trên (tên NV / tên DV / mã HĐ), `OrderByDescending(NgayTao)`. Thống kê đếm riêng bằng `GroupBy` (RatHaiLong/HaiLong/KhongHaiLong). Tổng số khách nhận link: POST sang `Booking/ShortLink/TongSoKhachNhanLinkDanhGia`. Paging in-memory `Skip/Take` theo `PageInfo`.
- **`ExportExcelPhanHoiDichVu`**: ép `PageSize = 5000`, gọi lại `GetListPhanHoiDichVu` (giữ filter + quyền), build Excel bằng GemBox (license set sẵn) — bảng tổng hợp 2 hàng + bảng chính.
- **`UpdatePhanHoiDichVu`**: set `IsProcessed`, `MarkDirty`, update.
- **For Unit**: `TimTheoDieuKienDsDetailForUnit` *(trace)* lọc `IdDonVi == userLogin.DonViId` (+ KeyWord/KieuLoai), paging; `InsertKhDanhGiaDetailForUnit` (cần `IsAdmin`, kiểu loại ∈ {1,2,3}, tên bắt buộc, chống trùng tên trong đơn vị); `UpdateKhDanhGiaDetailForUnit` (chỉ sửa `TenChiTiet`, đoạn đổi `KieuLoai` đã comment); `InitDataForUnit` copy bản mẫu `IdDonVi==Guid.Empty` cùng ngành nghề + kiểu loại, `RemoveAll` tên đã có (ToLower) rồi insert. **For SA** y hệt nhưng `IdDonVi == Guid.Empty` và cần `IsSuperUser`.

## Bug-prone / lưu ý kỹ thuật
- **`GetListPhanHoiDichVu` xử lý phần lớn in-memory:** kéo toàn bộ bản ghi của đơn vị về (`ToListAsync()` trước khi paging), filter keyword + paging đều ở memory → **rủi ro performance** khi đơn vị có nhiều đánh giá. Đoạn LINQ-to-DB tối ưu cũ (GroupJoin) đã bị comment.
- **Hai query lệch điều kiện ngày:** query lấy data dùng `NgayTao.Date >= TuNgay.Date / <= DenNgay.Date`; query đếm tổng (`cntDataDetail`) dùng `>= TuNgay.Date` và `< DenNgay.Date.AddDays(1)`. Khác nhau cách chặn biên ngày → đếm và list có thể lệch ở mốc ngày `DenNgay`.
- **Filter keyword áp 2 lần** (DB: `MaHoaDon.Contains`, rồi in-memory: tên NV/DV/mã HĐ) — chú ý khi đổi logic, dễ double-filter.
- **`UpdatePhanHoiDichVu` chỉ `[SBAuthorize()]`** (permission cụ thể đang comment) → ai đăng nhập trong đơn vị cũng cập nhật trạng thái xử lý được; cân nhắc siết quyền.
- **Update danh mục không đổi được KieuLoai** (đã comment ở cả ForUnit/ForSA) — chỉ sửa `TenChiTiet`. Nếu cần đổi kiểu loại phải xóa + tạo lại.
- **Chống trùng MaDanhGia bằng loop + `Task.Delay(100)`** — không có giới hạn vòng lặp, về lý thuyết kẹt nếu va chạm liên tục (thực tế hiếm).
- **`KhachHangDanhGiaChildren` là bảng danh mục nhận xét**, không phải "children của đánh giá" — tên dễ gây hiểu nhầm; chi tiết đánh giá thực tế là `KhachHangDanhGiaDetail`.
- **Phân biệt 3 kênh đối tượng ShortLink** (MaDanhGia / DonHangOffline / DonHangOnline) trong `SaveDanhGiaKhachHang` — sai loại → không resolve được đơn → "Mã đánh giá không tồn tại".
- **Đoạn chặn "đã đánh giá rồi" bị comment** (`MucDoHaiLong > 0` → return) → hiện khách có thể gửi đánh giá lại, ghi đè mức + nhận xét.

## File source
- BE entities: `Customer/Customer.DataContext/Entity/KhachHangDanhGia.cs`, `KhachHangDanhGiaDetail.cs`, `KhachHangDanhGiaChildren.cs`
- BE controller: `Customer/Customer.API/Controllers/KhachHangDanhGiaController.cs`, `Customer/Customer.API/Areas/SandBoxArea/KhachHangDanhGiaController.cs`
- BE service: `Customer/Customer.Service/Generated/KhachHangDanhGiaService.cs`
- BE DTO/enum: `Libs/Models.Core/DTO/Customer.Client/KhachHangDanhGia/KhachHangDanhGiaDto.cs`, `Customer/Customer.Service/Generated/Enums/EnumKhachHang.cs`
- FE service: `src/app/services/customer/khachHang-danhGia.service.ts`
- FE màn hình: `src/app/pages/Customer/{phan-hoi-dich-vu, danhgia-khachhang, danhgia-chitiet-unit, danhgia-chitiet-sa, danhgia-thanhcong}/`
- FE routing: `src/app/pages/Customer/customer-routing.module.ts`
