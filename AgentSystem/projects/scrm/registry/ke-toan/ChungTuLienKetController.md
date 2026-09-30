---
controller: ChungTuLienKetController
module: ke-toan
service: Accounting
man_hinh_ui: "Nội bộ + hỗ trợ màn chi tiết phiếu kế toán: tab/khối 'Chứng từ liên quan' trên màn xem phiếu (kế toán/kho). Cho phép tìm & gắn phiếu liên quan theo mã, xem danh sách phiếu liên quan, xóa liên kết."
muc_dich: "Quản lý liên kết (graph) giữa các chứng từ/phiếu với nhau (phiếu kế toán ↔ phiếu kho): thêm liên kết nếu chưa tồn tại, tìm & thêm phiếu liên quan theo mã (gọi sang Warehouse lấy thông tin phiếu), lấy danh sách phiếu liên quan, xóa liên kết theo id."
alias: ["chứng từ liên kết", "phiếu liên quan", "chung tu lien ket", "liên kết chứng từ", "phiếu liên kết", "chứng từ liên quan"]
auth: public
state_machine: []
deprecated: false
entities: [ChungTuLienKet]
stored_procs: [PRC_SCRM_Accounting_ChungTuLienKet_InsertIfNotExist, PRC_SCRM_Accounting_ChungTuLienKet_LayDanhSachPhieuLienQuan]
depends: [WarehouseService/LayPhieuTheoMa (lấy thông tin phiếu kho theo mã chứng từ + mã phiếu + chi nhánh), SqlSugar (stored procedure)]
cross_module_in: []
cross_module_out: [WarehouseProduct (LayPhieuTheoMa — đọc thông tin phiếu kho để tạo liên kết, không ghi)]
---

# ChungTuLienKetController — Liên kết chứng từ (graph phiếu ↔ phiếu)

> Controller mỏng, lo quan hệ N-N giữa các phiếu/chứng từ (1 bản ghi `ChungTuLienKet` = 1 cạnh: phiếu gốc → phiếu ref). Ghi/đọc qua stored procedure (`PRC_SCRM_Accounting_ChungTuLienKet_*`). Khi tìm theo mã thì gọi sang Warehouse `LayPhieuTheoMa` để resolve id/mã phiếu ref thật rồi mới gắn. Không bút toán, không công nợ. `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]` (TienIch ghi nhãn "Báo cáo kế toán" — có vẻ copy nhầm nhãn).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm liên kết nếu chưa có | (gọi nội bộ / khi gắn phiếu) | `POST api/ChungTuLienKet/ChungTuLienKet_InsertIfNotExist` | `SBAuthorize()` (any login) | public | có đủ thông tin phiếu gốc + phiếu ref | Build `SugarParameter` (IdDonVi/IdChiNhanh/MaChungTu/IdPhieu/MaPhieu + …Ref + DienGiai), gọi SP `..._InsertIfNotExist` (SP tự chống trùng). Trả `true`. | **SP** `PRC_SCRM_Accounting_ChungTuLienKet_InsertIfNotExist` | `[SBAuthorize()]` trống. `catch{...; return; throw;}` → `throw` là dead code sau return (L98). |
| 2 | Tìm & thêm phiếu liên quan theo mã | nút "Thêm phiếu liên quan" (nhập mã) | `POST api/ChungTuLienKet/ChungTuLienKet_SearchAndAdd_ByMaPhieu` | `SBAuthorize()` (any login) | public | nhập mã chứng từ ref + mã phiếu ref | Convert tham số, gọi Warehouse `LayPhieuTheoMa(MaChungTuRef, MaPhieuRef, IdChiNhanhRef, token)`. Không thấy → "Không tìm thấy phiếu." Thấy → set Id/Ma ref từ kết quả Warehouse rồi gọi `ChungTuLienKet_InsertIfNotExist`. | call Warehouse + **SP** insert | `[SBAuthorize()]` trống. `throw` dead code (L144). |
| 3 | Lấy danh sách phiếu liên quan | khối "Chứng từ liên quan" trên màn chi tiết | `POST api/ChungTuLienKet/ChungTuLienKet_LayDanhSachPhieuLienQuan` | `SBAuthorize()` (any login) | public | mở chi tiết 1 phiếu | Gọi SP `..._LayDanhSachPhieuLienQuan` với (IdDonVi/IdChiNhanh/MaChungTu/IdPhieu/MaPhieu), fill `ResChungTuLienKet_LayDanhSachPhieuLienQuan`. | **SP** `..._LayDanhSachPhieuLienQuan` | `[SBAuthorize()]` trống. `throw` dead code (L175). |
| 4 | Xóa liên kết theo id | nút "Bỏ liên kết" | `GET api/ChungTuLienKet/ChungTuLienKet_XoaById/{id}` | `SBAuthorize()` (any login) | public | mỗi dòng liên kết | Tìm `ChungTuLienKet` theo id, nếu có → `DeleteAsync` (hard delete). Luôn trả `true` (kể cả khi id không tồn tại). | **EF** `ChungTuLienKet` | `[SBAuthorize()]` trống. Hard delete + trả true cả khi không tìm thấy (không báo "không tồn tại"). `throw` dead code (L203). |

## Quan hệ
- **Gọi sang:** Warehouse `LayPhieuTheoMa` (resolve phiếu kho theo mã, chỉ ĐỌC). Ghi/đọc liên kết qua stored procedure (SqlSugar).
- **Dữ liệu (entities):** `ChungTuLienKet` (cạnh: IdPhieu/MaPhieu/MaChungTu × IdPhieuRef/MaPhieuRef/MaChungTuRef + IdChiNhanh(Ref) + DienGiai).
- **Ripple / đồng bộ:** KHÔNG. Chỉ là metadata liên kết phiếu; không sinh bút toán, không chạm công nợ, không bắn message.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/ChungTuLienKetController.cs`
  - Cả 4 endpoint `[SBAuthorize()]` trống → chỉ cần login, không key. (Controller phụ trợ nên rủi ro thấp.)
  - `TienIchAttribute(... "Báo cáo kế toán" ...)` (L15) — nhãn TienIch không khớp chức năng (liên kết chứng từ, không phải báo cáo); chỉ ảnh hưởng hiển thị/phân nhóm tiện ích.
- BE `Accounting/Accounting.Service/Generated/ChungTuLienKetService.cs`
  - L98 / L144 / L175 / L203: pattern `return MethodResult...ResultWithError(...); throw;` → câu `throw;` là dead code (không bao giờ chạy) ở cả 4 method.
  - L184-198 `ChungTuLienKet_XoaById`: hard delete, không soft-delete; trả `true` ngay cả khi không tìm thấy bản ghi (idempotent nhưng không phân biệt được "đã xóa" vs "không tồn tại").
- DTO: `Models.Core.DTO.Accounting.Client.ChungTuLienKetModel` (Req…_InsertIfNotExist / _SearchAndAdd_ByMaPhieu / _LayDanhSachPhieuLienQuan, Res…_LayDanhSachPhieuLienQuan).
