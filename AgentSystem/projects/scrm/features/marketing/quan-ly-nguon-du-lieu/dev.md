# Dev — Quản lý nguồn dữ liệu

> Service: **Contact** (BE .NET) cho cả 2 controller. Module nghiệp vụ marketing. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Component |
|---|---|---|
| Quản lý nguồn dữ liệu (sản phẩm landing) | landing-page module | sanpham-landing.service (cấu hình chia số/chat, kênh bán hàng, import) |
| Quản lý kênh tiếp nhận | landing-page/ql-kenh-tiep-nhan | `DanhMucKenhTiepNhanComponent` |

## Endpoint / Controller (trace registry)

**SanPhamLandingController** (service Contact, auth **mixed**) — registry `registry/sales/SanPhamLandingController.md`:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1-2 | Thêm / Cập nhật nguồn | `POST ThemMoi` · `POST CapNhatThongTin/{id}` | `SanPhamLandingController_ThemMoi` / `_CapNhatThongTin` (AD-TONG/CN/MCN, QL-MKT, MKT-LEAD, MKT) |
| 3 | Cho sale tạo contact thủ công | `POST ChangeIsAllowSaleTaoThuCong/{id}` | `SanPhamLandingController_ChangeIsAllowSaleTaoThuCong` |
| 4 | Cấu hình chia số / chia chat / kênh bán hàng | `POST ChangeCauHinhChiaSo` · `ChangeCauHinhChiaChat` · `ChangeKenhBanHang` | ChiaSo/KenhBanHang: key tương ứng; **ChiaChat: key rỗng** |
| 6 | Gán user MKT | `POST UpdateMarketingUser` | `SanPhamLandingController_UpdateMarketingUser` (AD-TONG/CN/MCN) |
| 7 | Tìm / dropdown / theo id | `POST TimTheoDieuKienAsync` · `GET TimListLandingDropdownAsync` · `GET TimTheoId/{id}` | key tương ứng (vai rộng) |
| 8 | Xóa nguồn | `POST Xoa/{id}` | `SanPhamLandingController_Xoa` (AD-TONG/CN/MCN, MKT) |
| 9 | Landing data (tìm/lưu/xóa) | `POST Sanpham_LandingData_TimTheoDieuKien` · `_Save` · `_Xoa/{idLandingData}` | `[SBAuthorize]` (login) |
| 10 | Import nguồn (template/DS/xóa/import/insert) | `GET DownloadTemplateNguonDuLieuAsync` · `POST GetDanhSachNguonDuLieuImport` · `DeleteNguonDuLieuImport` · `ImportTemplateNguonDuLieu` · `InsertDataImportToSanPhamLanding` | `SanPhamLandingController_*` (vai MKT) |
| 11 | Cập nhật toàn bộ data landing | `GET UpdateDataLanding` | `SanPhamLandingController_UpdateData` (AD-TONG/CN/MCN) |
| 12 | Lấy landing theo id (cache) / theo ids | `GET GetSanPhamLandingById/{id}` (AllowAnonymous) · `POST GetListLandingByIds` (SBAuthorize) | mixed |
| — | [private] landing data / dropdown filter | `GetLandingDataPrivate`, `ThemSanPhamLandingDataPrivate`, `CapNhatThongTinLandingDataPrivate`, `GetDropdownLandingFilter` | `[PrivateSBAuthorize]` |
| — | [anonymous] Ecommerce lấy landing | `TimTheoIdWithNoAuthen/{id}` | `[AllowAnonymous]` |

**DMKenhTiepNhanController** (service Contact, auth **mixed**) — registry `registry/sales/DMKenhTiepNhanController.md`. Quyền **HT-AD, DV-AD-TONG**:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Tìm theo điều kiện | `POST TimTheoDieuKien` | `DMKenhTiepNhan_TimTheoDieuKien` |
| 2 | Thêm mới | `POST ThemMoi` | `DMKenhTiepNhan_ThemMoi` |
| 3 | Cập nhật | `POST CapNhatThongTin` | `DMKenhTiepNhan_CapNhatThongTin` |
| 4 | Xóa | `POST Xoa/{id:Guid}` | `DMKenhTiepNhan_Xoa` |
| 5 | Chi tiết theo Id | `GET GetById/{id:Guid}` | `DMKenhTiepNhan_GetById` |
| — | [private] lấy chi tiết 1 kênh | `TimTheoId_Private/{id:Guid}` | `[PrivateSBAuthorize]` |

## Entities
- **SanPhamLanding** — nguồn dữ liệu + cấu hình chia số/chat/kênh bán hàng + user MKT.
- **SanPhamLandingData** — số liệu landing theo FB Ads/ngày.
- **DMKenhTiepNhan** — danh mục kênh tiếp nhận theo đơn vị.

## Luồng code
- **Landing:** `SanPhamLandingController` → `SanPhamLandingService` / `SanPhamLandingDataService`, AutoMapper, GemBox (template/import Excel), Redis (`GetCacheLandingById`). Facebook/Ads đẩy landing data qua endpoint `[PrivateSBAuthorize]` (map `SanPhamLandingDataFB` → `SanPhamLandingData`, check NotFound trước khi update).
- **Kênh tiếp nhận:** `DMKenhTiepNhanController` → `DMKenhTiepNhanService` (EF CRUD), `ICommonCacheService` (clear cache), AutoMapper. Mỗi endpoint validate `ModelState` trước.

## Bug-prone / lưu ý
- **`SanPhamLandingController.cs:66`** — `ChangeCauHinhChiaChat` `[SBAuthorize("", ...)]` **key permission rỗng** → chỉ chặn bằng danh sách vai, lệch convention so với `ChangeCauHinhChiaSo`.
- **`SanPhamLandingController.cs:185,283`** — `TimTheoIdWithNoAuthen`, `GetSanPhamLandingById` `[AllowAnonymous]`: trả thông tin nguồn/landing không qua token, chỉ dựa id → cần đảm bảo không lộ dữ liệu nhạy cảm.
- **`SanPhamLandingController.cs:263`** — `UpdateDataLanding` dùng HTTP **GET** cho thao tác cập nhật hàng loạt → không idempotent-safe, dễ trigger ngoài ý muốn (nên POST).
- **Cache landing:** `GetSanPhamLandingById` đọc cache Redis → cần làm mới cache sau Create/Update/Delete nguồn, nếu không web booking đọc dữ liệu cũ.
- **`DMKenhTiepNhanController.cs:43,56`** — `ThemMoi` & `CapNhatThongTin` **KHÔNG gọi `DMKenhTiepNhan_RemoveByDonVi`** sau khi ghi (chỉ `Xoa` clear cache ở `:78`) → sau thêm/sửa, cache đơn vị **lệch** tới khi xóa hoặc cache hết hạn. Nên thêm clear cache ở 2 endpoint này.
- **`DMKenhTiepNhanController.cs:30`** — param `grindInfo` sai chính tả (đúng `gridInfo`) — vô hại, mùi code.
- Quyền kênh tiếp nhận **hẹp** (HT-AD, DV-AD-TONG) — non-admin đơn vị không CRUD được. Controller này không có bản sinh đôi SandboxArea.

## File source (path:line)
- Registry: `projects/scrm/registry/sales/SanPhamLandingController.md`, `registry/sales/DMKenhTiepNhanController.md`.
- BE (service Contact): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\SanPhamLandingController.cs` (`:66`, `:185`, `:263`, `:283`), `...\DMKenhTiepNhanController.cs` (`:30`, `:43`, `:56`, `:78`).
