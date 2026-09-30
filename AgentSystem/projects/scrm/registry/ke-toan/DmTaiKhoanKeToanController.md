---
controller: DmTaiKhoanKeToanController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Danh mục > 'Hệ thống tài khoản kế toán': cây TK Nợ/Có (tree theo IdCha, cấp tính từ độ dài số TK). Nút: Thêm mới, Sửa, Xóa, Khởi tạo dữ liệu đơn vị (copy bộ TK mẫu hệ thống về đơn vị). Lọc: Keyword (số TK / tên TK)."
muc_dich: "CRUD danh mục hệ thống tài khoản kế toán (sơ đồ TK kiểu cây). 2 tầng dữ liệu: TK mẫu hệ thống (IdDonVi = null) và TK riêng của đơn vị (IdDonVi = DonViId). Là bảng tra số tài khoản (SoTaiKhoan) cho engine bút toán & các phiếu kế toán."
alias: ["danh mục tài khoản kế toán", "hệ thống tài khoản", "sơ đồ tài khoản", "chart of accounts", "tài khoản kế toán", "tk kế toán", "DmTaiKhoanKeToan", "số tài khoản"]
auth: public
state_machine: []
deprecated: false
entities: [DmTaiKhoanKeToan]
stored_procs: []
depends: [IRedisCacheService (cache "TaiKhoanKeToan_Entity_ByDonVi_{idDonVi}"), IRepository<DmTaiKhoanKeToan>]
cross_module_in: []
cross_module_out: []
---

# DmTaiKhoanKeToanController — Danh mục hệ thống tài khoản kế toán (chart of accounts dạng cây)

> CRUD danh mục TK kế toán. **2 tầng:** TK mẫu hệ thống `IdDonVi == null` (dùng làm gốc khởi tạo), và TK của đơn vị `IdDonVi == DonViId`. Tất cả endpoint controller là `[SBAuthorize()]` trống (chỉ cần đăng nhập, không gắn permission key). Service có cả nhóm `_SA` (super admin, sửa bộ mẫu `IdDonVi == null`) nhưng controller **chỉ expose nhóm thường (theo đơn vị)** — nhóm `*_SA` không có endpoint. `Cap` (cấp TK) tự tính theo độ dài `SoTaiKhoan` (≥3 ký tự → `length-2`). Cache theo `TaiKhoanKeToan_Entity_ByDonVi_{idDonVi}` (TTL 60), invalidate sau mọi ghi.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Load cây TK | Lưới tree + lọc Keyword | `POST api/DmTaiKhoanKeToan/GetTreePagingAsync` | — (`[SBAuthorize()]` trống) | public | màn danh mục TK | SuperUser/CSKH → `idDonVi = null` (xem bộ mẫu hệ thống); còn lại → `idDonVi = DonViId`. Lấy node cha (`IdCha == null`) phân trang theo `SoTaiKhoan`, load toàn bộ node con rồi `BuildChildren` đệ quy gắn vào cha. Lọc keyword áp cả cha lẫn con. | EF `DmTaiKhoanKeToan` | Đếm/paging chỉ trên node **cha**; lọc keyword ở con nhưng node con vẫn load **toàn bộ** (không paging) → cây lớn nặng. |
| 2 | Thêm mới TK | Popup "Thêm tài khoản" / Lưu | `POST api/DmTaiKhoanKeToan/ThemMoi` | — | public | nút Thêm mới | Validate SoTaiKhoan + TenTaiKhoan bắt buộc. Chặn trùng `SoTaiKhoan` trong cùng `IdDonVi == DonViId`. Gán `IdDonVi = DonViId`, tính `Cap`. Insert + xóa cache đơn vị. | EF Insert | Message trùng lặp chữ "đã tồn tại đã tồn tại" (L287). |
| 3 | Sửa TK | Popup sửa / Lưu | `POST api/DmTaiKhoanKeToan/CapNhat` | — | public | chọn 1 node | Bắt buộc Id; validate như #2. Tìm theo `Id && IdDonVi == DonViId` (không sửa được TK mẫu hệ thống). Chặn trùng SoTaiKhoan (trừ chính nó) trong đơn vị. Update + tính lại Cap + xóa cache đơn vị. | EF Update | — |
| 4 | Tìm theo Id | (load form sửa) | `POST api/DmTaiKhoanKeToan/TimTheoId?id=` | — | public | trước khi mở form sửa | Trả 1 `ResListTaiKhoanKeToanDto` theo Id (không lọc đơn vị). | EF | Không lọc IdDonVi → có thể xem chi tiết TK của đơn vị khác nếu biết Id. |
| 5 | Xóa TK | Nút Xóa / confirm | `POST api/DmTaiKhoanKeToan/Xoa?id=` | — | public | mỗi node | Tìm theo Id (không lọc đơn vị) → `DeleteAsync` (xóa cứng). Xóa cache: có IdDonVi → xóa key đơn vị đó; null → xóa toàn bộ prefix. | EF Delete (hard) | (a) **Không lọc IdDonVi khi tìm** → biết Id là xóa được cả TK đơn vị khác. (b) Không check TK đang được dùng (con cháu / phiếu kế toán tham chiếu) trước khi xóa → có thể mồ côi cây / lệch bút toán. |
| 6 | Khởi tạo dữ liệu đơn vị | Nút "Khởi tạo" | `POST api/DmTaiKhoanKeToan/KhoiTaoDuLieuDonVi` | — | public | đơn vị chưa có / thiếu TK | Copy bộ TK mẫu hệ thống (`IdDonVi == null`) về đơn vị: chỉ thêm `SoTaiKhoan` chưa có. Sinh Id mới + dựng lại `IdCha` theo mapping Id-cũ→Id-mới (giữ quan hệ cây). Đơn vị đã đủ → trả "Đơn vị đã có đầy đủ tài khoản kế toán mẫu." | EF đọc mẫu + bulk Insert | Quan hệ cha-con dựng lại theo `SoTaiKhoan` đối chiếu mẫu (L251) — nếu mẫu có 2 TK trùng SoTaiKhoan thì map IdCha có thể sai. |

## Quan hệ
- **Gọi sang:** chỉ repo nội bộ + Redis cache. Không gọi service khác.
- **Dữ liệu (entities):** `DmTaiKhoanKeToan` (Id, IdDonVi, IdCha, SoTaiKhoan, TenTaiKhoan, Cap, KieuLoai, IsCanNhapDoiTuong) kế thừa `BaseEntity`.
- **Ripple / đồng bộ:** không bắn message. Là **bảng tra cứu**: số tài khoản (SoTaiKhoan) được dùng làm input cho `TaiKhoanNgamDinh` (template bút toán) và engine `ButToanService` (`GetAllTaiKhoanKeToanEntity` được `ButToanService`/`CommonService` đọc để map số TK).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.Service/Generated/DmTaiKhoanKeToanService.cs`
  - L287 / L86: message "Số tài khoản đã tồn tại đã tồn tại trong hệ thống" (lặp chữ).
  - `Xoa` (L372-391) & `TimTheoId` (L304) **không lọc `IdDonVi`** → tra/xóa chéo đơn vị nếu biết Id (khác hẳn `CapNhat` có lọc đơn vị).
  - `Xoa` xóa cứng (`DeleteAsync`), không kiểm tra TK còn node con / còn được tham chiếu ở phiếu/bút toán.
  - `KhoiTaoDuLieuDonVi` dựng `IdCha` mới bằng cách match `SoTaiKhoan` với mẫu (L251) — phụ thuộc SoTaiKhoan duy nhất trong bộ mẫu.
  - `GetAllTaiKhoanKeToanEntity` (L507): không có data đơn vị thì fallback bộ mẫu hệ thống (`IdDonVi == null`) — đơn vị chưa Khởi tạo vẫn có TK để map bút toán.
- Controller `Accounting/Accounting.API/Controllers/DmTaiKhoanKeToanController.cs`: mọi endpoint `[SBAuthorize()]` trống — không gắn permission key (chỉ cần login).
- Entity `Accounting/Accounting.DataContext/Entity/DmTaiKhoanKeToan.cs`.
