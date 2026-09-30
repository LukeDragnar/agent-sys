---
controller: SinhImeiTuDongController
module: kho
service: WarehouseProduct
man_hinh_ui: "WarehouseProduct > màn 'Sinh Imei' (route sinh-imei-tu-dong): nút Thêm mới / In imei / lưới + Xóa + lọc date-range/keyword/Trạng thái in. Màn in tem (route /in-don/sinh-imei) đọc cấu hình in. Cũng được gọi từ popup Phiếu nhập (sinh imei tự động khi nhập kho)."
muc_dich: "Sinh mã IMEI/seri tự động cho sản phẩm: tạo IMEI (tiền tố + ddMMyy + số tự tăng theo đơn vị), gắn lô-NSX-HSD-số đăng ký-giá bán, đánh dấu đã in, xoá, lưu cấu hình in (Mongo), xuất Excel để gán sản phẩm."
alias: [sinh imei, sinh imei tu dong, imei tu dong, seri tu dong, sinh ma imei, tao imei, in imei, SinhImeiTuDong]
auth: mixed
state_machine: [ChuaIn, DaIn]   # TrangThaiIn: 1=ChưaIn, 2=ĐãIn
deprecated: false
entities: [SinhImeiTuDong, SanPhamImei, SanPham, PhieuNhap]
stored_procs: [PRC_SCRM_WarehouseProduct_SinhImeiTuDong_Search]
depends: [PhieuNhap, SanPhamImei, SanPham, PRC_SCRM_WarehouseProduct_SinhImeiTuDong_Search, MongoDB(SinhImeiTuDongCauHinhInDto), GemBox.Spreadsheet]
cross_module_in: [PhieuNhapController]   # PhieuNhapService gọi ThemMoi (IsGenTuDong=true + IdPhieuNhap) để bulk-sinh IMEI khi nhập kho tích "Sinh IMEI tự động"
cross_module_out: []
---

# SinhImeiTuDongController — Sinh IMEI / seri tự động

> Controller CRUD nhẹ trên bảng `SinhImeiTuDong`. Mỗi bản ghi = 1 mã IMEI (`Imei` = tiền tố + ddMMyy + SoTuTang PadLeft 6) gắn thông tin Lô/NSX/HSD/Số đăng ký/Giá bán. Scope mọi thao tác theo `DonViId` (+`IdChiNhanh` khi tạo) của user. TienIch: `TI_Kho`. Cấu hình in tem lưu Mongo (`SinhImeiTuDongCauHinhInDto`), không phải SQL.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (lưới) | Lưới "Sinh Imei" / nút Tìm kiếm + lọc date-range + keyword + Trạng thái in | `POST api/SinhImeiTuDong/TimTheoDieuKien` | `SinhImeiTuDong_TimTheoDieuKien` | public | luôn hiện | Gọi store `PRC_SCRM_WarehouseProduct_SinhImeiTuDong_Search` (param @IdDonVi,@IdChiNhanh,@TrangThaiIn,@Keyword,@PageIndex,@PageSize,@TuNgay,@DenNgay). Scope cố định @IdDonVi + @IdChiNhanh của user. Table[0]=data (SinhImeiTuDongResponseDto), Table[1][0][0]=total. FE mặc định lọc trangThaiIn=1 (Chưa in), date-range 7 ngày gần nhất. | SP | `LogCommandV2` nằm trong nhánh `if(true || ...)` (L109) → mọi lần search đều log command store, không phụ thuộc super/CSKH. |
| 2 | Thêm mới IMEI | Nút "Thêm mới" → popup "Thêm mới imei" | `POST api/SinhImeiTuDong/ThemMoi` | `SinhImeiTuDong_ThemMoi` | public | luôn hiện | Validate (theo thứ tự): TienTo bắt buộc, ≤2 ký tự, **chỉ là số `^\d{1,2}$`**; SoLuong bắt buộc >0 và ≤10000; NgaySanXuat ≤ NgayHetHanSanPham; SoDangKy ≤255; LoSanPham ≤255. Sinh `Imei` qua `GenerateIMEI` (tiền tố + ddMMyy + SoTuTang PadLeft 6, >999999 thì bỏ pad), `SoTuTang`=MAX(SoTuTang) theo IdDonVi +1 (tăng dần trong vòng lặp). `TrangThaiIn=ChuaIn`. BulkInsert. Trả list IMEI đã sinh. | EF (BulkInsert) | TimeStart null → set Now. `IsGenTuDong=true` → cộng +4ms NgayTao mỗi bản ghi (dùng khi bulk từ phiếu nhập). SoTuTang reset theo NGÀY KHÔNG đúng — MAX lấy theo IdDonVi không lọc ngày (khác SinhLoDate). |
| 3 | Lưu / cập nhật cấu hình in tem | Màn in tem `/in-don/sinh-imei`: "Lưu cấu hình" / popup tạo cấu hình | `POST api/SinhImeiTuDong/ThemMoiCauHinhIn` · `.../CapNhatCauHinhIn` · `.../DsCauHinh` | `SBAuthorize` (any đăng nhập) | public | màn in tem | Lưu/đọc cấu hình in (khổ giấy, cỡ chữ, hiện Lô/NSX/HSD/SĐK/Giá) vào **Mongo** `SinhImeiTuDongCauHinhInDto` theo DonVi+ChiNhanh. DsCauHinh: cấu hình của mình (IsOfMe=true, sort NgayCapNhat desc) đứng trước, của người khác sau. CapNhat: nếu không phải người tạo → FE mở popup tạo mới thay vì update. | Mongo | Không có key riêng — bất kỳ user đăng nhập tạo/sửa được cấu hình in. |
| 4 | In imei (đánh dấu đã in) | Nút "In imei" (tick dòng → mở tab `/in-don/sinh-imei`) | `POST api/SinhImeiTuDong/InDon` | `SBAuthorize()` (any đăng nhập) | cần tick ≥1 dòng | Nhận `List<string> imeis`. Lấy bản ghi theo `Imei IN imeis AND IdDonVi=user`. Rỗng → "Không tìm thấy dữ liệu". Set `TrangThaiIn=DaIn(2)` + MarkDirty, UpdateAsync. Trả true. | EF | Render tem (TSPL/print-js) ở FE; BE chỉ đổi trạng thái in. FE gọi InDon **không await/không check kết quả** (fire-and-forget, L329/333) → trạng thái in có thể không cập nhật mà UI không báo lỗi. |
| 5 | Xóa IMEI | Icon thùng rác trên từng dòng | `GET api/SinhImeiTuDong/Xoa/{id:Guid}` | `SinhImeiTuDong_Xoa` | public | mỗi dòng lưới | `DeleteAsync(id)` (BaseService) — xóa cứng theo Id. Trả kết quả. | EF | **Không** check IdDonVi và **không** chặn khi IMEI đã gắn phiếu nhập hoàn thành (khác SinhLoDate có chặn) → có thể xóa IMEI của phiếu đã chốt. |
| 6 | Xuất Excel gán sản phẩm | (gọi từ lưới chọn dòng) | `POST api/SinhImeiTuDong/ExportExcelGanSanPham` · `.../ExportExcelGanSanPhamFromSanPhamImei` | `SinhImeiTuDong_ExportExcelGanSanPham` · `..._ExportExcelGanSanPhamFromSanPhamImei` | public | chọn ≥1 dòng | Nhận `List<Guid> ids`. Load template `Template/ImportPhieuNhap_Imei.xlsx` (GemBox). Bản 1: từ `SinhImeiTuDong.Id` → join `SanPhamImei.Imei` → fill IMEI + Mã/Tên SP đã gán + danh mục SP vào sheet `danh-sach`/`danh-muc-san-pham`. Bản FromSanPhamImei: id là `SanPhamImei.Id`. Rỗng → "Không có dữ liệu để xuất". Trả file `DanhSachImei_GanSanPham.xlsx`. | EF + GemBox | License GemBox hardcode trong service (L332/440). Không thấy caller trong FE repo hiện tại → endpoint gọi từ nơi khác / chưa nối FE. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
1. **TienTo** — bắt buộc, độ dài ≤2, regex `^\d{1,2}$` (chỉ chữ số). Sai → trả lỗi tương ứng, không sinh mã.
2. **SoLuong** — bắt buộc, >0, ≤10000. Vi phạm → "Số lượng không hợp lệ!" / "Số lượng không được vượt quá 10000!".
3. **NgaySanXuat ≤ NgayHetHanSanPham** — vi phạm → "Ngày hết hạn phải lớn hơn ngày sản xuất!". (FE thêm validator chặn HSD ≤ NSX.)
4. **Số tự tăng (`SoTuTang`)** — lấy `MAX(SoTuTang)` theo `IdDonVi` (KHÔNG lọc ngày), +1, tăng dần trong vòng lặp sinh SoLuong mã. Format `Imei`: `{TienTo}{ddMMyy}{SoTuTang}`, SoTuTang PadLeft(6,'0') khi ≤999999, ngược lại in nguyên. → IMEI **không reset theo ngày** (chuỗi số liên tục theo đơn vị), khác SinhLoDate (reset theo ngày).
5. **KHÔNG check trùng IMEI** — không validate IMEI đã tồn tại (không có cờ kiểu IsBoQuaCheckTrungLoDate). Dựa hoàn toàn vào SoTuTang để duy nhất → race condition nếu sinh đồng thời (MAX không khóa) có thể trùng IMEI.

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có endpoint `PrivateSBAuthorize`. Việc bulk-sinh IMEI từ phiếu nhập đi qua **DI nội bộ** (`ISinhImeiTuDongService.ThemMoi`), không qua HTTP — xem "Luồng xuyên module".
- FE service `sinhImeiTuDong.service.ts` còn hàm `GetListImeiByPhieu(idPhieu)` trỏ `GET api/SinhImeiTuDong/GetListImeiByPhieu/{idPhieu}` **không tồn tại** ở controller → dead code FE (404 nếu gọi).

## Quan hệ
- **Gọi sang:** stored proc `PRC_SCRM_WarehouseProduct_SinhImeiTuDong_Search` (#1); `Helper.LogCommandV2` (log store); Mongo `SinhImeiTuDongCauHinhInDto` (#3); EFCore BulkInsert (#2); `_context.SanPhamImei`/`_context.SanPham` (export #6); GemBox.Spreadsheet (#6). Dùng SqlSugar (`_dbDapper`) cho store, EFCore cho EF.
- **Dữ liệu (entities):** ghi/đọc `SinhImeiTuDong` (Imei, TrangThaiIn, TienTo, SoTuTang, LoSanPham, NgaySanXuat, NgayHetHanSanPham, SoDangKy, GiaBan, IdPhieuNhap, IdDonVi/IdChiNhanh); đọc `SanPhamImei` + `SanPham` (join để export gán SP). Cấu hình in ở Mongo.
- **Ripple / đồng bộ:** KHÔNG có event/consumer cross-service. Side-effect chỉ trong bảng SinhImeiTuDong + Mongo cấu hình in.

## Luồng xuyên module
- **Sinh IMEI tự động khi nhập kho:** popup Phiếu nhập tích "Sinh IMEI tự động" (`PhieuNhap.SinhImeiTuDong=true`, áp xuống từng dòng SP IMEI tại `PhieuNhapService` L869) → khi xử lý phiếu, `PhieuNhapService` (L1879-1897) tạo `ThemMoiImeiTuDongDto{ TienTo=GenerateTienTo(), IsGenTuDong=true, IdPhieuNhap, Lô/NSX/HSD/SĐK/GiáBán từ phiếu }` rồi gọi DI `_sinhImeiTuDongService.ThemMoi(...)` để bulk-sinh IMEI gắn vào phiếu nhập (lỗi → throw). Đây là cạnh vào từ `PhieuNhapController`/luồng nhập kho. Mỗi mã +4ms NgayTao để bảo đảm thứ tự/duy nhất.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/SinhImeiTuDongService.cs:109-112` `LogCommandV2` trong nhánh `if (true || ...)` → luôn log store mỗi lần search (điều kiện super/CSKH bị vô hiệu).
- `...Service/Generated/SinhImeiTuDongService.cs:126-158` `GenerateIMEI`: MAX SoTuTang theo `IdDonVi` (KHÔNG lọc ngày) → chuỗi số liên tục, không reset theo ngày; race condition khi sinh đồng thời (MAX không khóa) → khả năng trùng `Imei`. Biến `randomDigit` (L142) khai báo nhưng không dùng.
- `...Service/Generated/SinhImeiTuDongService.cs` `DeleteAsync` (qua BaseService, controller L83): xóa cứng theo Id, **không** scope IdDonVi và **không** chặn khi IMEI gắn phiếu nhập đã HoanThanh (khác `SinhLoDateTuDong.Xoa` có chặn) → rủi ro xóa IMEI của chứng từ đã chốt + xóa chéo đơn vị.
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/SinhImeiTuDongController.cs:48,55,63,70` `ThemMoiCauHinhIn`/`DsCauHinh`/`CapNhatCauHinhIn`/`InDon` chỉ `[SBAuthorize]` (không key riêng) → bất kỳ user đăng nhập thao tác được.
- FE `services/warehouseproduct/sinhImeiTuDong.service.ts:48-51` `GetListImeiByPhieu` trỏ endpoint **không tồn tại** ở controller → dead code FE (404 nếu gọi).
- FE `pages/in-don/in-don-sinh-imei/in-don-sinh-imei.component.ts:329,333` gọi `InDon(...)` **không await / không kiểm tra kết quả** → đánh dấu "Đã in" có thể fail âm thầm.
