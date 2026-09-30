# Dev — Quản lý trạng thái khách hàng

> Chỉ dành cho dev. Service: **Customer**. Trace: `TrangThaiKhachHang_TimTheoDieuKien`.

## Màn hình & route (FE — Angular 15 + ng-zorro)
- Component chính: `TrangthaiKhachhangComponent`
  - `src/app/pages/Customer/trangthai-khachhang/trangthai-khachhang.component.ts` (+ `.html`, `.scss`)
  - Route: `Customer/customer-routing.module.ts` → `path: 'trang-thai-khach-hang'`.
  - Extends `BaseListComponent`; dùng `GridColumn` cho bảng (cột: `ten`, `stt`, `backgroundColor`, `ngayCapNhat`).
  - `ngOnInit()` → `getNganhNghe()` (chọn ngành nghề[0]) → `initGrid()` → `gridLoadData()`.
  - `gridLoadData()` gọi `trangThaiKhachHangService.timTheoDieuKien(filter)` với `filter = { keyWord, ...gridOption, idNganhNghe }`.
  - Lưu: `onSaveAll()` → `UpdateMany(gridData)`; `onSave(item)` → `create`/`update`; `onDeleteDv(id)` → `delete(id)`.
- Service FE: `src/app/services/customer/trangthai-khachhang.service.ts`
  - Extends `BaseService`, base url `${SERVICE_API_CUSTOMER}api/TrangThaiKhachHang`.
  - `timTheoDieuKien` / `create` / `update` / `delete` kế thừa từ `BaseService` (POST `TimTheoDieuKien`, …).
  - Riêng: `UpdateMany`, `GetAllTrangThaiKhachHangByDonVi`, và nhóm `*_SA` (Sandbox area, gọi `api/SanboxArea/TrangThaiKhachHang/*`).
- Màn admin/template (dữ liệu mẫu theo ngành nghề): `quan-ly-trangthai-khachhang-admin/` → route `trang-thai-khach-hang-admin`.

## Controller (BE — .NET 6)
- `Customer/Customer.API/Controllers/TrangThaiKhachHangController.cs`
  - `[TienIchAttribute("TrangThaiKhachHang", "Trạng thái khách hàng", MaTienIchSCRM.TI_Customer)]`, route `api/[controller]`.
  - Endpoints & permission (`SBAuthorize`):
    - `POST TimTheoDieuKien` — `TrangThaiKhachHang_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV).
    - `POST UpdateMany` — `TrangThaiKhachHang_UpdateMany` (DV-AD-TONG). Có `ModelState` check.
    - `POST ThemMoi` — `TrangThaiKhachHang_ThemMoi` (DV-AD-TONG, DV-QLDV) → sau insert **clear cache** theo `CurrentUser.DonViId`.
    - `POST CapNhatThongTin` — `TrangThaiKhachHang_CapNhatThongTin` (DV-AD-TONG, DV-QLDV).
    - `GET TimTheoId/{id}` — `TrangThaiKhachHang_TimTheoId`.
    - `POST Xoa/{id}` — `TrangThaiKhachHang_Xoa` (DV-AD-TONG, DV-QLDV) → check tồn tại rồi `DeleteAsync` + **clear cache**.
    - `GET GetAllTrangThaiKhachHangByDonVi` — danh sách để gắn cho khách, mở cho nhiều role nghiệp vụ.
    - `GET GetAllTrangThaiKhachHangByDonViPrivate/{idDonVi}` — `[PrivateSBAuthorize]` (gọi nội bộ service-to-service).
- Sandbox controller (bản `*_SA`): `Customer/Customer.API/Areas/SandBoxArea/TrangThaiKhachHangController.cs`.

## Entities (Customer.DataContext)
- `Entity/TrangThaiKhachHang.cs` : `BaseEntity` — `IdDonVi?`, `IdChiNhanh?`, `Ten` (nvarchar), `BackgroundColor?` (varchar), `Stt?`, `IdNganhNghe?`.
  - Dữ liệu mẫu (template): `IdDonVi == Guid.Empty` + `IdNganhNghe == <ngành>`.
- `Entity/KhachHangTrangThaiKhachHang.cs` : `BaseEntity` — bảng liên kết khách ↔ trạng thái: `IdDonVi?`, `IdChiNhanh?`, `IdTrangThaiKhachHang?`, `IdKhachHang?`.
- DbContext: `Customer.DataContext/Generated/DbContexts/CustomerContext.cs`; mapping: `Generated/Configs/MappingProfile.cs`.

## DTO (Libs/Models.Core)
- `DTO/Customer.Client/TrangThaiKhachHang/TrangThaiKhachHangDto.cs` — Id?, IdDonVi?, IdChiNhanh?, Ten, BackgroundColor?, Stt?, IdNganhNghe?.
- `DTO/Customer.Client/TrangThaiKhachHang/TrangThaiKhachHangFilterDto.cs` : `GridInfo` — `KeyWord?`, `IdNganhNghe?`.

## Luồng code (Service)
`Customer/Customer.Service/Generated/TrangThaiKhachHangService.cs` (interface `ITrangThaiKhachHangService : IBaseService<TrangThaiKhachHang>`):
- **`TimTheoDieuKien(filter, userLogin)`**: `TableWithNolock().Where(IdDonVi == userLogin.DonViId)`, lọc `Ten.Contains(KeyWord)` nếu có, `OrderBy(Stt)`. **Trả về `IQueryable` (chưa materialize)** bọc trong `MethodResult<object>` — phân trang/execute do tầng response/EF xử lý.
- **`InsertAsync` (override)**: validate `Ten` rỗng; check trùng (`IsDelete==false && Ten==Trim && IdDonVi && IdChiNhanh`); set `IdDonVi`, `IdChiNhanh` từ `userLogin` rồi `base.InsertAsync`.
- **`UpdateAsync` (override)**: check trùng tên (loại trừ chính `Id`); `MarkDirty(Ten, BackgroundColor, Stt)`; `base.UpdateAsync`; **clear cache**.
- **`UpdateMany(listData, userLogin)`**: tách `listDataUpdate` (Id != null) / `listDataInsert` (Id == null); validate từng item (rỗng, trùng với DB qua `listKhongUpdate`, trùng nội bộ lô, Stt/Color null); map DTO→entity bằng `AutoMapperConfiguration.Mapper`; `MarkDirty`; `UpdateAsync`/`InsertAsync` theo lô; **clear cache**. Lỗi đầu tiên → return error, không lưu lô.
- **`KhoiTao(idDonVi, idNganhNghe)`**: copy template (`IdDonVi==Guid.Empty && IdNganhNghe==idNganhNghe`) sang đơn vị; nếu đơn vị đã có data → báo "đã được khởi tạo"; set `NguoiTao/CapNhatUserName="Dữ liệu mẫu"`, `IdChiNhanh=Guid.Empty`; **clear cache**.
- Nhóm **`*Template`** (`TimTheoDieuKienTemplate`, `UpdateManyTemplate`, `InsertAsyncTemplate`, `UpdateAsyncTemplate`): quản lý bộ dữ liệu mẫu (`IdDonVi==Guid.Empty`), thêm `MarkDirty(IdNganhNghe)` — dùng cho màn admin/sandbox.
- **`GetAllTrangThaiKhachHangByDonVi()` / `...Private(idDonVi)`**: đọc **cache** trước (`GetCache_TrangThaiKH_DonVi`), miss thì query DB + `SetCache_TrangThaiKH_DonVi`; map sang DTO, `OrderBy(Stt)`.

### Cache (`Customer.Service/Generated/Cache/CommonCacheService.cs`)
- `ClearCache_TrangThaiKhachHang_DonVi(idDonVi)`, `GetCache_TrangThaiKH_DonVi(idDonVi)`, `SetCache_TrangThaiKH_DonVi(idDonVi, data, ttl=1440 phút)`. Key trong `CommonKeyCache.cs`.

### Khởi tạo theo đơn vị
- `Customer/Customer.API/Consumer/KhoiTaoMacDinhTheoDonViConsumer.cs` — `IConsumer<KhoiTaoDonViPermisionMessage>`: chạy song song `ConfigDoiDiemService.KhoiTao` + `_trangThaiKhachHangService.KhoiTao(IdDonVi, IdNganhNghe)`. **try/catch nuốt exception** (lỗi khởi tạo không báo ra).

### Gắn trạng thái cho khách (liên quan)
- `Customer/Customer.Service/Generated/KhachHangService.cs` dùng `IRepository<KhachHangTrangThaiKhachHang>`: ví dụ `UpdateKhachHangTrangThaiPrivate` — xóa hết link cũ của khách (`IdDonVi+IdKhachHang`) rồi insert lại theo `ListTrangThaiKhachHang`, chỉ insert trạng thái thuộc cùng đơn vị (`x.IdDonVi == userLogin.DonViId`).

## Bug-prone / lưu ý khi sửa
- **`TimTheoDieuKien` trả `IQueryable` chưa execute** (không `ToList`/phân trang trong service). Cẩn thận khi đổi return type hoặc thêm filter — phân trang phụ thuộc tầng ngoài. `FilterDto` kế thừa `GridInfo` (có PageInfo) nhưng service **không** áp `Skip/Take` (đoạn paging bị comment ở bản Template).
- **Validate trùng tên dùng `Trim()`** ở insert/update nhưng so sánh `Contains`/`==` **case-sensitive theo collation DB** — phụ thuộc collation, không tự lower-case.
- **Cache theo đơn vị**: mọi thay đổi (Insert/Update/Delete/UpdateMany/KhoiTao) phải `ClearCache_TrangThaiKhachHang_DonVi`. Quên clear → `GetAllTrangThaiKhachHangByDonVi` trả data cũ (TTL 1440 phút = 1 ngày). Lưu ý `ThemMoi` clear ở **controller**, còn `Update/Delete/UpdateMany` clear trong **service** → không đồng nhất, dễ sót khi thêm endpoint mới.
- **Consumer khởi tạo nuốt exception** → nếu khởi tạo trạng thái mẫu lỗi, đơn vị mới sẽ không có trạng thái mà không có log/báo lỗi.
- **`UpdateMany` validate kiểu fail-fast** (break ở lỗi đầu tiên) → user chỉ thấy 1 lỗi mỗi lần lưu lô.
- **Permission lệch nhau giữa endpoint**: xem (`TimTheoDieuKien`) cho 4 role; nhưng `UpdateMany` chỉ `DV-AD-TONG`, còn `ThemMoi/CapNhat/Xoa` thêm `DV-QLDV`. Khi đổi quyền cần đồng bộ.
- **Xóa là soft delete** (`IsDelete` qua BaseEntity); link `KhachHangTrangThaiKhachHang` của khách **không** được dọn khi xóa trạng thái ở màn này (chỉ dọn khi cập nhật trạng thái của từng khách).

## File source
- BE controller: `Customer/Customer.API/Controllers/TrangThaiKhachHangController.cs`
- BE controller (sandbox): `Customer/Customer.API/Areas/SandBoxArea/TrangThaiKhachHangController.cs`
- BE service: `Customer/Customer.Service/Generated/TrangThaiKhachHangService.cs`
- BE cache: `Customer/Customer.Service/Generated/Cache/CommonCacheService.cs`, `.../CommonKeyCache.cs`
- BE consumer: `Customer/Customer.API/Consumer/KhoiTaoMacDinhTheoDonViConsumer.cs`
- BE entities: `Customer/Customer.DataContext/Entity/TrangThaiKhachHang.cs`, `.../KhachHangTrangThaiKhachHang.cs`
- BE liên quan (gắn cho khách): `Customer/Customer.Service/Generated/KhachHangService.cs`
- DTO: `Libs/Models.Core/DTO/Customer.Client/TrangThaiKhachHang/{TrangThaiKhachHangDto,TrangThaiKhachHangFilterDto}.cs`
- FE component: `src/app/pages/Customer/trangthai-khachhang/trangthai-khachhang.component.{ts,html,scss}`
- FE service: `src/app/services/customer/trangthai-khachhang.service.ts`
- FE route: `src/app/pages/Customer/customer-routing.module.ts`
- FE màn admin/template: `src/app/pages/Customer/quan-ly-trangthai-khachhang-admin/`
