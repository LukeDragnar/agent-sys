---
controller: PhieuNhapController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý tồn kho > tab Nhập kho (phieu-nhap.component) — list phiếu nhập, nút Thêm mới + menu thao tác per-row"
muc_dich: "Quản lý phiếu nhập kho: tạo/sửa/hoàn thành phiếu → cộng tồn + tính giá nhập trung bình (giá vốn) + đồng bộ chứng từ kế toán; import/quét IMEI, sinh lô date, in ấn."
alias: ["nhập kho", "phiếu nhập", "nhập hàng vào kho", "nhập mua từ NCC", "nhập trả lại từ khách", "import imei nhập", "đồng bộ kế toán phiếu nhập"]
phu_thuoc: [WarehouseProduct, Accounting, Contact, Logistic/Order, Report, MongoDB, SocketHub]
ripple: [KhoChiTiet (tồn + GiaNhapTrungBinh/giá vốn), Accounting (chứng từ liên kết + thu/chi), PhieuMuaHang (cập nhật trạng thái thu hộ), SanPhamImei, PhieuXuat (xuất trả NCC)]
---

# PhieuNhap — Nhập kho (Quản lý phiếu nhập kho)

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP + path) | Quyền (sbAuthorize) | Hành vi đúng (expected) | Known-issue |
|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (icon search) | POST `api/PhieuNhap/TimTheoDieuKien` | `PhieuNhap_TimTheoDieuKien` | Trả list phiếu nhập theo kho/chi nhánh/trạng thái/phân loại/ngày/người tạo | - |
| 2 | Xuất Excel danh sách | menu "Export To Excel" / "Chức năng khác > Export" | POST `api/PhieuNhap/ExportExcelPhieuNhap` | `PhieuNhap_ExportExcelPhieuNhap` | File `DanhSachPhieuNhap.xlsx`; lỗi → 503 nếu `Service_Unavailable`, ngược lại 500 code `EXPORT_ERROR` | - |
| 2b | Xuất Excel (biến thể) | (gọi nội bộ `exportExcelDs`) | POST `api/PhieuNhap/ExportPhieuNhap` | `[SBAuthorize()]` (mở — chỉ cần đăng nhập) | File `DanhSachPhieuNhap.xlsx`; lỗi → 500 `EXPORT_ERROR` | 2 endpoint export gần trùng nhau — cần xác nhận FE dùng cái nào |
| 3 | Thêm mới phiếu nhập | "Thêm mới phiếu nhập" (icon plus) → `onAddPhieuNew` | POST `api/PhieuNhap/ThemMoi` | `PhieuNhap_ThemMoi` | Tạo phiếu nhập (mặc định nháp/đang xử lý). Nếu hoàn thành ngay → cộng tồn + giá vốn (xem luồng) | - |
| 4 | Xem chi tiết / mở sửa | "Xem" / "Sửa" (icon eye/edit) → `onXem`,`onEdit` | GET `api/PhieuNhap/TimTheoId/{id}` | `PhieuNhap_TimTheoId` | Trả full chi tiết phiếu để view/edit | - |
| 5 | Cập nhật / Hoàn thành phiếu | "Sửa" (icon edit, ẩn khi đã hoàn thành trừ canEdit) | POST `api/PhieuNhap/CapNhatThongTin` | `PhieuNhap_CapNhatThongTin` | Sửa được khi CHƯA hoàn thành; chuyển `HoanThanh` → cộng tồn `KhoChiTiet`, tính lại `GiaNhapTrungBinh`, sinh IMEI/lô date tự động, đồng bộ kế toán (xem luồng) | Method ~1800 dòng, nhiều nhánh phân loại nhập → high bug-risk |
| 6 | Đồng bộ lại sang kế toán | "Đồng bộ kế toán" (icon sync) → `dongBoLaiSangKeToan` | POST `api/PhieuNhap/DongBoLaiSangKeToanTheoId/{id}` | `PhieuNhap_DongBoLaiSangKeToanTheoId` | CHỈ phiếu `HoanThanh` **và KHÔNG có IdPhieuMuaHang**; phiếu từ PMH đã hạch toán qua PMH → chặn. FE ẩn nút khi có idPhieuMuaHang | - |
| 7 | Xóa phiếu nháp | "Xóa" (icon delete, chỉ trạng thái Nháp) → `onXoaPhieuNhap` | POST `api/PhieuNhap/XoaPhieuNhapNhap/{id}` | `[SBAuthorize()]` (mở) | CHỈ xóa trạng thái `Nhap`; chặn nếu thuộc phiếu chuyển kho / đã có IMEI / IMEI import chờ quét. Không đụng tồn (nháp chưa cộng tồn) | Quyền `[SBAuthorize()]` rỗng — không gắn permission key riêng, cần xác nhận có chủ ý |
| 8 | Revert phiếu nhập | (không thấy nút trên list) | POST `api/PhieuNhap/RevertPhieuNhap` | `PhieuNhap_RevertPhieuNhap` | **ĐÃ KHÓA**: return ngay "Không cho revert nữa" — code phía dưới dead | Dead code: toàn bộ logic trừ tồn sau `return` (PhieuNhapService.cs:4833) |
| 9 | Download file mẫu import | "Import To Excel" flow | GET `api/PhieuNhap/DownloadTemplatePhieuNhapAsync` | `PhieuNhap_DownloadTemplatePhieuNhapAsync` | File `TemplatePhieuNhap.xlsx` | - |
| 10 | Import file phiếu nhập | "Import To Excel" → `onImportExcel` | POST `api/PhieuNhap/ImportTemplatePhieuNhap` | `PhieuNhap_ImportTemplatePhieuNhap` | Đẩy file vào bảng tmp import, validate | - |
| 11 | Get danh sách bản ghi import | (màn import) | POST `api/PhieuNhap/GetDanhSachPhieuNhapImport` | `PhieuNhap_GetDanhSachPhieuNhapImport` | List bản ghi tmp import theo status | - |
| 12 | Xóa bản ghi import tmp | (màn import) | POST `api/PhieuNhap/DeleteTmpPhieuNhapImport` | `PhieuNhap_DeleteTmpPhieuNhapImport` | Xóa tmp theo statusImport | - |
| 13 | Insert tmp → phiếu nhập | (màn import) | POST `api/PhieuNhap/InsertTmpPhieuNhapToPhieuNhap` | `PhieuNhap_InsertTmpPhieuNhapToPhieuNhap` | Chuyển bản ghi tmp hợp lệ thành phiếu nhập thật | - |
| 14 | Download template IMEI | "Import Imei" flow | GET `api/PhieuNhap/DownloadTemplatePhieuNhapNhapImei/{idPhieu}` | `PhieuNhap_DownloadTemplatePhieuNhapNhapImei` | File `TemplatePhieuNhap_NhapImei.xlsx` | - |
| 15 | Import IMEI theo phiếu | "Import Imei" → `onImportImei` (chỉ phiếu có IMEI + DangXuLy) | POST `api/PhieuNhap/ImportTemplatePhieuNhapNhapImei/{idPhieu}` | `PhieuNhap_ImportTemplatePhieuNhapNhapImei` | Đẩy IMEI vào tmp theo phiếu | - |
| 16 | Get ds import IMEI | (màn import imei) | POST `api/PhieuNhap/GetDanhSachPhieuNhapImportNhapImei` | `PhieuNhap_GetDanhSachPhieuNhapImportNhapImei` | List tmp IMEI import | - |
| 17 | Insert tmp IMEI → SP IMEI | (màn import imei) | POST `api/PhieuNhap/InsertTmpPhieuNhapImportNhapImeiToSpImei` | `PhieuNhap_InsertTmpPhieuNhapImportNhapImeiToSpImei` | Chuyển IMEI tmp thành `SanPhamImei` thật của phiếu | - |
| 18 | Xóa tmp IMEI import | (màn import imei) | POST `api/PhieuNhap/DeleteTmpPhieuNhapImportImei` | `PhieuNhap_DeleteTmpPhieuNhapImportImei` | Xóa bản ghi tmp IMEI | - |
| 19 | Xem IMEI | "Xem imei" | POST `api/PhieuNhap/ViewImei` | `PhieuNhap_ViewImei` | Trả list IMEI của phiếu | - |
| 20 | Download file lỗi IMEI | (màn import imei) | GET `api/PhieuNhap/DownloadFileLoi/{idPhieu}` | `PhieuNhap_DownloadFileLoi` | File `..._FileLoi.xlsx` các dòng IMEI lỗi | - |
| 21 | Lấy SP + SL chờ/đã import | (màn import imei) | GET `api/PhieuNhap/LayDanhSachSanPhamImeiImport/{id}/{type}` | `PhieuNhap_LayDanhSachSanPhamImeiImport` | List SP của phiếu kèm SL chờ nhập / đã import | - |
| 22 | Xóa IMEI của phiếu | "Xóa imei" | POST `api/PhieuNhap/XoaImei` | `PhieuNhap_XoaImei` | Xóa IMEI phiếu nhập | - |
| 23 | In danh sách IMEI | "In danh sách imei" (chỉ HoanThanh) → `onPrintImei` | GET `api/PhieuNhap/TimSanPhamImeiTheoIdPhieu/{idPhieu}` | `PhieuNhap_TimSanPhamImeiTheoIdPhieu` | Trả data IMEI để in cho phiếu | - |
| 23b | In IMEI theo list IDs | (màn ds sản phẩm imei) | POST `api/PhieuNhap/TimSanPhamImeiTheoListIds` | `PhieuNhap_TimSanPhamImeiTheoListIds` | Trả data IMEI để in theo list ids | - |
| 24 | In danh sách lô date | "In danh sách lô date" / V2 / lẻ (chỉ HoanThanh) → `onPrintLoDate*` | GET `api/PhieuNhap/TimSanPhamLoDateTheoIdPhieu/{idPhieu}` | `PhieuNhap_TimSanPhamLoDateTheoIdPhieu` | Trả data lô date để in | - |
| 24b | In lô date theo phiếu+phiếu SP | "In danh sách lô date V2/lẻ" | POST `api/PhieuNhap/TimSanPhamLoDateTheoIdPhieuAndPhieuSp` | `PhieuNhap_TimSanPhamLoDateTheoIdPhieuAndPhieuSp` | Trả data lô date theo id phiếu + phiếu SP | - |
| 25 | Sinh lô date | "Sinh lô date" (phiếu từ PMH, chưa hoàn thành) → `onSinhLoDate` | POST `api/PhieuNhap/SinhLoDatePhieuMuaHang` | `[SBAuthorize()]` (mở) | Sinh lô date tự động cho phiếu nhập tạo từ PMH | Permission key bị comment, chỉ còn `[SBAuthorize()]` rỗng (PhieuNhapService.cs ~ controller:382) |
| 26 | In phiếu nhập | (nút in) → `PrintPhieuNhap` | GET `api/PhieuNhap/PrintPhieuNhap/{idPhieu}` | `[SBAuthorize()]` (mở) | Trả data in phiếu | - |
| 27 | Xuất trả NCC | "Xuất trả NCC" (icon rollback, chỉ HoanThanh) → `onTaoPhieuXuatHuy` | (gọi sang PhieuXuat) | `PhieuXuat_ThemMoi` | Tạo phiếu xuất trả nhà cung cấp từ phiếu nhập đã hoàn thành | Quyền là `PhieuXuat_ThemMoi` (controller khác), không phải permission của PhieuNhap |

### Endpoint private (service-to-service, không có nút UI — `[PrivateSBAuthorize]`)
| # | Hành động | Endpoint | Caller | Hành vi đúng |
|---|---|---|---|---|
| P1 | Gen SP từ phiếu mua hàng | POST `GenPhieuMuaHangSanPhamDto` | Accounting (PhieuMuaHang) | Gen dto SP cho màn tạo phiếu nhập từ PMH |
| P2 | Thêm phiếu nhập từ PMH | POST `ThemMoiFromPhieuMuaHang` | Accounting | Tạo phiếu nhập liên kết với phiếu mua hàng |
| P3 | Lấy PN theo refs PMH | POST `GetPhieuNhapByPhieuRefsPrivate` | Accounting | Trả PN theo list idPhieuRefs + idDonVi |
| P4 | Hoàn thành PN theo PMH | POST `HoanThanhPhieuNhapTheoPhieuMuaHangPrivate` | Accounting (PMH hoàn thành) | Đi đúng luồng `CapNhatThongTin` → set HoanThanh → cộng tồn + giá vốn (KHÔNG đồng bộ KT vì PMH đã hạch toán) |
| P5 | Huỷ PN theo PMH | POST `HuyPhieuNhapTheoPhieuMuaHangPrivate` | Accounting (PMH huỷ) | Huỷ PN liên kết, **chỉ khi PN chưa hoàn thành** |
| P6 | Insert nhiều PN từ bán trả lại | POST `InsertMultiPhieuNhapFormBanTraLai` | Order/Logistic | Tạo PN từ phiếu bán hàng trả lại |
| P7 | Tạo PN từ phiếu trả lại hàng bán | POST `CreateFromPhieuTraLaiHangBan` | Order/Logistic | Tạo PN từ phiếu trả lại hàng bán |
| P8 | Gen SP trả lại hàng mua | POST `GenPhieuTraLaiHangMuaSanPhamDto` | Accounting | Gen dto SP cho phiếu trả lại hàng mua |

## Quan hệ
- **Gọi sang:**
  - `Accounting` — đồng bộ chứng từ liên kết + tạo thu/chi khi phiếu hoàn thành (`CommonThuChiPhieuNhap`, chứng từ `EnumChungTu_Ma.PN`); cập nhật trạng thái + thu hộ phiếu mua hàng (`_commonService.CapNhatTrangThaiPhieuAndThuHo`).
  - `Logistic/Order` — `GetChiTietDonLogistic`, rollback đơn khi nhập trả lại từ khách (`TaoBanGhiRollBack`).
  - `SocketHub` — bắn socket `ThanhToanChiHo` khi PN hoàn thành + có idPhieuMuaHang.
  - `MongoDB` — `PhieuNhapInfo` (snapshot SP logistic requests).
- **Dữ liệu (entities):** `PhieuNhap`, `PhieuNhapSanPham`, `KhoChiTiet` (tồn + `GiaNhapTrungBinh`), `SanPhamImei`, `DmKho` (tình trạng + phân loại nhập), `NhaCungCap`, `Kho`, `TmpPhieuNhapImportNhapImei`.
- **Ripple / đồng bộ:** đụng PN hoàn thành → ảnh hưởng tồn kho (`KhoChiTiet.SoLuong`), giá vốn (`GiaNhapTrungBinh`), chứng từ kế toán, công nợ NCC (qua thu/chi), và trạng thái phiếu mua hàng liên kết.

## Luồng xuyên module (thao tác lớn: Hoàn thành phiếu nhập)
```
CapNhatThongTin(DmKhoMaTinhTrang = HoanThanh)
  → validate (không sửa được nếu đã HoanThanh / từ phiếu chuyển kho / từ PMH thu-cũ-đổi-mới)
  → với mỗi SP: cộng tồn KhoChiTiet.SoLuong += SL
  → tính lại GiaNhapTrungBinh (giá vốn bình quân gia quyền):
       = (giáTB_cũ * SL_cũ + giáNhập * SL_nhập) / (SL_cũ + SL_nhập)   (PhieuNhapService.cs:563-564)
  → nếu SinhImeiTuDong: sinh SanPhamImei tự động
  → nếu SinhLoDateTuDong: sinh lô date
  → nếu KHÔNG có idPhieuMuaHang: đồng bộ chứng từ Kế toán + tạo thu/chi (công nợ NCC)
  → nếu CÓ idPhieuMuaHang: gọi Accounting.CapNhatTrangThaiPhieuAndThuHo + bắn socket (KHÔNG đồng bộ KT lại — PMH đã hạch toán)
```
Triage nhanh khi "tồn sai / giá vốn sai sau nhập": kiểm tra nhánh `dmKho.Ma == HoanThanh` trong `CapNhatThongTin`, công thức `GiaNhapTrungBinh`, và `PhanLoaiNhapEnum` (nhập mua NCC / trả lại từ khách / hủy-hoàn giao vận có nhánh xử lý khác nhau).

## Lỗi đã biết / lưu ý kỹ thuật
- **RevertPhieuNhap đã bị khóa hoàn toàn** — `PhieuNhapService.cs:4833` return "Không cho revert nữa" ngay đầu hàm; toàn bộ logic trừ tồn + giá vốn phía dưới là dead code (giữ làm tham khảo). Nếu cần đảo phiếu → dùng "Xuất trả NCC" (PhieuXuat).
- **`CapNhatThongTin` quá lớn (~1800 dòng, dòng 2964→~4756)** với nhiều nhánh theo `PhanLoaiNhapEnum` + trạng thái → bug-prone nhất controller. Mỗi thay đổi cần trace riêng nhánh: nhập mua NCC, nhập trả lại từ khách, nhập từ PMH, hủy/hoàn giao vận.
- **Công thức giá vốn revert (dead) sai dấu khi SL bằng nhau** — `PhieuNhapService.cs:4890-4898`: khi `SoLuong == item.SoLuong` set `GiaNhapTrungBinh = 0` (mất giá vốn). Hiện không chạy do revert bị khóa, nhưng nếu mở lại cần xem.
- **Quyền không đồng nhất**: nhiều endpoint nhạy cảm dùng `[SBAuthorize()]` rỗng (XoaPhieuNhapNhap, SinhLoDatePhieuMuaHang, ExportPhieuNhap, PrintPhieuNhap) — không gắn permission key riêng → chỉ cần đăng nhập là gọi được. Cần xác nhận có chủ ý.
- **Hai endpoint export gần trùng** (`ExportExcelPhieuNhap` có quyền vs `ExportPhieuNhap` quyền rỗng) — cần xác nhận FE dùng cái nào, tránh nhầm.
- **Guard đồng bộ kế toán phụ thuộc `IdPhieuMuaHang`**: PN sinh từ PMH KHÔNG đồng bộ KT (đã hạch toán qua PMH). FE ẩn nút "Đồng bộ kế toán" khi `data.idPhieuMuaHang` có giá trị; BE chặn lại ở `DongBoLaiSangKeToanTheoId` (PhieuNhapService.cs:2434). Hai chỗ phải đồng bộ — sửa 1 chỗ nhớ chỗ kia.
- **Controller `partial`** (`public partial class PhieuNhapController`) — có thể còn endpoint ở file partial khác; node này cover file Controllers/PhieuNhapController.cs. *Cần xác nhận* có file partial bổ sung không.
- Lưu ý trùng tên: tồn tại `Accounting/Accounting.API/Controllers/PhieuNhapController.cs` (controller khác cùng tên ở service Accounting) — node này chỉ mô tả bản WarehouseProduct.
