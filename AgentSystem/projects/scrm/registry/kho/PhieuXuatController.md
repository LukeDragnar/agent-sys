---
controller: PhieuXuatController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý tồn kho > tab Xuất kho > danh sách phiếu xuất (WarehouseProduct/quanly-kho/phieu-xuat)"
muc_dich: "Quản lý phiếu xuất kho: tạo/sửa/hoàn thành phiếu xuất, trừ tồn kho + ghi giá vốn, đồng bộ sang kế toán."
alias: [phiếu xuất, xuất kho, phieu xuat, PX, xuất hàng, xuất sử dụng, xuất thu ngân, xuất giao vận]
phu_thuoc:
  - PhieuXuatService (WarehouseProduct.Service)
  - DongBoKeToanService (sync phiếu xuất → Accounting)
  - CauHinhKhoService (ChoPhepTonKhoAm, super-admin sửa phiếu hoàn thành)
  - CommonService.SinhMaPhieu (sinh mã PX)
  - AccountingService.GetTkNgamDinhByDonVi (tài khoản ngầm định khi hoàn thành SX)
  - DropdownCommonWarehouseService (DmKho: TinhTrangXuat, PhanLoaiXuat)
ripple:
  - KhoChiTiet.SoLuong (trừ tồn khi tạo HoanThanh / cộng lại khi Revert)
  - SanPhamImei / SanPhamLoDate (gán IMEI, lô-date theo phiếu)
  - PhieuXuatSanPham.GiaVon + ThanhTien (giá vốn xuất)
  - Accounting (DongBoKeToanService.DongBoPhieuXuat — hạch toán phiếu xuất)
  - QueueSyncKhoChiTietLichSu (lịch sử biến động tồn kho)
---

# PhieuXuat — Xuất kho (ripple tồn / giá vốn / kế toán)

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền | Hành vi đúng (expected) | Known-issue |
|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút "Tìm kiếm" | POST `TimTheoDieuKien` | `PhieuXuat_TimTheoDieuKien` | Trả JSON `{table1=rows, table2[0].totalRecord}`; FE tô màu theo `maTinhTrang` (HoanThanh xanh / DangXuLy cam / Nhap xám). | FE parse `JSON.parse(rs.data)` — đổi shape SP là vỡ grid |
| 2 | Xuất Excel danh sách | menu "Export" | POST `ExportExcelPhieuXuat` | `PhieuXuat_ExportExcelPhieuXuat` | Trả file `DanhSachPhieuXuat.xlsx`. Lỗi → 503 nếu `Service_Unavailable`, else 500 code `EXPORT_ERROR`. | — |
| 3 | Thêm mới phiếu xuất | nút "Thêm mới" → `onAddNewPhieu()` | POST `ThemMoi` | `PhieuXuat_ThemMoi` | Validate input + kho thuộc chi nhánh user; check tồn đủ (chặn nếu `SoLuong` thiếu trừ khi `isBoQuaTonKhoAm`); nếu trạng thái Hoàn thành → **trừ KhoChiTiet.SoLuong**, ghi GiaVon/ThanhTien, gán IMEI/LoDate, sync kế toán + lịch sử. | Tồn-âm chỉ mở khi `ChoPhepTonKhoAm`/kho `IsChoPhepChotAm` + có mã đơn hàng |
| 4 | Tìm theo id | (mở popup chi tiết) | GET `TimTheoId/{id}` | `PhieuXuat_TimTheoId` | Trả phiếu + chi tiết sản phẩm. | — |
| 5 | Cập nhật phiếu | nút "Sửa" → `onEdit()` | POST `CapNhatThongTin` | `PhieuXuat_CapNhatThongTin` | **Chặn sửa khi đã Hoàn thành** (trừ super-admin + phiếu thủ công). Phiếu XuatSuDung có guard riêng. | FE `canEditPhieuXuatHoanThanh()` phải khớp guard BE `SuaPhieuHoanThanhService.PhieuXuat.cs` |
| 6 | Revert phiếu | (toolbar popup) | POST `RevertPhieuXuat` | `PhieuXuat_RevertPhieuXuat` | **Cộng lại KhoChiTiet.SoLuong** (hoàn tồn) trong transaction, đổi trạng thái → DangXuLy, đẩy `QueueSyncKhoChiTietLichSu`. CHẶN với XuatSuDung/XuatThuNgan/ChuyenChoXuatHang/XuatGiaoVan. | Trừ tồn dùng raw SQL (`GenStringSql...`); rollback nếu `rowsAffected < count` |
| 7 | Xóa phiếu nháp | menu "Xóa" (chỉ `maTinhTrang==Nhap`) | POST `XoaPhieuXuatNhap/{id}` | (mở, `[SBAuthorize()]`) | Chỉ xóa phiếu trạng thái Nháp (chưa gắn kho/IMEI/kế toán). | FE chỉ hiện nút khi trạng thái Nhap |
| 8 | Đồng bộ lại sang kế toán | menu (chỉ `==HoanThanh`) | POST `DongBoLaiSangKeToanTheoId/{id}` | `PhieuXuat_DongBoLaiSangKeToanTheoId` | **Chỉ cho phiếu Hoàn thành**; set IdPhanLoai/TinhTrang rồi gọi `DongBoKeToanService.DongBoPhieuXuat`. | FE confirm dialog hardcode tiếng Việt (chưa đa ngôn ngữ) |
| 9 | Download template import | — | GET `DownloadTemplatePhieuXuatAsync` | `PhieuXuat_DownloadTemplatePhieuXuatAsync` | Trả `TemplatePhieuXuat.xlsx`. | — |
| 10 | Import phiếu xuất (Excel) | nút "Import" → `onImportExcel()` | `GetDanhSachPhieuXuatImport` / `ImportTemplatePhieuXuat` / `DeleteTmpPhieuXuatImport` / `InsertTmpPhieuXuatToPhieuXuat` | `PhieuXuat_*Import*` | Luồng staging: upload → list tmp → xóa/insert chính thức. | — |
| 11 | Import / quét IMEI | menu "Import imei" (`isCoSanPhamImei && DangXuLy`), "Quét imei trực tiếp" | `DownloadTemplatePhieuXuatNhapImei` / `ImportTemplatePhieuXuatNhapImei/{idPhieu}` / `GetDanhSachPhieuXuatImportNhapImei` / `InsertTmpPhieuXuatImportNhapImeiToSpImei` / `DeleteTmpPhieuXuatImportImei` / `DownloadFileLoi/{idPhieu}` / `ViewImei` / `XoaImei` | `PhieuXuat_*Imei*` | Gán IMEI vào SanPhamImei của phiếu; file lỗi tải về để sửa. | Import imei chỉ mở khi phiếu DangXuLy + có sản phẩm imei |
| 12 | Tạo PX từ phiếu bán hàng | (internal) | POST `ThemMoiFromPhieuBanHang` | `[PrivateSBAuthorize]` | Tạo phiếu xuất tự động từ phiếu bán hàng (ripple: trừ tồn, giá vốn, kế toán). | Chỉ gọi nội bộ giữa service |
| 13 | Tạo PX từ trả lại hàng bán | (internal) | POST `PhieuXuat_TaoTuTraLaiHangBan` | `[PrivateSBAuthorize]` | Sinh phiếu xuất tương ứng nghiệp vụ trả hàng bán. | internal |
| 14 | Hoàn thành SP (đơn online/offline) | (internal) | POST `HoanThanhSanPham` | `[PrivateSBAuthorize]` | Sinh phiếu xuất `XuatKhac/SanXuat`, tính **giá vốn theo SP cha**, lấy TK ngầm định kế toán, trạng thái Hoàn thành. | Chỉ xử lý sản phẩm cấu thành (`IsSanPhamCauThanh`) |
| 15 | Lấy PX theo đơn hàng / phiếu bán hàng | — | GET `PhieuXuat_GetByIdDonHang` (`[AllowAnonymous]`), `PhieuXuat_GetByIdPhieuBanHang`, `TimIdTheoMaDon/{maDon}`, `TimIdTheoIdPhieuBanHang/{id}`, `GetPhieuXuatTheoListIds` | mở / Private | Tra cứu liên kết PX ↔ đơn hàng / phiếu bán hàng. | `PhieuXuat_GetByIdDonHang` để `[AllowAnonymous]` — lưu ý bảo mật |
| 16 | Lấy tồn IMEI/LoDate theo SP | — | POST `GetImeiOrLoDateSanPhamAsync` | `[SBAuthorize]` | Trả tồn chi tiết IMEI/lô-date để chọn khi xuất. | — |
| 17 | In phiếu xuất | nút "In" | GET `PrintPhieuXuat/{idPhieu}` | `[SBAuthorize()]` | Trả dữ liệu/template in phiếu. | — |

## Quan hệ
- **Gọi sang:** `PhieuXuatService` (toàn bộ logic) · `DongBoKeToanService.DongBoPhieuXuat` (hạch toán) · `AccountingService.GetTkNgamDinhByDonVi` (TK ngầm định khi hoàn thành SX) · `CauHinhKhoService` (tồn âm, super-admin) · `CommonService.SinhMaPhieu` · `DropdownCommonWarehouseService` (DmKho).
- **Dữ liệu (entities):** `PhieuXuat`, `PhieuXuatSanPham` (GiaVon, ThanhTien), `KhoChiTiet` (SoLuong tồn), `SanPhamImei`, `SanPhamLoDate`, `DmKho` (TinhTrangXuat / PhanLoaiXuat), `Kho` (chi nhánh, IsChoPhepChotAm), `SanPham`/`SanPhamCauThanh`.
- **Ripple/đồng bộ:** trừ/cộng `KhoChiTiet.SoLuong` (raw SQL `GenStringSqlTruSoLuongKho*` / `GenStringSqlCongSoLuongKho`) → `QueueSyncKhoChiTietLichSu` (lịch sử tồn) → `DongBoKeToanService.DongBoPhieuXuat` (Accounting). Revert = đảo chiều tồn + về DangXuLy.

## Luồng xuyên module (nếu có)
- **Bán hàng/Logistic → Kho:** `ThemMoiFromPhieuBanHang` / `HoanThanhSanPham` (đơn online/offline) sinh phiếu xuất tự động → trừ tồn → ghi giá vốn → sync kế toán. Phiếu mang `IdDonHang` / `IdDonHangLogistic` / `IdPhieuBanHang` → phiếu loại này **không sửa được** (kể cả super-admin) và bị chặn Revert nếu là XuatSuDung/XuatThuNgan/ChuyenChoXuatHang/XuatGiaoVan.
- **Kho → Kế toán:** mọi phiếu Hoàn thành đẩy sang Accounting qua `DongBoPhieuXuat`; có thể đồng bộ lại thủ công (action #8). Accounting có `PhieuXuatController` riêng (Accounting.API) — node này chỉ phủ phía WarehouseProduct.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- BE `WarehouseProduct/.../PhieuXuatService.cs:570` — trừ tồn bằng raw SQL `GenStringSqlTruSoLuongKhoCoDungTichV2`; sai param dung tích → tồn lệch.
- BE `PhieuXuatService.cs:800` — chặn xuất khi `khoChiTietSoLuongTruoc.SoLuong < item.SoLuong` trừ khi `isBoQuaTonKhoAm`; tồn-âm chỉ bật khi `ChoPhepTonKhoAm`/`IsChoPhepChotAm` + có mã đơn (`:714`, `:720`).
- BE `PhieuXuatService.cs:1406` — `CapNhatThongTin` chặn sửa khi `DmKhoMaTinhTrang == HoanThanh`; ngoại lệ super-admin xử lý ở `SuaPhieuHoanThanhService.PhieuXuat.cs`.
- BE `PhieuXuatService.cs:2070` — `RevertPhieuXuat` chặn 4 phân loại (XuatSuDung/XuatThuNgan/ChuyenChoXuatHang/XuatGiaoVan); cộng tồn trong transaction, `rowsAffected < count` → rollback (`:2142`).
- BE `PhieuXuatService.cs:1366` — `DongBoLaiSangKeToanTheoId` chỉ chạy với phiếu Hoàn thành.
- BE `PhieuXuatService.cs:5476` — `HoanThanhSanPham` lấy TK ngầm định qua `accountingService.GetTkNgamDinhByDonVi(..., EnumChungTu_Ma.PX, SanXuat, ...)`; giá vốn tính theo SP cha (`dicGiaVonTheoSanPhamCha`).
- BE `PhieuXuatController.cs:303` — `PhieuXuat_GetByIdDonHang` đặt `[AllowAnonymous]` (lưu ý bảo mật, không filter DonVi như các API khác).
- FE `phieu-xuat.component.ts:282` — `canEditPhieuXuatHoanThanh()` phải đồng bộ với guard BE; chặn sửa nếu phiếu có `idDonHang/idDonHangLogistic/idPhieuBanHang/idTransaction`.
- FE `phieu-xuat.component.ts:202` — grid phụ thuộc `JSON.parse(rs.data)["table1"]`/`["table2"]`; đổi shape API là vỡ.
- FE `phieu-xuat.component.ts:553` — `dongBoLaiSangKeToan()` dùng confirm dialog hardcode tiếng Việt (chưa qua `tsb.t`).
- FE `phieu-xuat.component.html:200,204,214` — nút Đồng bộ kế toán (HoanThanh) / Import imei (DangXuLy + imei) / Xóa (Nhap) hiển thị theo `maTinhTrang`.
