---
controller: ChungTuKhacController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Chứng từ nghiệp vụ khác: màn lập chứng từ hạch toán tự do (nhiều dòng Nợ/Có, mỗi dòng 1 cặp TkNo–TkCo + đối tượng nợ/có). Lưới danh sách (TimTheoDieuKien), xem chi tiết, xóa, xuất Excel."
muc_dich: "Lập/quản lý 'chứng từ nghiệp vụ khác' — bút toán hạch toán thủ công đa dòng (TkNo/TkCo + đối tượng nợ/có + số tiền), lưu trong EF transaction, đồng bộ danh mục đối tượng + đẩy MISA (nếu đơn vị có cấu hình kết nối kế toán), và sinh bút toán hạch toán."
alias: ["chứng từ khác", "chứng từ nghiệp vụ khác", "chung tu khac", "hạch toán thủ công", "bút toán tự do", "phiếu nghiệp vụ khác"]
auth: public
state_machine: []
deprecated: false
entities: [ChungTuKhac, ChungTuKhacChiTiet, DataReceiveTaoPhieu, PhieuKeToanHistory, CauHinhKetNoiKeToan]
stored_procs: []
depends: [MisaDanhMucSync (đồng bộ đối tượng nợ/có), CauHinhKetNoiKeToan (quyết định đẩy MISA), ButToanService/ButToan_PhieuChungTuKhac_Save (sinh bút toán), PhieuKeToanHistoryService/DeletePhieuKeToan (xóa), GemBox.Spreadsheet (export), EF transaction]
cross_module_in: []
cross_module_out: [MISA (DataReceiveTaoPhieu — job đẩy chứng từ nếu đơn vị có cấu hình kết nối kế toán)]
---

# ChungTuKhacController — Chứng từ nghiệp vụ khác (hạch toán thủ công đa dòng)

> Chứng từ hạch toán tự do: mỗi chi tiết là 1 cặp `TkNo`–`TkCo` + đối tượng nợ/có + số tiền. Lưu phiếu + chi tiết trong EF transaction; sau commit thì đồng bộ danh mục đối tượng nợ/có sang MISA, chỉ insert `DataReceiveTaoPhieu` (queue đẩy MISA) **nếu đơn vị có `CauHinhKetNoiKeToan` mặc định**, rồi sinh bút toán bằng `ButToan_PhieuChungTuKhac_Save`. `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lập chứng từ khác | nút "Lưu" (popup lập chứng từ) | `POST api/ChungTuKhac/InsertChungTu` | `SBAuthorize()` (any login) | public | popup lập, đã nhập ≥1 dòng Nợ/Có | `ValidateRequest`. `SoChungTu` trống → `SinhMaPhieu(PhieuNghiepVuKhac)`, có thì chống trùng (DonVi×CN×SoChungTu). Tạo `ChungTuKhac` + N `ChungTuKhacChiTiet` (TkNo/TkCo/SoTien + đối tượng nợ/có). EF transaction insert phiếu + chi tiết. Sau commit: với mỗi chi tiết đồng bộ đối tượng Nợ & Có sang MISA; nếu có `CauHinhKetNoiKeToan` mặc định (CN, fallback đơn vị) → insert `DataReceiveTaoPhieu(Status=Received)`. Cuối cùng `ButToan_PhieuChungTuKhac_Save`. | **EF** `ChungTuKhac`(+ChiTiet), `DataReceiveTaoPhieu` | `[SBAuthorize()]` trống → ghi chứng từ không kiểm key (chỉ login). |
| 2 | Danh sách chứng từ khác | Lưới list + bộ lọc | `POST api/ChungTuKhac/TimTheoDieuKien` | `ChungTuKhac_TimTheoDieuKien` | public | màn danh sách | Non-admin ép `IdChiNhanh=userLogin.IdChiNhanh`. Lọc DonVi + CN + keyword(SoChungTu/DienGiai) + khoảng ngày `NgayChungTu` + `TrangThaiDongBo` (ChuaDongBo→StatusSendPartner null). Paging, order DESC `NgayCapNhat`. Trả kèm `StatusSendPartner`, `SinhButToanSuccess`, `SinhButToanMessErr`. | **EF** `ChungTuKhac` | Endpoint DUY NHẤT có key thật. |
| 3 | Xem chi tiết | (mở chi tiết) | `POST api/ChungTuKhac/ViewChiTiet?idChungTu=` | `SBAuthorize()` (any login) | public | click 1 dòng | Lấy phiếu + chi tiết theo id+DonVi. | **EF** `ChungTuKhac`(+ChiTiet) | `[SBAuthorize()]` trống. |
| 4 | Xóa chứng từ | nút "Xóa" / confirm | `POST api/ChungTuKhac/XoaPhieu?idChungTu=` | `SBAuthorize()` (any login) | public | phiếu KHÔNG có `IdHoaDon` | Tìm theo id+DonVi+CN. **`IdHoaDon` có giá trị → chặn "Phiếu được tạo tự động, không thể xóa"** (chứng từ sinh từ màn thanh toán). Hợp lệ → `PhieuKeToanHistoryService.DeletePhieuKeToan` (ghi history + xóa). | **EF** + `PhieuKeToanHistoryService` | `[SBAuthorize()]` trống. Validate message "Id phiếu bù trừ không hợp lệ" sai ngữ cảnh (copy từ BuTru). L346. |
| 5 | Xuất Excel | nút "Xuất Excel" | `POST api/ChungTuKhac/ExportExcelChungTuKhac` | `SBAuthorize()` (any login) | public | có dữ liệu | Lọc giống #2, build GemBox; `result.Data==null` → `NotFound`. Trả `ChungTuNghiepVuKhac.xlsx`. | **EF** + GemBox | `[SBAuthorize()]` trống. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Chống trùng số chứng từ:** trống → tự sinh `SinhMaPhieu(PhieuNghiepVuKhac)`; có giá trị → chống trùng (IdDonVi, IdChiNhanh, SoChungTu).
- **Đẩy MISA có điều kiện:** chỉ insert `DataReceiveTaoPhieu` khi tìm thấy `CauHinhKetNoiKeToan` mặc định của chi nhánh (fallback `IsApDungDonVi=true`). Không có cấu hình → KHÔNG tự đẩy (user tích thủ công trên lưới để đẩy sau).
- **Khóa xóa chứng từ tự sinh:** `IdHoaDon != null` (chứng từ tạo tự động từ màn thanh toán) → không cho xóa thủ công.
- **Hạch toán đa dòng:** mỗi `ChungTuKhacChiTiet` = 1 cặp TkNo/TkCo + đối tượng Nợ (NhomDoiTuongNo/IdDoiTuongNo...) và đối tượng Có (…Co) + SoTien; bút toán Nợ/Có do `ButToan_PhieuChungTuKhac_Save` sinh sau.

## Quan hệ
- **Gọi sang:** `MisaDanhMucSync.CheckDongBoDoiTuong` (đối tượng nợ + có mỗi dòng), `ButToanService.ButToan_PhieuChungTuKhac_Save`, `PhieuKeToanHistoryService.DeletePhieuKeToan` (khi xóa).
- **Dữ liệu (entities):** `ChungTuKhac` + `ChungTuKhacChiTiet`; `CauHinhKetNoiKeToan` (đọc để quyết định đẩy MISA); `DataReceiveTaoPhieu` (queue MISA); `PhieuKeToanHistory` (qua service xóa).
- **Ripple / đồng bộ:** Tạo → đồng bộ danh mục đối tượng MISA + (nếu có cấu hình) queue đẩy chứng từ MISA + sinh bút toán. Xóa → ghi history + xóa (qua PhieuKeToanHistoryService).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/ChungTuKhacController.cs`
  - Chỉ `TimTheoDieuKien` (L49) có key `ChungTuKhac_TimTheoDieuKien`; `InsertChungTu`/`XoaPhieu`/`ViewChiTiet`/`ExportExcelChungTuKhac` đều `[SBAuthorize()]` trống → **lập & xóa chứng từ không kiểm permission key** (chỉ login).
- BE `Accounting/Accounting.Service/Generated/ChungTuKhacService.cs`
  - L346: `XoaPhieu` validate trả "Id phiếu bù trừ không hợp lệ" — message copy nhầm từ BuTru (ngữ cảnh sai, chỉ ảnh hưởng text).
  - L256: `ButToan_PhieuChungTuKhac_Save(chungTuKhac.Id, null)` gọi với `userLogin=null` (sinh bút toán không có user) — đúng pattern nội bộ nhưng lưu ý truy vết.
  - L214-226: cấu hình đẩy MISA fallback CN→đơn vị; nếu không có cấu hình thì phiếu vẫn lưu nhưng không vào queue đẩy.
- DTO: `Models.Core.DTO.Accounting.Client.ChungTuKhac` (ChungTuKhacRequest, ChungTuKhacGridInfo, chi tiết Nợ/Có).
- Entity `ChungTuKhac.cs` (`9b83ccf54e`, 2026-07-06): bổ sung `[Column(TypeName=...)]` cho các cột string (VARCHAR(255)/NVARCHAR(500)/NVARCHAR(100)/NVARCHAR(1000)/VARCHAR(100)) — chuẩn hóa kiểu cột nvarchar/varchar (mirror ChungTuMuaDichVu), tránh EF map `nvarchar(max)`. Thuần schema, không đổi hành vi.
