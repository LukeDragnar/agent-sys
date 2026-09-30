# Tính lại giá vốn theo kỳ

> **Module**: WarehouseProduct | **Route FE**: nút trên `/warehouse-product/quan-ly-ton-kho` (mở modal, không trang riêng) | **Controller prefix BE**: `api/GiaVonTheoKy`
>
> ⚠️ **QUY TẮC DUY TRÌ TÀI LIỆU** — Mỗi lần sửa code thuộc feature này (BE `WarehouseProduct/.../GiaVonTheoKy*`, Mongo collection `GiaVonTheoKyJobUpdateLog`, FE component `tinh-lai-gia-von-modal/**`, migration script SQL liên quan) **PHẢI cập nhật lại file này** ngay trong cùng commit.

---

## 1. Tính năng là gì

Tính lại giá vốn hàng hoá theo kỳ (tháng/quý/năm) cho từng (SP × Kho) bằng công thức trung bình gia quyền từ các phiếu nhập đủ điều kiện. Snapshot lưu vào bảng riêng + áp ngược về phiếu xuất, đơn hàng quầy, đơn hàng logistic trong kỳ. Tiến trình chạy ngầm qua MassTransit, có UI theo dõi (không realtime — user manual reload).

**Lý do nghiệp vụ:** giá vốn (`KhoChiTiet.GiaNhapTrungBinh`) hiện tính rolling mỗi lần nhập, dễ sai/lệch khi sửa phiếu cũ. Tính năng cho phép kế toán đồng bộ lại giá vốn cho 1 khoảng thời gian, đảm bảo phiếu xuất + đơn hàng kỳ đó cùng tham chiếu 1 con số nhất quán.

**Trạng thái V1 — scope hẹp:**
- Chỉ Admin tổng đơn vị dùng được.
- Mỗi đơn vị tối đa 1 tiến trình tại 1 thời điểm.
- Snapshot lưu granularity **tháng** (chọn quý/năm → tách thành 3/12 row tháng, mỗi tháng giá vốn độc lập).
- KHÔNG có revert tự động — tính sai chạy lại job khác để đè (UPSERT in-place).
- KHÔNG SignalR — user vào modal, bấm "Tải lại" để xem tiến độ.

---

## 2. Vị trí trong UI

- Nút **"Tính lại giá vốn theo kỳ"** trên toolbar trang `/warehouse-product/quan-ly-ton-kho`.
- Chỉ hiện khi `user.IsAdminTongDonVi = true` + feature flag `CauHinhWarehouseProductSetting.IsBatTinhLaiGiaVonTheoKy = true`.
- Bấm → mở `nz-modal` width ~900px (`TinhLaiGiaVonModalComponent`).
- Modal có 3 tab; tab nào hiển thị tuỳ trạng thái:
  - **Tab "Tiến trình đang chạy"** — hiện ra (active mặc định) nếu API `GET DangCoTienTrinhDangChay` trả `HasRunning=true`.
  - **Tab "Chạy tính mới"** — disable khi có job đang chạy.
  - **Tab "Lịch sử"** — luôn có, paging job đã kết thúc.

---

## 3. Cấu hình bật/tắt theo đơn vị

Bám pattern feature flag của tính năng "Sửa/Xóa phiếu hoàn thành" (`PhieuHoanThanhChinhSua`):

- Lưu trong [CauHinh.cs](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.DataContext/Entity/CauHinh.cs) — field `JsonSetting_SuperAdmin` (JSON-serialize).
- Thêm property mới `IsBatTinhLaiGiaVonTheoKy: bool` vào DTO `CauHinhWarehouseProductSetting_SuperAdmin`.
- **Default `true` ở giai đoạn dev** để debug; **revert `false` trước go-live** rồi bật thủ công cho khách đã được training.
- Cache key có sẵn: `CK_CauHinhWarehouseProductSetting_{IdDonVi}` ([CauHinhService.cs:56](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.Service/Generated/CauHinhService.cs#L56)).
- Mọi endpoint `api/GiaVonTheoKy/*` gắn attribute dùng chung `[RequireFeatureFlag(nameof(CauHinhWarehouseProductSetting.IsBatTinhLaiGiaVonTheoKy))]` → 403 nếu tắt.

---

## 4. Backend — file layout

| Loại | Đường dẫn |
|------|-----------|
| Entity SQL | `WarehouseProduct/WarehouseProduct.DataContext/Entity/KhoChiTietGiaVonTheoKy.cs`, `GiaVonTheoKyJob.cs` |
| Mongo collection | `WarehouseProduct/WarehouseProduct.DataContext/CollectionMongoDB/GiaVonTheoKyJobUpdateLog.cs` |
| DbContext map | `WarehouseProduct.DataContext/Generated/DbContexts/WarehouseProductDataContext.cs` |
| Migration SQL | `WarehouseProduct/Database/Migrations/20260601_AddGiaVonTheoKyTables.sql` |
| Enum | `Libs/Models.Core/Enum/WarehouseProduct/EnumGiaVonTheoKy.cs` |
| Service | `WarehouseProduct.Service/Generated/GiaVonTheoKy/{IGiaVonTheoKyJobService, GiaVonTheoKyJobService, IGiaVonRecalculator, AvgEligibleImportRecalculator, GiaVonTheoKyApplyService}.cs` |
| Controller | `WarehouseProduct/WarehouseProduct.API/Controllers/GiaVonTheoKyController.cs` |
| Consumer | `WarehouseProduct/WarehouseProductConsumer.API/Consumer/RecalculateGiaVonTheoKyConsumer.cs` |
| Contract DTO | `Libs/Models.Core/DTO/WarehouseProduct.Client/GiaVonTheoKy/GiaVonTheoKyDto.cs` |
| MassTransit message | `Libs/Models.Core/MessageQueue/WarehouseProduct/RecalculateGiaVonTheoKyCommand.cs` |

DI register: Program.cs / DependencyInjectionService trong `WarehouseProduct.Service/Generated/Configs/`.

---

## 5. DB Schema

### 5.1 `KhoChiTietGiaVonTheoKy` (SQL Server)

Snapshot giá vốn của 1 SP tại 1 kho cho **1 tháng** theo 1 công thức. Mỗi `(DonVi, Kho, SP, KyNam, KyThang, CongThuc)` có đúng 1 row — UPSERT in-place khi chạy lại.

| Field | Type | Mô tả |
|-------|------|------|
| (BaseEntity) | — | Id, IsDelete, NgayTao, NguoiTao*, NgayCapNhat, NguoiCapNhat*, NgayXoa, NguoiXoa* |
| `IdDonVi` | Guid | Tenant |
| `IdJob` | Guid | Job tạo/cập nhật row này gần nhất |
| `IdChiNhanh` | Guid? | Chi nhánh của kho (denormalize tiện query) |
| `IdKho` | Guid | |
| `IdSanPham` | Guid | |
| `KyNam` | int | |
| `KyThang` | int | 1-12, luôn có giá trị |
| `CongThuc` | int | 1 = AvgEligibleImport (V1), 2 = HistoricalReplay (V2) |
| `IdDonViTinhNhoNhat` | Guid? | Đơn vị nhỏ nhất tại thời điểm tính (snapshot từ `BangQuyDoiJson` của SP) |
| `GiaVonTinhRa` | decimal(18,4) | **Giá vốn / 1 đơn vị nhỏ nhất** (đã quy đổi) |
| `TongSoLuongNhapDonViNhoNhat` | decimal(18,4) | SUM(SoLuong × SoLuongToDvNho dòng) |
| `TongTienNhap` | decimal(18,4) | SUM(GiaNhap × SoLuong) — tổng tiền thực trả |
| `SoPhieuNhapMuaNCC` | int | |
| `SoPhieuNhapTuChuyenKho` | int | |
| `IsFallbackAllHistory` | bit | True = tháng này không có nhập, fallback toàn lịch sử của `(SP, Kho)` |
| `TrangThaiSnapshot` | int | 1=Draft, 2=Applied (V1 dùng 2 trạng thái này; 3 dành cho V2) |
| `KyLoaiNguonGoc` | int | **Audit** — kỳ user chọn khi tạo job: 1=Tháng, 2=Quý, 3=Năm |

**Unique:** `(IdDonVi, IdKho, IdSanPham, KyNam, KyThang, CongThuc)` WHERE `IsDelete = 0`.

**Indexes:**
- `IX_KhoChiTietGiaVonTheoKy_IdJob`
- `IX_KhoChiTietGiaVonTheoKy_IdDonVi_KyNam_KyThang_IdKho`
- `IX_KhoChiTietGiaVonTheoKy_IdSanPham_IdKho_KyNam_KyThang` (lookup khi apply phiếu xuất)

### 5.2 `GiaVonTheoKyJob` (SQL Server)

**Mỗi lần user click "Chạy tính" = INSERT đúng 1 row mới.** Bảng là log lịch sử các lần chạy.

| Field | Type | Mô tả |
|-------|------|------|
| (BaseEntity) | — | |
| `IdDonVi` | Guid | |
| `KyLoai` | int | 1=Tháng, 2=Quý, 3=Năm |
| `KyNam` | int | |
| `KyThang` | int? | |
| `KyQuy` | int? | |
| `NgayBatDauKy`, `NgayKetThucKy` | DateTime | |
| `DsThangCanTinh` | NVARCHAR(MAX)? | JSON list `{KyNam, KyThang}` các tháng job phải sub-compute |
| `CongThuc` | int | 1=AvgEligibleImport |
| `IsGhiDe` | bit | |
| `IsKyBaoGomHienTai` | bit | Computed: `NgayBatDauKy <= now <= NgayKetThucKy` → có update `KhoChiTiet.GiaNhapTrungBinh` |
| `IsTatCaChiNhanh` | bit | True → bỏ qua `IdsChiNhanhFilter` |
| `IdsChiNhanhFilter` | NVARCHAR(MAX)? | JSON list Guid. NULL khi `IsTatCaChiNhanh=1` |
| `IsTatCaKho` | bit | |
| `IdsKhoFilter` | NVARCHAR(MAX)? | |
| `IsTatCaSanPham` | bit | |
| `IdsSanPhamFilter` | NVARCHAR(MAX)? | |
| `TrangThai` | int | 1=Queued, 2=Running, 3=Completed, 4=Failed, 5=Cancelled, 6=PausedByError |
| `PhaseHienTai` | int | 1=ComputeSnapshot, 2=UpdateKhoChiTietHienTai, 3=UpdatePhieuXuat, 4=UpdateDonHang, 5=UpdateDonHangLogistic, 6=SyncES, 7=SyncKeToan, 8=Done |
| `IsProcessing` | bit | Lock (theo `SyncNhapXuatKeToan`) |
| `SoLanRetry` | int | |
| `TongSnapshotPhaiTinh`, `DaTinhSnapshot` | int | |
| `TongKhoChiTietPhaiUpdate`, `DaUpdateKhoChiTiet` | int | |
| `TongPhieuXuatPhaiUpdate`, `DaUpdatePhieuXuat` | int | |
| `TongDonHangPhaiUpdate`, `DaUpdateDonHang` | int | |
| `TongDonLogisticPhaiUpdate`, `DaUpdateDonLogistic` | int | |
| `NgayBatDauXuLy`, `NgayHoanThanhXuLy` | DateTime? | |
| `ThoiGianChayMs` | bigint? | |
| `MessageLoi` | NVARCHAR(2000)? | |
| `JsonErrorPayload` | NVARCHAR(MAX)? | |
| `NguonChay` | int | 1=ManualByUser |
| `IpChay` | VARCHAR(50)? | |

**Indexes:** `(IdDonVi, TrangThai)`, `(NgayTao DESC)`, `(TrangThai, IsProcessing)`.

### 5.3 `GiaVonTheoKyJobUpdateLog` (MongoDB)

Pattern theo [SanPhamImeiLichSu.cs:24](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.DataContext/CollectionMongoDB/SanPhamImeiLichSu.cs#L24).

```csharp
[BsonIgnoreExtraElements]
public class GiaVonTheoKyJobUpdateLog : BaseEntityMongoDB
{
    [BsonIgnoreIfNull] public Guid? IdDonVi { get; set; }
    [BsonIgnoreIfNull] public Guid? IdJob { get; set; }
    // 1=PhieuXuatSanPham, 2=DonHangSanPham, 3=DonHangLogisticSanPham,
    // 4=KhoChiTiet (GiaNhapTrungBinh), 5=KhoChiTietGiaVonTheoKy (audit UPSERT)
    [BsonIgnoreIfNull] public int? LoaiBang { get; set; }
    [BsonIgnoreIfNull] public Guid? IdBanGhi { get; set; }
    [BsonIgnoreIfNull] public Guid? IdSanPham { get; set; }
    [BsonIgnoreIfNull] public Guid? IdKho { get; set; }
    [BsonIgnoreIfNull] public Guid? IdDonHangRef { get; set; }
    [BsonIgnoreIfNull] public Guid? IdChiNhanh { get; set; }
    [BsonIgnoreIfNull] public double? GiaVonCu { get; set; }
    [BsonIgnoreIfNull] public double? GiaVonMoi { get; set; }
    [BsonIgnoreIfNull] public double? SoLuong { get; set; }
    [BsonIgnoreIfNull] public double? ChenhLechTien { get; set; }
    [BsonIgnoreIfNull] public Dictionary<string, object?>? SnapshotCu { get; set; }
    [BsonIgnoreIfNull] public DateTime? NgayUpdate { get; set; }
}
```

**Mongo indexes:**
- `{ IdJob: 1, LoaiBang: 1 }`
- `{ IdBanGhi: 1, LoaiBang: 1 }`
- `{ IdSanPham: 1, NgayUpdate: -1 }`
- `{ IdDonVi: 1, NgayUpdate: -1 }`
- TTL `{ NgayUpdate: 1 } expireAfterSeconds: 180 days`

---

## 6. API endpoints

Base: `api/GiaVonTheoKy/`. Mọi endpoint cần Admin tổng đơn vị + feature flag.

| Method | Endpoint | Mô tả |
|--------|---------|------|
| GET | `DangCoTienTrinhDangChay` | FE gọi khi mở modal — query latest job `TrangThai IN (1,2,6)`. Resp: `{ HasRunning, Job? }` |
| POST | `EnqueueRecalculate` | Submit form. Reject 409 nếu đơn vị còn bất kỳ job nào `TrangThai IN (1,2,6)`. Validate `IsTatCa*` ⇔ `Ids*Filter` rỗng |
| POST | `KiemTraChongLan` | Preflight cho FE → list snapshot sẽ bị UPSERT đè |
| POST | `UocLuongPhamVi` | FE realtime hiển thị "X chi nhánh / Y kho / Z SP" khi user đổi filter |
| GET | `TimTheoDieuKien` | Paging list job (Lịch sử) |
| GET | `ChiTietJob/{idJob}` | Counter + phase + computed `PhanTramTienDo` |
| GET | `ChiTietSnapshot/{idJob}` | Paging `KhoChiTietGiaVonTheoKy` của job |
| GET | `ChiTietUpdateLog/{idJob}` | Paging Mongo log theo `LoaiBang` |
| POST | `HuyJob/{idJob}` | Set `TrangThai=Cancelled`; consumer check cờ mỗi batch |

---

## 7. Luồng nghiệp vụ

```
FE (modal trên /quan-ly-ton-kho)
  GET DangCoTienTrinhDangChay → tab "Đang chạy" hoặc "Chạy mới"
  POST EnqueueRecalculate → BE validate + INSERT GiaVonTheoKyJob (Queued) + publish RecalculateGiaVonTheoKyCommand
       ↓
WarehouseProductConsumer.API — RecalculateGiaVonTheoKyConsumer
  Phase 1: ComputeSnapshot (cho mỗi tháng trong DsThangCanTinh)
           → UPSERT KhoChiTietGiaVonTheoKy, audit Mongo LoaiBang=5
  Phase 2: UpdateKhoChiTietHienTai (chỉ nếu IsKyBaoGomHienTai=1)
           → UPDATE KhoChiTiet.GiaNhapTrungBinh với snapshot tháng hiện tại, Mongo LoaiBang=4
  Phase 3: UpdatePhieuXuat (paging 100/lần, lookup snapshot theo YEAR+MONTH NgayXuat, convert đơn vị)
           → Mongo LoaiBang=1
  Phase 4: UpdateDonHang (DonHangSanPham — lookup theo NgayDonHang)
           → Mongo LoaiBang=2 + publish event Order recalc total
  Phase 5: UpdateDonHangLogistic
           → Mongo LoaiBang=3
  Phase 6: SyncESDocument (publish reindex KhoChiTiet)
  Phase 7: SyncAccounting (publish DongBoPhieuXuatGiaVonUpdated)
  Phase 8: Done (TrangThai=Completed)

Mỗi batch: UPDATE counter trên GiaVonTheoKyJob (SaveChanges xuống DB).
KHÔNG SignalR — user phải manual reload modal mới thấy tiến độ mới.
```

---

## 8. Business logic & rules

### 8.1 Phiếu nhập đủ điều kiện (V1)

- ✅ Nhập mua từ NCC (`IdDmPhanLoaiNhap = NhapMuaNCC`)
- ✅ Nhập sinh từ phiếu chuyển kho (`IdDmPhanLoaiNhap = NhapChuyenKho` — kho đích nhận hàng giá X từ kho nguồn)
- ❌ Loại trừ: `IdDonHang != null` (rollback đơn quầy) hoặc `IdDonHangLogistic != null` (rollback đơn logistic), hoặc nhập từ sản xuất.

Cần BE chốt list `IdDmPhanLoaiNhap` thực tế khi implement (whitelist an toàn hơn blacklist).

### 8.2 Quy đổi đơn vị tính (BẮT BUỘC)

- **Đơn vị chuẩn = đơn vị nhỏ nhất** trong `SanPham.BangQuyDoiJson` (đơn vị có `SoLuongToDvNho = 1`).
- **Công thức per dòng `PhieuNhapSanPham`:**
  - `TiLeVeDonViNhoNhat` = `SoLuongToDvNho` của `IdDonViTinh` dòng đó
  - `SoLuongDonViNhoNhat = SoLuong × TiLeVeDonViNhoNhat`
  - `TienDongQuyDoi = GiaNhap × SoLuong` (tổng tiền thực trả, không phụ thuộc đơn vị)
- **Group-by (SP, Kho, Tháng):**
  - `TongSoLuongNhapDonViNhoNhat = SUM(SoLuongDonViNhoNhat)`
  - `TongTienNhap = SUM(TienDongQuyDoi)`
- **Bình quân gộp cuối kỳ CÓ mang tồn đầu kỳ** (implemented 2026-06-22):
  - `SL_đầu(nhỏ nhất) = Σ SL nhập − Σ SL xuất` có `NgayTacDong < đầu kỳ` (quy đổi nhỏ nhất, clamp ≥ 0; xuất loại phân loại 11)
  - `GiáVốnĐầuKỳ = GiaVonTinhRa` snapshot kỳ gần nhất TRƯỚC (per đơn vị nhỏ nhất)
  - `GiaVonTinhRa = (SL_đầu × GiáVốnĐầuKỳ + TongTienNhap) / (SL_đầu + TongSoLuongNhapDonViNhoNhat)`
  - Không có snapshot kỳ trước → bỏ seed: `GiaVonTinhRa = TongTienNhap / TongSoLuongNhapDonViNhoNhat` (hành vi cũ)
  - Cài đặt: [`AvgEligibleImportRecalculator.ApplyOpeningBalanceAsync`](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.Service/Generated/GiaVonTheoKy/AvgEligibleImportRecalculator.cs)
- **Khi apply lên phiếu xuất/đơn hàng:** convert ngược về đơn vị dòng:
  - `GiaVon_dòng = GiaVonTinhRa × SoLuongToDvNho_của_IdDonViTinh_dòng`

Reuse helper [`KhoChiTietService.QuyDoiSoLuongTheoDonViTinh(...)`](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.Service/Generated/KhoChiTietService.cs#L539) hoặc xây helper mới chuyên về đơn vị nhỏ nhất.

### 8.3 Snapshot granularity = tháng

User chọn:
- **Tháng** → 1 sub-compute → UPSERT 1 row.
- **Quý** → **3 sub-compute độc lập** (mỗi tháng giá vốn riêng từ phiếu nhập của riêng tháng đó).
- **Năm** → 12 sub-compute độc lập.

Khi job chọn Năm 2026 chạy giữa năm (now = 15/06/2026) → `DsThangCanTinh` = T1..T6 (T7-T12 bị skip vì kỳ tương lai, xem §8.5).

### 8.4 Cập nhật `KhoChiTiet.GiaNhapTrungBinh` (có điều kiện)

- `IsKyBaoGomHienTai=1` (kỳ chứa Now) → CÓ update KhoChiTiet với snapshot tháng hiện tại.
- `IsKyBaoGomHienTai=0` (kỳ quá khứ) → KHÔNG update KhoChiTiet, chỉ phiếu xuất + đơn hàng + đơn logistic của kỳ.

### 8.5 Cấm kỳ tương lai

- `NgayBatDauKy <= DateTime.Now` (bắt buộc).
- FE date picker disable tháng/quý chưa bắt đầu (tooltip giải thích).
- Job Năm chạy giữa năm: tự skip các tháng `NgayBatDauThang > Now` khỏi `DsThangCanTinh`.

### 8.6 Lock 1-job-per-đơn-vị

- API `EnqueueRecalculate` reject 409 nếu đơn vị còn bất kỳ job `TrangThai IN (Queued, Running, PausedByError)`.
- Consumer khi pick message: `UPDATE GiaVonTheoKyJob SET IsProcessing=1, TrangThai=2 WHERE Id=@id AND IsProcessing=0` — affected = 0 thì abort.

### 8.7 Phạm vi chạy + UPSERT đè

- 3 cặp `IsTatCa* + Ids*Filter`: chi nhánh, kho, SP. Nếu `IsTatCa*=true` → `Ids*Filter=null` (đỡ lưu 10K Id).
- UPSERT in-place per `(DonVi, Kho, SP, Tháng, CongThuc)` — Mongo log audit trước UPDATE.
- **KHÔNG có Revert ở V1** — tính sai → chạy lại job khác đè.

### 8.8 Fallback khi tháng không có nhập

Tháng T không có phiếu nhập đủ điều kiện cho (SP, Kho) → tính trên TOÀN BỘ lịch sử nhập đủ điều kiện của (SP, Kho) đó (không filter `NgayTacDong`), đánh dấu `IsFallbackAllHistory=true`.

### 8.9 Cảnh báo "cần tính lại" khi quá khứ thay đổi (`TrangThaiCanhBao`)

Khi dữ liệu quá khứ đổi → snapshot giá vốn **từ kỳ bị đổi cho tới hiện tại** bị đánh dấu `TrangThaiCanhBao != 0` (FE hiện icon "Cần tính lại"). Cơ chế: publish `EventMarkSnapshotsStaleMessage` → [`EventMarkSnapshotsStaleConsumer`](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProductConsumer.API/Consumer/GiaVonTheoKy/EventMarkSnapshotsStaleConsumer.cs) UPDATE batch `KyNam > batDau OR (KyNam = batDau AND KyThang >= batDau)`.

| Nguồn thay đổi quá khứ | Trigger | Phạm vi đánh dấu | Mã `TrangThaiCanhBao` |
|---|---|---|---|
| Sửa phiếu nhập hoàn thành | `GiaNhap/SoLuong/IdDonViTinh/TiLeQuyDoi` đổi, hoặc thêm/xóa dòng | kho phiếu, SP đổi, từ tháng phiếu nhập | 1 `CanTinhLaiDoSuaPhieuNhap` |
| Xóa phiếu nhập hoàn thành | xóa | — | 2 `CanTinhLaiDoXoaPhieuNhap` |
| Sửa phiếu chuyển kho | sửa | 2 kho | 3 `CanTinhLaiDoSuaPhieuChuyenKho` |
| **Sửa phiếu xuất hoàn thành** | `SoLuong/IdDonViTinh/TiLeQuyDoi` đổi, hoặc thêm/xóa dòng (giá bán KHÔNG tính) | kho xuất, SP đổi, từ tháng phiếu xuất | 4 `CanTinhLaiDoSuaPhieuXuat` |
| **Reset / rà soát tồn kho** (tái tạo phiếu / reset toàn kho) | hoàn thành job | TOÀN ĐƠN VỊ (UPDATE thẳng SQL, không qua message) | 5 `CanTinhLaiDoResetTonKho` |

> Sửa phiếu **xuất** ảnh hưởng giá vốn vì tồn đầu kỳ `SL_đầu = Σnhập − Σxuất` (xem §8.2) — chỉ đúng từ khi áp dụng công thức gộp cuối kỳ có mang tồn đầu kỳ (2026-06-22).
> Idempotent: chỉ đánh dấu row đang `BinhThuong (0)` → retry không nhân đôi.

### 8.10 Phạm vi phiếu được phép sửa (`SuaPhieuHoanThanhService`) — cập nhật 2026-06-22

Guard "sửa phiếu hoàn thành" (block-list theo field):

| Loại phiếu | Sửa? | Lý do |
|---|---|---|
| Thủ công | ✅ | gốc |
| Từ **phiếu mua hàng** (`IdPhieuMuaHang`) / **bán hàng** (`IdPhieuBanHang`) | ✅ mở | single-leg; **CHƯA** đẩy kế toán/công nợ (chỉ tác động kho) |
| **Sản xuất** — nhập thành phẩm (`MaDmPhanLoaiNhap=NhapThanhPham`) + xuất NVL (`KieuLoaiXuat=SanXuat`) | ✅ mở | KHÔNG coupled trong replay giá vốn → an toàn tồn/giá vốn; chấp nhận mất khớp BOM/giá thành |
| **Chuyển kho** (`IdTransaction` + `KieuLoaiXuat≠SanXuat`) | ❌ chặn | coupled engine (vế đến lấy giá vốn vế đi) → sửa lẻ làm sai giá vốn |
| Từ **đơn hàng** (`IdDonHang`/`IdDonHangLogistic`) | ❌ chặn | rollback đơn |

Guard nhập: chặn `IdDonHang \|\| IdDonHangLogistic \|\| IdTransaction`.
Guard xuất: chặn `IdDonHang \|\| IdDonHangLogistic \|\| (IdTransaction && KieuLoaiXuat≠SanXuat)`.
> TODO: đẩy ngược kế toán (công nợ NCC / doanh thu-COGS) khi sửa phiếu mua/bán hàng — phần (B) chưa làm.

---

## 9. Convention query (BẮT BUỘC)

- **`.TableWithNolock()`** cho mọi query LINQ ([feedback_scrm_always_nolock](../../../memory/feedback_scrm_always_nolock.md)).
- **KHÔNG dùng OR trong WHERE** — chia case bằng conditional `.Where()` chain (EF Core auto AND vào 1 SQL duy nhất). SQL Server query plan tối ưu AND-chain tốt hơn OR nhiều.
- **Build 1 query LINQ duy nhất**, không tách thành N query gọi DB nhiều lần. Pseudo-code:
  ```csharp
  var q = _phieuNhap.TableWithNolock().Where(/* common */);
  if (!job.IsTatCaChiNhanh) q = q.Where(x => idsChiNhanh.Contains(x.IdChiNhanh));
  if (!job.IsTatCaKho)      q = q.Where(x => idsKho.Contains(x.IdKho));
  if (!job.IsTatCaSanPham)  q = q.Where(x => _phieuNhapSP.TableWithNolock()
                                      .Any(pns => pns.IdPhieuNhap == x.Id
                                               && idsSP.Contains(pns.IdSanPham)));
  ```
- **Paging 100/lần** khi quét bảng to (PhieuNhap, PhieuXuat, DonHang…): `OrderBy(stable_key).Skip(offset).Take(100)` lặp đến hết; aggregate `SUM` **in-memory** bằng `Dictionary<(IdSP, IdKho), AggregateState>` running totals. KHÔNG load toàn bộ vào RAM.

---

## 10. Frontend — Angular

Path: `D:/WORK/SCRM/GIT/FE/scrm_frontend/src/app/pages/WarehouseProduct/quan-ly-ton-kho/tinh-lai-gia-von-modal/`

| Component | Trách nhiệm |
|-----------|------------|
| `tinh-lai-gia-von-modal.component.ts` | Modal container, gọi `DangCoTienTrinhDangChay` lúc init, chọn tab phù hợp |
| `tab-chay-moi/tab-chay-moi.component.ts` | Form chọn kỳ + 3 scope (chi nhánh/kho/SP, mỗi cái có toggle "Tất cả" / "Cụ thể") + 2 lần confirm |
| `tab-dang-chay/tab-dang-chay.component.ts` | Job hiện hành: phase + counter, nút "Tải lại", "Hủy job" |
| `tab-lich-su/tab-lich-su.component.ts` | Paging job đã kết thúc, click row → modal chi tiết |
| `chi-tiet-job-modal/chi-tiet-job-modal.component.ts` | Modal phụ chi tiết job (counter + log Mongo). Nút "Tải lại" thủ công |

Service: `D:/WORK/SCRM/GIT/FE/scrm_frontend/src/app/services/warehouseproduct/gia-von-theo-ky.service.ts` — wrap các endpoint.

**Form chạy:**
- `nz-radio-group` KyLoai (Tháng/Quý/Năm)
- `nz-date-picker` mode tương ứng — disable kỳ tương lai
- 3 scope: mỗi cái radio "Tất cả" / "Cụ thể". "Cụ thể" → `nz-select` mode="multiple"; SP có modal phụ với server-side search + filter theo Nhóm SP / Ngành nghề.
- Banner cảnh báo nếu kỳ chứa hôm nay: "Sẽ cập nhật cả giá vốn hiện tại các kho đã chọn."
- Tóm tắt phạm vi realtime: "Sẽ chạy cho X chi nhánh / Y kho / Z SP" (qua `UocLuongPhamVi`).
- **2 lần confirm:** modal 1 tóm tắt; modal 2 bắt user gõ chuỗi xác nhận (vd "TINH LAI THANG 6/2026").

**Tab "Đang chạy" / "Chi tiết job":** KHÔNG realtime, không poll, không SignalR. User bấm "Tải lại" → gọi `GET ChiTietJob/{idJob}` lấy snapshot trạng thái.

---

## 11. Mở rộng V2 — Công thức tái hiện lịch sử (`CongThuc = 2`)

**Khả thi:** có. SCRM đã có audit trail đầy đủ ở [KhoChiTietLichSu](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.DataContext/Entity/KhoChiTietLichSu.cs) — đủ field để replay rolling state.

**Thuật toán (per cặp SP × Kho):**
1. `SELECT KhoChiTietLichSu WHERE NgayTacDong <= NgayKetThucKy ORDER BY NgayTacDong ASC`.
2. State machine `(soLuong, tongTienVon) → giaNhapTrungBinh` mỗi bước.
3. Snapshot giá vốn của từng phiếu xuất tại đúng thời điểm.

**V2 cần thêm:**
- 1 file `HistoricalReplayRecalculator : IGiaVonRecalculator` (CongThuc = 2)
- 1 bảng phụ `KhoChiTietGiaVonReplayChiTietPhieuXuat (IdJob, IdPhieuXuatSanPham, GiaVonTaiThoiDiem)` — 1 SP có nhiều giá vốn khác nhau trong cùng kỳ, không nhồi vào 1 row tháng được
- 1 index mới trên `KhoChiTietLichSu` `(IdSanPham, IdKho, IdDonVi, NgayTacDong)` nếu thiếu
- Xử lý edge case sửa phiếu retroactive (`PhieuHoanThanhChinhSua` có thể tạo entry revert — cần flag để skip khi replay)

V2 KHÔNG đụng schema V1 — discriminator `CongThuc` đã chuẩn bị sẵn.

---

## 12. Test scenarios (verification)

| # | Scenario | Expect |
|---|---------|--------|
| 1 | Smoke V1 end-to-end | 8 phase pass, snapshot khớp tính tay đến 0.01 |
| 2 | Retry consumer giữa Phase 3 | Idempotent, không double-update |
| 3 | Multi-tenant 2 đơn vị song song | Message partition theo `IdDonVi`, không cản nhau |
| 4 | Rerun-to-fix (thay revert) | Job mới đè job cũ qua UPSERT, Mongo log có 2 entry |
| 5 | `IsKyBaoGomHienTai` | Kỳ chứa Now → update KhoChiTiet; kỳ quá khứ → không update |
| 6 | UPSERT in-place | Chạy T1 → chạy Q1: row T1 UPDATE (giá tháng 1 độc lập), Mongo log audit |
| 7 | Cấm kỳ tương lai | T7/2026 (now=15/06/2026) → 400 reject; Năm 2026 OK nhưng skip T7-T12 |
| 8 | Scope theo CN/Kho/SP + "Tất cả" | 3 case scope pass; BE reject khi `IsTatCa=true` mà `Ids*Filter` không rỗng |
| 9 | Quy đổi đơn vị | 10 thùng + 30 chai → 84.666,67/chai (KHÔNG ra 317.500) |
| 10 | 1-job-per-đơn-vị + UX modal | Có job Running → tab "Đang chạy" auto open; Enqueue lần 2 → 409 |
| 11 | Query plan | List job dùng `IX_GiaVonTheoKyJob_IdDonVi_TrangThai`; apply phiếu xuất dùng `IX_KhoChiTietGiaVonTheoKy_IdSanPham_IdKho_KyNam_KyThang` |
| 12 | Dung lượng | KhoChiTietGiaVonTheoKy ~150 MB/năm/đơn vị; Mongo log có TTL 180 ngày |

---

## 13. Checklist fix bug

- [ ] Giá vốn snapshot sai → check `BangQuyDoiJson` của SP + công thức quy đổi đơn vị nhỏ nhất; verify `KhoChiTietService.QuyDoiSoLuongTheoDonViTinh` trả đúng tỷ lệ
- [ ] Phiếu xuất kỳ X chưa update giá vốn → check lookup `IX_KhoChiTietGiaVonTheoKy_IdSanPham_IdKho_KyNam_KyThang` + verify `YEAR/MONTH(NgayXuat)` match tháng snapshot
- [ ] Job stuck Running không tiến → check `IsProcessing`, log MassTransit retry, có thể consumer crash → restart consumer
- [ ] User không bấm được nút "Tính lại giá vốn theo kỳ" → check role Admin tổng + feature flag `IsBatTinhLaiGiaVonTheoKy`
- [ ] Enqueue trả 409 dù không có job chạy → query trực tiếp `GiaVonTheoKyJob WHERE TrangThai IN (1,2,6)`; có thể có job zombie không được consumer pick → manual UPDATE `TrangThai=Cancelled`
- [ ] Giá vốn trên đơn hàng quầy/logistic chưa update → check phase 4/5 đã chạy chưa (PhaseHienTai), có thể publish event Order/OrderLogistic recalc cache không

---

## 13b. So sánh với Reset tồn kho + hướng hội tụ (Hướng A)

Engine bình quân gia quyền chuẩn của hệ thống nằm ở [reset-ton-kho.md](./reset-ton-kho.md) (`ReplayGiaVon`/Anchor). V1 của tính năng này **khác hẳn** về công thức:

| | Reset tồn kho (`ReplayGiaVon`) | Giá vốn theo kỳ V1 |
|---|---|---|
| Công thức | BQ gia quyền **liên hoàn** (replay từng movement) | TB gộp **chỉ phiếu nhập trong kỳ** |
| Tồn đầu kỳ | Mang theo running `(GiáTB, SL)` | ❌ Không |
| Xuất/hoàn/chuyển kho | Xử lý đầy đủ | ❌ Bỏ qua |
| Phạm vi | Toàn đơn vị, từ đầu | 1 kỳ + scope |

**Hướng A đang chốt:** đổi ruột Phase 1 (recalculator) sang **replay có seed tồn đầu kỳ, giới hạn theo kỳ** — tái dùng engine của reset để ra số GIỐNG HỆT reset. Hạ tầng áp giá vốn (Phase 2–7) giữ nguyên.

**Seed đầu kỳ:**
- `GiáTB đầu kỳ` = `GiaVonTinhRa` snapshot kỳ liền trước (path-dependent — KHÔNG lấy được bằng SUM; thiếu snapshot kỳ trước thì phải bootstrap = replay full history trước đầu kỳ).
- `SL tồn đầu kỳ` = aggregate `Σ SL nhập − Σ SL xuất` (NgayTacDong < đầu kỳ), hoặc `SoLuongSau` cuối cùng trong [KhoChiTietLichSu](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.DataContext/Entity/KhoChiTietLichSu.cs).

### Điểm cần thống nhất / observations (khi refactor sang Hướng A)
1. **Cờ loại trừ chưa nhất quán:** reset loại bằng `PhieuNhap.IsDaHuyTrongDonHang = 1`; V1 loại bằng `IdDonHang/IdDonHangLogistic != null`. Phải thống nhất, nếu không kết quả lệch nhau.
2. **Phiếu hoàn type 6 (`HoanGiaoVan`) / 7 (`NhapHangBanBiTraLai`):** reset **không loại** mà re-value (type 6 re-nhập ở GiáTB hiện tại). Loại hẳn → lệch reset. (Phiếu đối soát hoàn tạo tại `Accounting/.../PhieuBanTraLaiService.cs`, đặc trưng `MaDmPhanLoaiNhap=7` + `MaChungTuGoc='HBBTL'`.)
3. **Phase 1 không checkpoint trong tháng** — chỉ time-box giữa các tháng; tháng dữ liệu lớn không ngắt được giữa chừng.
4. **N+1 query** khi UPSERT snapshot (mỗi SP×Kho 1 lần `FirstOrDefault`) — nên batch-load.
5. **`DaTinhSnapshot += agg.Count`** cộng dồn mỗi lần chạy → resume/restart có thể overcount progress (không critical, chỉ là %).

---

## 14. Liên quan

- Plan đầy đủ chi tiết kỹ thuật: `~/.claude/plans/b-y-gi-t-i-mu-n-lucky-dewdrop.md` (cho 1 lần làm — không vào repo).
- [reset-ton-kho.md](./reset-ton-kho.md) — engine bình quân gia quyền tham chiếu (Hướng A hội tụ về đây).
- [warehouse.md](./warehouse.md) — context WarehouseProduct module.
- [accounting.md](./accounting.md) — Phase 7 sync giá vốn sang chứng từ kế toán.

---

## Changelog

| Ngày | Người | Thay đổi |
|------|-------|----------|
| 2026-06-01 | teamdev1 | Khởi tạo spec V1 — schema (KhoChiTietGiaVonTheoKy + GiaVonTheoKyJob + Mongo log), 8 phase orchestrator, feature flag, scope CN/Kho/SP với cặp `IsTatCa*` + `Ids*Filter`, paging 100, granularity tháng, KHÔNG revert, KHÔNG SignalR |
| 2026-06-20 | teamdev1 | Bổ sung §13b — so sánh với Reset tồn kho + hướng hội tụ Hướng A (replay seed theo kỳ) + 5 observations khi refactor Phase 1 |
| 2026-06-22 | teamdev1 | **Implement Hướng B (gộp cuối kỳ CÓ mang tồn đầu kỳ)** vào `AvgEligibleImportRecalculator`: thêm seed `SL_đầu × GiáVốnĐầuKỳ` (SL_đầu = Σnhập−Σxuất trước kỳ quy đổi nhỏ nhất; GiáVốnĐầuKỳ = snapshot kỳ trước). Chỉ tính kỳ user chọn, KHÔNG lan tới hiện tại. Build 0 errors. §8.2 cập nhật công thức |
| 2026-06-22 | teamdev1 | **Cảnh báo stale khi quá khứ đổi** (§8.9): wire publish stale cho sửa phiếu **xuất** (`CanTinhLaiDoSuaPhieuXuat=4`) + **reset/rà soát tồn kho** (`CanTinhLaiDoResetTonKho=5`, UPDATE thẳng toàn đơn vị). Sửa phiếu nhập/xóa/chuyển kho đã có sẵn. Build 0 errors |
| 2026-06-22 | teamdev1 | **Nới guard sửa phiếu (§8.10)** — phần (A): mở sửa phiếu từ phiếu mua hàng / bán hàng + sản xuất (nhập thành phẩm sẵn mở, xuất NVL thêm exception `KieuLoaiXuat=SanXuat`). Vẫn chặn đơn hàng + chuyển kho. CHƯA đẩy kế toán (phần B). Build 0 errors |
