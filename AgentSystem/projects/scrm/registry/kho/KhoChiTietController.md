---
controller: KhoChiTietController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý tồn kho (quanly-kho-hangban) · Kiểm tra tồn kho (kiemtra-tonkho: báo cáo tồn kho đơn vị + kiểm tra tồn kho chi nhánh)"
muc_dich: "Xem / báo cáo tồn kho: tra cứu số lượng tồn theo kho-chi nhánh-sản phẩm, báo cáo tồn kho đơn vị, dự báo hết hàng, check số lượng đủ/không khi tạo phiếu. Controller CHỈ-ĐỌC (không insert/update KhoChiTiet)."
alias: [ton kho, tồn kho, kho chi tiet, quan ly ton kho, kiem tra ton kho, bao cao ton kho, inventory, stock, KhoChiTiet]
phu_thuoc: [SanPham, Kho, DichVuSanPham, SanPhamCauThanh, CauHinh, SanPhamImei, sp_ReportWarehouse_KhoChiTiet_Search, PRC_WarehouseProduct_BaoCaoTonKho, CauHinhService, CommonService, EcommerceConnectService, PartnerConnect/Bravo]
ripple: []   # CHỈ-ĐỌC — không phát sinh thay đổi dữ liệu / không bắn message đồng bộ. (Job đẩy số lượng sang ecommerce nằm trong service nhưng KHÔNG expose qua controller.)
---

# KhoChiTiet — Xem / Báo cáo tồn kho (read-only)

> Controller VIEW/REPORT thuần đọc. Mọi action đều `GET`/`POST` để **truy vấn**, không ghi `KhoChiTiet`. Hầu hết logic nặng nằm ở **stored procedure**; service chỉ build tham số (DonViId, ChiNhanh, phân quyền che giá nhập) rồi serialize DataSet.

## Hành động

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền | Hành vi đúng (expected) | Known-issue |
|---|---|---|---|---|---|---|
| 1 | Tìm tồn kho theo điều kiện (grid) | Lưới "Quản lý tồn kho" / nút Tìm kiếm | `POST api/KhoChiTiet/TimTheoDieuKien` | `KhoChiTiet_TimTheoDieuKien` (AD/KT/QLDV/QL-KHO...) | Bắt buộc `ListIdNhomSanPham` — rỗng → trả `{table1:[],table2:[{totalRecord:0}]}`. Gọi store `sp_ReportWarehouse_KhoChiTiet_Search` (param @IdDonVi,@IdChiNhanh,@IdKho,@KeyWord,@ListIdNhomSanPham(join ";"),@AnGiaNhap,@CheGiaNhap,@PageIndex,@PageSize). Keyword tìm tên SP/barcode/mã, trim, rỗng→null. Scope cố định theo DonViId + IdChiNhanh user. | Ẩn/che **giá nhập trung bình** quyết định bằng `@AnGiaNhap`/`@CheGiaNhap` đẩy xuống store: khác chi nhánh tổng → ẩn; role thuộc `IdRoles_Che_GiaNhap` → ẩn+che (admin/CSKH/adminTong bỏ qua). Log store nếu chạy > ngưỡng cấu hình. |
| 2 | Xuất Excel tồn kho | Nút "Xuất Excel" (Quản lý tồn kho) | `POST api/KhoChiTiet/ExportExcelKhoChiTiet` | `KhoChiTiet_ExportExcelKhoChiTiet` | Bắt buộc `ListIdNhomSanPham`. **KHÔNG dùng store** — query EF (join SanPham+Kho+SanPhamImei), filter theo DonViId+ChiNhanh, paging theo PageInfo, xuất file `QuanLyTonKho.xlsx` (GemBox). Cột: STT, Mã SP, SP, Tên kho, SL, SL chờ xuất, Giá nhập TB, SL dung tích, DS IMEI (IMEI TrangThai=2 cùng IdKho). | Export đi đường riêng (EF) khác đường lưới (store) → **rủi ro lệch số liệu / lệch logic ẩn giá** giữa grid và file. Không áp `@AnGiaNhap` ở export. |
| 3 | Lấy SL tồn 1 SP toàn chi nhánh | (internal/nút check) | `GET api/KhoChiTiet/TimSoLuongTheoId/{id}` | `KhoChiTiet_TimSoLuongTheoId` | `SUM(SoLuong)` theo DonViId+IdChiNhanh+IdSanPham (gộp mọi kho của chi nhánh). id rỗng → lỗi "Id là bắt buộc". | — |
| 4 | Kiểm tra đủ SL để tạo phiếu | (gọi khi tạo phiếu xuất/dịch vụ) | `POST api/KhoChiTiet/CheckSoLuongKho` | `KhoChiTiet_CheckSoLuongKho` | Theo `CauHinh.JsonSetting`: nếu `ChoPhepTonKhoAm=false` → so SL kho mặc định của chi nhánh với SL yêu cầu (qua API đối tác nếu `IsKiemTraTonKhoQuaApi`, else SUM KhoChiTiet). Xử lý quy đổi ĐVT, sản phẩm cấu thành (theo định lượng), SP đi kèm dịch vụ. Kho Bravo + cho phép check API đối tác → gọi PartnerConnect `GetInventory`. Thiếu → trả message lỗi. | Logic dài, nhiều nhánh; `SoLuong` null → lỗi "Số lượng là bắt buộc". `IsSanPhamCauThanh=true` bỏ qua check SP cha, chỉ check cấu thành. |
| 5 | Tồn kho 1 SP tại 1 kho | (popup phiếu) | `POST api/KhoChiTiet/GetTonKhoBySanPham` | `SBAuthorize` (any) | Trả `{SoLuong, GiaVon=GiaNhapTrungBinh}` theo DonViId+ChiNhanh+IdKho+IdSanPham; không thấy → `{SoLuong:0,GiaVon:0}`. | — |
| 6 | Báo cáo tồn theo SP, view theo chi nhánh | "Kiểm tra tồn kho chi nhánh" | `POST api/KhoChiTiet/GetTonKhoSanPham_ViewChiNhanh` | `KhoChiTiet_GetTonKhoSanPham_ViewChiNhanh` | Bắt buộc `IdSanPham`. Gọi store `PRC_WarehouseProduct_SoLuongTonKhoSanPham_TheoChiNhanh`. Luôn đi đường SQL (comment: cần thêm Giới hạn tồn kho nên không dùng Elastic). | — |
| 7 | Báo cáo tồn + dự báo hết hàng | "...(Dự báo hết hàng)" | `POST api/KhoChiTiet/GetTonKhoSanPham_ViewChiNhanh_DuBaoHetHang` | `KhoChiTiet_..._DuBaoHetHang` | Set `IsDuBaoHetHang=true` rồi gọi cùng handler #6. Nếu cấu hình `IsTinhGioiHanTonKhoTuDong` → bắt buộc `KieuKhoangNgay`, tính tuNgay/denNgay/soNgay đẩy vào store. | Thiếu `KieuKhoangNgay` khi bật giới hạn tự động → lỗi yêu cầu chọn kiểu khoảng ngày. |
| 8 | SL bán trong tuần theo SP | (báo cáo) | `POST api/KhoChiTiet/GetSoLuongBanTrongTuan_TheoSanPham` | `SBAuthorize` | Store `PRC_WarehouseProduct_SoLuongBanTrongTuan_TheoSanPham`. | — |
| 9 | SL bán trong khoảng TG theo SP | (báo cáo) | `POST api/KhoChiTiet/GetSoLuongBanTrongKhoangThoiGian_TheoSanPham` | `SBAuthorize` | Store `PRC_WarehouseProduct_SoLuongBanTrongKhoangThoiGian_TheoSanPham`. | — |
| 10 | SL giới hạn tồn kho theo SP | (báo cáo) | `POST api/KhoChiTiet/GetSoLuongGioiHanTonKho_TheoSanPham` | `SBAuthorize` | Store `PRC_WarehouseProduct_GioiHanTonKho_TheoSanPham`. | — |
| 11 | Báo cáo tồn kho & sản lượng bán | "Báo cáo tồn kho đơn vị" / nút Xem | `POST api/KhoChiTiet/ReportTonKhoDonVi` | `KhoChiTiet_ReportTonKhoDonVi` | ModelState valid. **Non-admin bị ép `IdChiNhanh = user.IdChiNhanh`** (chống xem chéo chi nhánh). Store `PRC_WarehouseProduct_BaoCaoTonKho`. | Ép chi nhánh là invariant bảo mật — đừng bỏ. |
| 12 | Check nhu cầu tồn kho theo chi nhánh | "Checklist tạo phiếu" | `POST api/KhoChiTiet/CheckNhuCauTonKhoTheoChiNhanh` | `KhoChiTiet_CheckNhuCauTonKhoTheoChiNhanh` | ModelState valid → `CheckNhuCauTonKhoTheoChiNhanh` (theo danh sách SP). | — |
| 13 | Xuất Excel báo cáo tồn kho đơn vị | Nút "Xuất Excel" (Báo cáo tồn kho) | `POST api/KhoChiTiet/ExportExcelReportTonKhoDonVi` | `KhoChiTiet_ExportExcelReportTonKhoDonVi` (chỉ AD-TONG/AD-CN/AD-MCN) | ModelState valid; non-admin ép `IdChiNhanh`. Xuất `BaoCaoTonKho.xlsx`. | Quyền hẹp hơn #11 (chỉ admin) — chủ ý. |
| 14 | Validate điều kiện trước khi export | (gọi trước nút Xuất Excel) | `POST api/KhoChiTiet/ExportExcelReportTonKhoDonVi_CheckDieuKien` | `SBAuthorize` | Non-admin ép `IdChiNhanh`. Validate: `OffSet>=0`, `PageSize>0`, `PageSize<=10000`. OK → trả `true`. | Chặn export > 10.000 bản ghi (guard hiệu năng). |
| 15 | Check tồn kho (public) | (website/đối tác) | `POST api/KhoChiTiet/CheckTonKho` | `AllowAnonymous` | Không cần đăng nhập; trả `CheckTonKhoResponse`. | **Anonymous endpoint** — lưu ý bề mặt tấn công, không truyền UserLogin. |
| 16 | DS kho theo IdKho + IdSp | (internal service-to-service) | `POST api/KhoChiTiet/GetDsKhoByIdKhoAndIdSp` | `PrivateSBAuthorize` | Gọi nội bộ giữa service (store `PRC_KhoChiTiet_GetByIdKho_And_ListIdSanPham`). | Private auth — không phải UI. |
| 17 | Tồn kho tất cả kho theo SP | (popup chọn kho) | `POST api/KhoChiTiet/GetTonKhoAllBySanPham` | `SBAuthorize` | Trả `List<KhoChiTietDto>` tồn theo SP mọi kho. | — |
| 18 | Tồn + giá nhập theo list SP | (popup phiếu nhiều SP) | `POST api/KhoChiTiet/GetTonKhoGiaNhapByListSanPham` | `SBAuthorize` | Map sang `GetTonKhoByListSanPham`. Lưu ý FE service trỏ URL `api/Common/GetTonKhoByListSanPham` (khác route controller). | **Lệch route FE↔BE**: BE route `KhoChiTiet/GetTonKhoGiaNhapByListSanPham`, FE gọi `Common/GetTonKhoByListSanPham`. |

## Quan hệ
- **Gọi sang:**
  - Stored proc (đường chính): `sp_ReportWarehouse_KhoChiTiet_Search` (#1), `PRC_WarehouseProduct_SoLuongTonKhoSanPham_TheoChiNhanh` (#6/#7), `PRC_WarehouseProduct_BaoCaoTonKho` (#11), `PRC_WarehouseProduct_SoLuongBan*`/`GioiHanTonKho` (#8-10), `PRC_KhoChiTiet_GetByIdKho_And_ListIdSanPham` (#16).
  - Service nội bộ: `CauHinhService` (GetCauHinhInit / che giá nhập / giới hạn tồn kho tự động), `CommonService` (CheckSoLuongHopLe, dropdown ĐVT), `EcommerceConnectService` (job đẩy SL — không qua controller), `ClientHelper` → `ReportService` (GetTimeToLog) + `PartnerConnect` (Bravo GetInventory).
- **Dữ liệu (entities):** đọc `KhoChiTiet` (SoLuong, SoLuongChoXuat, GiaNhapTrungBinh, SoLuongDungTichChiTiet, TenDungTich, IdKho/IdChiNhanh/IdDonVi/IdSanPham), join `SanPham`, `Kho`, `SanPhamImei`, `DichVuSanPham`, `SanPhamCauThanh`, `CauHinh`.
- **Ripple/đồng bộ:** KHÔNG có (read-only). Controller không sửa tồn kho. (Đồng bộ tồn → ecommerce do `JobSendProductQuantityToEcommerce` trong service xử lý, trigger bởi thay đổi ở controller GHI tồn — phiếu nhập/xuất/chuyển — không phải controller này.)

## Luồng xuyên module (nếu có)
- **Check tồn khi tạo phiếu (#4 CheckSoLuongKho):** module Phiếu (phieu-xuat / phieu-mua-hang / dịch vụ) gọi sang để chặn tạo phiếu khi SL không đủ → phụ thuộc `CauHinh.ChoPhepTonKhoAm`, `IsKiemTraTonKhoQuaApi`, kho Bravo (PartnerConnect). Đây là điểm KhoChiTiet ảnh hưởng nghiệp vụ ghi của module khác (gián tiếp, chỉ validate).
- **Đẩy tồn sang Ecommerce:** read tồn ở đây + `SanPhamThayDoiThongTin` → `EcommerceConnectService.SaveProductQuantity` (qua job, không expose endpoint).

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/KhoChiTietController.cs`
  - L196-202 `CheckTonKho` = `[AllowAnonymous]` (endpoint công khai, không UserLogin).
  - L204-210 `GetDsKhoByIdKhoAndIdSp` = `[PrivateSBAuthorize]` (chỉ gọi nội bộ).
  - L223-229 `GetTonKhoGiaNhapByListSanPham` route BE ≠ URL FE service (FE trỏ `Common/GetTonKhoByListSanPham`).
  - L127-130 / L156-159 / L177-179: non-admin bị ép `IdChiNhanh` ở Report/Export (invariant bảo mật, đừng bỏ).
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/KhoChiTietService.cs`
  - L133-220 `TimTheoDieuKienAsync`: phụ thuộc store `sp_ReportWarehouse_KhoChiTiet_Search`; ẩn/che giá nhập tính ở L160-182 rồi đẩy xuống store (`@AnGiaNhap`/`@CheGiaNhap`).
  - L704-838 `XuatExcelQuanLyTonKho`: export đi **EF (LINQ join)**, KHÔNG dùng store như grid → nguy cơ lệch số liệu & không áp logic ẩn giá nhập. Lưu ý khi sửa filter phải đồng bộ 2 đường.
  - L770 `SpreadsheetInfo.SetLicense(...)`: license GemBox hardcode trong code.
  - L868-... `GetTonKhoSanPham_ViewChiNhanh`: `if (true)` ép luôn dùng SQL, bỏ nhánh Elastic (do cần dữ liệu Giới hạn tồn kho).
