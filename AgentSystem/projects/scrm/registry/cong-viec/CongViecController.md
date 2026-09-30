---
controller: CongViecController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc (menu Công việc / cong-viec) — lưới cây công việc cha-con, tab động (Gantt/Lịch/List), inline edit từng ô, popup chi tiết + lịch sử + auto-sắp xếp thời gian + import Excel"
muc_dich: "CRUD + tìm kiếm cây công việc (CongViec) — đường tìm tự chọn Elasticsearch hoặc stored proc theo cấu hình đơn vị; lưu/xóa mềm (snapshot khôi phục), cập nhật từng trường inline, import Excel, auto-sắp xếp lịch."
alias: [CongViec, "công việc", task, "cong viec", "công việc cha con", workflow task]
auth: mixed
state_machine: [CHUA_BAT_DAU, DANG_XU_LY, HOAN_THANH]
deprecated: false
entities: [CongViec, CongViecUser, CongViecMoRong, CongViecFile, CongViecMauField, DmCongViecMauField, DmTrangThai, DmMucDoUuTien, LichSuXoa]
stored_procs: [sp_CongViec_TimTheoDieuKien]
depends: [ICongViecService, ICauHinhService, IElasticSearch, IMongoDbClient, IPublishEndpoint/MassTransit, ESExtendedService]
cross_module_in: [QuyTrinhThucThiController (KhoiChay/BatDau tạo công việc gốc + spawn con; Xoa lượt delegate XoaTheoIdAsync), CongViecExternalController (Save dùng chung ICongViecService)]
cross_module_out: [ESExtended (publish CongViecChanged → reindex/xóa doc), MongoDB (CongViecLichSu audit log)]
---

# CongViecController — Quản lý công việc (cây cha-con + tìm kiếm ES/SP)

> Controller chính của module. **Tìm kiếm 2 đường**: `ResolveUseEsAsync` đọc `CauHinh.JsonSetting_SuperAdmin.IsUseElasticSearch` (per đơn vị) → fallback `Search:CongViec:UseES` (global, default true). ES path giữ cây bằng "tập hiển thị" (CV khớp filter + mọi cha tổ tiên). Mọi action ghi đều **publish `CongViecChanged`** (MassTransit) để ESExtended reindex và ghi **CongViecLichSu** vào MongoDB (audit). Xóa là **soft-delete + snapshot vào LichSuXoa** (khôi phục được), KHÔNG xóa cứng.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm công việc (lưới cây, phân trang) | Lưới Công việc + filter tab | `POST api/CongViec/TimTheoDieuKien` | `CongViec_TimTheoDieuKien` (đa role: AD/KT/QLDV/SALE/MKT/KHO...) | public | màn cong-viec | `ResolveUseEsAsync` → ES (`TimTheoDieuKienESAsync`) hoặc SP (`TimTheoDieuKienBySpAsync`). Filter động qua `TabFilter.Conditions` (match all/any), ext-field `ext_<Ma>`, người thực hiện/phê duyệt. CV cha default sort NgayTao DESC, CV con ASC. AdminTong bỏ filter UserId. Trả grid + total + ExtFields + Files + SoCongViecCon. | **SP** `sp_CongViec_TimTheoDieuKien` (3 table: data/users/extfields) **hoặc ES** index `CongViec` của đơn vị (resolve qua ESExtended HTTP) | ES path: security filter theo người tạo/giao đã **comment** (L394-403) → mọi user thấy toàn bộ CV đơn vị (chỉ chặn AdminTong ở SP path). ES visibleIds cap 10.000 → cây lớn có thể thiếu nhánh. |
| 2 | Xem chi tiết công việc | Mở popup chi tiết | `POST api/CongViec/TimTheoId/{id}` | `[SBAuthorize]` (any login) | public | popup | Trả `CongViecDetailDto` (gồm field mở rộng, người thực hiện/phê duyệt, file). Scope theo `CurrentUser.DonViId`. | EF | — |
| 3 | Lưu công việc (thêm/sửa) | Nút "Lưu" / "Thêm mới" | `POST api/CongViec/Save` | `[SBAuthorize]` | public | form | `dto.Id` rỗng → tạo (sinh mã nếu trống, check trùng mã, tính Level = cha+1, **chặn vượt cấp** theo `CauHinh.DoSauCongViecCon` mặc định 3). Có Id → update. Sync `CongViecMoRong` (field động) + `CongViecUser` COTMORONG (field user). Ngày dự kiến strip về date-only. Ghi CongViecLichSu + publish CongViecChanged. | EF + Mongo audit | Endpoint dùng chung cho cả Thêm và Sửa (phân biệt bằng Id). |
| 4 | Xóa công việc (mềm) | Nút "Xóa" | `POST api/CongViec/Xoa/{id}` | `[SBAuthorize]` | public | — | Gom subtree (đệ quy IdCongViecCha) + các lượt QuyTrinhThucThi liên quan (gồm nested) → **soft-delete + snapshot Ids vào LichSuXoa** (Loai="CongViec"), publish CongViecChanged isDelete=true. Khôi phục được ở màn Lịch sử xóa. | EF (transaction) + LichSuXoa | Xóa kéo cả lượt thực thi gắn CV gốc trong subtree — ripple lớn, lưu ý khi xóa CV cha. |
| 5 | Lấy thông tin trước khi xóa | Confirm xóa (đếm ảnh hưởng) | `POST api/CongViec/LayThongTinXoa/{id}` | `[SBAuthorize]` | public | dialog confirm | Trả `SoCongViec` + `SoQuyTrinhThucThi` sẽ bị xóa (gọi `ThuThapPhamViXoaAsync`, không ghi DB). | EF | — |
| 6 | Cập nhật 1 trường (inline) | Sửa ô trực tiếp trên lưới | `POST api/CongViec/CapNhatTruong` | `[SBAuthorize]` | public | inline cell | `dto.Field` (ma/tieuDe/moTa/idTrangThai/idMucDoUuTien/ngay.../estimate/congViecMauId/ext-field). Lưu giá trị cũ → ghi CongViecLichSu (Mongo). Field user → sync CongViecUser. | EF + Mongo audit | Optimistic-concurrency (OT conflict bằng ClientTimestamp) **đã comment** (L2961-2965) → ghi đè không cảnh báo khi 2 user sửa cùng lúc. |
| 7 | Lịch sử công việc | Tab "Lịch sử" trong popup | `POST api/CongViec/LayLichSuCongViec/{idCongViec}` (pageIndex/pageSize) | `[SBAuthorize]` | public | popup | Đọc `CongViecLichSu` từ **MongoDB** (sort Ngay DESC, phân trang), scope IdDonVi. | **Mongo** collection CongViecLichSu | Lịch sử nằm ở Mongo (không EF) — đổi schema field phải sửa cả nơi ghi (Luu/CapNhatTruong/Spawn). |
| 8 | Auto sắp xếp thời gian | Nút "Tự động sắp xếp" (preview) | `POST api/CongViec/TuDongSapXepThoiGian` | `[SBAuthorize]` | public | — | Tính dates mới cho cây CV — **KHÔNG lưu DB**, FE preview rồi gọi CapNhatTruong khi confirm. | EF (read-only) | Chỉ tính, không persist — phải confirm thủ công mới ghi. |
| 9 | Tải template import | Nút "Tải file mẫu" | `GET api/CongViec/DownloadTemplateImport?congViecMauId=` | `[SBAuthorize]` | public | — | Sinh template Excel **động** (cột theo đơn vị + theo mẫu nếu chọn) → file `Template_CongViec.xlsx`. | EF + GemBox | — |
| 10 | Import công việc từ Excel | Nút "Import" | `POST api/CongViec/ImportExcel` (IFormFile, congViecMauId, kiemThu) | `[SBAuthorize]` | public | — | File rỗng → "Chưa chọn file.". Tối đa 1000 bản ghi, trả danh sách lỗi theo dòng. `kiemThu=true` → validate-only không ghi. | EF + GemBox | Giới hạn cứng 1000 dòng. |
| 11 | Dropdown user / nhóm / cột group-by | Dropdown filter | `POST api/CongViec/LayDanhSachUser` · `LayDanhSachNhomPhanTrang` · `LayDanhSachCotGroupByDuoc` | `[SBAuthorize]` | public | filter/group | Trả danh sách user, nhóm phân trang, cột group-by được phép. | EF | — |

## Endpoint nội bộ (service-to-service)
- [internal] `XoaTheoIdAsync(id, userLogin, loaiGhiNhan, idGocGhiNhan, tenGoiGhiNhan)` — overload có tham số ghi nhận, **QuyTrinhThucThiController.XoaTheoId gọi vào** để LichSuXoa được tag Loai="QuyTrinhThucThi" + IdGoc=lượt id (hiện đúng tab khôi phục). Endpoint UI `Xoa/{id}` gọi overload mặc định (Loai="CongViec").
- [internal] `SpawnCongViecConTuFlowAsync`, `GanNguoiThucHienGocAsync`, `ApplyThongTinMacDinhGocAsync` — **QuyTrinhThucThiService** gọi khi Khởi chạy/Bắt đầu lượt (tạo công việc gốc + sinh con từ flow). Không có endpoint riêng.

## Quy tắc nghiệp vụ (gating)
1. **Chọn ES hay SP**: `CauHinh.JsonSetting_SuperAdmin.IsUseElasticSearch` có giá trị → dùng giá trị đó; null → global `Search:CongViec:UseES` (default true). ES lỗi resolve index → trả lỗi "Đơn vị chưa có index".
2. **Giới hạn cấp con** (chỉ khi tạo CV con): `level = cha.Level + 1`; nếu `level > CauHinh.DoSauCongViecCon + 1` (mặc định 3 cấp con) → lỗi "Chỉ cho phép tối đa N cấp công việc con".
3. **Trùng mã**: mã tự sinh luôn duy nhất; mã user nhập → check `IsTrungMaCongViecAsync` trong đơn vị (bỏ qua chính nó khi sửa).

## Quan hệ
- **Gọi sang:** `ICongViecService` (toàn bộ); `ICauHinhService` (cấu hình ES + độ sâu con); `IElasticSearch` + ESExtended HTTP (resolve index, search); `IMongoDbClient` (lịch sử).
- **Dữ liệu (entities):** `CongViec` (gốc) + `CongViecUser` (người thực hiện/COTMORONG) + `CongViecMoRong` (field động EAV) + `CongViecFile` + map `DmTrangThai`/`DmMucDoUuTien`/`CongViecMauField`.
- **Ripple/đồng bộ:** mọi ghi → publish `CongViecChanged` (MassTransit) cho **ESExtended** reindex/xóa doc; ghi audit **CongViecLichSu** (Mongo). Xóa snapshot vào **LichSuXoa** để khôi phục.

## Luồng xuyên module
1. **Khởi chạy quy trình → tạo công việc:** QuyTrinhThucThi.KhoiChay tạo CongViec gốc (CHUA_BAT_DAU) → BatDau chuyển DANG_XU_LY + `SpawnCongViecConTuFlowAsync` sinh cây con → mỗi CV publish CongViecChanged.
2. **Xóa lượt thực thi:** QuyTrinhThucThi.Xoa (có CV gốc) delegate `CongViec.XoaTheoIdAsync` với Loai="QuyTrinhThucThi" → soft-delete subtree + snapshot.

## Lỗi đã biết / lưu ý kỹ thuật
- `CongViecService.cs:394-403` — security filter ES (chỉ thấy CV mình tạo/giao) đã comment → ES path không giới hạn theo user.
- `CongViecService.cs:560` (`MaxMatched=10000`) — cap tập hiển thị ES, cây >10k node có thể mất nhánh.
- `CongViecService.cs:2961-2965` (`CapNhatTruongAsync`) — OT conflict detection đã comment → last-write-wins, mất cảnh báo đụng độ.
- `CongViecExternalController` dùng chung `ICongViecService.LuuAsync` nhưng **không xác thực user thật** (xem node CongViecExternalController) → CV tạo từ ngoài gắn đơn vị cố định.
