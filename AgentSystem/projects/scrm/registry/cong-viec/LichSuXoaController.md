---
controller: LichSuXoaController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Lịch sử xóa - khôi phục (danh sách bản ghi đã xóa mềm: công việc/lượt thực thi → khôi phục lại hoặc xóa vĩnh viễn)"
muc_dich: "Quản lý lịch sử xóa mềm (LichSuXoa): liệt kê các cụm đã xóa, khôi phục (reset IsDelete dựa snapshot Ids) hoặc xóa vĩnh viễn bản ghi lịch sử."
alias: [LichSuXoa, "lịch sử xóa", "thùng rác", "khôi phục công việc", recycle bin, restore, "undo xóa"]
auth: mixed
state_machine: []
deprecated: false
entities: [LichSuXoa, CongViec, CongViecUser, CongViecMoRong, CongViecFile, QuyTrinhThucThi]
stored_procs: []
depends: [ILichSuXoaService, IPublishEndpoint/MassTransit]
cross_module_in: [CongViecController (Xoa → ghi LichSuXoa), QuyTrinhThucThiController (Xoa → ghi LichSuXoa)]
cross_module_out: [ESExtended (publish CongViecChanged sau khôi phục)]
---

# LichSuXoaController — Lịch sử xóa & khôi phục (thùng rác công việc)

> Bản ghi LichSuXoa được tạo bởi `CongViec.XoaTheoId` / `QuyTrinhThucThi.XoaTheoId`, mang **snapshot Ids** (PhamViXoaSnapshot: CongViecIds, CongViecUserIds, CongViecMoRongIds, CongViecFileIds, QuyTrinhThucThiIds) + cờ `DaKhoiPhuc`. Khôi phục = load các bản ghi soft-deleted (`IgnoreQueryFilters`) theo snapshot rồi **reset IsDelete/NgayXoa/NguoiXoa** về null. KHÔNG sinh lại bản ghi mới — chỉ "bật lại" bản ghi cũ.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm lịch sử xóa (lưới) | Lưới Lịch sử xóa | `POST api/LichSuXoa/TimTheoDieuKien` | `LichSuXoa_TimTheoDieuKien` (chỉ `DV-AD-TONG`) | mixed | màn lịch sử xóa | Filter Loai (CongViec/QuyTrinhThucThi), keyword(TenGoi), DaKhoiPhuc, khoảng ngày xóa. Trả SoCongViec/SoQuyTrinhThucThi/người xóa, sort NgayTao DESC. | EF | Quyền siết cứng `DV-AD-TONG` → chỉ admin tổng đơn vị thấy/quản lý thùng rác. |
| 2 | Khôi phục | Nút "Khôi phục" | `POST api/LichSuXoa/KhoiPhuc/{id}` | `[SBAuthorize]` (any login) | mixed | bản ghi DaKhoiPhuc=false | DaKhoiPhuc=true → lỗi "đã khôi phục trước đó". Snapshot rỗng/hỏng → lỗi. Load CV/CongViecUser/MoRong/File/lượt theo snapshot (bypass query filter) → reset IsDelete=false. Set DaKhoiPhuc=true. Transaction. Publish CongViecChanged cho từng CongViecId. | EF (IgnoreQueryFilters, transaction) | **Quyền action lệch quyền list**: list cần `DV-AD-TONG` nhưng KhoiPhuc/XoaVinhVien chỉ `[SBAuthorize]` (any login) → user thường biết id vẫn gọi được. |
| 3 | Xóa vĩnh viễn | Nút "Xóa vĩnh viễn" | `POST api/LichSuXoa/XoaVinhVien/{id}` | `[SBAuthorize]` (any login) | mixed | — | Xóa cứng bản ghi **LichSuXoa** (không reload/không hard-delete CV trong snapshot). | EF | Chỉ xóa bản ghi lịch sử — CV soft-deleted vẫn nằm DB nhưng **mất đường khôi phục** (snapshot biến mất). |

## Quan hệ
- **Gọi sang:** `ILichSuXoaService`.
- **Dữ liệu (entities):** `LichSuXoa` (gốc + snapshot JSON); khôi phục động chạm `CongViec`/`CongViecUser`/`CongViecMoRong`/`CongViecFile`/`QuyTrinhThucThi` qua Ids snapshot.
- **Ripple/đồng bộ:** sau khôi phục publish `CongViecChanged` (isDelete=false) → ESExtended reindex doc trở lại.

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuXoaController.cs:29` & `:37` — `KhoiPhuc`/`XoaVinhVien` chỉ `[SBAuthorize]`, KHÔNG siết `DV-AD-TONG` như `TimTheoDieuKien` (L21) → bất đối xứng phân quyền.
- `LichSuXoaService.cs:228-245` (`XoaVinhVien`) — chỉ hard-delete bản ghi LichSuXoa, KHÔNG dọn CV soft-deleted trong snapshot → dữ liệu rác tồn đọng + mất khả năng khôi phục.
- `LichSuXoaService.cs:154-179` (`KhoiPhuc`) — dùng `IgnoreQueryFilters` để load bản ghi đã soft-delete; nếu bản ghi đã bị xóa cứng nơi khác thì snapshot khôi phục thiếu (im lặng).
