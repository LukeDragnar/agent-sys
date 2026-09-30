---
controller: DonViDoiTacTraGopController
module: ke-toan
service: Accounting
man_hinh_ui: "Quản lý danh mục > Cấu hình đối tác trả góp theo đơn vị: chọn/bỏ chọn các đối tác trả góp (DMDoiTacTraGop) áp dụng cho đơn vị + cấu hình chi tiết tài khoản trả góp (gắn NCC, mã/tên tài khoản, chi nhánh, sử dụng). Lưới cấu hình, thêm/sửa/xóa cấu hình, map nhanh checkbox."
muc_dich: "Cấu hình đối tác trả góp ở mức ĐƠN VỊ (mapping DMDoiTacTraGop ↔ đơn vị + tài khoản trả góp gắn NCC). Cung cấp danh sách đối tác trả góp khả dụng cho luồng thanh toán/tạo NCC trả góp; CRUD cấu hình theo (đơn vị × chi nhánh)."
alias: ["đối tác trả góp theo đơn vị", "cấu hình trả góp", "cau hinh tra gop", "đơn vị đối tác trả góp", "map đối tác trả góp", "tài khoản trả góp", "cấu hình đối tác trả góp", "quỹ trả góp"]
auth: public
state_machine: []
deprecated: false
entities: [DonViDoiTacTraGop, DMDoiTacTraGop]
stored_procs: []
depends: [BaseService/Repository (EF CRUD), AutoMapper, RedisCache (key "DoiTacTraGop_ByDonVi_{donVi}"), DMDoiTacTraGop (join lấy tên/ảnh đối tác, lọc IdPhuongThucThanhToan=8), userLogin.IsAdminTong() (gate bắt buộc chi nhánh)]
cross_module_in: []
cross_module_out: []
---

# DonViDoiTacTraGopController — Cấu hình đối tác trả góp theo đơn vị

> Cấu hình mapping + tài khoản trả góp ở mức ĐƠN VỊ: 1 bản ghi `DonViDoiTacTraGop` = (IdDonVi × IdDoiTacTraGop × IdChiNhanh × MaTaiKhoan) gắn NCC. Mọi endpoint scope cứng `IdDonVi = userLogin.DonViId`. Hai kiểu dùng: (1) **map nhanh** check/uncheck đối tác (`MapCauHinhDoiTac`) — chỉ lưu IdDonVi+IdDoiTacTraGop; (2) **cấu hình đầy đủ** tài khoản trả góp gắn NCC (`ThemMoiCauHinhTraGopTaoNcc`/`UpdateTraGopById`) — có validate + chống trùng. Join `DMDoiTacTraGop` (lọc `IdPhuongThucThanhToan=8`) để lấy tên/ảnh/mã đối tác. **Phần CRUD cơ bản (TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien) bị comment toàn bộ** ở controller — chỉ còn các endpoint cấu hình. Mọi `[SBAuthorize]` không kèm key (chỉ login). `[TienIch(MaTienIchSCRM.TI_Chung)]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách đối tác trả góp của đơn vị | combobox/list đối tác khả dụng (form thanh toán) | `GET api/DonViDoiTacTraGop/GetDSDoiTacTraGop` | `SBAuthorize` (any login) | public | form chọn đối tác trả góp | Lấy cấu hình theo `IdDonVi`; ưu tiên `IdChiNhanh==userLogin.IdChiNhanh`, KHÔNG có → fallback `IdChiNhanh==null`. Join `DMDoiTacTraGop` (PTTT=8) → `ResDonViDoiTacTraGop` (tên/ảnh/mã + thông tin tài khoản + NCC). | **EF** `DonViDoiTacTraGop` join `DMDoiTacTraGop` (in-memory) | Join LINQ-to-objects (B4: `from x in listDonViChiNhanh join y in table2`) — `table2` là IQueryable nhưng x đã ToList → enumerate table2 1 lần. Cache đã comment (không cache). |
| 2 | DS đối tác kèm trạng thái checked | màn map (checkbox toàn bộ đối tác) | `GET api/DonViDoiTacTraGop/GetDSDoiTacTraGopCheckedByDonVi` | `SBAuthorize` (any login) | public | màn cấu hình map | LEFT JOIN `DMDoiTacTraGop`(PTTT=8) ← `DonViDoiTacTraGop`(IdDonVi); mỗi đối tác → `IsChecked/SuDung = đã có cấu hình`. | **EF** join 2 bảng | — |
| 3 | Lưới cấu hình đối tác trả góp | Lưới list cấu hình + lọc CN/keyword | `POST api/DonViDoiTacTraGop/GetDanhSachCauHinhDoiTacTraGop` | `SBAuthorize` (any login) | public | màn danh sách cấu hình | Filter theo IdDonVi; lọc `IdChiNhanh` (có giá trị) hoặc `IdChiNhanh==null` khi `IsTkDonVi==true`; keyword theo Ma/TenTaiKhoan. LEFT JOIN `DMDoiTacTraGop` lấy TenDoiTac/Images/MaDoiTac. OrderBy `NgayCapNhat` desc, paging. | **EF** join 2 bảng + Count | — |
| 4 | Xem 1 cấu hình | (mở chi tiết) | `POST api/DonViDoiTacTraGop/GetDonViTraGopById` (body=Guid id) | `SBAuthorize` (any login) | public | click 1 dòng | Lấy theo `Id && IdDonVi`; null → "Không tìm thấy thông tin đối tương". | **EF** `DonViDoiTacTraGop` | typo message "đối tương". |
| 5 | Map nhanh check/uncheck | nút "Lưu" (màn checkbox đối tác) | `POST api/DonViDoiTacTraGop/MapCauHinhDoiTac` | `SBAuthorize` (any login) | public | màn map | So `ListIdChecked` vs cấu hình hiện có (theo IdDonVi): mới chọn → insert `DonViDoiTacTraGop{IdDonVi,IdDoiTacTraGop}` (KHÔNG set chi nhánh/tài khoản); bỏ chọn → delete. Xóa cache `DoiTacTraGop_ByDonVi_{donVi}`. | **EF** Insert + Delete batch | Insert chỉ set IdDonVi+IdDoiTacTraGop (thiếu MaTaiKhoan/NCC/chi nhánh) → bản ghi map nhanh khác bản ghi cấu hình đầy đủ; cần biết FE dùng luồng nào. |
| 6 | Thêm cấu hình trả góp (gắn NCC) | nút "Thêm mới" cấu hình | `POST api/DonViDoiTacTraGop/ThemMoiCauHinhTraGopTaoNcc` | `SBAuthorize` (any login) | public | popup thêm cấu hình | `ValidateCauHinhTraGop` (xem Quy tắc) → tạo `DonViDoiTacTraGop` đầy đủ (chi nhánh, NCC, mã/tên TK, SuDung, IsTkDonVi, PTTT, IdLoaiQuy mặc định QuyTraGop). Insert. | **EF** `DonViDoiTacTraGop` | — |
| 7 | Sửa cấu hình trả góp | nút "Sửa" cấu hình | `POST api/DonViDoiTacTraGop/UpdateTraGopById` | `SBAuthorize` (any login) | public | popup sửa | `ValidateCauHinhTraGop(isUpdate=true)` (loại trừ chính nó khi check trùng) → tìm theo `Id && IdDonVi` (null → "Không tìm thấy") → set field + MarkDirty + Update. **Ép `IdLoaiQuy = ViDienTu`** (khác Insert dùng QuyTraGop). | **EF** `DonViDoiTacTraGop` | Insert dùng `IdLoaiQuy=QuyTraGop` (default entity) nhưng Update ghi đè `IdLoaiQuy=ViDienTu` → lệch loại quỹ giữa thêm và sửa (rủi ro nghiệp vụ, cần xác nhận đúng ý đồ). |
| 8 | Xóa cấu hình | nút "Xóa" | `POST api/DonViDoiTacTraGop/DeleteDonViTraGopById` (body=Guid id) | `SBAuthorize` (any login) | public | mỗi dòng | Tìm theo `Id && IdDonVi` (null → "Không tìm thấy") → `DeleteAsync(entity)`. | **EF** `DonViDoiTacTraGop` | — |
| 9 | Toàn bộ danh mục đối tác trả góp | combobox chọn đối tác (popup cấu hình) | `GET api/DonViDoiTacTraGop/GetDanhMucDoiTacTraGop` | `SBAuthorize` (any login) | public | popup chọn đối tác | Lấy tất cả `DMDoiTacTraGop` `PTTT=8` (không scope đơn vị — master). | **EF** `DMDoiTacTraGop` | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Scope đơn vị:** mọi đọc/ghi đều `IdDonVi == userLogin.DonViId`. Đối tác khả dụng luôn lọc `DMDoiTacTraGop.IdPhuongThucThanhToan == 8` (PTTT trả góp).
- **Validate cấu hình đầy đủ (#6/#7 — `ValidateCauHinhTraGop`):**
  1. Không phải Admin tổng (`!userLogin.IsAdminTong()`) → `IdChiNhanh` bắt buộc (null/Empty → lỗi "Id chi nhánh là bắt buộc"). Admin tổng → chi nhánh không bắt buộc.
  2. `IdNhaCungCap` bắt buộc (null/Empty → lỗi).
  3. `IdDoiTacTraGop` bắt buộc.
  4. `MaTaiKhoan`, `TenTaiKhoan` bắt buộc.
  5. Chống trùng theo (IdDonVi, IdChiNhanh, IdDoiTacTraGop, MaTaiKhoan); khi update loại trừ `Id` của chính nó → trùng = lỗi "Đối tượng đã tồn tại trong hệ thống".
- **Ưu tiên chi nhánh khi đọc list (#1):** lấy theo `IdChiNhanh==userLogin.IdChiNhanh`; nếu rỗng mới fallback `IdChiNhanh==null` (cấu hình cấp đơn vị).
- **IdLoaiQuy:** Insert (#6) để default entity `QuyTraGop`; Update (#7) ghi đè `ViDienTu` → KHÔNG nhất quán (xem known-issue).

## Quan hệ
- **Gọi sang:** không cross-service; chỉ EF (`DonViDoiTacTraGop`, `DMDoiTacTraGop`) + RedisCache.
- **Dữ liệu (entities):** `DonViDoiTacTraGop` (IdDonVi, IdDoiTacTraGop, IdChiNhanh, MaTaiKhoan, TenTaiKhoan, SuDung, IsTkDonVi, IdNhaCungCap/Ma/Ten, IdLoaiQuy, IdPhuongThucThanhToan). Đọc `DMDoiTacTraGop` (master, lọc PTTT=8).
- **Ripple / đồng bộ:** `MapCauHinhDoiTac` xóa cache `DoiTacTraGop_ByDonVi_{donVi}`. Không bắn message/event. Cấu hình này là input cho luồng thanh toán/tạo NCC trả góp (báo cáo trả góp đọc theo `IdDoiTacTraGop`).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/DonViDoiTacTraGopController.cs`
  - L26-111: nguyên khối CRUD cơ bản (TimTheoId/ThemMoi/CapNhatThongTin/Xoa/TimTheoDieuKien) **comment toàn bộ** → controller chỉ còn 9 endpoint cấu hình; `TimTheoDieuKienAsync` ở service vẫn tồn tại nhưng KHÔNG có endpoint expose (dead path).
  - L114-205: tất cả `[SBAuthorize]` không kèm key → chỉ login (không phân quyền chi tiết).
  - #4/#8 nhận `Guid id` qua body (`[HttpPost]` không route param) — FE phải gửi raw Guid trong body.
- BE `Accounting/Accounting.Service/Generated/DonViDoiTacTraGopService.cs`
  - L208 (`UpdateTraGopById`): ép `entity.IdLoaiQuy = (int)EnumThuChi.EnumLoaiQuy.ViDienTu;` trong khi Insert (`ThemMoiCauHinhTraGopTaoNcc`) để mặc định `QuyTraGop` → loại quỹ lệch giữa thêm/sửa. Cần xác nhận ý đồ.
  - L179/L189: message "Không tìm thấy thông tin đối tương." (typo "đối tương").
  - L120-137 (`GetDSDoiTacTraGopByDonVi`): join LINQ-to-objects (`from x in listDonViChiNhanh` đã ToList); cache đã comment (B5) → mỗi call query DB.
  - `MapCauHinhDoiTac` (L329): insert bản ghi map chỉ có IdDonVi+IdDoiTacTraGop (không tài khoản/NCC/chi nhánh) — khác cấu hình đầy đủ; xóa cache `DoiTacTraGop_ByDonVi_{donVi}` (không kèm chi nhánh, trong khi #1 đọc cache theo donVi+chiNhanh đã comment).
  - `TimTheoDieuKienAsync` (L73): if/else OrderBy `NgayTao` giống nhau (sort vô tác dụng) — nhưng không có endpoint gọi (đã comment ở controller).
- DTO: `Models.Core.DTO.Accounting.Client.DonViDoiTacTraGop` (`DonViDoiTacTraGopDto`, `DonViDoiTacTraGopFilterDto`, `ReqGetListCauHinhDoiTac`, `ReqThemMoiCauHinhTraGopTaoNcc`, `ResDonViDoiTacTraGop`, `DoiTacTraGopCheck`, `ListMapCauHinh`). Entity: `Accounting/Accounting.DataContext/Entity/DonViDoiTacTraGop.cs`.
