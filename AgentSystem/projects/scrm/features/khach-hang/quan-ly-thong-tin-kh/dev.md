# Dev — Quản lý thông tin khách hàng

> Service: **Customer** (BE .NET) · FE module **Customer** (Angular 15 + ng-zorro). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Danh sách khách hàng (KH 360) | `/customer/ql-khachhang` | `QuanlyKhachhangComponent` → con `DanhsachKhachhhangComponent` |
| Hồ sơ chi tiết khách (drawer) | `/customer/khachhang-detail` | `QuanlyKhachhangDetailComponent` |
| Danh mục khách hàng | `/customer/dm-khachhang` | `DmKhachhangComponent` |

- Route gốc module: `app-routing.module.ts:104` (`path: 'customer'` → lazy `CustomerModule`).
- Khai báo route con: `customer-routing.module.ts:31` (ql-khachhang), `:44` (dm-khachhang).
- `DanhsachKhachhhangComponent` extends `BaseListComponentV1`; set `maPage = ListMaPage_Filter.KHACH_HANG_360`, `TableColumnKey = ListMaPage_GridColumns.KHACH_HANG_360`.

## MaTinhNang / Controller
**Danh sách khách hàng** — `KhachHangController` (`[TienIchAttribute("KhachHang", "Quản lý khách hàng", MaTienIchSCRM.TI_Customer)]`):
| MaTinhNang (SBAuthorize) | Route | Mục đích |
|---|---|---|
| `KhachHang_SearchKhachHang360` | `POST api/KhachHang/SearchKhachHang360` | Danh sách KH (paged) |
| `KhachHang_SearchKhachHang360_TotalRecord` | `POST .../SearchKhachHang360_TotalRecord` | Tổng số bản ghi |
| `KhachHang_SearchKhachHang360_ThongKe` | `POST .../SearchKhachHang360_ThongKe` | Thống kê nhanh (tổng/tạo hôm nay/sinh nhật) |
| `KhachHang_SearchKhachHang360` (reuse) | `POST .../ExportExcelKhachHang` | Xuất Excel |
| `SearchKhachHang360_GetIdsKhachHang` | `POST .../SearchKhachHang360_GetIdsKhachHang` | Lấy danh sách Id theo điều kiện (hiện FE comment out) |

Role yêu cầu (360): `DV-AD-TONG, DV-QLDV, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, DV-SALE, DV-QL-SALE, DV-SALE-LEAD, DV-MKT, DV-QL-MKT, DV-MKT-LEAD`.

**Danh mục khách hàng** — `DMKhachHangController` (`[TienIchAttribute("DMKhachHangController", "Danh mục khách hàng", MaTienIchSCRM.TI_Customer)]`), tất cả role `DV-AD-TONG`:
| MaTinhNang | Route |
|---|---|
| `DMKhachHangController_TimTheoDieuKien` | `POST api/DMKhachHang/TimTheoDieuKien` |
| `DMKhachHangController_Create` / `_Update` / `_Delete` / `_GetById` | `POST .../Create`, `/Update`, `/Delete/{id}`, `/GetById/{id}` |
| (SBAuthorize trống) `DropdownDMKhachHang`, `DropdownDMKhachHangByKieuLoai/{kieuloai}` | dropdown |

## Entities
- **`KhachHang : BaseEntity`** — `Customer.DataContext/Entity/KhachHang.cs:9`. Bảng khách hàng chính. Trường đáng chú ý: `IdDonVi`, `IdChiNhanh`, `Ma`, `HoVaTen`, `HoVaTenKhongDau`, `SoDienThoai`/`SoDienThoaiPhu`, `GioiTinh` (1 Nam/2 Nữ/3 Khác), `NgaySinh`, `IdNhomKhachHang`/`IdHangKhachHang`/`IdNguonKhachHang`, `SaleUserId`/`MarketingUserId` (+ `*Begin`), địa chỉ theo mã (`MaQuocGia/MaTinh/MaHuyen/MaXa`), các trường free-text được map qua DM (`NgheNghiep`, `TonGiao`, `TinhTrangKhachHang`, `TinhTrangSuDung`, `HieuQuaSuDung`, `ChatLuongData`, `LyDoTuChoi`, `TinhTrangHonNhan`, `Truong`, `Lop`, `DotTuyenSinh`, `TrangThai`, `NganhHocQuanTam`, `LoaiCayTrong`), điểm/công nợ (`SoDiemHienTai`, `DiemTamTru`, `SoTienTichLuy`, `SoTienNoDonHang`...), `IsKhachCu`, `MaNhaCungCap`.
- **`DMKhachHang`** — danh mục lựa chọn phân loại; phân biệt bằng `KieuLoai` (enum `EnumKieuLoaiDMKhachHang`, `Libs/Models.Core/Enum.cs:652`, giá trị 0–13). Có cache theo đơn vị (Redis).

## Luồng code
**Search 360** (`KhachHangStoreService.SearchKhachHang360`, `Customer.Service/Generated/KhachHangStoreService.cs:101`):
1. Lấy cache hạng KH + nhóm KH + cấu hình đơn vị + dropdown DM khách hàng.
2. Xác định `loaiNhanVien` (1 Sale / 3 MKT) theo input hoặc role.
3. `GetCheckPermissionViewDropdownListNhomVaUser(...)` (`:900`) → ra `idNhomString`, `idUserString`, `isAdminChonCoCauToChuc`. Nếu cấu hình `IsLeadManagerViewCustomerOfEmployee=false` và user thuần Sale/MKT → ép `idUserString = userLogin.Id`, `idNhomString = null`.
4. Build ~45 `SugarParameter` → gọi **stored proc `SCRM_Customer_KhachHang360`** qua SqlSugar (`_dbDapper.Ado.UseStoredProcedure().GetDataSetAll`).
5. `ObjectHelper.FillCollection<KhachHangDto>` rồi làm giàu: map nhóm/hạng/DM → `*Text`, ghép tên Sale/MKT từ cache user, ghép địa chỉ (`GetListThongTinDiaChi`), `TinhSoTuoi` (`:450`), che SĐT theo `CauHinhCheSoTheoUserLogin`.
- `SearchKhachHang360_TotalRecord` (`:464`) → SP `..._TotalRecord`; `_ThongKe` (`:784`) → SP `..._ThongKe`; `_GetIdsKhachHang` (`:624`) → SP `..._GetIdsKhachHang`.
- `ExportExcelKhachHang` (`:942`): ép PageSize=5000, gọi lại `SearchKhachHang360`, publish `KhachHang360Filter_Event` (RabbitMQ, ghi lịch sử xuất), build file bằng GemBox.Spreadsheet.

**Danh mục** (`DMKhachHangService`, `Customer.Service/Generated/DMKhachHangService.cs:44`):
- `TimTheoDieuKien` (`:44`): query EF `_repos.TableWithNolock()` lọc `IdDonVi`, keyword theo Ma/Ten, `KieuLoai`; map `TenKieuLoai` từ enum description; paging thủ công.
- `Create`/`Update` (`:94`/`:110`): `Validate` (`:128`) check format mã + trùng Mã/Tên trong cùng `KieuLoai`+đơn vị; sau ghi DB gọi `ClearCache_DMKhachHang_DonVi`.
- `Delete` (`:192`): xóa cứng record của đúng đơn vị, clear cache.

## Ngày sinh đồng bộ từ Contact (delta 07/2026)
> KH nhận **ngày sinh từ contact** qua event `EventUpdateCustomerFromContactMessage` (thêm field `NgaySinh`). Nguồn: khi tạo/sửa contact có ngày sinh, hoặc import contact có cột "Ngày sinh". Verify tại `KhachHangService.cs` (`scrm_build` @ `4c4f92260a`).
- **Tạo KH mới từ contact** (`InsertCustomerAnonymous`, `KhachHangService.cs:6528`): set `NgaySinh = payload.NgaySinh` khi khởi tạo entity KhachHang.
- **KH đã tồn tại — backfill KHÔNG override** (`KhachHangService.cs:6449-6461`): chỉ set `checkExsit.NgaySinh = payload.NgaySinh` khi `checkExsit.NgaySinh == null && payload.NgaySinh != null` → **giữ nguyên** ngày sinh cũ nếu KH đã có (contact KHÔNG ghi đè). Có ghi `KhachHangLichSuThayDoiThongTin` (CodeFiled=`NgaySinh`, OldValue=null).
- Các consumer khác cũng set thẳng `NgaySinh = payload.NgaySinh` (`:1691`, `:5090`) hoặc backfill khi null (`:1498-1509`) — kiểm tra ngữ cảnh từng luồng trước khi sửa (một số override, một số chỉ backfill).
- **Nguồn ngày sinh phía Contact:** xem `sales/quan-ly-contact/dev.md` (mục delta 07/2026) — Marketing_Insert/Update + import; validate không cho ngày sinh > hôm nay.

## Bug-prone
- **`KhachHangController.Xoa` (`KhachHangController.cs:330`)** luôn return lỗi "Tính năng xóa khách hàng không còn được sử dụng!" — không xóa thật. Đừng tin route này còn xóa.
- **Truyền sai `loaiNhanVien`**: nếu FE không set đúng (1/3), permission filter Sale/MKT cho ra tập khách khác → khách "biến mất" tưởng là bug quyền. `SearchKhachHang360` ép lại theo role nhưng vẫn dùng `input.LoaiNhanVien` cho `@LoaiNhanVien` của SP (chú ý lệch giữa biến local `loaiNhanVien` và param truyền vào SP — param dùng `input.LoaiNhanVien`, không phải biến đã override).
- **Logic phụ thuộc 3 cấu hình** (`ChiNhanhHienThiTatCaKhachHangDonVi`, `IsLeadManagerViewCustomerOfEmployee`, che số) lặp lại y hệt ở cả 4 method search → sửa 1 nhớ sửa cả 4 (Search/TotalRecord/ThongKe/GetIds), dễ lệch.
- **List vs TotalRecord lệch số**: 2 SP khác nhau, cùng tham số quyền nhưng nếu thiếu param ở 1 SP (so sánh kỹ list param 2 hàm) → tổng record không khớp data trả về.
- **DM tạo trùng**: validate trùng theo `KieuLoai + Ma` và `KieuLoai + Ten`, **không** validate khi UPDATE nếu `model.Id` null (chỉ báo "Id không được để trống").
- **Che số điện thoại**: khi `IsCheSo=true && IsChoPhepGoiDien=false` thì `item.SoDienThoai` set null nhưng `SoDienThoaiHienThi` vẫn là dạng mask — FE phải dùng đúng field, đừng đọc `SoDienThoai`.
- **Export bị 503**: khi service xuất bận trả `Message="Service_Unavailable"` → controller `ExportExcelKhachHang` map ra HTTP 503 (FE đọc `error.status === 503`).

## File source (path:line)
- BE controller list: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.API\Controllers\KhachHangController.cs:54` (`SearchKhachHang360`), `:66`, `:91`, `:330` (Xoa), `:791` (ExportExcel).
- BE controller DM: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.API\Controllers\DMKhachHangController.cs:24` (TimTheoDieuKien).
- BE service search: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.Service\Generated\KhachHangStoreService.cs:101` / `:464` / `:624` / `:784` / `:942`.
- BE service DM: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.Service\Generated\DMKhachHangService.cs:44`.
- Entity KhachHang: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.DataContext\Entity\KhachHang.cs:9`.
- Filter DTO `KhachHang360Filter`: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Libs\Models.Core\DTO\Customer.Client\KhachHang\KhachHangFilterDto.cs:6`.
- Enum `EnumKieuLoaiDMKhachHang`: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Libs\Models.Core\Enum.cs:652`.
- Stored procedures (DB, không trong repo source): `SCRM_Customer_KhachHang360`, `..._TotalRecord`, `..._ThongKe`, `..._GetIdsKhachHang`.
- FE list: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Customer\quanly-khachhang\danhsach-khachhhang\danhsach-khachhhang.component.ts:54` (search `:592`, export `:656`).
- FE parent: `...\quanly-khachhang\quanly-khachhang.component.ts:17`.
- FE DM: `...\Customer\dm-khachhang\dm-khachhang.component.ts:20` (enum + list kiểu loại `:176`).
- FE routing: `...\Customer\customer-routing.module.ts:31` / `:44`.
