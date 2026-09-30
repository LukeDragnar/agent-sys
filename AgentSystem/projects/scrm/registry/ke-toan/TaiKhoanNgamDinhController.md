---
controller: TaiKhoanNgamDinhController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Thiết lập > 'Tài khoản ngầm định': lưới nhóm theo (Mã chứng từ × Mã loại chứng từ × Mã loại quỹ), mỗi nhóm = bộ dòng template Nợ/Có. Popup view/sửa chi tiết các dòng; lọc theo Mã chứng từ / Mã loại chứng từ / Mã loại quỹ + keyword."
muc_dich: "Quản lý template bút toán ngầm định: mỗi dòng = 1 vế Nợ hoặc Có (LoaiNoCo) gắn GroupCode để ghép cặp, theo (MaChungTu, MaLoaiChungTu, MaLoaiChungTuPhu, MaLoaiQuy). Là INPUT chính cho engine ButToanService sinh cặp Nợ/Có khi tạo phiếu kế toán."
alias: ["tài khoản ngầm định", "tk ngầm định", "tài khoản mặc định bút toán", "template bút toán", "định khoản ngầm định", "TaiKhoanNgamDinh", "cấu hình định khoản", "tài khoản hạch toán mặc định"]
auth: mixed   # UI: SBAuthorize trống; nội bộ: PrivateSBAuthorize (service-to-service)
state_machine: []
deprecated: false
entities: [TaiKhoanNgamDinh, DMChungTu, DMLoaiChungTu]
stored_procs: []
depends: [ICommonCacheService (cache TaiKhoanNgamDinh_DonVi / _Final), IRepository<TaiKhoanNgamDinh>, IRepository<DMChungTu>, IRepository<DMLoaiChungTu>]
cross_module_in:
  - "ButToanService (engine sinh bút toán — đọc template qua GetTkNgamDinhByDonVi)"
  - "PhieuBanHangService / PhieuBanTraLaiService / PhieuMuaHangService / ChungTuKhacService / ThuChiService (gọi GetTkNgamDinhByDonVi khi tạo phiếu)"
cross_module_out: []
---

# TaiKhoanNgamDinhController — Template tài khoản ngầm định (sinh cặp Nợ/Có cho bút toán)

> **Bảng mẫu định khoản.** Mỗi bản ghi = 1 vế (`LoaiNoCo` = "NO"/"CO") với `TkNgamDinh` (số TK), gắn `GroupCode` để engine ghép thành cặp Nợ–Có. Khóa nghiệp vụ: `(MaChungTu, MaLoaiChungTu, MaLoaiChungTuPhu, MaLoaiQuy, LoaiNoCo, GroupCode)`.
> **2 tầng dữ liệu:** mẫu mặc định `IdDonVi == null/Guid.Empty` (super user / CSKH sửa) + override theo đơn vị (`IdDonVi == DonViId`). `GetTaiKhoanNgamDinh_DonVi_Final` merge: default làm nền, đơn vị **ghi đè** theo cùng (MaChungTu, MaLoaiChungTu, MaLoaiQuy, GroupCode, LoaiNoCo), dòng mới thì add thêm.
> Endpoint UI dùng `CurrentUser` + `[SBAuthorize()]` trống; 2 endpoint `*_ForService`/`GetTkNgamDinhByDonVi` là `[PrivateSBAuthorize]` cho service-to-service. Đọc dữ liệu chủ yếu từ **cache** (Redis), chỉ `ViewTheoId` lấy base row thẳng DB.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/load lưới nhóm | Lưới + lọc (Mã CT, Mã loại CT, Mã loại quỹ, Keyword) | `POST api/TaiKhoanNgamDinh/TimTheoDieuKien` | — | public | màn TK ngầm định | Lấy list merged theo đơn vị (cache), gom nhóm theo (MaChungTu, MaLoaiChungTu, MaLoaiQuy); mỗi nhóm gắn TenChungTu/TenLoaiChungTu (DMChungTu/DMLoaiChungTu) + TenLoaiQuy (map enum). Map từng dòng → `TkNo`/`TkCo` theo LoaiNoCo. Lọc keyword/điều kiện rồi paging in-memory. | EF `DMChungTu`,`DMLoaiChungTu` + cache | (a) Phân trang **in-memory** sau khi build hết group (load full rồi cắt) → bộ template lớn nặng. (b) `totalCount` đếm trước khi lọc keyword/điều kiện → tổng paging lệch số sau lọc (L212 vs L403+). |
| 2 | Thêm/Sửa hàng loạt | Popup cấu hình nhóm / Lưu | `POST api/TaiKhoanNgamDinh/CreateOrUpdateAsync` | — | public | nút Lưu | Nhận `List<TaiKhoanNgamDinhDto>`. Validate từng dòng: MaChungTu, LoaiNoCo, TkNgamDinh bắt buộc. Khóa upsert = 6 trường (xem header). Dòng có key → **update**; chưa có → **insert** (đơn vị thường thì clone meta từ default cùng key); key còn trong DB mà không có trong request → **delete**. Field meta (MaCot/GroupCode/LoaiNoCo/Formula/SinhButToan...) **chỉ super user/CSKH** mới được sửa. Clear cache đơn vị (và toàn bộ nếu sửa default). | EF Insert/Update/Delete | Empty request (`!listReq.Any()`) chặn ở controller; nhưng "không có trong request → delete" = nếu FE chỉ gửi 1 phần nhóm có thể **xóa nhầm** các dòng cùng MaChungTu không gửi lên (phụ thuộc FE gửi đủ cả nhóm). |
| 3 | Cập nhật 1 dòng | (sửa 1 dòng template) | `POST api/TaiKhoanNgamDinh/UpdateAsync` | — | public | sửa 1 bản ghi | Bắt buộc Id, TkNgamDinh, LoaiNoCo. Non-super → `donViId = DonViId`; super/CSKH → `donViId = Guid.Empty` (sửa default). Tìm theo `Id && IdDonVi == donViId`. Chặn trùng theo (MaChungTu, MaLoaiChungTu, GroupCode, LoaiNoCo, MaCot[, MaLoaiQuy]). Update + MarkDirty. **Không clear cache** sau update. | EF Update | **Không clear cache** ở `UpdateAsync` (khác `CreateOrUpdateAsync`) → lưới/engine có thể đọc cache cũ tới khi TTL hết. |
| 4 | Xóa nhóm | Nút Xóa nhóm | `POST api/TaiKhoanNgamDinh/DeleteAsync?maChungTu=&maLoaiChungTu=&maLoaiQuy=` | — | public | mỗi nhóm | Xóa các dòng `IdDonVi == DonViId && MaChungTu && MaLoaiChungTu && (MaLoaiQuy null hoặc == param)` (xóa cứng, nhiều dòng). | EF Delete | (a) `entity` là `List` → check `is null` luôn false (list rỗng vẫn gọi DeleteAsync, không báo "không tìm thấy"). (b) **Không clear cache** sau xóa. |
| 5 | Xem chi tiết nhóm theo Id | Popup view | `POST api/TaiKhoanNgamDinh/ViewTheoId?id=` | — | public | mở 1 nhóm | Lấy base row theo Id (thẳng DB), chặn nếu IdDonVi không thuộc đơn vị/null. Lấy details cùng (MaChungTu, MaLoaiChungTu, MaLoaiQuy) từ cache merged + add các dòng dùng chung (MaLoaiChungTu null). | DB + cache | — |
| 6 | Xem theo mã tham chiếu | (chọn Mã CT/loại để xem mẫu) | `POST api/TaiKhoanNgamDinh/ViewTheoRef` (body MaChungTu/MaLoaiChungTu/MaLoaiQuy) | — | public | khi thêm mới, chọn option | Lấy details theo (MaChungTu, MaLoaiChungTu) từ cache merged + dòng dùng chung; base row = phần tử đầu list (chỉ để fill header). | cache | `baseRow = lstTaiKhoanNgamDinh.First()` (L829) — header Id/MaChungTu của base row có thể KHÔNG khớp ref được chọn (lấy bừa phần tử đầu). |
| 7 | Xem chi tiết (raw) | — | `POST api/TaiKhoanNgamDinh/ViewChiTiet?id=` | — | public | — | `GetByIdAsync(id)` raw entity (BaseService). | EF | Không lọc đơn vị. |

## Endpoint nội bộ (private / service-to-service)
- [internal] **ButToan & các service phiếu → `GET api/TaiKhoanNgamDinh/GetTkNgamDinhByDonVi`** (`PrivateSBAuthorize`): trả template đã lọc `IsActive == true && SinhButToan == true`, theo (MaChungTu, MaLoaiChungTu[, MaLoaiChungTuPhu, MaLoaiQuy]), `OrderBy(OrderIndex)`. Đây là cửa chính engine bút toán lấy template.
- [internal] **`GET api/TaiKhoanNgamDinh/GetTkNgamDinhByDonVi_ForService`**: như trên nhưng **không** lọc `SinhButToan` (lấy cả dòng chỉ để hiển thị/tính, không sinh bút toán).

## Quy tắc nghiệp vụ (ghép cặp Nợ/Có — blueprint cho engine ButToan)
1. **Merge 2 tầng** (`GetTaiKhoanNgamDinh_DonVi_Final`): default (IdDonVi null/Empty) làm nền → đơn vị ghi đè theo (MaChungTu, MaLoaiChungTu, MaLoaiQuy, GroupCode, LoaiNoCo); dòng mới của đơn vị → add.
2. **Lọc cho engine** (`GetTkNgamDinhByDonVi`): `IsActive == true && SinhButToan == true`, khớp MaChungTu; `MaLoaiChungTu` khớp **hoặc null** (dòng dùng chung); tương tự MaLoaiChungTuPhu/MaLoaiQuy (null = áp dụng chung).
3. **Ghép cặp:** `ButToanService` `GroupBy(GroupCode)` — mỗi GroupCode sinh **1 cặp Nợ–Có**; dòng `IsSummary == true` xử lý trước (tổng), `IsSummary != true` sau (chi tiết). `LoaiNoCo` quyết định vế Nợ ("NO") hay Có ("CO"); `FormulaTk`/`Formula` định công thức số TK & số tiền.
4. **Override quyền sửa meta:** chỉ super user/CSKH được sửa các cột định nghĩa (MaCot, LoaiNoCo, GroupCode, Formula, FormulaTk, OrderIndex, IsSummary, SinhButToan...) ở `CreateOrUpdateAsync`; đơn vị thường chỉ sửa LocTaiKhoan/TkNgamDinh/IsActive/DienGiai.

## Quan hệ
- **Gọi sang:** `ICommonCacheService` (cache template theo đơn vị + final-merged), `IRepository<DMChungTu>`/`<DMLoaiChungTu>` (đổ tên hiển thị).
- **Dữ liệu (entities):** `TaiKhoanNgamDinh` (xem entity). `TkNgamDinh` là số TK tham chiếu sang `DmTaiKhoanKeToan.SoTaiKhoan`.
- **Ripple / đồng bộ:** đổi template → clear cache đơn vị → lần tạo phiếu kế toán kế tiếp engine `ButToanService` đọc template mới sinh bút toán (cặp Nợ/Có) khác đi. Không bắn message; ảnh hưởng gián tiếp qua engine bút toán của mọi phiếu (BH/MH/Trả lại/Thu chi/Chứng từ khác).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.Service/Generated/TaiKhoanNgamDinhService.cs`
  - `UpdateAsync` (L951-1029) & `DeleteAsync` (L1032-1047) **không clear cache** → đọc qua cache có thể trễ tới khi TTL hết (lệch với `CreateOrUpdateAsync` có clear cache L639-647).
  - `DeleteAsync` L1036: `entity` là `List<...>`, check `is null` không bao giờ true → list rỗng vẫn `DeleteAsync` (no-op) thay vì trả "Không tìm thấy".
  - `TimTheoDieuKien`: paging in-memory; `totalCount` (L212) tính trước khi lọc keyword/điều kiện (L403-417) → tổng phân trang không khớp số sau lọc.
  - `ViewTheoRef` L829: `baseRow = lstTaiKhoanNgamDinh.First()` lấy bừa phần tử đầu danh sách merged → các field header (Id/MaChungTu/MaLoaiQuy) của response không bám theo ref được chọn.
  - `CreateOrUpdateAsync`: cơ chế "key có trong DB nhưng không có trong request → delete" (L624-625) — nếu FE không gửi đủ tất cả dòng cùng MaChungTu thì các dòng thiếu bị xóa.
- Entity `Accounting/Accounting.DataContext/Entity/TaiKhoanNgamDinh.cs` — cờ chính: `IsActive`, `SinhButToan`, `IsSummary`, `GroupCode`, `LoaiNoCo`, `OrderIndex`, `Formula`/`FormulaTk`.
- Controller `Accounting/Accounting.API/Controllers/TaiKhoanNgamDinhController.cs`: UI = `[SBAuthorize()]` trống; `GetTkNgamDinhByDonVi*` = `[PrivateSBAuthorize]`.
