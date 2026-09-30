---
controller: ButToanController
module: ke-toan
service: Accounting
man_hinh_ui: "Chủ yếu NỘI BỘ (engine sinh bút toán hạch toán Nợ/Có). Một phần hỗ trợ UI: khối 'Bút toán/Hạch toán' khi xem chi tiết phiếu (ButToan_GetByIdPhieu), và popup chọn tài khoản ngầm định khi cấu hình/lập phiếu (GetTaiKhoanNgamDinh_ForSelect)."
muc_dich: "Engine sinh/xóa BÚT TOÁN hạch toán (cặp Nợ–Có) cho mọi loại phiếu kế toán/kho: mua hàng, trả lại hàng mua, bán hàng, bán bị trả lại, bù trừ công nợ, phiếu thu, phiếu chi, phiếu xuất, phiếu nhập, chứng từ khác. Mỗi phiếu được sinh bút toán theo template tài khoản ngầm định (GroupCode → 1 cặp Nợ/Có, Formula tính số tiền)."
alias: ["bút toán", "hạch toán", "but toan", "sinh bút toán", "định khoản", "tài khoản nợ có", "tài khoản ngầm định", "hạch toán phiếu"]
auth: mixed
state_machine: []
deprecated: false
entities: [ButToan, TaiKhoanNgamDinh, DmTaiKhoanKeToan, ThuChi, ThuChiDonHang, PhieuMuaHang, PhieuBanHang, PhieuNhap, PhieuXuat, BuTruCongNo, ChungTuKhac]
stored_procs: []
depends: [TaiKhoanNgamDinhService/GetTkNgamDinhByDonVi (template TK theo MaChungTu/MaLoaiChungTu/MaLoaiQuy), DmTaiKhoanKeToanService (danh mục TK kế toán), EvaluateFormula (tính số tiền từ phiếu/chi tiết), XuLyThemSuaXoaButToan (upsert idempotent bút toán)]
cross_module_in: []
cross_module_out: []
---

# ButToanController — Engine sinh bút toán hạch toán Nợ/Có

> NỘI BỘ là chính: được các service phiếu (mua/bán/thu/chi/nhập/xuất/bù trừ/chứng từ khác) GỌI SANG sau khi tạo/sửa phiếu để sinh định khoản. Mỗi `ButToan_<Loại>_Save(idPhieu)` đọc phiếu + chi tiết, lấy template tài khoản ngầm định theo (MaChungTu, MaLoaiChungTu, MaLoaiQuy), gom theo `GroupCode` để mỗi nhóm sinh 1 cặp Nợ–Có, tính số tiền bằng `EvaluateFormula(template.Formula, phieuBase, detail)`, gắn đối tượng Nợ/Có, rồi `XuLyThemSuaXoaButToan` (upsert idempotent theo idPhieu — sinh lại = xóa cũ thêm mới). Kết quả cập nhật cờ `SinhButToanSuccess/SinhButToanMessErr` lên phiếu gốc. **Phần lớn endpoint không có permission key** (engine nội bộ). `auth: mixed`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem bút toán của 1 phiếu | khối "Hạch toán/Bút toán" trên màn chi tiết phiếu | `GET api/ButToan/ButToan_GetByIdPhieu?idPhieu=&maChungTu=` | `SBAuthorize()` (any login) | public | mở chi tiết phiếu | Lấy list `ButToan` theo (idPhieu, maChungTu), enrich, trả về để hiển thị định khoản. | **EF** `ButToan` | `[SBAuthorize()]` trống. |
| 2 | Lấy TK + TK ngầm định cho form | popup chọn tài khoản khi cấu hình/lập phiếu | `POST api/ButToan/GetTaiKhoanNgamDinh_ForSelect` | (không có) | anonymous | khi mở form chọn TK | Theo `ListFieldName`: với mỗi field trả `{field}_List` (danh mục TK lọc theo `LocTaiKhoan` prefix), `{field}_MacDinh` (TK ngầm định), `{field}_IsShowForm`. Lấy template qua `GetTkNgamDinhByDonVi_ForService` + danh mục TK `GetAllTaiKhoanKeToanEntity`. | **EF** `TaiKhoanNgamDinh`, `DmTaiKhoanKeToan` | KHÔNG có `[SBAuthorize]` nào (cả attribute bị comment) → **endpoint anonymous, không yêu cầu login**. Controller.cs L193-194. |

## Endpoint nội bộ (private / service-to-service)
> Các endpoint `ButToan_*_Save` trên controller là `[HttpGet]` không key (nhiều cái `[SBAuthorize()]` bị COMMENT → thực chất **anonymous nếu gọi trực tiếp**), nhưng thực tế được GỌI NỘI BỘ từ service tương ứng ngay sau khi tạo/sửa phiếu (truyền `userLogin=null`). Gom theo loại phiếu:

- **Sinh bút toán theo phiếu** — mỗi cái đọc phiếu loại đó, sinh cặp Nợ/Có theo template:
  - `ButToan_PhieuMuaHang_Save` (maChungTu **MH**) — gọi từ `PhieuMuaHangService`.
  - `ButToan_PhieuTraLaiHangMua_Save` — trả lại hàng mua.
  - `ButToan_PhieuBanHang_Save` (**BH**) — gọi từ `PhieuBanHangService`.
  - `ButToan_PhieuBanHangBiTraLai_Save` — bán bị trả lại (`PhieuBanTraLaiService`).
  - `ButToan_PhieuBuTru_Save` — bù trừ công nợ (sau `InsertPhieuBuTru`).
  - `ButToan_PhieuThu_Save` (**PT**) / `ButToan_PhieuChi_Save` (**PC**) — phiếu thu/chi (đọc `ThuChi` + `ThuChiDonHang`). Quỹ nhận tiền vào TK Nợ (NhomDoiTuong=QUY_TIEN, LoaiDoiTuong=maLoaiQuy), đối tượng giảm tiền ở Có.
  - `ButToan_PhieuXuat_Save` / `ButToan_PhieuNhap_Save` (**PN**) — xuất/nhập kho.
  - `ButToan_PhieuChungTuKhac_Save` — chứng từ khác (gọi từ `ChungTuKhacService.InsertChungTu` L256).
- **Xóa bút toán theo phiếu** — `GET ButToan_Xoa_IdPhieu?idPhieu=&maChungTu=`: xóa toàn bộ `ButToan` theo (idPhieu, maChungTu) + reset cờ `SinhButToanSuccess/MessErr=null` trên phiếu gốc (switch theo maChungTu: MH→PhieuMuaHang, BH→PhieuBanHang, PT/PC→ThuChi, PN→PhieuNhap, …). Dùng khi xóa/sửa phiếu để định khoản được sinh lại.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Template-driven (TaiKhoanNgamDinh):** `GetTkNgamDinhByDonVi(IdDonVi, MaChungTu, MaLoaiChungTu, MaLoaiQuy)` trả danh sách template; chỉ dòng `SinhButToan==true` mới sinh bút toán. Đơn vị tự thiết lập → ưu tiên TK của đơn vị, không có → fallback TK hệ thống (IdDonVi=null).
- **GroupCode → 1 cặp Nợ–Có:** gom template theo `GroupCode`, mỗi nhóm đại diện 1 dòng định khoản; `IsSummary==true` → sinh 1 lần theo phiếu (head), `IsSummary!=true` → sinh theo từng chi tiết.
- **Formula tính tiền:** `EvaluateFormula(template.Formula, phieuBase, detail)`; `soTien==0` → bỏ qua (không sinh dòng).
- **Idempotent:** `XuLyThemSuaXoaButToan(lstMoi, idPhieu, …)` so với bút toán cũ của phiếu → thêm/sửa/xóa cho khớp (sinh lại an toàn). Lỗi → set `SinhButToanSuccess=false` + `SinhButToanMessErr` (cắt 100 ký tự) trên phiếu.
- **Lọc TK cho form (`GetTaiKhoanNgamDinh_ForSelect`):** `LocTaiKhoan` (chuỗi prefix nối) → chỉ TK `SoTaiKhoan.StartsWith(prefix)`; rỗng → trả toàn bộ danh mục TK của đơn vị.

## Quan hệ
- **Gọi sang:** `TaiKhoanNgamDinhService` (template TK + TK ngầm định cho form), `DmTaiKhoanKeToanService` (danh mục TK kế toán). Đọc đa số entity phiếu (ThuChi, PhieuMuaHang/BanHang, PhieuNhap/Xuat, BuTruCongNo, ChungTuKhac).
- **Dữ liệu (entities):** ghi `ButToan` (mỗi bản ghi = 1 dòng định khoản: TkNo/TkCo + đối tượng Nợ/Có + SoTien + Ref phiếu); cập nhật cờ `SinhButToanSuccess/MessErr` lên phiếu gốc.
- **Ripple / đồng bộ:** Bút toán là dữ liệu nguồn cho báo cáo kế toán + đẩy MISA (sổ định khoản). Controller này KHÔNG tự bắn message; các service phiếu gọi `*_Save`/`Xoa_IdPhieu` để đồng bộ định khoản với vòng đời phiếu.

## Luồng xuyên module
- **Sinh bút toán phiếu thu (ví dụ):** tạo phiếu thu (ThuChi) → service gọi `ButToan_PhieuThu_Save(idPhieu)` → đọc ThuChi + ThuChiDonHang → lấy template (PT, KieuLoaiChiTiet, LoaiQuy) → gom GroupCode → mỗi dòng: Nợ = quỹ nhận tiền (TK ngân hàng/quỹ của phiếu), Có = đối tượng (KH/đơn hàng), số tiền = EvaluateFormula → `XuLyThemSuaXoaButToan` → cập nhật `SinhButToanSuccess` trên phiếu.
- **Sửa/xóa phiếu:** `ButToan_Xoa_IdPhieu(idPhieu, maChungTu)` xóa định khoản cũ + reset cờ phiếu → (nếu sửa) gọi lại `*_Save` để sinh lại.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/ButToan/ButToanController.cs`
  - Nhiều `[SBAuthorize()]` bị COMMENT (L30,43,69,108,134 → MuaHang/TraLaiHangMua/BanHangBiTraLai/PhieuChi/PhieuNhap) → các `*_Save` này **anonymous nếu gọi qua HTTP**. `GetTaiKhoanNgamDinh_ForSelect` (L193-194) cũng không có attribute → anonymous. Là engine nội bộ nhưng vẫn expose route công khai không auth → **rủi ro: gọi trực tiếp có thể sinh/đổi định khoản**.
  - L169-184: `GetTkNgamDinhByDonVi` (controller) + L201-230: `GetAllTaiKhoanKeToan` đều đã comment → dead code trong file controller.
- BE `Accounting/Accounting.Service/Generated/ButToan/ButToanService.cs`
  - File rất lớn (~2441 dòng): 11 hàm `ButToan_<Loại>_Save` lặp cùng khung (đọc phiếu → template → GroupCode → Generate → XuLyThemSuaXoaButToan → set cờ) — sửa logic chung phải đồng bộ cả 11.
  - L2379-2400 (`GetTaiKhoanNgamDinh_ForSelect`): khối lấy TK ngầm định trực tiếp từ repos đã comment, thay bằng `GetTkNgamDinhByDonVi_ForService` — lưu ý khi truy vết nguồn TK.
  - `*_Save` truyền `userLogin` có thể null (gọi nội bộ) → các nhánh dùng userLogin phải null-safe.
- DTO/enum: `Models.Core.Enum.Accounting.EnumAccounting.EnumChungTu_Ma` (MH/BH/PT/PC/PN/PX/BT…), `Models.Core.DTO.Accounting.Client.TaiKhoanNgamDinh` (ReqGetTaiKhoanNgamDinh_ForSelect), entity `ButToan` (Accounting.DataContext).
