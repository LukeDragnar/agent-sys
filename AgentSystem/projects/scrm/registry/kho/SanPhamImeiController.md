---
controller: SanPhamImeiController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Danh sách IMEI (route /ds-imei, ViewSanphamImei) — lưới tra cứu IMEI/seri + tab Lịch sử thao tác + popup Sửa hạ cấp. Các action quét (nhập/xuất/chuyển/nhận) gọi từ popup edit phiếu nhập/xuất/chuyển kho + màn Thủ kho tác nghiệp."
muc_dich: "Quản lý vòng đời IMEI/seri của sản phẩm: tra cứu theo điều kiện, nhập/xóa IMEI thủ công, quét IMEI nhập/xuất/chuyển kho trực tiếp, nhận IMEI từ phiếu chuyển, kiểm tra hợp lệ IMEI khi tạo/chốt phiếu, đổi hạ cấp, xem lịch sử thao tác."
alias: [imei, seri, san pham imei, quan ly imei, ds imei, danh sach imei, quet imei, kiem tra imei, SanPhamImei]
auth: mixed
state_machine: [ChoNhapKho, DaNhapKho, DangChuyenKho, DaXuatKho]
deprecated: false
entities: [SanPhamImei, SanPhamImeiLichSu]
stored_procs: [PRC_SCRM_WarehouseProduct_SanPhamImei_Search, PRC_SCRM_WarehouseProduct_SanPhamImei_Search_Api]
depends: [SanPham, Kho, KhoChiTiet, PhieuNhap, PhieuNhapSanPham, PhieuXuat, PhieuXuatSanPham, PhieuChuyenKho, PhieuChuyenKhoSanPham, SanPhamChaHaCap, NhaCungCap, CauHinhService, CommonService, MongoDB(SanPhamImeiLichSu), MassTransit]
cross_module_in: [Sale/Order (CheckImeiBindSanPhamDonHang khi mapping imei vào SP đơn hàng), AuthenPartner/đối tác (TimTheoDieuKienPrivate, GetSanPhamImei)]
cross_module_out: [SanPhamImeiLichSu (Mongo, qua MassTransit EventSanPhamImeiLogMessage), KhoChiTiet (cộng tồn khi quét nhận phiếu chuyển đã duyệt)]
---

# SanPhamImeiController — Quản lý IMEI / seri sản phẩm

> Controller quản trị vòng đời 1 IMEI qua 4 trạng thái: `ChoNhapKho → DaNhapKho → DangChuyenKho → DaXuatKho` (có thể nhập lại sau khi xuất → `IsNhapLai=true`). Scope mặc định ép theo `DonViId` + `IdChiNhanh` của user. Mọi thao tác ghi đều **bắn message MassTransit `EventSanPhamImeiLogMessage`** → consumer ghi lịch sử vào Mongo (`SanPhamImeiLichSu`). Phần lớn là endpoint quét/check gọi từ popup phiếu, không phải nút riêng trên màn DS IMEI.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm IMEI theo điều kiện (lưới) | Lưới "Danh sách IMEI" / nút Tìm | `POST api/SanPhamImei/TimTheoDieuKien` | `SanPhamImei_TimTheoDieuKien` | public | luôn | Gọi SP `PRC_SCRM_WarehouseProduct_SanPhamImei_Search` (param @IdDonVi,@IdChiNhanh,@KhoId,@IdNhomSanPham,@IdSanPhamCha,@IdSanPham,@TrangThaiImei,@Keyword,@TuNgay,@DenNgay,@IdNhaCungCap,@PageIndex,@PageSize). `TrangThaiImei` ở FE map 1=DaNhapKho,2=ChoNhapKho,3=DaXuatKho,4=DangChuyenKho. Map text trạng thái/kiểu nhập/trạng thái SP từ enum; ghép `SpHaCapGhiChu (x% - y%)`. Trả table[0]=data, table[1]=total. | SP | Scope cố định DonViId+IdChiNhanh (không xem chéo). Chỉ log câu SP khi user IsSuperUser/IsCSKH. |
| 2 | Nhập kho IMEI (thủ công) | Nút "Nhập kho" trong popup phiếu nhập | `POST api/SanPhamImei/NhapKhoImei` | `SanPhamImei_NhapKhoImei` | public | IMEI đang `ChoNhapKho` | Tìm IMEI theo DonVi+ChiNhanh (+IdPhieu nếu có). Chỉ cho khi `TrangThai=ChoNhapKho` → set `DaNhapKho`. Log `NhapKhoImei`. | EF | IMEI rỗng / không thấy / sai trạng thái → message lỗi. |
| 3 | Xóa IMEI | Nút "Xóa" (lưới imei phiếu) | `POST api/SanPhamImei/XoaImei` | `SanPhamImei_XoaImei` | public | phiếu nhập/chuyển CHƯA hoàn thành | Bắt buộc Imei + IdSanPham. Nếu IMEI gắn phiếu nhập/chuyển **đã HoanThanh → cấm xóa**. Xóa trong transaction (ExecutionStrategy) + trừ `PhieuNhapSanPham.SoLuongImeiDaImport -= 1`. Log `XoaImei`. | EF (transaction) | Message success ghi nhầm "Đồng bộ quyền thành công" (L351). |
| 4 | Nhập kho IMEI trực tiếp (quét) | Quét IMEI ở popup/thủ kho nhập | `POST api/SanPhamImei/NhapKhoImeiTrucTiep` | `SanPhamImei_NhapKhoImeiTrucTiep` | public | phiếu nhập tồn tại, SP không tích sinh IMEI tự động | Validate IMEI (không tiếng Việt/space), IdPhieu/IdSanPham/IdPhieuNhapSanPham. Chặn nếu IMEI `DaNhapKho` / phiếu chuyển chưa hoàn thành; `DaXuatKho` cho nhập lại (`IsNhapLai=true`). Chặn phiếu nhập sinh từ phiếu chuyển. Chặn vượt SL cần quét. Update IMEI cũ hoặc insert mới (snapshot NCC từ phiếu). Quét IMEI cuối + cấu hình `TuDongChuyenTrangThaiPhieuKhiQuetImei` → set phiếu `HoanThanh`. Log `NhapKhoImeiTrucTiep`. | EF | Nhánh quyết "IMEI cuối" (L514) so `SoLuong-1` chỉ đúng khi `sanPhamImeiAny != null`; tạo IMEI mới không bao giờ auto-hoàn-thành phiếu. |
| 5 | Xuất kho IMEI trực tiếp (quét) | Quét IMEI ở popup phiếu xuất | `POST api/SanPhamImei/XuatKhoImeiTrucTiep` | `SanPhamImei_XuatKhoImeiTrucTiep` | public | IMEI thuộc phiếu, chưa vượt SL | Bắt buộc Imei/IdPhieu/IdSanPham. Chặn vượt SL imei cần quét. Set IMEI `DaXuatKho`, lưu `IdPhieuNhapCu`=phiếu cũ, `IdPhieu`=phiếu xuất, `IsQuetXuatKhoTrucTiep=true`. Transaction update IMEI + copy Lô/NSX/HSD/SĐK sang PhieuXuatSanPham. Log `XuatKhoImeiTrucTiep`. | EF (transaction) | catch nuốt exception, không rollback-báo lỗi rõ (L1154) → quét lỗi có thể trả success giả. |
| 6 | Chuyển kho IMEI trực tiếp (quét) | Quét IMEI ở popup phiếu chuyển | `POST api/SanPhamImei/ChuyenKhoImeiTrucTiep` | `SanPhamImei_ChuyenKhoImeiTrucTiep` | public | IMEI `DaNhapKho`, phiếu chuyển hợp lệ | Đánh dấu IMEI `DangChuyenKho`, gắn phiếu chuyển. Log `ChuyenKhoImeiTrucTiep`. Cặp với #8 ở đầu nhận. | EF | Permission name trong attribute ghi nhãn "Xuất kho imei trực tiếp" (copy nhầm, L135). |
| 7 | Nhận IMEI từ phiếu chuyển (quét đầu đến) | Quét IMEI nhận ở kho đến | `POST api/SanPhamImei/QuetImeiNhanTuPhieuChuyen` | `SanPhamImei_QuetImeiNhanTuPhieuChuyen` | public | IMEI `DangChuyenKho`; phiếu `HoanThanh`; không bật "duyệt đồng thời nhập kho" | Bắt buộc Imei/IdPhieu. IMEI phải `DangChuyenKho`. Chặn nếu phiếu bật `DuyetPhieuChuyenKhoDongThoiNhapKhoImei` (auto khi duyệt). Non-admin phải là quản kho của kho đến. **Chỉ cộng `KhoChiTiet` +1 khi phiếu HoanThanh + DaDuyet** (chưa duyệt → chỉ đánh flag). Log `QuetImeiNhanTuPhieuChuyen`. | EF + tính `KhoChiTiet` | Điểm DUY NHẤT controller ghi tồn KhoChiTiet → cẩn trọng double-count nếu re-scan. |
| 8 | Đổi hạ cấp IMEI | Popup "Sửa hạ cấp" (view-sanpham-imei-edit-hacap) | `POST api/SanPhamImei/ThayDoiHaCapImei` | `SanPhamImei_ThayDoiHaCapImei` | public | IMEI `DaNhapKho` | Bắt buộc IMEI `DaNhapKho`. Gán `IdSanPhamChaHaCap`, `GiaBanTheoSpHaCap`, parse `SanPhamChaHaCapJson` → set `GiaTriHaCapTu/Den`, `SpHaCapGhiChu`. Log `SuaHaCapSanPham`. | EF | — |
| 9 | Lịch sử thao tác IMEI | Tab "Lịch sử" (view-sanpham-imei-lich-su) | `POST api/SanPhamImei/LichSuThaoTacImei` | `SBAuthorize` (any) | luôn | Đọc Mongo `SanPhamImeiLichSu` filter DonViId + Imei (+KieuLoai), sort NgayTao desc, paging. Map ActionName/KieuLoaiName từ enum. | Mongo | Filter ẩn lịch sử-bị-xóa theo role đã **comment off** (L2001-2005) → mọi role thấy cả entry xóa. |
| 10 | Check IMEI hợp lệ (theo IMEI) | (gọi khi quét/validate) | `POST api/SanPhamImei/KiemTraImeiV2` · `GET .../KiemTraImei/{imei}` | `SBAuthorize` (any) | luôn | Tìm IMEI theo DonVi → lấy Kho → chi nhánh → gọi chéo `CommonService.LayTenChiNhanh`. Trả `{TenChiNhanh, TenKho}`. V2 (POST) xử lý IMEI có ký tự đặc biệt ("/") không qua URL được. | EF + CommonService | GET không truyền được IMEI chứa "/" → phải dùng V2. |
| 11 | Check IMEI + kho/SP (true/false) | (validate quét nhập/xuất) | `POST api/SanPhamImei/CheckSanPhamImei/{imei}` · `.../CheckSanPhamImeiAndKho` · `.../CheckSanPhamImeiAndKhoVer1` | `SBAuthorize` (any) | luôn | Chuỗi check: tồn tại → đúng kho → đúng SP → không `DangChuyenKho` → phiếu (nhập/chuyển/xuất) hoàn thành đúng kỳ vọng. AndKho trả bool; Ver1 trả `GetSpImeiDto` + tính `GiaTien` theo bảng hạ cấp nếu `IsSanPhamHaCap`. | EF | Logic 3 method gần trùng (copy-paste); sửa rule phải đồng bộ cả 3 + #13. |
| 12 | Lấy thông tin IMEI (chi tiết) | (popup phiếu xuất/bán) | `POST api/SanPhamImei/GetSanPhamImeiAndKho` | `SBAuthorize` (any) | luôn | Như #11 nhưng trả `GetSpImeiDto` đầy đủ. Khi không thấy theo chi nhánh → kiểm tra lại toàn đơn vị: nếu IMEI `DangChuyenKho` trả message rõ "đang chuyển kho" (vì lúc chuyển `IdChiNhanh` đã đổi sang chi nhánh đích). | EF | Quirk filter chi nhánh khi `DangChuyenKho` (L2159) — đừng bỏ fallback. |

## Endpoint nội bộ (private / service-to-service)
- [internal `PrivateSBAuthorize`] `POST TimTheoDieuKienPrivate/{idDonVi:Guid}` — đối tác/AuthenPartner tra IMEI theo đơn vị, scope ép `DonViId=idDonVi`. Gọi SP `PRC_SCRM_WarehouseProduct_SanPhamImei_Search_Api`, trả `SanPhamImeiSearchApiDto`.
- [internal `PrivateSBAuthorize`] `POST GetSanPhamImei` — lấy chi tiết IMEI (kèm kho mặc định chi nhánh, cờ IsPhieuNhap/Chuyen/Xuat) cho service khác.
- [internal `PrivateSBAuthorize`] `POST CheckImeiBindSanPhamDonHang` — module **Sale/Order** gọi sang để xác thực IMEI hợp lệ trước khi map vào sản phẩm của đơn hàng (cùng chuỗi check kho/SP/phiếu như #11, trả `GetSpImeiDto` + GiaTien hạ cấp).

## Quan hệ
- **Gọi sang:** SP `PRC_SCRM_WarehouseProduct_SanPhamImei_Search(_Api)` (lưới); `CommonService.LayTenChiNhanh` (KiemTraImei, gọi chéo Core); `CauHinhService.GetAsync` (cờ `TuDongChuyenTrangThaiPhieuKhiQuetImei`); `IPublishEndpoint.Publish(EventSanPhamImeiLogMessage)` (mọi thao tác ghi → lịch sử).
- **Dữ liệu (entities):** ghi/đọc `SanPhamImei` (Imei, TrangThai, IdKho, IdPhieu, IdPhieuSanPham, IdPhieuNhapCu, IsNhapLai, hạ cấp, snapshot NCC). Đọc `PhieuNhap/PhieuXuat/PhieuChuyenKho(+SanPham)`, `SanPham`, `Kho`, `SanPhamChaHaCap`, `NhaCungCap`. Ghi `KhoChiTiet` (chỉ #7). Lịch sử ghi Mongo `SanPhamImeiLichSu`.
- **Ripple / đồng bộ:** MassTransit `EventSanPhamImeiLogMessage` → `EventLogsSanPhamImeiLichSuConsumer` → `CreateLogsSanPhamImei` ghi Mongo (mọi action #2-#8). Quét nhận phiếu chuyển đã duyệt (#7) cộng tồn `KhoChiTiet` kho đến.

## Luồng xuyên module
- **Chuyển kho IMEI:** kho đi quét `ChuyenKhoImeiTrucTiep` (#6, IMEI→DangChuyenKho) → phiếu chuyển HoanThanh+DaDuyet → kho đến quét `QuetImeiNhanTuPhieuChuyen` (#7, IMEI→DaNhapKho + KhoChiTiet kho đến +1). Nếu phiếu bật `DuyetPhieuChuyenKhoDongThoiNhapKhoImei` thì IMEI auto-nhập khi DUYỆT phiếu (không qua #7 thủ công).
- **Map IMEI vào đơn hàng (Sale):** Order gọi `CheckImeiBindSanPhamDonHang` (private) để validate IMEI trước khi gắn vào SP đơn → ảnh hưởng xuất kho/chốt đơn (action log `QuetImeiMappingSanPhamTrongDonHang`, `ThanhToanDonHang`... do flow đơn hàng phát, không qua controller này).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/SanPhamImeiController.cs`
  - L135-138 `ChuyenKhoImeiTrucTiep`: attribute label ghi nhầm "Xuất kho imei trực tiếp" (copy-paste, key vẫn đúng `SanPhamImei_ChuyenKhoImeiTrucTiep`).
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/SanPhamImeiService.cs`
  - L351 `XoaImei`: message success ghi nhầm "Đồng bộ quyền thành công".
  - L514-517 `NhapKhoImeiTrucTiep`: nhánh tính "IMEI cuối cùng" chỉ chạy khi `sanPhamImeiAny != null` → insert IMEI mới không tự hoàn-thành phiếu dù đủ SL.
  - L1154-1157 `XuatKhoImeiTrucTiep`: catch nuốt exception, không trả lỗi → có thể trả success giả khi transaction fail.
  - L2001-2005 `LichSuThaoTacImeiAsync`: filter ẩn entry IsDelete theo role bị comment off → mọi role thấy lịch sử bị xóa.
  - L11/13 method `CheckSanPhamImei`/`CheckSanPhamImeiAndKho(Ver1)`/`GetSanPhamImeiAndKho`/`CheckImeiBindSanPhamDonHang`: chuỗi check kho/SP/phiếu gần trùng nhau (4-5 bản copy) — sửa rule phải đồng bộ tất cả.
