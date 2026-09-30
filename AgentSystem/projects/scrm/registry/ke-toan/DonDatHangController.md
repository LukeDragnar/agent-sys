---
controller: DonDatHangController
module: ke-toan
service: Accounting
man_hinh_ui: "Kho > màn 'Đơn đặt hàng' (TienIch = TI_Kho, nhưng controller thuộc service Accounting). Lưới list + bộ lọc (keyword, trạng thái, kho, trạng thái thanh toán, đã tạo PMH, nhóm/SP, khoảng ngày). Nút: Thêm mới (form + dòng SP), Sửa, Xoá (chỉ khi Nháp), Chuyển trạng thái (duyệt/xử lý), Tạo phiếu mua hàng, Cập nhật thanh toán, Import Excel (popup)."
muc_dich: "Quản lý đơn đặt hàng (PO) với quy trình 2 pha: duyệt (Nháp→Chờ duyệt→Đã duyệt) và xử lý mua (Chờ xử lý→Đang xử lý→Hoàn thành). Là nguồn để tạo phiếu mua hàng; tự hoàn thành khi đã nhận đủ hàng."
alias: ["đơn đặt hàng", "don dat hang", "đặt hàng", "DDH", "PO", "purchase order", "đơn mua hàng", "đặt mua hàng"]
auth: mixed
state_machine: [Nhap, ChoDuyet, DaDuyet, KhongDuyet, TrinhLai, ChoXuLy, DangXuLy, TamDungXuLy, DaHoanThanh, DaHuy]
deprecated: false
entities: [DonDatHang, DonDatHangSanPham, TmpDonDatHangImport, TmpDonDatHangSpImport]
stored_procs: []
depends: [CommonService (SinhMaDonDatHang), WarehouseProduct service (cấu hình quyền tạo đơn theo chi nhánh), PhieuMuaHangService (ThemMoiAsync tạo PMH, HuyPhieuMuaHang khi huỷ cascade)]
cross_module_in: [PhieuMuaHangController/PhieuMuaHangService (CapNhatSoLuongDaNhan + CapNhatDaTaoPhieuMuaHang khi tạo PMH, KiemTraVaCapNhatHoanThanh khi nhận đủ hàng)]
cross_module_out: [Accounting/PhieuMuaHang (TaoPhieuMuaHang sinh phiếu mua từ đơn; ChuyenTrangThai→DaHuy cascade HuyPhieuMuaHang)]
---

# DonDatHangController — Đơn đặt hàng (PO 2 pha: duyệt → xử lý mua, sinh phiếu mua hàng)

> Đơn đặt hàng (purchase order) — controller thuộc **service Accounting** nhưng TienIch gắn **TI_Kho** (menu Kho). State machine 10 trạng thái (`TrangThaiDonDatHangEnum`): Nhap=1, ChoDuyet=2, DaDuyet=3 (**không bao giờ persist — auto→ChoXuLy**), KhongDuyet=4 (terminal), TrinhLai=5, ChoXuLy=6, DangXuLy=7, TamDungXuLy=8, DaHoanThanh=9 (terminal), DaHuy=10 (terminal). 2 pha: **duyệt** (cần quyền duyệt) và **xử lý mua** (cần quyền tạo). Ripple chính: `TaoPhieuMuaHang` sinh phiếu mua hàng; `KiemTraVaCapNhatHoanThanh` tự hoàn thành đơn khi nhận đủ. Auth **mixed** (đa số endpoint có key riêng + role; 1 endpoint thiếu auth; 1 `[PrivateSBAuthorize]`). 100% EF, không message bus (đồng bộ HTTP/internal call).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + bộ lọc | `POST api/DonDatHang/TimTheoDieuKien` | `DonDatHang_TimTheoDieuKien` | public | màn đơn đặt hàng | EF query theo DonVi + phân quyền CN (super/CSKH/admin bypass; user theo CN). Lọc keyword (mã/TenNCC), trạng thái, kho, trạng thái thanh toán, cờ đã tạo PMH, nhóm/SP, khoảng ngày (3 loại). Paging + summary SP/đơn. | **EF** `DonDatHang` | — |
| 2 | Thêm mới đơn | Form / Lưu | `POST api/DonDatHang/ThemMoi` | `DonDatHang_ThemMoi` | public | nút Thêm | Validate NCC + CN + ≥1 SP (SL>0); check quyền tạo theo CN (cấu hình từ WarehouseProduct). Validate quyền duyệt nếu trạng thái=ChoDuyet. Sinh mã nếu trống. Nếu `CanKiemDuyet=false`: ChoDuyet→ChoXuLy tự động. Insert đơn + dòng SP (transaction). | **EF** transaction | — |
| 3 | Xem chi tiết | mở đơn | `GET api/DonDatHang/TimTheoId/{id}` | `DonDatHang_TimTheoId` | public | click dòng | Load đơn + dòng SP (order SoThuTu). | **EF** | — |
| 4 | Cập nhật đơn | Sửa / Lưu | `POST api/DonDatHang/CapNhat` | `DonDatHang_CapNhat` | public | trạng thái ∈ {Nhap, TrinhLai} | Re-validate (NCC/CN/SP). Replace toàn bộ dòng SP (delete old → insert new). Transaction. | **EF** transaction | — |
| 5 | Xoá đơn | nút Xoá / confirm | `DELETE api/DonDatHang/Xoa/{id}` | `DonDatHang_Xoa` | public | chỉ trạng thái Nhap | **Hard delete** đơn + dòng SP (transaction). | **EF** | Xoá cứng. |
| 6 | Cập nhật thanh toán | nút "Thanh toán" | `POST api/DonDatHang/CapNhatThanhToan` | `DonDatHang_CapNhatThanhToan` | public | trên đơn | Set cờ `DaThanhToan`. | **EF** | — |
| 7 | Chuyển trạng thái | nút Duyệt / Xử lý / Tạm dừng / Huỷ | `POST api/DonDatHang/ChuyenTrangThai` | `DonDatHang_ChuyenTrangThai` | public | theo state machine | Validate quyền: pha 1 (DaDuyet/KhongDuyet/TrinhLai) cần quyền duyệt; pha 2 (DangXuLy/TamDungXuLy/DaHoanThanh/DaHuy) cần quyền tạo. Validate luồng chuyển hợp lệ. DaDuyet auto→ChoXuLy. Nếu →DaHuy + `HuyPhieuMuaHangLienQuan=true` → cascade `HuyPhieuMuaHang` cho mọi PMH chưa terminal (fail-fast). | **EF** + PhieuMuaHangService | Cascade huỷ **không atomic** (xem Lỗi đã biết). |
| 8 | Cập nhật SL đã nhận | (nội bộ + thủ công) | `POST api/DonDatHang/CapNhatSoLuongDaNhan` | `DonDatHang_CapNhatSoLuongDaNhan` | public | đang xử lý | Cộng `SoLuongDaNhan`. | **EF** | — |
| 9 | Tạo phiếu mua hàng | nút "Tạo phiếu mua hàng" | `POST api/DonDatHang/TaoPhieuMuaHang` | `DonDatHang_TaoPhieuMuaHang` | public | trạng thái ∈ {ChoXuLy, DangXuLy, TamDungXuLy} | Map dòng SP → tạo PMH (loại MuaHangVaoKho, state DangXuLy) qua `PhieuMuaHangService.ThemMoiAsync`; nếu OK set `DaTaoPhieuMuaHang=true`. | **EF** + PhieuMuaHangService | — |
| 10 | Danh sách PMH theo đơn | tab "Phiếu mua hàng" | `GET api/DonDatHang/LayDanhSachPhieuMuaHang/{idDonDatHang}` | `DonDatHang_LayDanhSachPhieuMuaHang` | public | mở đơn | Query PMH theo IdDonDatHang (wrap `PhieuMuaHangService.TimTheoDieuKienAsync`). | **EF** | — |
| 11 | Kiểm tra quyền tạo đơn | (load màn) | `GET api/DonDatHang/KiemTraQuyenTaoDon` | `DonDatHang_ThemMoi` | public | mở màn | Check user có quyền tạo đơn tại CN của mình. | logic | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `POST CapNhatDaTaoPhieuMuaHang` ([FromBody] Guid) — set cờ `DaTaoPhieuMuaHang=true`; gọi từ `PhieuMuaHangService` sau khi tạo PMH. **KHÔNG có attribute `[SBAuthorize]`** → bất kỳ ai (kể cả chưa đăng nhập, tuỳ pipeline) gọi được — xem Lỗi đã biết.
- [internal] `POST KiemTraVaCapNhatHoanThanh` (`[PrivateSBAuthorize]`) — nhận data SL đã nhận; nếu mọi SP nhận ≥ đặt → set đơn = DaHoanThanh. Fallback `userLogin.DonViId` nếu payload thiếu IdDonVi (gọi nội bộ từ luồng nhận hàng phiếu mua).

## Endpoint Import (partial `DonDatHangController.Import.cs`, tất cả `[SBAuthorize()]` trống)
- `GET DownloadTemplateDonDatHang` → `TemplateDonDatHang.xlsx` (25 cột).
- `POST ImportTemplateDonDatHang` (IFormFile) → validate (type/NCC/CN/kho/SP/nhân viên), lưu `TmpDonDatHangImport` + `TmpDonDatHangSpImport`.
- `POST GetDanhSachDonDatHangImport` → list dòng tmp theo statusImport.
- `POST DeleteTmpDonDatHangImport` → xoá tmp.
- `POST InsertTmpDonDatHangToDonDatHang` → claim batch (TokenXuLy, UPDATE TOP race-safe) rồi dùng `ThemMoi` tạo đơn thật; xoá tmp thành công, giữ tmp lỗi.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **State machine** (`KiemTraLuongChuyenTrangThai`): Nhap→ChoDuyet; ChoDuyet→{DaDuyet(auto→ChoXuLy)|KhongDuyet|TrinhLai}; TrinhLai→ChoDuyet; ChoXuLy→DangXuLy; DangXuLy→{DaHoanThanh|TamDungXuLy}; TamDungXuLy→DangXuLy; bất kỳ non-terminal →DaHuy.
- **Phân quyền 2 pha:** pha duyệt cần quyền duyệt (canDuyet); pha xử lý mua cần quyền tạo (canTao).
- **DaDuyet không bao giờ là trạng thái lưu trữ** — luôn auto chuyển ChoXuLy.
- **Sửa/Xoá khoá theo trạng thái:** CapNhat chỉ {Nhap, TrinhLai}; Xoa chỉ Nhap (hard delete).
- **`CanKiemDuyet=false`** (đơn vị không bật duyệt) → ChoDuyet bỏ qua duyệt, vào thẳng ChoXuLy.

## Quan hệ
- **Gọi sang:** CommonService (sinh mã), WarehouseProduct (cấu hình quyền tạo đơn theo CN), PhieuMuaHangService (tạo/huỷ PMH).
- **Dữ liệu (entities):** `DonDatHang`, `DonDatHangSanPham`, bảng tmp import.
- **Ripple / đồng bộ:** tạo phiếu mua hàng (Accounting), cascade huỷ PMH khi huỷ đơn, tự hoàn thành khi nhận đủ. Không pub/sub — đồng bộ qua internal service call.

## Luồng xuyên module
- **Tạo PMH từ đơn** `TaoPhieuMuaHang` → `PhieuMuaHangService.ThemMoiAsync` sinh phiếu mua (DangXuLy, MuaHangVaoKho), back-ref IdDonDatHang, set `DaTaoPhieuMuaHang=true`.
- **PMH tạo xong** (chiều ngược) → `PhieuMuaHangService` gọi `CapNhatSoLuongDaNhan` + `CapNhatDaTaoPhieuMuaHang` về đơn.
- **Nhận đủ hàng** → `KiemTraVaCapNhatHoanThanh` set đơn = DaHoanThanh.
- **Huỷ đơn** (→DaHuy, HuyPhieuMuaHangLienQuan) → cascade `HuyPhieuMuaHang` cho PMH liên quan chưa terminal.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/DonDatHangController.cs`
  - **L92-97 `CapNhatDaTaoPhieuMuaHang` KHÔNG có `[SBAuthorize]`/`[PrivateSBAuthorize]`** → endpoint thiếu phân quyền hoàn toàn; bất kỳ ai gọi được để bật cờ `DaTaoPhieuMuaHang` cho đơn bất kỳ (rủi ro sai lệch dữ liệu). Nên thêm `[PrivateSBAuthorize]` (vì là internal call).
  - Import endpoints (`DonDatHangController.Import.cs`) dùng `[SBAuthorize()]` trống.
- BE `Accounting/Accounting.Service/Generated/DonDatHangService.cs`
  - Cascade huỷ PMH (≈L803-830) **không atomic**: nếu PMH-A huỷ xong rồi PMH-B fail → PMH-A đã HuyBo, đơn chưa DaHuy → trạng thái lệch, không tự rollback (comment trong code thừa nhận semi-atomic).
  - `XuLyTrangThaiBanDau` (≈L909-922): logic ChoDuyet→ChoXuLy (khi !CanKiemDuyet) và DaDuyet→ChoXuLy đi 2 nhánh khác nhau → dễ nhầm khi đọc.
  - Import: batch size 50 + stuck timeout 30 phút hardcode (không lấy từ config).
