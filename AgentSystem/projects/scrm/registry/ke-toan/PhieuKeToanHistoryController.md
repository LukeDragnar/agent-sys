---
controller: PhieuKeToanHistoryController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > màn 'Lịch sử phiếu kế toán' — list audit các phiếu kế toán đã xóa/khôi phục/đổi trạng thái. Lọc: Loại phiếu, Trạng thái xử lý, Keyword (mã phiếu). Thao tác per-row: Đổi trạng thái (đã xử lý), Xóa bản ghi lịch sử."
muc_dich: "Quản lý LỊCH SỬ/AUDIT phiếu kế toán: ghi nhận hành động DELETE/RESTORE phiếu (ThuChi, ChungTuKhac...) + cho phép xóa thực phiếu gốc kèm chi tiết + bút toán trong transaction, đổi trạng thái xử lý bản ghi lịch sử, xóa dòng lịch sử."
alias: ["lịch sử phiếu kế toán", "audit phiếu kế toán", "xóa phiếu kế toán", "khôi phục phiếu kế toán", "phiếu kế toán history", "trạng thái xử lý phiếu"]
auth: internal
state_machine: [CHUA_XU_LY, DA_XU_LY, DA_KHOI_PHUC]
deprecated: false
entities: [PhieuKeToanHistory, ThuChi, ThuChiDonHang, ChungTuKhac, ChungTuKhacChiTiet, ButToan]
stored_procs: []
depends: [AccountingDataContext (transaction + ExecutionStrategy), BranchService (đã comment, chưa dùng)]
cross_module_in: []
cross_module_out: []
---

# PhieuKeToanHistoryController — Lịch sử / audit phiếu kế toán

> View + thao tác trên bảng audit `PhieuKeToanHistory`. Mỗi bản ghi = 1 hành động (DELETE/RESTORE) lên 1 phiếu kế toán gốc, kèm `LoaiPhieu`, `MaPhieu`, `ActionType`, `TrangThaiXuLy`, `Note`. `DeletePhieuKeToan` không chỉ ghi lịch sử mà còn **xóa cứng phiếu gốc + chi tiết + bút toán** trong transaction (mẫu generic theo loại phiếu). Hiện chỉ hỗ trợ `ThuChi` và `ChungTuKhac`; loại khác trả "chưa hỗ trợ". `RestorePhieuKeToan` đã bị comment ở controller (chỉ còn method service).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm lịch sử | Lưới + lọc (Loại phiếu, Trạng thái xử lý, keyword mã phiếu) | `POST api/PhieuKeToanHistory/TimTheoDieuKien` | `[SBAuthorize()]` (any login) | internal | màn lịch sử | Lọc `IdDonVi` + keyword (`MaPhieu.Contains`) + `LoaiPhieu` + `TrangThaiXuLy`; order `NgayCapNhat DESC`, paging. Trả list + total. | **EF** `PhieuKeToanHistory` | — |
| 2 | Xóa phiếu kế toán (xóa thật phiếu gốc + ghi sử) | "Xóa" trên phiếu kế toán gốc | `POST api/PhieuKeToanHistory/DeletePhieuKeToan` | `[SBAuthorize()]` | internal | phiếu ThuChi / ChungTuKhac | `DeleteGeneric`: lấy phiếu gốc theo (Id, IdDonVi) → tạo `PhieuKeToanHistory{ActionType=DELETE, TrangThai=CHUA_XU_LY}` → transaction: xóa `ButToan` theo IdPhieu + xóa chi tiết + xóa phiếu chính. Loại ≠ {ThuChi, ChungTuKhac} → "chưa được hỗ trợ". | **EF** transaction (`ThuChi`/`ChungTuKhac` + detail + `ButToan`) | Xóa **cứng** (DeleteAsync) cả phiếu, chi tiết, bút toán — không soft-delete. Khối gọi `_branchService.DeletePhieuThuChi` (xóa ThuChi bên Branch) đã bị comment (L224-228) → xóa ThuChi KHÔNG đồng bộ ngược sang Branch. |
| 3 | Đổi trạng thái xử lý | "Đánh dấu đã xử lý" (chọn nhiều) | `POST api/PhieuKeToanHistory/ChuyenTrangThaiPhieu` | `[SBAuthorize()]` | internal | có chọn ≥1 dòng | Set `TrangThaiXuLy=DA_XU_LY` cho các Id thuộc `IdDonVi`. Rỗng Ids → "Vui lòng chọn bản ghi...". | **EF** `PhieuKeToanHistory` | Luôn set DA_XU_LY (không toggle về CHUA_XU_LY). |
| 4 | Xóa bản ghi lịch sử | "Xóa" trên dòng lịch sử | `POST api/PhieuKeToanHistory/XoaPhieu` | `[SBAuthorize()]` | internal | mỗi dòng | Xóa cứng 1 bản ghi `PhieuKeToanHistory` theo (Id, IdDonVi). Id rỗng → lỗi. | **EF** `PhieuKeToanHistory` | Tham số `Guid id` nhận qua body (POST) — *cần xác nhận FE gửi đúng dạng*. |
| 5 | Khôi phục phiếu | (đã ẩn) | `POST RestorePhieuKeToan` | — | internal | — | **ĐÃ COMMENT ở controller** (L43-54). Method service còn (set `IsDelete=false` + ghi sử RESTORE) nhưng KHÔNG có endpoint expose. | EF (dead — không route) | Endpoint bị bỏ; trên UI không gọi được restore. |

## Quan hệ
- **Gọi sang:** `AccountingDataContext` (transaction + `CreateExecutionStrategy`); `IBranchService` (inject nhưng phần gọi xóa ThuChi bên Branch đang comment).
- **Dữ liệu (entities):** `PhieuKeToanHistory` (audit); xóa kéo theo `ThuChi`+`ThuChiDonHang` HOẶC `ChungTuKhac`+`ChungTuKhacChiTiet`, và `ButToan` liên quan (`IdPhieu == entity.Id`).
- **Ripple / đồng bộ:** xóa phiếu gốc → mất bút toán tương ứng. **KHÔNG** đồng bộ ngược sang Branch (đoạn `_branchService.DeletePhieuThuChi` đang comment) → rủi ro lệch dữ liệu ThuChi giữa Accounting và Branch.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuKeToanHistoryController.cs:43-54` — `RestorePhieuKeToan` bị comment toàn bộ → không có route; method service `RestorePhieuKeToan` (PhieuKeToanHistoryService.cs:85) thành dead (không caller).
- BE `Accounting/Accounting.Service/Generated/PhieuKeToanHistoryService.cs:224-228` — phần gọi `_branchService.DeletePhieuThuChi` để xóa đồng bộ bên Branch bị comment → xóa ThuChi chỉ xảy ra ở Accounting, không lan sang Branch.
- BE `...PhieuKeToanHistoryService.cs:195-233` (`DeletePhieuKeToan`) — chỉ map `ThuChi` + `ChungTuKhac`; loại phiếu khác (PhieuNhap/PhieuXuat/PhieuChuyenKho/PhieuBanHang/PhieuMuaHang) hiện trả "chưa được hỗ trợ" dù enum `TypeLoaiPhieu` có liệt kê.
- Tất cả endpoint `[SBAuthorize()]` rỗng — chỉ cần đăng nhập, dù thao tác xóa cứng phiếu + bút toán (nhạy cảm). *Cần xác nhận có chủ ý.*
- `XoaPhieu` (#4) chỉ xóa bản ghi audit, KHÔNG khôi phục phiếu gốc — đừng nhầm với việc đảo hành động delete.
