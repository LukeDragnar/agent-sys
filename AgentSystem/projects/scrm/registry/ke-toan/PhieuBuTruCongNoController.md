---
controller: PhieuBuTruCongNoController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Phiếu bù trừ công nợ: màn lập phiếu bù trừ giữa công nợ phải thu (KH) và phải trả (NCC) của cùng 1 đối tượng. Bước lấy chứng từ (GetChungTu) → chọn dòng phải thu/phải trả → lập phiếu (InsertPhieuBuTru). Lưới danh sách (TimTheoDieuKien), xem chi tiết, xóa phiếu."
muc_dich: "Lập/quản lý phiếu bù trừ công nợ: gom chứng từ phải thu (từ Customer) + phải trả (từ Warehouse) của 1 đối tượng, tạo phiếu bù trừ, ghi nhận giảm công nợ 2 bên (KH & NCC) trong 1 transaction, đẩy sang MISA, và rollback công nợ khi xóa phiếu."
alias: ["bù trừ công nợ", "phiếu bù trừ", "bu tru cong no", "cấn trừ công nợ", "bù trừ phải thu phải trả", "công nợ bù trừ"]
auth: public
state_machine: []
deprecated: false
entities: [BuTruCongNo, BuTruCongNoChiTiet, AccountingRollback, PhieuKeToanHistory, DataReceiveTaoPhieu]
stored_procs: []
depends: [CustomerService/GetCongNoPhaiThu + GhiNhanBuTruCongNoKH, WarehouseService/GetCongNoPhaiTra + GhiNhanBuTruCongNoNCC, RollbackAccounting (CallApiRoleBackWarehouse/Customer), MisaDanhMucSync, EF transaction (BeginTransaction/ExecutionStrategy), ButToanController/ButToan_PhieuBuTru_Save (sinh bút toán)]
cross_module_in: []
cross_module_out: [Customer (GhiNhanBuTruCongNoKH — giảm công nợ phải thu KH; rollback khi xóa), WarehouseProduct (GhiNhanBuTruCongNoNCC — giảm công nợ phải trả NCC; rollback khi xóa), MISA (DataReceiveTaoPhieu — job đẩy phiếu bù trừ)]
---

# PhieuBuTruCongNoController — Phiếu bù trừ công nợ (cấn trừ phải thu ↔ phải trả)

> Nghiệp vụ tài chính có RIPPLE 2 chiều cross-service: 1 phiếu bù trừ = giảm đồng thời công nợ phải thu (Customer) và phải trả (Warehouse) của cùng đối tượng, bọc trong EF transaction + bản ghi `AccountingRollback` để khôi phục nếu 1 bên fail. Có chống xóa phiếu tạo tự động (`IsTaoTuDong`). Bút toán hạch toán Nợ/Có do `ButToan_PhieuBuTru_Save` sinh riêng. `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy chứng từ phải thu/phải trả | nút "Lấy chứng từ" (popup lập phiếu) | `POST api/PhieuBuTruCongNo/GetChungTu` | `SBAuthorize()` (any login) | public | đã chọn đối tượng + TK phải thu/trả + ngày | Validate `MaDoiTuong`, `NgayBuTru`, ít nhất 1 trong TkPhaiThu/TkPhaiTra. Gọi Customer `GetCongNoPhaiThu` (gắn thêm thông tin phiếu bán hàng theo IdDonHang) + Warehouse `GetCongNoPhaiTra` (chạy `Task.WhenAll`). Trả gộp list `ChungTuButruViewModel` (mỗi dòng = 1 chứng từ thu/trả). | call cross-service + EF `PhieuBanHang` | `[SBAuthorize()]` trống. Nếu Customer trả lỗi → fail luôn; nếu Warehouse 1 task lỗi → trả lỗi (foreach kiểm). |
| 2 | Lập phiếu bù trừ | nút "Lưu phiếu bù trừ" | `POST api/PhieuBuTruCongNo/InsertPhieuBuTru` | `SBAuthorize()` (any login) | public | popup lập phiếu, đã chọn dòng | `ValidateBuTruRequest`. Sinh `SoChungTu` nếu trống (`SinhMaPhieu`), nếu có thì chống trùng (DonVi×CN×SoChungTu). Tạo `BuTruCongNo` + N `BuTruCongNoChiTiet`. Tạo `AccountingRollback`. Trong EF transaction: insert phiếu + chi tiết → `Task.WhenAll(GhiNhanBuTruCongNoNCC, GhiNhanBuTruCongNoKH)`; cả 2 OK → commit + set rollback ThanhCong; bất kỳ bên fail → set rollback=Rollback + KHÔNG commit. Thành công → đồng bộ danh mục MISA + insert `DataReceiveTaoPhieu` (job đẩy MISA); nếu `IsTaoTuDong` thì cập nhật `IdPhieuLienQuan` vào PhieuMuaHang/PhieuBanHang. | **EF** `BuTruCongNo`(+ChiTiet), `AccountingRollback`, `DataReceiveTaoPhieu` + call cross-service | `[SBAuthorize()]` trống → không kiểm key (chỉ login). Ghi nhận công nợ 2 bên chạy trong `Task.Run` bên trong transaction (DbContext không thread-safe) — rủi ro nếu 2 call cùng chạm context (cần xác nhận: 2 call này là HTTP cross-service nên có thể an toàn). |
| 3 | Danh sách phiếu bù trừ | Lưới list + bộ lọc | `POST api/PhieuBuTruCongNo/TimTheoDieuKienAsync` | `PhieuBuTruCongNo_TimTheoDieuKien` | public | màn danh sách | Lọc theo điều kiện `BuTruCongNoFilterDto`, paging. (Endpoint DUY NHẤT có key phân quyền thật.) | **EF** `BuTruCongNo` | — |
| 4 | Xem chi tiết phiếu | (mở chi tiết) | `GET api/PhieuBuTruCongNo/ViewChiTiet?idPhieuBuTru=` | `SBAuthorize()` (any login) | public | click 1 phiếu | Lấy phiếu + chi tiết theo id. | **EF** `BuTruCongNo`(+ChiTiet) | `[SBAuthorize()]` trống. |
| 5 | Xóa phiếu bù trừ | nút "Xóa" / confirm | `POST api/PhieuBuTruCongNo/XoaPhieu?idPhieuBuTru=` | `SBAuthorize()` (any login) | public | phiếu KHÔNG phải tạo tự động | Tìm phiếu theo id+DonVi+CN. `IsTaoTuDong==true` → chặn "được tạo tự động, không thể xóa". Trong transaction: ghi `PhieuKeToanHistory`(DELETE) → hard delete phiếu + chi tiết → rollback công nợ từng chi tiết (`ChungTuPhaiTra`→ rollback Warehouse; `ChungTuPhaiThu`→ rollback Customer); mọi rollback OK → commit, ngược lại RollbackAsync. | **EF** `BuTruCongNo`,`BuTruCongNoChiTiet`,`PhieuKeToanHistory` + call rollback cross-service | Xóa cứng (hard delete). Bút toán đã sinh KHÔNG thấy bị xóa ở đây (cần `ButToan_Xoa_IdPhieu(maChungTu=BT)` riêng — xác nhận có gọi không). |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Sinh/chống trùng số chứng từ:** `SoChungTu` trống → tự sinh (`SinhMaPhieu(PhieuBuTru)`); có giá trị → chống trùng theo (IdDonVi, IdChiNhanh, SoChungTu).
- **Ghi nhận bù trừ 2 bên là all-or-nothing:** Insert chỉ commit khi CẢ Customer (`GhiNhanBuTruCongNoKH`) lẫn Warehouse (`GhiNhanBuTruCongNoNCC`) trả Success. Một bên fail → toàn bộ transaction rollback + `AccountingRollback` đánh dấu để job khôi phục.
- **Chứng từ phải thu/phải trả phân loại theo `KieuLoai`:** `"ChungTuPhaiThu"` (KH) vs `"ChungTuPhaiTra"` (NCC); `SoTienBuTru > 0` mới đưa vào danh sách ghi nhận giảm công nợ.
- **Phiếu tạo tự động (`IsTaoTuDong`):** không cho xóa thủ công; khi tạo tự động thì ghi ngược `IdPhieuLienQuan` vào phiếu mua/bán gốc, và `DataReceiveTaoPhieu.Status=Pending` (chưa đẩy ngay); tạo thủ công → `Status=Received` (job bốc gửi MISA).

## Quan hệ
- **Gọi sang:** Customer (`GetCongNoPhaiThu`, `GhiNhanBuTruCongNoKH`), Warehouse (`GetCongNoPhaiTra`, `GhiNhanBuTruCongNoNCC`), `RollbackAccounting.CallApiRoleBackCustomer/Warehouse` (khi xóa), `MisaDanhMucSync.CheckDongBoDoiTuong`.
- **Dữ liệu (entities):** `BuTruCongNo` + `BuTruCongNoChiTiet`; `AccountingRollback` (cờ IsCustomer/IsWarehouse để job khôi phục); `PhieuKeToanHistory` (log xóa); `DataReceiveTaoPhieu` (queue đẩy MISA).
- **Ripple / đồng bộ:** Tạo phiếu → giảm công nợ KH (Customer) + NCC (Warehouse) → đẩy MISA; Xóa phiếu → hoàn (rollback) công nợ 2 bên + log history. Bút toán Nợ/Có: `ButToan_PhieuBuTru_Save` (ButToanController).

## Luồng xuyên module
- **Lập phiếu bù trừ:** chọn đối tượng → GetChungTu (Customer phải thu + Warehouse phải trả) → InsertPhieuBuTru → [EF transaction] insert phiếu + GhiNhanBuTruCongNoKH (Customer giảm phải thu) + GhiNhanBuTruCongNoNCC (Warehouse giảm phải trả) → commit → MISA danh mục + DataReceiveTaoPhieu → (riêng) ButToan_PhieuBuTru_Save sinh hạch toán.
- **Xóa phiếu bù trừ:** XoaPhieu → [EF transaction] PhieuKeToanHistory(DELETE) + hard delete + rollback công nợ KH/NCC theo từng chi tiết → commit.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuBuTruCongNoController.cs`
  - Chỉ `TimTheoDieuKienAsync` (L72) có key `PhieuBuTruCongNo_TimTheoDieuKien`; `GetChungTu`/`InsertPhieuBuTru`/`ViewChiTiet`/`XoaPhieu` đều `[SBAuthorize()]` trống → chỉ cần login. **InsertPhieuBuTru (ghi tài chính) không có permission key** — rủi ro phân quyền.
  - L41-52: `TaoPhieuBuTru` (endpoint cũ) đã comment toàn bộ → dead/loại bỏ.
- BE `Accounting/Accounting.Service/Generated/BuTruCongNoService.cs`
  - L733-746: `GhiNhanBuTruCongNoNCC`/`KH` chạy trong `Task.Run(...)` rồi `Task.WhenAll` bên TRONG `BeginTransactionAsync` của EF context — cần xác nhận 2 call này hoàn toàn là HTTP (không chạm `_accountingDataContext`) để tránh DbContext concurrency.
  - L848-851: phân biệt `IsTaoTuDong` để set `DataReceiveTaoPhieu.Status` Pending/Received.
  - XoaPhieu (L272): hard delete phiếu + chi tiết; KHÔNG thấy gọi xóa bút toán đã sinh — cần kiểm có gọi `ButToan_Xoa_IdPhieu` ở luồng nào khác không.
- DTO/enum: `Models.Core.DTO.Accounting.Client.BuTruCongNo` (BuTruRequest, BuTruCongNoFilterDto, GetChungTuRequest, ChungTuButruViewModel).
