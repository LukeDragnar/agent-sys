---
controller: SinhLoDateTuDongController
module: kho
service: WarehouseProduct
man_hinh_ui: "WarehouseProduct > màn 'Sinh lô date' (route sinh-lo-date-tu-dong): nút Thêm mới / In lô date / lưới + Xóa. Cũng được gọi từ popup Phiếu nhập (sinh lô date tự động khi nhập kho)."
muc_dich: "Sinh mã lô date / hạn sử dụng tự động cho sản phẩm: tạo mã (tiền tố + ddMMyy + số tự tăng theo đơn vị/ngày), gắn lô-NSX-HSD-số đăng ký-giá bán, đánh dấu đã in, xoá lô."
alias: [sinh lo date, sinh lô date, lo date tu dong, lô date, han su dung, HSD, NSX, sinh ma lo date, lot date, sinh imei lo date]
auth: mixed
state_machine: [ChuaIn, DaIn]   # TrangThaiIn: 1=ChưaIn, 2=ĐãIn
deprecated: false
entities: [SinhLoDateTuDong, PhieuNhap]
stored_procs: [PRC_SCRM_WarehouseProduct_SinhLoDateTuDong_Search]
depends: [PhieuNhap, PRC_SCRM_WarehouseProduct_SinhLoDateTuDong_Search, StringHelper]
cross_module_in: [PhieuNhapController]   # popup phiếu nhập gọi ThemMoi (IsGenTuDong) để sinh lô date tự động khi nhập kho
cross_module_out: []
---

# SinhLoDateTuDongController — Sinh lô date / hạn sử dụng tự động

> Controller CRUD nhẹ trên bảng `SinhLoDateTuDong`. Mỗi bản ghi = 1 mã lô date (`Ma` = tiền tố + ddMMyy + số tự tăng) gắn thông tin Lô/NSX/HSD/Số đăng ký/Giá bán. Scope mọi thao tác theo `DonViId` của user. TienIch: `TI_Kho`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (lưới) | Lưới "Sinh lô date" / nút Tìm kiếm + lọc date-range + keyword + Trạng thái in | `POST api/SinhLoDateTuDong/TimTheoDieuKien` | `SinhLoDateTuDong_TimTheoDieuKien` | public | luôn hiện | Gọi store `PRC_SCRM_WarehouseProduct_SinhLoDateTuDong_Search` (param @IdDonVi,@IdChiNhanh,@TrangThaiIn,@Keyword,@PageIndex,@PageSize,@TuNgay,@DenNgay). Scope cố định @IdDonVi + @IdChiNhanh của user. Table[0]=data, Table[1][0][0]=total. Giữ `TenSanPhamGoc`=tên gốc rồi convert `TenSanPham` sang **không dấu** (StringHelper.utf8Convert1). | SP | `LogCommandV2` luôn chạy (nhánh `if(true || ...)`) → mọi lần search đều log command store, không phụ thuộc super/CSKH. |
| 2 | Thêm mới lô date | Nút "Thêm mới" → popup "Thêm mới lô date" | `POST api/SinhLoDateTuDong/ThemMoi` | `SinhLoDateTuDong_ThemMoi` | public | luôn hiện | Validate (theo thứ tự): TienTo bắt buộc, ≤2 ký tự, **chỉ là số `^\d{1,2}$`**; NgaySanXuat ≤ NgayHetHanSanPham; SoDangKy ≤255; LoSanPham bắt buộc + ≤255. Nếu `IsBoQuaCheckTrungLoDate=false` → chặn nếu (IdDonVi, LoSanPham) đã tồn tại ("Lô sản xuất đã tồn tại!"). Sinh `Ma` qua `GenerateIMEI` (tiền tố + ddMMyy + SoTuTang PadLeft 3, >999 thì bỏ pad), `SoTuTang`=MAX trong ngày theo IdDonVi +1. `TrangThaiIn=ChuaIn`. BulkInsert. Trả list mã đã sinh. | EF (BulkInsert) | TimeStart null → set Now. `IsGenTuDong=true` → cộng +4ms mỗi bản ghi để NgayTao khác nhau (dùng khi bulk từ phiếu nhập). 1 lần gọi sinh **đúng 1 mã** (GenerateIMEI(...,soLuong:1)). |
| 3 | In lô date (đánh dấu đã in) | Nút "In lô date" (chọn dòng tick → in) | `POST api/SinhLoDateTuDong/InDon` | `SBAuthorize()` (any đăng nhập) | public | cần tick ≥1 dòng | Nhận `List<string> imeis` (mã đã chọn). Lấy bản ghi theo `Ma IN imeis AND IdDonVi=user`. Rỗng → "Không tìm thấy dữ liệu". Set `TrangThaiIn=DaIn(2)` + MarkDirty, UpdateAsync. Trả true. | EF | Chỉ đổi trạng thái in, **không** sinh file in ở BE — render phiếu in nằm ở FE. |
| 4 | Xóa lô date | Icon thùng rác trên từng dòng | `GET api/SinhLoDateTuDong/Xoa/{id:Guid}` | `SinhLoDateTuDong_Xoa` | public | mỗi dòng lưới | Lấy theo (Id, IdDonVi=user); null → "Không tìm thấy data". Nếu có `IdPhieuNhap` → tìm `PhieuNhap`; nếu phiếu `DmKhoMaTinhTrang=HoanThanh` → chặn "Phiếu nhập đã hoàn thành. Không được xóa lô date". Ngược lại Delete. Trả true. | EF | Xóa cứng (`DeleteAsync`), không soft-delete. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
1. **TienTo** — bắt buộc, độ dài ≤2, regex `^\d{1,2}$` (chỉ chữ số). Sai bất kỳ → trả lỗi tương ứng, không sinh mã.
2. **NgaySanXuat ≤ NgayHetHanSanPham** — vi phạm → "Ngày hết hạn phải lớn hơn ngày sản xuất!".
3. **Trùng lô** — chỉ check khi `IsBoQuaCheckTrungLoDate=false`: cặp (IdDonVi, LoSanPham) đã có → chặn. Cờ true → bỏ qua check (dùng khi sinh hàng loạt theo phiếu nhập).
4. **Số tự tăng (`SoTuTang`)** — reset theo NGÀY + theo ĐƠN VỊ: lấy MAX(SoTuTang) của IdDonVi với `NgayTao.Date == Now.Date`, +1. Format `Ma`: `{TienTo}{ddMMyy}{SoTuTang}`, SoTuTang PadLeft(3,'0') khi ≤999, ngược lại in nguyên.
5. **Chặn xóa khi phiếu nhập hoàn thành** — invariant: lô date đã gắn `IdPhieuNhap` mà phiếu `HoanThanh` thì không cho xóa (giữ toàn vẹn chứng từ nhập).

## Quan hệ
- **Gọi sang:** stored proc `PRC_SCRM_WarehouseProduct_SinhLoDateTuDong_Search` (#1); repo `PhieuNhap` (kiểm tra trạng thái phiếu khi xóa #4); `StringHelper.utf8Convert1` (bỏ dấu tên SP); `Helper.LogCommandV2` (log store). Dùng SqlSugar (`_dbDapper`) cho store + EFCore BulkInsert.
- **Dữ liệu (entities):** ghi/đọc `SinhLoDateTuDong` (Ma, TrangThaiIn, TienTo, SoTuTang, LoSanPham, NgaySanXuat, NgayHetHanSanPham, SoDangKy, GiaBan, IdPhieuNhap, TenSanPham, MaSanPham, IdDonVi/IdChiNhanh); đọc `PhieuNhap` (DmKhoMaTinhTrang).
- **Ripple / đồng bộ:** KHÔNG có event/consumer cross-service. Side-effect chỉ trong bảng SinhLoDateTuDong.

## Luồng xuyên module
- **Sinh lô date tự động khi nhập kho:** popup Phiếu nhập (FE `popup-phieu-nhap-edit`, `sinh-lodate-phieumuahang`) gọi `ThemMoi` với `IsGenTuDong=true` + `IdPhieuNhap` để bulk-sinh lô date gắn vào phiếu nhập → đây là điểm `PhieuNhapController`/luồng nhập kho đẩy dữ liệu vào controller này (cạnh vào). Mỗi mã +4ms NgayTao để bảo đảm thứ tự/duy nhất.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/SinhLoDateTuDongController.cs:48-54` `InDon` chỉ `[SBAuthorize()]` (không key riêng) — bất kỳ user đăng nhập đổi được trạng thái in.
- BE `...Service/Generated/SinhLoDateTuDongService.cs:64-67` LogCommandV2 nằm trong nhánh `if (true || ...)` → luôn log store mỗi lần search (điều kiện super/CSKH bị vô hiệu).
- `...Service/Generated/SinhLoDateTuDongService.cs:90-121` `GenerateIMEI`: MAX SoTuTang theo (IdDonVi + NgayTao.Date=Now.Date) → đổi ngày là reset chuỗi số; race condition nếu sinh đồng thời (MAX không khóa) → khả năng trùng `Ma`.
- `...Service/Generated/SinhLoDateTuDongService.cs:226-244` `XoaLoDate` là **xóa cứng**; chặn khi phiếu nhập gắn đã HoanThanh.
- FE `services/warehouseproduct/sinhLoDateTuDong.service.ts` còn 3 hàm `ThemMoiCauHinhIn` / `DsCauHinh` / `CapNhatCauHinhIn` trỏ endpoint **không tồn tại** ở controller hiện tại → dead code FE (sẽ 404 nếu gọi).
