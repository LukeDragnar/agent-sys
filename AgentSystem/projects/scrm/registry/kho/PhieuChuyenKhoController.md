---
controller: PhieuChuyenKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý tồn kho > tab Chuyển kho (phieu-chuyenkho.component) — list phiếu chuyển kho, nút Thêm mới/Tạo lượt nhặt/Import + menu thao tác per-row (Sửa/Xem/Duyệt/Đồng bộ KT/PXK/Quét IMEI/Xóa)"
muc_dich: "Chuyển kho nội bộ giữa 2 kho/chi nhánh: 1 phiếu chuyển sinh đồng thời PhieuXuat (kho nguồn) + PhieuNhap (kho đích); hoàn thành → trừ tồn kho nguồn, duyệt (bên nhận) → cộng tồn kho đích; có lô-date, IMEI (quét/import), phát hành PXK điện tử, đồng bộ kế toán."
alias: ["chuyển kho", "phiếu chuyển kho", "điều chuyển kho", "chuyển hàng giữa kho/chi nhánh", "duyệt phiếu chuyển", "nhận hàng chuyển kho", "phát hành PXK chuyển kho", "import chuyển kho"]
auth: mixed
state_machine: [Nhap(4), DangXuLy(1), HoanThanh(2), DaHuy(3)]   # + trục duyệt riêng: TrangThaiDuyetChuyenKho [ChoDuyet(1), DaDuyet(2), TuChoi(3)]
deprecated: false
entities: [PhieuChuyenKho, PhieuChuyenKhoSanPham, PhieuChuyenKhoThongTinPXK, PhieuChuyenKhoLuotNhat, PhieuChuyenKhoPhieuNhat, KhoChiTiet, SanPhamLoDate, SanPhamImei, PhieuNhap, PhieuXuat]
stored_procs: []
depends: [WarehouseProduct, Accounting (DongBoKeToanService), Kho/ChiNhanh, CauHinhService, CommonService (SinhMaPhieu), GemBox (export/template)]
cross_module_in: [Accounting (UpdateTrangThaiPhatHanhHoaDon callback hóa đơn điện tử)]
cross_module_out: [KhoChiTiet (tồn + GiaNhapTrungBinh 2 đầu kho), PhieuNhap + PhieuXuat (sinh kèm mỗi phiếu chuyển), SanPhamLoDate (2 đầu kho), SanPhamImei (trạng thái DangChuyenKho→DaNhapKho), Accounting (DongBoPhieuChuyenKho chứng từ + hóa đơn PXK điện tử)]
---

# PhieuChuyenKho — Chuyển kho (điều chuyển hàng giữa kho/chi nhánh)

> **Mô hình 2 đầu kho:** 1 phiếu chuyển kho = 1 `PhieuChuyenKho` (header) + N `PhieuChuyenKhoSanPham`, đồng thời sinh **1 `PhieuXuat` (kho nguồn IdTuKho)** + **1 `PhieuNhap` (kho đích IdDenKho)** liên kết. Ripple tồn **tách 2 bước**: **Hoàn thành** (bên chuyển) → TRỪ tồn kho nguồn; **Duyệt** (bên nhận) → CỘNG tồn kho đích. Đây là điểm khác bản chất so với PhieuNhap/PhieuXuat (1 đầu kho).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" → `onSearch` | POST `api/PhieuChuyenKho/TimTheoDieuKien` | `PhieuChuyenKho_TimTheoDieuKien` | public | luôn | List phiếu theo kho nguồn/đích/chi nhánh/tình trạng/trạng thái duyệt/trạng thái phát hành PXK/ngày | EF | - |
| 2 | Xuất Excel danh sách | "Export To Excel" → `exportExcelDs` | POST `api/PhieuChuyenKho/ExportExcelPhieuChuyen` | `[SBAuthorize()]` (mở) | public | luôn | File `DanhSachPhieuChuyen.xlsx`; lỗi → 503 nếu `Service_Unavailable`, ngược lại 500 code `EXPORT_ERROR` | EF | Quyền key bị **comment**, chỉ còn `[SBAuthorize()]` rỗng → chỉ cần đăng nhập (controller:34-35). Cần xác nhận có chủ ý |
| 3 | Thêm mới phiếu chuyển | "Thêm mới phiếu" → `onAddNewPhieu` | POST `api/PhieuChuyenKho/ThemMoi` | `PhieuChuyenKho_ThemMoi` | public | luôn | Tạo phiếu chuyển + sinh kèm PhieuXuat (nguồn) & PhieuNhap (đích). Nếu lưu HoanThanh ngay → trừ tồn kho nguồn (xem luồng) | EF | - |
| 4 | Xem / mở sửa | "Sửa"/"Xem" → `onEdit(id,maTinhTrang)` / `onXem` | GET `api/PhieuChuyenKho/TimTheoId/{id}` | `PhieuChuyenKho_TimTheoId` | public | luôn | Trả full chi tiết phiếu (SP, lô-date, IMEI, PXK) để view/edit | EF | - |
| 5 | Cập nhật / Hoàn thành phiếu | "Sửa" (mở popup edit) → `CapNhatThongTin` | POST `api/PhieuChuyenKho/CapNhatThongTin` | `PhieuChuyenKho_CapNhatThongTin` | public | KHÔNG sửa khi đã `HoanThanh`; KHÔNG hạ về `Nhap` nếu đã rời `Nhap` | **Chỉ chi nhánh NGUỒN (`IdTuChiNhanh == userLogin.IdChiNhanh`) được sửa**. Chuyển `HoanThanh` → trừ tồn + giá vốn kho nguồn; yêu cầu đủ TkNo/TkCo từng SP; nếu không phải admin tổng/CN phải là quản kho của kho nguồn | EF | Method rất lớn (1975→~3100), nhiều nhánh lô-date/IMEI/dung tích → bug-prone |
| 6 | Duyệt phiếu chuyển (bên nhận) | "Duyệt phiếu chuyển kho" → `onDuyetPhieuChuyenKho` | POST `api/PhieuChuyenKho/DuyetPhieuChuyenKho` | `PhieuChuyenKho_DuyetPhieuChuyenKho` | public | hiện khi `trangThaiDuyet===1 (ChoDuyet)` + user thuộc `idDenChiNhanh` + đang quản lý `idDenKho` | **Chỉ chi nhánh ĐÍCH duyệt** (`IdDenChiNhanh==IdChiNhanh`); phiếu phải `HoanThanh`. Duyệt → CỘNG tồn kho đích + lô-date đích + finalize IMEI đã quét nhận (→ `DaNhapKho`). Idempotent: đã `DaDuyet` trả success không cộng lại | EF | - |
| 6b | Duyệt + nhập kho IMEI | "Duyệt và nhập kho imei" → `onDuyetPhieuChuyenKho(...,true)` | POST `api/PhieuChuyenKho/DuyetPhieuChuyenKho` (cờ `IsTuNhapKhoImei`) | `PhieuChuyenKho_DuyetPhieuChuyenKho` | public | hiện khi cấu hình `duyetPhieuChuyenKhoDongThoiNhapKhoImei` + `trangThaiDuyet===1` + `isCoSanPhamImei` + user `idDenChiNhanh` | Như #6 + auto-import IMEI sang kho đích trong cùng thao tác duyệt | EF | - |
| 7 | Đồng bộ lại sang kế toán | "Đồng bộ kế toán" → `dongBoLaiSangKeToan` | POST `api/PhieuChuyenKho/DongBoLaiSangKeToanTheoId/{id}` | `PhieuChuyenKho_DongBoLaiSangKeToanTheoId` | public | hiện khi `maTinhTrang==HoanThanh` | **CHỈ phiếu `HoanThanh` VÀ `DaDuyet`** mới đồng bộ (BE chặn); gọi `DongBoKeToanService.DongBoPhieuChuyenKho` tạo chứng từ | EF | FE chỉ check HoanThanh để hiện nút; BE còn chặn thêm DaDuyet (service:1947) — lệch điều kiện, bấm khi chưa duyệt sẽ báo lỗi |
| 8 | Xóa phiếu nháp | "Xóa" → `onXoaPhieuChuyenKho` | POST `api/PhieuChuyenKho/XoaPhieuChuyenKhoNhap/{id}` | `[SBAuthorize()]` (mở) | public | chỉ hiện khi `maTinhTrang==Nhap` | Xóa phiếu trạng thái `Nhap` + xóa PhieuNhap/PhieuXuat nháp liên kết. Không đụng tồn (nháp chưa cộng/trừ) | EF | Quyền `[SBAuthorize()]` rỗng — không gắn permission key, cần xác nhận |
| 9 | Revert phiếu chuyển | (không thấy nút trên list — cần xác nhận) | POST `api/PhieuChuyenKho/RevertPhieuChuyenKho` | `PhieuChuyenKho_RevertPhieuChuyenKho` | public | - | Đảo tồn 2 đầu kho theo phiếu (cộng lại kho nguồn, trừ kho đích). **Cần xác nhận có còn dùng / có nút FE không** | EF | Khác PhieuNhap (revert đã khóa cứng) — bản PhieuChuyenKho **chưa thấy khóa**, logic còn chạy. Cần trace kỹ nếu sửa |
| 10 | Thông tin PXK | "Thông tin PXK" → `openThongTinPXK` | GET `GetThongTinPXK/{id}` · POST `UpsertThongTinPXK` | `PhieuChuyenKho_GetThongTinPXK` / `_UpsertThongTinPXK` | public (role hẹp) | luôn | Get/Upsert thông tin phiếu xuất kho điện tử (`PhieuChuyenKhoThongTinPXK`) | EF | role hẹp hơn các action khác (chỉ AD/QL-KHO) |
| 11 | Phát hành PXK điện tử | "Phát hành PXK" → `openPhatHanhHoaDon` | POST `PhatHanhPhieuXuatKho` · POST `PreviewPhieuXuatKho` | `PhieuChuyenKho_PhatHanhPhieuXuatKho` / `_PreviewPhieuXuatKho` | public (role hẹp) | hiện khi `tinhTrangPhieuChuyen==2 (HoanThanh)` + `trangThaiPhatHanhHoaDon!=1` | Phát hành / xem trước hóa đơn xuất kho điện tử | EF | - |
| 11b | Callback trạng thái phát hành HĐ | (gọi từ ngoài) | POST `UpdateTrangThaiPhatHanhHoaDon` | **(không gắn `[SBAuthorize]`)** | mixed | - | Cập nhật trạng thái phát hành hóa đơn từ response NCC HĐĐT (`InvoiceResponseDto`) | EF | **Endpoint không có attribute auth** — cần xác nhận có guard khác / IP allowlist (controller:380) |
| 12 | Tải / lịch sử PXK | "Tải PXK" / "Lịch sử phát hành PXK" | (FE: `onDownloadPXK` / `onShowListInvoice`) | — | public | "Tải" khi `data.invNo`; "Lịch sử" khi `trangThaiPhatHanhHoaDon!=null` | Tải file PXK / xem lịch sử phát hành | - | - |
| 13 | Quét IMEI chuyển (kho nguồn) | "Quét Imei chuyển kho trực tiếp" → `onQuetImeiChuyenKhoTrucTiep` | (popup quét) | — | public | `isCoSanPhamImei` + `DangXuLy` + user `idTuChiNhanh` + quản lý `idTuKho` | Quét IMEI xuất ở kho nguồn | EF | - |
| 13b | Rollback IMEI quét trực tiếp | (trong popup quét) | POST `RollbackImeiQuetTrucTiepTheoSanPham` | `PhieuChuyenKho_RollbackImeiQuetTrucTiepTheoSanPham` | public | - | Hủy IMEI đã quét trực tiếp theo SP | EF | - |
| 14 | Quét IMEI nhận (kho đích) | "Quét Imei nhập kho trực tiếp" → `onQuetImeiNhanKhoTrucTiep` | (popup quét nhận) | — | public | `isCoSanPhamImei` + `HoanThanh` + user `idDenChiNhanh` + quản lý `idDenKho` | Quét IMEI nhận ở kho đích (đánh dấu `IsDaQuetNhanChuyenKho`, finalize khi Duyệt) | EF | - |
| 14b | Xóa IMEI đã nhận chuyển kho | (popup) | POST `XoaImeiDaNhanChuyenKho` | `PhieuChuyenKho_XoaImeiDaNhanChuyenKho` | public | - | Xóa IMEI đã nhận ở kho đích | EF | - |
| 15 | Import phiếu chuyển (Excel) | "Import To Excel" → `onImportExcel` | GET `DownloadTemplatePhieuChuyenKhoAsync` · POST `ImportTemplatePhieuChuyenKho` · POST `GetDanhSachPhieuChuyenKhoImport` · POST `DeleteTmpPhieuChuyenKhoImport` · POST `InsertTmpPhieuChuyenKhoToPhieuChuyenKho` | `PhieuChuyenKho_*` tương ứng | public | màn import | Luồng import: tải template → đẩy file vào `TmpPhieuChuyenKhoImport` → list/xóa tmp → insert tmp → phiếu thật | EF | - |
| 16 | Import IMEI phiếu chuyển | "Import Imei" → `onImportImei` | GET `DownloadTemplatePhieuChuyenKhoNhapImei/{idPhieu}` · POST `ImportTemplatePhieuChuyenKhoNhapImei/{idPhieu}` · POST `GetDanhSachPhieuChuyenKhoImportNhapImei` · POST `InsertTmpPhieuChuyenKhoImportNhapImeiToSpImei` · POST `DeleteTmpPhieuChuyenKhoImportImei` | `PhieuChuyenKho_*` tương ứng | public | `isCoSanPhamImei` + `DangXuLy` + user kho nguồn | Đẩy IMEI vào tmp theo phiếu → chuyển thành `SanPhamImei` thật | EF | - |
| 16b | Xem / xóa / file lỗi IMEI | "Xem imei" / "Download file lỗi" | POST `ViewImei` · POST `XoaImei` · GET `DownloadFileLoi/{idPhieu}` · POST `LayDanhSachSanPhamImeiImport` | `PhieuChuyenKho_ViewImei`/`_XoaImei`/`_DownloadFileLoi`/`PhieuChuyen_LayDanhSachSanPhamImeiImport` | public | màn import imei | Xem IMEI phiếu / xóa IMEI / tải file dòng lỗi / list SP kèm SL chờ-đã import | EF | key `PhieuChuyen_Lay...` thiếu hậu tố `Kho` (khác chuẩn) — cần xác nhận |
| 17 | Lượt nhặt (gom nhiều phiếu chuyển) | "Tạo lượt nhặt" → `onTaoLuotNhat` + màn chi tiết lượt nhặt | POST `TaoLuotNhat` · `GetDsLuotNhatPhieuChuyenKho` · `DeleteLuotNhatPhieuChuyenKho/{id}` · `GetDsPhieuNhatPhieuChuyenKho` · `GetDsSanPhamByPhieuChuyenKho` · `GetDsDetailPhieuNhatPhieuChuyenKho` · `GetDsPhieuChuyenKhoByLuotNhat` | **`[SBAuthorize]` rỗng** (key đã comment) | public | luôn | Gom nhiều phiếu chuyển thành "lượt nhặt" để soạn hàng (`PhieuChuyenKhoLuotNhat`/`PhieuChuyenKhoPhieuNhat`) | EF | **Toàn bộ nhóm lượt nhặt dùng `[SBAuthorize]` rỗng** — permission key bị comment hàng loạt (controller:280-334). Cần xác nhận |

## Quy tắc nghiệp vụ (gating tồn kho 2 đầu — blueprint trace)
1. **Ai được sửa/hoàn thành** → `phieuChuyenKho.IdTuChiNhanh == userLogin.IdChiNhanh` (chỉ chi nhánh NGUỒN). Sai → "Chỉ chi nhánh chuyển mới được phép sửa".
2. **Hoàn thành (`DmKhoMaTinhTrang = HoanThanh`)** → cần: (a) user thuộc CN nguồn; (b) nếu KHÔNG phải admin tổng/CN → phải nằm trong `khoNguon.UserQuanKho`; (c) mọi SP có đủ `TkNo`/`TkCo`. → TRỪ tồn `KhoChiTiet` kho nguồn (`isDaHoanThanhPhieuChuyen=true`, `isPhaiPheDuyet=true` ⇒ chỉ trừ nguồn, CHƯA cộng đích).
3. **Giá vốn kho nguồn khi trừ** (GenKhoChiTiet:670-679):
   - nếu `SoLuong_tồn == SoLuong_chuyển` → `GiaNhapTrungBinh = (giáTB_cũ + giáChuyển) / 2` ⚠️ (xem known-issue)
   - ngược lại → `GiaNhapTrungBinh = (giáTB_cũ*SL_cũ − giáChuyển*SL_chuyển) / (SL_cũ − SL_chuyển)`; rồi `SoLuong -= SL_chuyển`.
4. **Duyệt (bên nhận, `TrangThaiDuyet: ChoDuyet→DaDuyet`)** → cần `IdDenChiNhanh == userLogin.IdChiNhanh` + phiếu đã `HoanThanh`. → CỘNG tồn `KhoChiTiet` kho đích (`GenKhoChiTiet(..., isDaHoanThanh=false, isPhaiPheDuyet=false)` ⇒ chỉ cộng đích, KHÔNG trừ nguồn lại) + cộng lô-date đích + finalize IMEI đã quét nhận. Đã `DaDuyet` → trả success, không cộng lại (idempotent).
5. **Giá vốn kho đích khi cộng** (GenKhoChiTiet:711-713, SP non-IMEI): `GiaNhapTrungBinh = (giáTB_đích*SL_đích + giáChuyển*SL_imeiDaQuetNhan) / (SL_đích + SL_imeiDaQuetNhan)`; `SoLuong += SoLuongImeiDaQuetNhan`. Chưa có ở kho đích → tạo `KhoChiTiet` mới với `GiaNhapTrungBinh = Gia/TiLeQuyDoi`.
6. **Đồng bộ kế toán** → chỉ khi `HoanThanh` **VÀ** `DaDuyet` (service:1947) → `DongBoKeToanService.DongBoPhieuChuyenKho`.

## Quan hệ
- **Gọi sang:**
  - `DongBoKeToanService.DongBoPhieuChuyenKho` — sinh chứng từ kế toán (chỉ khi HoanThanh + DaDuyet).
  - `CommonService.SinhMaPhieu` — sinh mã `PhieuNhap`/`PhieuXuat` liên kết.
  - `CauHinhService.GetAsync` — cấu hình đơn vị (cache), gồm `duyetPhieuChuyenKhoDongThoiNhapKhoImei`.
  - Accounting (HĐĐT) — phát hành/preview PXK điện tử; callback `UpdateTrangThaiPhatHanhHoaDon`.
- **Dữ liệu (entities):** `PhieuChuyenKho`, `PhieuChuyenKhoSanPham`, `PhieuChuyenKhoThongTinPXK`, `PhieuChuyenKhoLuotNhat`/`PhieuChuyenKhoPhieuNhat(ChiTiet)`, `KhoChiTiet` (tồn + `GiaNhapTrungBinh` **2 đầu kho**), `SanPhamLoDate` (2 đầu), `SanPhamImei`, `PhieuNhap`/`PhieuNhapSanPham`, `PhieuXuat`/`PhieuXuatSanPham`, `TmpPhieuChuyenKhoImport`/`...NhapImei`.
- **Ripple / đồng bộ:** đụng tồn kho **2 đầu** (`KhoChiTiet.SoLuong` + `GiaNhapTrungBinh` kho nguồn lẫn kho đích), lô-date 2 đầu, IMEI (`DangChuyenKho→DaNhapKho`), chứng từ kế toán, hóa đơn PXK điện tử, và 2 phiếu Nhập/Xuất sinh kèm.

## Luồng xuyên module (thao tác lớn: Chuyển kho 2 đầu)
```
ThemMoi(phiếu chuyển)
  → tạo PhieuChuyenKho + PhieuChuyenKhoSanPham
  → sinh kèm PhieuXuat (IdTuKho, kho nguồn) + PhieuNhap (IdDenKho, kho đích) liên kết
  → (nếu lưu HoanThanh ngay) đi luôn bước Hoàn thành

CapNhatThongTin(DmKhoMaTinhTrang = HoanThanh)   [chi nhánh NGUỒN]
  → validate quyền (CN nguồn + quản kho), đủ TkNo/TkCo
  → GenKhoChiTiet(isDaHoanThanh=true, isPhaiPheDuyet=true)
        → TRỪ tồn KhoChiTiet kho NGUỒN + cập nhật GiaNhapTrungBinh nguồn
        → trừ lô-date kho nguồn
  → trạng thái phiếu = HoanThanh, TrangThaiDuyet = ChoDuyet
        (tồn kho ĐÍCH CHƯA cộng — chờ duyệt)

DuyetPhieuChuyenKho   [chi nhánh ĐÍCH]
  → validate (CN đích + phiếu HoanThanh + chưa DaDuyet)
  → GenKhoChiTiet(isDaHoanThanh=false, isPhaiPheDuyet=false)
        → CỘNG tồn KhoChiTiet kho ĐÍCH + GiaNhapTrungBinh đích (tạo mới nếu chưa có)
        → cộng/ghép lô-date kho đích
        → finalize IMEI đã quét nhận: SanPhamImei DangChuyenKho → DaNhapKho, cộng tồn IMEI kho đích
  → TrangThaiDuyet = DaDuyet
  → (nếu cờ IsTuNhapKhoImei / "Duyệt và nhập kho imei") auto-import IMEI luôn

DongBoLaiSangKeToanTheoId   [chỉ khi HoanThanh + DaDuyet]
  → DongBoKeToanService.DongBoPhieuChuyenKho → chứng từ kế toán
```
Triage nhanh "tồn lệch sau chuyển kho": xác định đang ở bước nào — **Hoàn thành chỉ trừ nguồn**, **Duyệt mới cộng đích**. Nếu kho đích chưa thấy tồn → phiếu chưa được bên nhận **Duyệt** (đúng thiết kế, không phải bug). Kiểm 2 lần gọi `GenKhoChiTiet` với cặp cờ `(isDaHoanThanh, isPhaiPheDuyet)`.

## Lỗi đã biết / lưu ý kỹ thuật
- **Giá vốn kho nguồn nghi sai khi `SoLuong tồn == SoLuong chuyển`** — `PhieuChuyenKhoService.cs:672`: set `GiaNhapTrungBinh = (giáTB_cũ + giáChuyển)/2` thay vì giữ/0-hóa. Trùng pattern lỗi với PhieuNhap revert (lấy trung bình cộng thay vì bình quân gia quyền). Khi tồn về 0, giá vốn nên là 0 hoặc giữ nguyên — công thức `/2` đáng ngờ. **Cần xác nhận** với nghiệp vụ.
- **Tồn 2 đầu lệch pha là CHỦ Ý:** Hoàn thành (nguồn) và Duyệt (đích) tách 2 thao tác/2 chi nhánh khác nhau → giữa 2 bước, tổng tồn toàn hệ thống "thiếu" đúng bằng SL đang chuyển (hàng đang trên đường). Đừng coi là bug.
- **`UpdateTrangThaiPhatHanhHoaDon` không có `[SBAuthorize]`** (controller:380) — endpoint nhận callback HĐĐT không gắn auth attribute. Cần xác nhận có guard khác (IP/secret) hay là lỗ hổng.
- **Quyền không đồng nhất:** `ExportExcelPhieuChuyen` (key comment → `[SBAuthorize()]` rỗng, controller:34-35), `XoaPhieuChuyenKhoNhap` (rỗng), và **toàn bộ nhóm Lượt nhặt** (`TaoLuotNhat`/`GetDs...`/`Delete...`, controller:280-334) đều dùng `[SBAuthorize]`/`[SBAuthorize()]` rỗng (key đã comment) → chỉ cần đăng nhập. Cần xác nhận có chủ ý.
- **Lệch điều kiện FE/BE nút "Đồng bộ kế toán":** FE hiện nút chỉ theo `maTinhTrang==HoanThanh` (html:510), BE chặn thêm `DaDuyet` (service:1947) → bấm khi chưa duyệt sẽ báo "Chỉ đồng bộ phiếu đã hoàn thành và đã duyệt". Sửa 1 bên nhớ bên kia.
- **Key permission lệch chuẩn đặt tên:** `PhieuChuyen_ExportExcelPhieuChuyen`, `PhieuChuyen_LayDanhSachSanPhamImeiImport` (thiếu `Kho`) trong khi phần lớn dùng tiền tố `PhieuChuyenKho_`. Cần xác nhận map permission đúng.
- **Controller `partial`** (`public partial class PhieuChuyenKhoController`) — node này cover `Controllers/PhieuChuyenKhoController.cs`. *Cần xác nhận* có file partial khác bổ sung endpoint không.
- **Controller liên quan cùng module:** `PhieuChuyenKhoSanPhamController.cs` (CRUD dòng SP) — node riêng nếu cần.
- **`CapNhatThongTin` rất lớn (~1100 dòng, 1975→~3100)** với nhánh lô-date/dung tích/IMEI → bug-prone nhất; mỗi sửa cần trace riêng nhánh (non-imei vs imei, có/không dung tích).
- **`RevertPhieuChuyenKho` còn sống** (khác PhieuNhap đã khóa) — logic đảo tồn 2 đầu vẫn chạy (service:3199). **Cần xác nhận** FE có nút gọi không; nếu mở cho user, trace kỹ chiều cộng/trừ 2 kho.
