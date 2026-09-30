# Dev — Thu chi (Quỹ / Thu chi + Số dư ban đầu)

> Service: **Accounting** (BE .NET) + cross-service Customer / WarehouseProduct. Controller chính `ThuChiController`, danh mục lý do ở `ThuChiLyDoController` (service **Branch**). Chỉ dành cho dev. Trace registry: `registry/ke-toan/ThuChiController.md`, `registry/ke-toan/ThuChiLyDoController.md`.

## Màn hình & route
- **Kế toán → Quỹ / Thu chi**: lưới phiếu thu-chi + panel tồn quỹ + tab xuất Excel theo loại phiếu; phần Số dư ban đầu.
- **Cấu hình → Lý do thu chi**: CRUD danh mục lý do (`ThuChiLyDoController`).

## Endpoint / Controller (trace registry)

**`ThuChiController`** (service Accounting, `auth: mixed`):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Danh sách phiếu + tồn quỹ | `POST api/ThuChi/DanhSach` | `ThuChi_DanhSach` |
| Chi tiết phiếu | `GET api/ThuChi/GetDetailPhieuChi?idThuChi=` | `[SBAuthorize()]` trống (any login) |
| Xuất Excel (5 loại) | `POST api/ThuChi/ExportPhieuThuTienMat` · `ExportPhieuThuTienGui` · `ExportPhieuChiTienMat` · `ExportPhieuChiTienGui` · `ExportPhieuChuyenTienNoiBo` | `[SBAuthorize()]` trống |
| Số dư ban đầu — thêm | `POST api/ThuChi/ThemMoiSoDuBanDau` | `[SBAuthorize()]` trống |
| Số dư ban đầu — xóa | `POST api/ThuChi/XoaSoDuBanDau?id=&typeView=` | `[SBAuthorize()]` trống |
| Số dư ban đầu — danh sách | `POST api/ThuChi/GetListSoDuBanDauPaging` | `[SBAuthorize()]` trống |

Internal (`[PrivateSBAuthorize]`, gọi từ Order):
- `POST api/ThuChi/UpdatePttt_ThuChiByDonHang` — khớp lại phiếu thu-chi của đơn khi đổi PTTT (xóa/insert/update theo `IdLoaiQuy`). `ThuChiService.cs:982`.
- `POST api/ThuChi/RollbackPttt_ThuChiByDonHang` — rollback khi luồng đơn lỗi. `ThuChiService.cs:1305`.

**`ThuChiLyDoController`** (service Branch, danh mục lý do, cache Redis `CK_ThuChiLyDo_{idDonVi}`):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Lưu lý do | `POST api/ThuChiLyDo/Luu` | `ThuChiLyDo_Xoa` (copy nhầm — xem bug) |
| Xóa lý do | `GET api/ThuChiLyDo/Xoa/{id}` | `ThuChiLyDo_Xoa` |
| Danh sách | `POST api/ThuChiLyDo/DanhSach` | `ThuChiLyDo_DanhSach` |
| Danh sách không phân trang | `GET api/ThuChiLyDo/DanhSachKhongPhanTrang?idDonVi=&loaiThuChi=&coMacDinh=` | `ThuChiLyDo_DanhSachKhongPhanTrang` |
| Chi tiết | `GET api/ThuChiLyDo/ChiTiet/{id}?idDonVi=` | `ThuChiLyDo_ChiTiet` |
| (internal) `GET api/ThuChiLyDo/GetAllLyDoThuChi/{idDonVi}` | `[PrivateSBAuthorize]` | — |

## Entities
- **`ThuChi`** — phiếu thu-chi chính. **`ThuChiDonHang`** — chi tiết phiếu gắn đơn hàng (có `MaHoaDon`, `IdTransaction`, `IdLoaiQuy`).
- **`SoDuBanDau`** — công nợ/quỹ khởi tạo: `NhomDoiTuong` (KH / NCC / Tài khoản), `LoaiDu`, `SoTien` mang dấu ± theo `KieuLoai` (Nợ=+, Có=−).
- **`ThuChiLyDo`** — danh mục lý do; `IdDonVi=DonViId` (riêng đơn vị) hoặc `Guid.Empty` (default hệ thống); `MaLyDo` + `MaKeToan` (map sang kế toán); `LoaiThuChi` ∈ [1,4].

## Luồng code
- **DanhSach** (`ThuChiService.cs`): ép `IdChiNhanh` cho non-admin; lọc `IdDonVi + IsDelete=false`; `CalculateSumOverView` → `TinhTonQuy` (đầu/cuối/hiện tại); `CalculateSum` → tổng Thu/Chi/Nạp/Rút; enrich tên KH qua Customer `FindCustomersById`.
- **Số dư ban đầu** (`SoDuBanDauService.cs:108`): validate IdChiNhanh/LoaiDoiTuong/KieuLoai(±1)/SoTien≠0/IdDoiTuong; KH → Customer `ThemMoiSoDuBanDauKhachHang`, NCC → Warehouse `ThemMoiSoDuBanDauNCC` (lỗi gọi → KHÔNG insert local), Tài khoản → chống trùng + insert nội bộ. Xóa (`:182`): cross-service trước, local sau (hard delete).
- **UpdatePttt_ThuChiByDonHang** (`ThuChiService.cs:982`): so khớp phiếu đã có (theo `IdTransaction`) với PTTT mới (theo `IdLoaiQuy`) → có DB-không request = xóa; có request-không DB = insert (clone bằng AutoMapper, gen `MaThuChi="THU"+yyyyMMddHHmmssfff`); cả hai = update SoTien.
- **ThuChiLyDo** (`ThuChiLyDoService.cs`): superuser/HT-AD ghi default (`IdDonVi=Empty`, set MaLyDo); user đơn vị → `IdDonVi=DonViId`. Validate `LoaiThuChi∈[1,4]`, `TenLyDo≤100`, chống trùng tên trong 1 loại + chống trùng MaLyDo (chỉ default). Clear cache sau Lưu/Xóa.

## Bug-prone / lưu ý
- **`DanhSach` — message sai ngữ cảnh** (`ThuChiService.cs:192-195`): khi Customer `FindCustomersById` trả null/empty → trả lỗi **"Không tìm thấy thông tin sản phẩm"**, có thể **chặn cả lưới** dù vẫn có phiếu (đặc biệt phiếu không gắn KH). Triage: KH thiếu chứ không phải hết phiếu.
- **Mã phiếu chi mang prefix "THU"** (`ThuChiService.cs:1055`): khi clone phiếu mới (luồng đổi PTTT), `MaThuChi` luôn gán prefix `"THU"` kể cả khi đó là dòng phiếu CHI → mã có thể sai prefix. `cần product xác nhận` về template loại quỹ.
- **Số dư ban đầu KH/NCC không cùng transaction với cross-service** (`SoDuBanDauService.cs:108-177` / `182-220`): insert/delete local và ghi/hoàn công nợ bên kia tách rời → nếu một bên fail sau khi bên kia đã xong → **lệch số dư**, không thấy rollback. Xóa là **hard delete**.
- **Phân quyền lỏng**: chỉ `DanhSach` có key thật; chi tiết, 5 export, 3 thao tác số dư ban đầu (kể cả thêm/xóa — ghi dữ liệu công nợ) đều `[SBAuthorize()]` trống → chỉ cần login. (`ThuChiController.cs:49-155`).
- **`ThuChiLyDo.Luu` gắn nhầm key** (`ThuChiLyDoController.cs:36`): dùng `ThuChiLyDo_Xoa` (copy nhầm) → quyền thêm/sửa = quyền xóa.
- **`ThuChiLyDo.Xoa` không re-check ownership** (`ThuChiLyDoService.cs:374`): xóa theo id thuần, không kiểm `IdDonVi` của bản ghi trước khi `DeleteAsync`.
- **`ThuChiLyDo.ChiTiet` ghi đè param `idDonVi`** (`ThuChiLyDoService.cs:232`): param FE truyền bị thay bằng `userLogin.DonViId` (admin → Empty) → param không có tác dụng.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\ThuChiController.cs` (`DanhSach` L35, `GetDetailPhieuChi`/Export*/SoDuBanDau* L49-155).
- BE service ThuChi: `...\Accounting\Accounting.Service\Generated\ThuChiService.cs:192` (DanhSach), `:982` (UpdatePttt), `:1055` (prefix THU), `:1305` (Rollback).
- BE service Số dư ban đầu: `...\Accounting\Accounting.Service\Generated\SoDuBanDauService.cs:108` (thêm), `:182` (xóa).
- BE lý do: `...\Branch\...\ThuChiLyDoController.cs:36`; `...\ThuChiLyDoService.cs:232` (ChiTiet), `:374` (Xoa), `:396` (GetAll internal).
- Caller cross-module (Order): `...\Order\Order.Service\Generated\OtherService\BranchService.cs:265,284` (build URL Update/Rollback PTTT).
- DTO: `Libs/Models.Core/DTO/Branch.Client/ThuChi/*` (ReqDanhSachThuChi, ReqThemMoiSoDuBanDau...), `Models.Core.DTO.Accounting.Client.SoDuBanDau`.
