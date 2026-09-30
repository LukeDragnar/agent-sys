---
controller: NhaCungCapController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Quản lý nhà cung cấp (route quan-ly-ncc, quanly-nhacungcap.component) — list NCC, nút Thêm mới + menu Export To Excel + Import; popup Thêm/Sửa (edit-quanly-nhacungcap)"
muc_dich: "Quản lý danh mục nhà cung cấp: thêm/sửa/xóa/tìm/import/export NCC, đồng bộ với khách hàng khi NCC đồng thời là KH, và cấp dữ liệu NCC cho các module khác (kế toán, sản phẩm, autocomplete)."
alias: ["nhà cung cấp", "NCC", "danh mục nhà cung cấp", "quản lý nhà cung cấp", "supplier", "vendor", "nhập NCC từ excel", "NCC đồng thời là khách hàng"]
auth: mixed                # UI dùng SBAuthorize(key); nhiều endpoint service-to-service [PrivateSBAuthorize]; vài endpoint SBAuthorize() rỗng
state_machine: []          # NCC không có vòng trạng thái (chỉ cờ SuDung dùng/ngừng dùng)
deprecated: false
entities: [NhaCungCap]
stored_procs: []
depends: [WarehouseProduct, CommonService, KhachHang/Contact, SanPham, Accounting/PhieuMuaHang]
cross_module_in: [Customer/KhachHang (UpdateNccFromKhachHang, KhachHangDongThoiLaNhaCungCap, TimNccAndKhachHang), Accounting/Order (GetNhaCungCapByCodes, GetMaNhaCungCapByNhom, DanhSachNhaCungCapTuDong*)]
cross_module_out: [KhachHang (đồng bộ NCC↔KH khi trùng mã), SanPham (UpdateSanPhamNhaCungCap)]
---

# NhaCungCapController — Quản lý nhà cung cấp

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút Tìm kiếm | POST `api/NhaCungCap/TimTheoDieuKien` | `NhaCungCap_TimTheoDieuKien` | public | màn list | Trả list NCC theo DonViId + ngành nghề user, filter keyword/cờ SuDung | EF | - |
| 2 | Thêm mới NCC (LEGACY) | (không nối) | POST `api/NhaCungCap/ThemMoi` | `NhaCungCap_ThemMoi` | public | — | Insert NCC. **FE đã bỏ** — gọi `NhaCungCap_Insert` thay thế (block ThemMoi bị comment trong edit component) | EF | FE comment out → endpoint legacy, không còn dùng từ UI; còn `virtual` để override |
| 3 | Cập nhật thông tin (LEGACY) | (không nối) | POST `api/NhaCungCap/CapNhatThongTin` | `NhaCungCap_CapNhatThongTin` | public | — | Update NCC theo id. **FE đã bỏ** — gọi `NhaCungCap_Udpate` thay thế | EF | FE comment out → legacy |
| 4 | Thêm mới NCC (thực dùng) | nút "Thêm mới" → popup `insertData` | POST `api/NhaCungCap/NhaCungCap_Insert` | `NhaCungCap_ThemMoi` | public | popup Thêm | Insert NCC; nếu trùng mã KH → trả `isConfirm` + `confirmCode` để FE xác nhận "NCC đồng thời là KH" rồi gửi lại kèm `confirmCode` | EF | Quyền key = `NhaCungCap_ThemMoi` (dùng chung với #2, KHÔNG có key riêng `_Insert`) |
| 5 | Cập nhật NCC (thực dùng) | nút "Sửa" → popup `updateData` | POST `api/NhaCungCap/NhaCungCap_Udpate` | `NhaCungCap_CapNhatThongTin` | public | popup Sửa | Update NCC; cùng cơ chế confirm trùng mã KH như #4 | EF | **Tên method/endpoint sai chính tả `Udpate`** (đúng phải Update) — BE+FE cùng sai nên khớp, ĐỪNG tự ý đổi (vỡ FE). Quyền key = `NhaCungCap_CapNhatThongTin` (dùng chung #3) |
| 6 | Tìm theo Id | mở popup Sửa | GET `api/NhaCungCap/TimTheoId/{id:Guid}` | `NhaCungCap_TimTheoId` | public | per-row | Trả NCC; **chặn cross-tenant**: chỉ trả khi `IdDonVi==CurrentUser.DonViId` && `IdNganhNghe==CurrentUser.IdNganhNghe`, ngược lại lỗi "Không tìm thấy nhà cung cấp!" | EF | Invariant bảo mật: guard đơn vị+ngành nghề, đừng bỏ |
| 7 | Xóa NCC | nút "Xóa" (popup xác nhận) | POST `api/NhaCungCap/Xoa/{id:Guid}` | `NhaCungCap_Xoa` (chỉ AD-TONG/AD-CN/AD-MCN) | public | per-row | Guard tenant trước; **chặn xóa nếu** NCC đã có phiếu mua hàng (`IsNccDaCoPhieuMuaHang`) hoặc đã có khách hàng liên quan theo mã (`IsNccDaCoKhachHang`) → trả message lỗi tương ứng | EF + CommonService | Quyền hẹp hơn các action khác (chỉ admin) — chủ ý |
| 8 | Xuất Excel danh sách | menu "Export To Excel" (`exportExcel`, hiện khi `showExportButton`) | POST `api/NhaCungCap/ExportExcelNhaCungCap` | `NhaCungCap_ExportExcelNhaCungCap` | public | menu | File `DanhSachNhaCungCap_{yyyyMMddHHmmss}.xlsx`; lỗi → 503 nếu `Service_Unavailable`, ngược lại 500 code `EXPORT_ERROR`; rỗng → 500 "Không có dữ liệu để xuất" | EF (GemBox) | - |
| 9 | Download file mẫu import | flow Import | POST `api/NhaCungCap/DownloadFileTemplateImportNhaCungCap` | `NhaCungCap_ImportNhaCungCap` | public | popup Import | File `ImportNhaCungCap_Template.xlsx` | EF | - |
| 10 | Import danh sách NCC | popup Import (`apiUpload`, upload file) | POST `api/NhaCungCap/ImportNhaCungCap` | `NhaCungCap_ImportNhaCungCap` | public | popup Import | Nhận `IFormFile`, validate + insert NCC; trả kết quả + id batch lỗi | EF | - |
| 11 | Download dòng import lỗi | flow Import (sau import) | POST `api/NhaCungCap/DownloadDataWrongNhaCungCap/{id}` | `NhaCungCap_ImportNhaCungCap` | public | sau import | File `ImportNhaCungCap_Error.xlsx` các dòng lỗi theo id batch | EF | - |
| 12 | Tìm theo mã | (autocomplete/popup phiếu) | GET `api/NhaCungCap/TimTheoMa/{ma}` | `SBAuthorize()` (rỗng — chỉ cần đăng nhập) | public | — | Trả NCC theo mã trong scope user | EF | Permission key gốc bị comment (`NhaCungCap_TimTheoMa`), còn `[SBAuthorize()]` rỗng — cần xác nhận có chủ ý |
| 13 | Autocomplete NCC | ô search NCC | POST `api/NhaCungCap/SearchNhaCungCapAutoComplete` | `SBAuthorize()` (rỗng) | public | — | Trả list NCC gợi ý theo payload | EF | Quyền rỗng (chỉ đăng nhập) |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] Customer/KhachHang gọi `POST UpdateNccFromKhachHang` — đồng bộ thông tin NCC khi sửa từ phía khách hàng.
- [internal] Customer/KhachHang gọi `POST KhachHangDongThoiLaNhaCungCap` — xử lý khi KH đổi mã hoặc KH đồng thời là NCC (giả định: trùng mã ⇒ là cùng một đối tượng).
- [internal] Customer/KhachHang gọi `GET TimNccAndKhachHang/{id:Guid}` — tra NCC kèm thông tin KH liên kết.
- [internal] Accounting/Order gọi `POST GetNhaCungCapByCodes` (`DoiTuongRequest`) — lấy NCC theo list mã (map chứng từ/đối tượng).
- [internal] Module nhóm gọi `POST GetMaNhaCungCapByNhom` (`ReqGetMaByNhom`) — lấy mã NCC theo nhóm.
- [internal] `POST DanhSachNhaCungCapTuDong` — list NCC tự động trong scope user.
- [internal] `GET DanhSachNhaCungCapTuDong_NoAuth?idDonVi=&useCace=` — list NCC tự động theo idDonVi, có cache (`useCace` mặc định true), KHÔNG cần user → dùng cho job/đồng bộ.

## Quan hệ
- **Gọi sang:** `CommonService` — `IsNccDaCoPhieuMuaHang` (check phiếu mua hàng trước khi xóa), `IsNccDaCoKhachHang` (check KH theo mã trước khi xóa). `SanPham` — `UpdateSanPhamNhaCungCap` (gán NCC cho SP, route ở SanPhamController, FE service gọi qua đây).
- **Dữ liệu (entities):** `NhaCungCap` (Ma, IdDonVi, IdNganhNghe, SuDung, thông tin liên hệ + ngân hàng + công nợ: SoNgayDuocNo/SoNgayNoToiDa). Quan hệ ngầm với `KhachHang` qua trùng `Ma`.
- **Ripple / đồng bộ:** NCC↔KH đồng bộ khi trùng mã (NCC vừa là khách hàng) — sửa NCC có thể kéo cập nhật KH và ngược lại (`UpdateNccFromKhachHang`, `KhachHangDongThoiLaNhaCungCap`). Xóa NCC bị chặn nếu còn ràng buộc (phiếu mua hàng / khách hàng) → tránh mồ côi dữ liệu kế toán.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/NhaCungCapController.cs`
  - L205-218: endpoint thực dùng là `NhaCungCap_Insert` / `NhaCungCap_Udpate` (FE submit qua đây). `ThemMoi`(L44) / `CapNhatThongTin`(L97) là **legacy**, FE đã comment out (edit-quanly-nhacungcap.component.ts L156-185).
  - L213 **lỗi chính tả `Udpate`** (route + method `NhaCungCap_Udpate`); FE service `nhacungcap.service.ts` L66-69 cũng dùng `NhaCungCap_Udpate` → khớp nhau. KHÔNG tự sửa thành Update khi chưa đổi cả 2 phía.
  - L43 chuỗi role thừa dấu phẩy `"DV-AD-TONG,,"` (cosmetic, không ảnh hưởng parse).
  - L130-131 / L146: `TimTheoMa` và `SearchNhaCungCapAutoComplete` để `[SBAuthorize()]` rỗng (permission key bị comment) → chỉ cần đăng nhập là gọi được.
  - L55-57 (TimTheoId), L108 (Xoa): guard cross-tenant bằng `DonViId` + `IdNganhNghe` — invariant bảo mật, giữ nguyên.
  - L17 `public partial class NhaCungCapController` — controller `partial`, có thể còn endpoint ở file partial khác; node này cover file Controllers/NhaCungCapController.cs. *Cần xác nhận* có file partial bổ sung không.
- Lưu ý trùng/liên quan: tồn tại controller riêng `NhaCungCapGhiNoController.cs` (công nợ ghi nợ NCC) — KHÁC node này, mô tả riêng khi cần.
