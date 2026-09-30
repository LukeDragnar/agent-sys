# Phân loại & đánh nhãn khách hàng — Dev

> CHỈ dev. Không đưa vào summarize/faq.
> BE: service **`Customer`** (toàn bộ). 3 trục độc lập: Nhãn (`NhanKhachHang`), Nhóm (`DMNhomKhachHang` + bảng nối `KhachHangNhomKhachHang`), Hạng (`DMHangKhachHang`, gán trên `KhachHang.IdHangKhachHang`).

## Màn hình & route (FE)
FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`
Prefix parent: `app-routing.module.ts:104` → `customer` (lazy `CustomerModule`). Route con: `pages/Customer/customer-routing.module.ts`.

| Màn hình | Route | Component | routing.ts:line |
|---|---|---|---|
| Nhãn khách hàng | `/customer/nhan-khach-hang` | `NhanKhachhangComponent` | `:88-90` |
| Danh sách nhóm khách hàng | `/customer/quanly-nhomkhachhang` | `KhachhangNhomkhachhangComponent` | `:52-54` |
| Danh sách hạng khách hàng | `/customer/quanly-hangkhachhang` | `KhachhangHangkhachhangComponent` | `:56-58` |
| Danh mục nhóm KH | `/customer/dm-nhomkhachhang` | `DmNhomkhachhangComponent` | `:76-78` |
| Danh mục hạng KH | `/customer/danh-muc-hang-khach-hang` | `DmHangKhachhangComponent` | `:100-102` |

> Có thêm bản `*-admin` (super-admin template) cho nhóm/hạng: `dm-nhomkhachhang-admin` (`:72`), `danh-muc-hang-khach-hang-admin` (`:104`/`:112` — khai báo trùng path, dòng `:112` chết). Tên display "Nhãn khách hàng" trong i18n: `Core/Core.API/Resources/i18n/vi.json:42` (`table-nhankhachhang`).

## Trace endpoints → Controller (MaTienIch)
BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`

| Trace | Controller | MaTienIch | File:line (action) |
|---|---|---|---|
| `NhanKhachHang_XoaNhan` | `NhanKhachHangController` | `TI_Customer` | `NhanKhachHangController.cs:56-64` |
| `KhachHangNhomKhachHang_TimTheoDieuKien` | `KhachHangNhomKhachHangController` | `TI_NhomKhachHang` | `KhachHangNhomKhachHangController.cs:36-46` |
| `DMNhomKhachHang_ThemMoi_DV` | `DMNhomKhachHangController` | `TI_NhomKhachHang` | `DMNhomKhachHangController.cs:74-80` |
| `KhachHang_TimKiemHangKhachHangTheoDieuKien` | `KhachHangController` | (Customer) | `KhachHangController.cs:104-114` |
| `DMHangKhachHang_ThemMoi_DV` | `DMHangKhachHangController` | `TI_HangKhachHang` | `DMHangKhachHangController.cs:58-68` |

> MaTienIch const: `Libs/SB.Core/Contants.cs:744 (TI_Customer), :753 (TI_NhomKhachHang), :754 (TI_HangKhachHang)`.
> `NhanKhachHangController` kế thừa `BaseDanhMucController<NhanKhachHang, NhanKhachHangDto>`; các controller nhóm/hạng kế thừa `BaseController`. Quyền khai báo danh mục (`ThemMoi_DV`/`KhoiTao`) = **DV-AD-TONG** duy nhất.

### Endpoints chính theo từng màn
**NhanKhachHangController** (`NhanKhachHangController.cs`):
- `POST TimTheoDieuKien` (`:36`), `POST UpdateMany` (`:44`), `POST XoaNhan/{id}` (`:56`), `POST ThemMoiNhan` (`:107`), `POST CapNhatThongTinNhan` (`:117`), `GET GetById/{id}` (`:125`), `GET GetAllNhanByDonVi` (`:73`) + 2 bản `*Private`.
- Sau ThemMoi/Xoa: `_commonCacheService.ClearCache_NhanKhachHang_DonVi(CurrentUser.DonViId)` (`:62, :113`).

**KhachHangNhomKhachHangController** (bảng nối khách↔nhóm):
- `POST TimTheoDieuKien` (`:36`), `POST GetListKhachHangByIdNhom` (`:24`), `GET GetListNhomByIdKhachHang/{id}` (`:50`), `POST CreateSingle` (`:69`), `POST CreateMulti` (`:57` — **đã bị comment hết thân hàm, return true rỗng**), `POST Update` (`:81`), `GET Delete/{id}` (`:93`), `GET GetConfigNhomKhachHang/{id}` (`:101`).

**DMNhomKhachHangController** (danh mục nhóm):
- `POST TimTheoDieuKien_DV` (`:66`), `POST ThemMoi_DV` (`:74`), `POST CapNhat_DV` (`:82`), `GET ChangeSuDung_DV/{id}` (`:98`), `GET ChangeChoPhepXetNangNhom_DV/{id}` (`:106`), `GET KhoiTaoMacDinh` (`:115`), `GET TimTheoId_DV/{id}` (`:123`), `GET DropDownDMNhomKhachHang/{isSuDung}` (`:132`). Khối `*_SA` (super-admin) còn comment.

**KhachHangController** (danh sách hạng):
- `POST TimKiemHangKhachHangTheoDieuKien` (`:104`) — chỉ liệt kê khách có hạng. (Controller này còn nhiều endpoint khác của hồ sơ KH, xem `customer.md`.)

**DMHangKhachHangController** (danh mục hạng):
- `POST TimTheoDieuKien_DV` (`:25`), `GET GetAllRankByDonVi_DV` (`:39`), `POST ThemMoi_DV` (`:58`), `GET TimTheoId_DV/{id}` (`:78`), `POST CapNhat_DV` (`:86`), `GET KhoiTao_DV` (`:98`), `GET GetConfigHangByIdKh/{idKh}` (`:106`), `GET GetCauHinhHangByIdKh/{idKh}` (`:114`). Bản cũ: `DMHangKhachHangOLDController.cs`.

## Entities
Path: `Customer/Customer.DataContext/Entity/`
- **`NhanKhachHang`** — `IdDonVi`, `IdChiNhanh`, `Ten`, `BackgroundColor`, `Stt`. (BaseEntity → có `NguoiTaoUserId`, `IsDeleted`...)
- **`KhachHangNhanKhachHang`** — bảng nối khách↔nhãn: `IdNhanKhachHang`, `IdKhachHang`, `IdDonVi`, `IdChiNhanh`.
- **`DMNhomKhachHang`** — `Ten`, `GhiChu`, `KieuLoai` (có/không điều kiện), `LoaiDieuKien`, `GiaTri` (điểm yêu cầu), `IsSuDung`, `IsChoPhepTuDongXetNangNhom`, `DonViGiamGia`, `GiaTriGiamGia`, `LevelUuTien`.
- **`KhachHangNhomKhachHang`** — bảng nối khách↔nhóm: `IdNhomKhachHang`, `IdKhachHang`, `IdDonVi`, `IdChiNhanh`, `IdDonHang`.
- **`DMHangKhachHang`** — `Ten`, `Color`, `IdImage`, `PriceTichLuy` (ngưỡng), `PriceDiscount`, `Type` (đơn vị giảm), `IsGiuHang`, `PriceDuyTri`, `CoCheXepHang`, `IsSuDung`. Hạng hiện tại của khách lưu ở `KhachHang.IdHangKhachHang`.
- Lịch sử (Mongo): `KhachHangNhomKhachHangLichSu`, `KhachHangLichSuTuongTac` (ghi khi đổi nhóm).

## Luồng code

### Xóa nhãn — `NhanKhachHangService.DeleteAsync` (`NhanKhachHangService.cs:403`)
1. Lấy danh sách bản ghi nối `KhachHangNhanKhachHang` theo `IdNhanKhachHang` (`:405`) + bản ghi nhãn (`:406`); null nhãn → "Bạn không tìm thấy bản ghi".
2. Quyền: nếu **không** có role `DV_AD_TONG` và `NguoiTaoUserId != userLogin.Id` → "Bạn không có quyền xóa nhãn này" (`:413-419`).
3. Transaction (ExecutionStrategy): xóa các bản ghi nối trước (`_reposNhanKh.DeleteAsync`), rồi `base.DeleteAsync(id)` (soft delete nhãn), commit (`:422-441`).
4. Controller xóa cache nhãn theo đơn vị sau khi service trả về (`Controller:62`).

### Danh sách nhóm — `KhachHangNhomKhachHangService.TimTheoDieuKien` (`KhachHangNhomKhachHangService.cs:339`)
1. Lấy cấu hình che số + cấu hình đơn vị; resolve `IdChiNhanh` theo `IsAdmin` / `ChiNhanhHienThiTatCaKhachHangDonVi` / `IsViewAll` (`:341-370`).
2. Query 3 bảng: `KhachHang` ⋈ `KhachHangNhomKhachHang` ⋈ `DMNhomKhachHang`, filter `IdDonVi`, keyword (HoVaTen/HoVaTenKhongDau/Ma/SĐT), `IdNhomKhachHang`, chi nhánh (`:373-398`).
3. Count → sort (mặc định HoVaTen desc) → paging → che SĐT theo cấu hình che số.

### Thêm nhóm (danh mục) — `DMNhomKhachHangService.Create_DV` (`DMNhomKhachHangService.cs:57`)
1. Trim tên → `ValidateData_DV` (`:437`): tên không trống/không trùng (ToLower, loại Id hiện tại), `DonViGiamGia` hợp lệ, `GiaTriGiamGia` không null, giảm % thì ≤ 100, `KieuLoai` ∈ {CoDieuKien, KhongDieuKien}.
2. Map DTO→entity; ép `IdDonVi`, `LevelUuTien=1`, `IsChoPhepTuDongXetNangNhom=false`.
3. `KhongDieuKien` → `GiaTri=null, LoaiDieuKien=null`; ngược lại `LoaiDieuKien = TheoGiaTri` (`:67-75`).
4. Insert → `LuuRedisDMNhomKH(DonViId)` → ghi `EventLichSuTacDong` (ADD) (`:76-86`).
- **KhoiTao_DV** (`:168`): clone nhóm template (`IdDonVi == Guid.Empty && IsSuDung == true`), bỏ qua mục trùng tên (ToLower), gán Id/IdDonVi mới rồi insert batch.

### Gán nhóm cho khách — `CreateSingle` (`KhachHangNhomKhachHangService.cs:103`)
1. Khách **đã có nhóm** (any theo `IdDonVi + IdKhachHang`) → chặn "đã có nhóm... chỉ có thể sửa, không thể tạo" (`:105`).
2. Lấy nhóm từ cache `LayTatCaDMNhomKH`; null → "Không tìm thấy nhóm..."; khách null → "Không tìm thấy khách...".
3. Nếu nhóm `CoDieuKien` và `GiaTri > SoDiemHienTai` → "chưa đủ điều kiện vào nhóm này" (`:115`).
4. Insert bản nối → ghi `KhachHangNhomKhachHangLichSu` (Mongo) + `KhachHangLichSuTuongTac` (ThayDoiNhom) (`:118-157`).
> `CreateMulti` (`:71`) hiện **no-op** (toàn bộ thân đã comment, chỉ `return true`).

### Danh sách hạng — `KhachHangService.TimKiemHangKhachHangTheoDieuKien` (`KhachHangService.cs:4116`)
1. Cấu hình che số + cấu hình đơn vị; resolve `IdChiNhanh` (giống nhóm) (`:4118-4147`).
2. Query: `KhachHang` (filter `IdHangKhachHang != null`) ⋈ `DMHangKhachHang` theo `IdHangKhachHang == hang.Id`; filter `IdHangKhachHang`, keyword (`:4149-4171`).
3. Count → sort (mặc định HoVaTen desc) → paging → che SĐT (`:4173-4194`).

### Thêm hạng (danh mục) — `DmHangKhachHangService.Insert_DV` (`DmHangKhachHangService.cs:128`)
1. Validate tuần tự: userLogin, `Ten` không trống, `PriceTichLuy`, `Type`, `PriceDiscount`, `Color` đều bắt buộc (`:133-146`).
2. Trùng tên trong đơn vị → "Tên đã tồn tại trong hệ thống" (`:155`).
3. Ép `CoCheXepHang = DiemTichLuy` + `IdDonVi`; `base.InsertAsync` → `ClearCache_DMHangKhachHang_DonVi(DonViId)` (`:161-164`).
- **KhoiTao_DV** (`:184`): clone hạng template (`IdDonVi == Guid.Empty`), bỏ qua trùng tên.

## Bug-prone
- **3 khái niệm dễ nhầm:** Nhãn (`NhanKhachHang`, nối nhiều-nhiều qua `KhachHangNhanKhachHang`) ≠ Nhóm (`DMNhomKhachHang`, 1 khách 1 nhóm, có mức giảm) ≠ Hạng (`DMHangKhachHang`, gán trên `KhachHang.IdHangKhachHang`, theo điểm). Sửa nhầm bảng/service rất dễ.
- **`CreateMulti` của `KhachHangNhomKhachHangService` đã bị comment toàn bộ → no-op** trả `true`. Đừng tin endpoint này gán nhóm hàng loạt — nó không làm gì.
- **1 khách = 1 nhóm:** chặn bằng `AnyAsync(IdDonVi + IdKhachHang)` ở `CreateSingle`. Muốn đổi nhóm phải dùng `Update`, không tạo mới.
- **Điều kiện nhóm dùng `SoDiemHienTai`** (không phải `SoDiemTichLuy`) — khác trục với hạng (hạng theo điểm tích lũy). Dễ nhầm khi debug "tại sao khách không vào được nhóm".
- **Xóa nhãn giới hạn theo người tạo** nếu không phải `DV_AD_TONG` — nhân viên không xóa được nhãn người khác.
- **Cache:** thiếu `ClearCache_NhanKhachHang_DonVi` / `LuuRedisDMNhomKH` / `ClearCache_DMHangKhachHang_DonVi` sau write → dropdown/list cũ. Nhãn xóa cache ở **controller** (không ở service) — sửa logic xóa nhãn nơi khác dễ quên clear cache.
- **`DMNhomKhachHang` không có `IdChiNhanh`** (nhóm theo đơn vị, không theo chi nhánh) trong khi bảng nối `KhachHangNhomKhachHang` lại có `IdChiNhanh` — chú ý khi filter chi nhánh.
- **Filter chi nhánh phụ thuộc cấu hình `ChiNhanhHienThiTatCaKhachHangDonVi` + `IsViewAll` + `IsAdmin`** (lặp lại ở cả TimTheoDieuKien nhóm và hạng) — sai cấu hình → khách "biến mất" khỏi danh sách.
- **Danh sách hạng chỉ lấy khách có `IdHangKhachHang != null`** — khách chưa được xét hạng sẽ không hiện, không phải lỗi.
- **Route admin trùng path:** `danh-muc-hang-khach-hang-admin` khai báo 2 lần (`customer-routing.module.ts:104` và `:112`) → bản dưới không bao giờ match. `DMHangKhachHangOLDController` còn tồn tại — cẩn thận sửa nhầm bản cũ.
- **Khai báo danh mục (ThemMoi_DV/KhoiTao) chỉ DV-AD-TONG** — nếu khách báo "không thêm được nhóm/hạng", check role trước khi debug logic.

## File source (path:line thật)
BE root: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`
- `Customer/Customer.API/Controllers/NhanKhachHangController.cs:36,56,107,117`
- `Customer/Customer.API/Controllers/KhachHangNhomKhachHangController.cs:24,36,57,69,81,93`
- `Customer/Customer.API/Controllers/DMNhomKhachHangController.cs:66,74,98,106,115`
- `Customer/Customer.API/Controllers/KhachHangController.cs:104`
- `Customer/Customer.API/Controllers/DMHangKhachHangController.cs:25,58,98`
- `Customer/Customer.Service/Generated/NhanKhachHangService.cs:238,403`
- `Customer/Customer.Service/Generated/KhachHangNhomKhachHangService.cs:71,103,339`
- `Customer/Customer.Service/Generated/DMNhomKhachHangService.cs:57,168,437`
- `Customer/Customer.Service/Generated/DmHangKhachHangService.cs:128,184`
- `Customer/Customer.Service/Generated/KhachHangService.cs:4116`
- `Customer/Customer.DataContext/Entity/{NhanKhachHang,KhachHangNhanKhachHang,DMNhomKhachHang,KhachHangNhomKhachHang,DMHangKhachHang}.cs`
- `Libs/SB.Core/Contants.cs:744,753,754` (MaTienIch)

FE root: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`
- `src/app/app-routing.module.ts:104` (prefix `customer`)
- `src/app/pages/Customer/customer-routing.module.ts:52,56,76,88,100`
- `src/app/pages/Customer/{nhan-khachhang,khachhang-nhomkhachhang,khachhang-hangkhachhang,dm-nhomkhachhang,dm-hang-khachhang}/`
