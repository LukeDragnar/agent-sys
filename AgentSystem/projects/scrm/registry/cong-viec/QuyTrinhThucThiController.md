---
controller: QuyTrinhThucThiController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Quy trình > Danh sách thực thi (khởi chạy 1 quy trình → tạo lượt thực thi + công việc gốc; bấm Bắt đầu để sinh cây công việc con theo flow)"
muc_dich: "Quản lý lượt thực thi quy trình (QuyTrinhThucThi): khởi chạy quy trình thành 1 lượt + công việc gốc, bắt đầu lượt (spawn công việc con từ flow), sinh/kiểm tra mã, xóa lượt (cascade qua CongViec)."
alias: [QuyTrinhThucThi, "thực thi quy trình", "lượt thực thi", "chạy quy trình", "khởi chạy quy trình", process run, process execution]
auth: internal
state_machine: [CHUA_BAT_DAU, DANG_XU_LY, HOAN_THANH]
deprecated: false
entities: [QuyTrinhThucThi, QuyTrinh, QuyTrinhLichSu, CongViec, DmTrangThai, LichSuXoa]
stored_procs: []
depends: [IQuyTrinhThucThiService, ICongViecService, IPublishEndpoint/MassTransit]
cross_module_in: []
cross_module_out: [CongViec (tạo công việc gốc + spawn con qua ICongViecService), ESExtended (publish CongViecChanged), LichSuXoa]
---

# QuyTrinhThucThiController — Lượt thực thi quy trình (khởi chạy → bắt đầu → sinh cây công việc)

> [KiemTraTienIch] bật. Vòng đời 2 nhịp: **Khởi chạy** (`KhoiChay`) tạo lượt + **công việc gốc** trạng thái `CHUA_BAT_DAU` (CHƯA sinh con) → **Bắt đầu** (`BatDau`) chuyển gốc sang `DANG_XU_LY`, set NgayBatDau, rồi gọi `ICongViecService.SpawnCongViecConTuFlowAsync` sinh cây công việc con từ flow của lượt. Mã trạng thái tra theo `DmTrangThai.Ma` của đơn vị (CHUA_BAT_DAU / DANG_XU_LY). Lượt có thể nest (IdQuyTrinhThucThiCha).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm lượt thực thi (lưới) | Lưới Danh sách thực thi | `POST api/QuyTrinhThucThi/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn danh sách | Filter keyword(Ma/TieuDe), IdQuyTrinh, IdTrangThai, người tạo, khoảng NgayBatDau, lượt cha, đối tượng. Enrich TenQuyTrinh/Version/TenTrangThai + **SoCongViec** (đếm CongViec qua FK IdQuyTrinhThucThi, trừ CV gốc). Sort NgayTao DESC. | EF | — |
| 2 | Chi tiết lượt thực thi | Mở popup lượt | `POST api/QuyTrinhThucThi/TimTheoId/{id}` | `[SBAuthorize]` | internal | popup | Trả header + FlowData (từ QuyTrinhLichSu) + danh sách CongViec thuộc lượt (trừ gốc, sort Level→Ma) + danh sách lượt con. | EF | — |
| 3 | **Khởi chạy quy trình** | Nút "Khởi chạy" | `POST api/QuyTrinhThucThi/KhoiChay` | `[SBAuthorize]` | internal | chọn 1 quy trình | Lấy bản ghi lịch sử mới nhất (Version cao nhất) — chưa có → lỗi "Quy trình chưa có phiên bản". Sinh/nhận mã (check trùng). Tạo lượt + **CongViec gốc** (Level 1, CHUA_BAT_DAU). Gán người thực hiện gốc (chọn lúc khởi chạy, fallback người khởi chạy) + áp ThongTinMacDinh. Transaction. Publish CongViecChanged. | EF (transaction) | — |
| 4 | **Bắt đầu lượt** | Nút "Bắt đầu" | `POST api/QuyTrinhThucThi/BatDau/{id}` | `[SBAuthorize]` | internal | lượt CHUA_BAT_DAU (NgayBatDau null) | Đã có NgayBatDau → lỗi "Lượt đã được bắt đầu". Chuyển gốc + lượt sang `DANG_XU_LY`, set NgayBatDau=now, **spawn cây công việc con** từ flow. Publish CongViecChanged cho TẤT CẢ CV của lượt. | EF (transaction) + ICongViecService | Idempotency chỉ chặn bằng NgayBatDau — nếu spawn lỗi giữa chừng rollback toàn bộ. |
| 5 | Sinh mã lượt | Auto-fill ô mã | `POST api/QuyTrinhThucThi/SinhMa/{idQuyTrinh}` | `[SBAuthorize]` | internal | form khởi chạy | `{MaCode quy trình}-{seq+1}` trong đơn vị; quy trình không MaCode → prefix "QTTT". | EF | — |
| 6 | Kiểm tra mã tồn tại | Validate ô mã realtime | `POST api/QuyTrinhThucThi/KiemTraMa` (body Ma) | `[SBAuthorize]` | internal | form | Trả true/false mã đã tồn tại trong đơn vị. | EF | — |
| 7 | Lấy thông tin trước khi xóa | Confirm xóa | `POST api/QuyTrinhThucThi/LayThongTinXoa/{id}` | `[SBAuthorize]` | internal | dialog confirm | Có CV gốc → delegate `CongViec.LayThongTinXoa` (đếm CV+lượt). Chưa có gốc → đếm lượt subtree. | EF | — |
| 8 | Xóa lượt (mềm) | Nút "Xóa" | `POST api/QuyTrinhThucThi/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Có CV gốc → delegate `CongViec.XoaTheoIdAsync` (cascade subtree + lượt nested) với Loai="QuyTrinhThucThi". Chưa BatDau (không CV gốc) → soft-delete lượt + lượt con (đệ quy) + snapshot LichSuXoa. | EF (transaction) + LichSuXoa | — |

## Quy tắc nghiệp vụ (vòng đời lượt)
1. **Khởi chạy**: quy trình phải có ít nhất 1 QuyTrinhLichSu (Version) → nếu chưa lưu quy trình → chặn. Tạo gốc CHUA_BAT_DAU, KHÔNG sinh con.
2. **Bắt đầu**: chỉ khi `NgayBatDau == null`. Gốc → DANG_XU_LY → spawn con.
3. **Mã lượt**: `MaCode-seq`, scope đơn vị, fallback prefix "QTTT".

## Quan hệ
- **Gọi sang:** `IQuyTrinhThucThiService`; `ICongViecService` (tạo/spawn/gán người/áp mặc định/xóa cascade); QuyTrinhLichSu (FlowData + Version).
- **Dữ liệu (entities):** `QuyTrinhThucThi` (gốc), `CongViec` (gốc + con), `QuyTrinh`/`QuyTrinhLichSu` (định nghĩa + version), `DmTrangThai` (CHUA_BAT_DAU/DANG_XU_LY).
- **Ripple/đồng bộ:** publish `CongViecChanged` → ESExtended reindex; xóa → snapshot LichSuXoa (khôi phục).

## Luồng xuyên module
1. **Khởi chạy → Bắt đầu:** chọn quy trình → KhoiChay (lượt + CV gốc CHUA_BAT_DAU) → BatDau (gốc DANG_XU_LY + spawn cây CV con) → các CV xuất hiện ở màn Công việc + ES.
2. **Xóa lượt đã bắt đầu:** delegate sang CongViec.XoaTheoId → soft-delete toàn subtree + lượt nested → 1 bản ghi LichSuXoa (khôi phục cả cụm).

## Lỗi đã biết / lưu ý kỹ thuật
- `QuyTrinhThucThiService.cs:390-393` (`BatDau`/`KhoiChay`) — trạng thái tra theo `DmTrangThai.Ma == "CHUA_BAT_DAU"/"DANG_XU_LY"`; nếu đơn vị **thiếu** bản ghi DmTrangThai mã chuẩn → IdTrangThai = null (lượt/CV không có trạng thái).
- `QuyTrinhThucThiService.cs:485-486` — chặn bắt đầu lại chỉ bằng `NgayBatDau.HasValue`; không có khóa concurrency 2 request Bắt đầu song song (race hiếm).
- Xóa luôn đi qua CongViecService khi có gốc → ripple lớn (xem node CongViecController #4).
