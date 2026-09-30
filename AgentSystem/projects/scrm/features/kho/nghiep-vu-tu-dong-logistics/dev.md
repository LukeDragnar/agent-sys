# Dev — Nghiệp vụ tự động theo trạng thái giao hàng (NghiepVu queue)

> Service: **OrderLogistic** (BE .NET). Framework xử lý nghiệp vụ async race-safe khi đơn logistics đổi trạng thái. Nghiệp vụ đang implement: **CHI_TIEU** (chi tiêu khách + công nợ Nợ/Trả qua Customer service). Đây là feature **nền** — không có màn FE người dùng; chỉ có màn quản trị nội bộ NghiepVuManagement (HT-CSKH). Xem thêm `patterns.md` mục "NghiepVu async queue pattern" và registry `kho/NghiepVuManagementController.md`.

## Màn hình & route
- **Không có route FE người dùng thường** (chạy nền).
- Màn quản trị/giám sát: `NghiepVuManagementController` (Quản lý hàng đợi — Pending/History/Thử lại), quyền `HT-CSKH`. Registry: `projects/scrm/registry/kho/NghiepVuManagementController.md`.

## Endpoint / Controller (trace registry)
**`NghiepVuManagementController`** — `OrderLogistic/OrderLogistic.API/Controllers/NghiepVuManagementController.cs`. Tất cả `[HttpPost]`, mỗi method 1 permission key riêng (`HT-CSKH`):
| MaTinhNang (SBAuthorize) | Route | Mục đích | Line |
|---|---|---|---|
| `NghiepVuManagement_TimDonHang` | `POST api/NghiepVuManagement/TimDonHang` | List đơn + đếm Pending/ThanhCong/ThatBai | `:27` |
| `NghiepVuManagement_TimPending` | `POST .../TimPending` | List bản ghi Pending (lọc lỗi `SoLanThuLai>0`) | `:39` |
| `NghiepVuManagement_TimHistory` | `POST .../TimHistory` | List History (lọc TrangThaiXuLy) | `:51` |
| `NghiepVuManagement_ThuLaiPending` | `POST .../ThuLaiPending/{id}` | Reset SoLanThuLai=0 + publish MQ | `:63` |
| `NghiepVuManagement_ThuLaiTuHistory` | `POST .../ThuLaiTuHistory/{id}` | Tạo lại Pending từ History Loi/BoQua | `:75` |

**Enqueue (không phải endpoint riêng — gọi nội bộ khi đổi trạng thái đơn):** `INghiepVuPendingService.TaoNghiepVuPendingInTransactionAsync(donHang, triggerEvent, userLogin)`. Các điểm gọi (trigger trạng thái giao):
- Webhook/đối tác VC: `OrderLogistic.Service/Generated/DeliveryUnitConnect/OrderService.cs:582`, `:1290`
- Cập nhật thủ công (thủ kho): `KhoTacNghiepService.cs:4532`, `:4748` (+ check `IsTrangThaiTangChiTieuAsync` `:4767`/`:4768`)
- Cập nhật đơn lẻ/hàng loạt: `DonHangService.cs:7142`, `:7430`, `:7584`; `DonHangMultipleService.cs:802`
- Ecommerce sync: `Ecommerce/DonHangService.cs:1715`, `:3097`
- Đối soát (DOISOAT): `DonHangTtghLogService.cs:411` (+ check tăng/giảm `:429`/`:430`)

## Entities
- **`DonHangLogisticNghiepVuPending : BaseEntity`** — `OrderLogistic.DataContext/Entity/DonHangLogisticNghiepVuPending.cs:11`. Hàng đợi chờ. Trường chính: `NghiepVu`, `ThoiGianDuDieuKien`, `ThoiGianXuLy` (= now+PhutDelay), `TriggerEvent` (UPDATE_STATUS|DOISOAT), `TrangThaiGiaoHang` (snapshot), `HuongXuLy` (1 Tăng/-1 Giảm), `SoTienTarget` (snapshot), `SoTienNo`, `ThoiGianTrangThaiDon`, `IsMoiNhat`, `SoLanThuLai`, `IsProcessing`, `ProcessingStartedAt`, `ProcessingToken`.
- **`DonHangLogisticNghiepVuHistory`** — `.../Entity/DonHangLogisticNghiepVuHistory.cs`. Lịch sử đã xử lý; `TrangThaiXuLy` theo `EnumNghiepVuHistoryTrangThai`.
- **`DonHangLogisticNghiepVuCauHinh`** — `.../Entity/DonHangLogisticNghiepVuCauHinh.cs:11`. Cấu hình theo `(IdDonVi, NghiepVu)`: `EventRun`, `PhutDelay` (default 60, -1=tắt), `TrangThaiTang` (default "31;32").
- **`DonHangLogisticChiTieuLog`** — audit từng bước cộng/trừ chi tiêu (`Huong`, `SoTien`, `IdPending` để dedup).

## Enums / Messages
- `EnumNghiepVu` (`Libs/Models.Core/Enum/DonHangLogistic/EnumNghiepVu.cs`): **chỉ có `CHI_TIEU`** đang dùng. `TRU_KHO`, `TAO_HOA_DON` là **comment placeholder** — chưa implement handler.
- `EnumNghiepVuHistoryTrangThai` (`OrderLogistic.Service/Generated/Enums/EnumNghiepVuHistoryTrangThai.cs`): ThanhCong=1, Loi=2, BoQua=3, DangRetry=4, FixDataThanhCong=5.
- Trạng thái giao vận `GiaoVan_TT_PS` (`Libs/Models.Core/Enum.cs:340+`): DaGiaoHang=31, DaThanhToan=32, GiaoHangMotPhan=35, DangHoan=40, DaHoan=41…
- `XuLyNghiepVuMessage` (`OrderLogistic.DataContext/Messages/XuLyNghiepVuMessage.cs:7`): chỉ `IdPending` + `NghiepVu`.

## Luồng code
1. **Enqueue** — `NghiepVuPendingService.TaoNghiepVuPendingCoreAsync` (`OrderLogistic.Service/Generated/NghiepVu/NghiepVuPendingService.cs:56`):
   - đọc cache cấu hình (`:65`), filter event (`:74`), tính `huongXuLy` (`:82`), `TinhSoTienTarget` (`:155`), `LayThoiGianTrangThaiDon` (MAX thủ công/webhook, `:167`), `HaIsMoiNhatAsync` (store `:182`), insert pending (`:121`), publish MQ (`:123`).
   - `TaoNghiepVuPendingInTransactionAsync` (`:46`) KHÔNG try-catch (throw để rollback transaction đơn); `TaoNghiepVuPendingAsync` (`:50`) nuốt lỗi (fire-and-forget).
2. **Consumer** — `XuLyNghiepVuConsumer.Consume` (`OrderLogisticConsumer.API/Consumer/XuLyNghiepVuConsumer.cs:25`): dựng pending stub → `XuLyMotNghiepVuPendingAsync`. **Cố ý không catch** (`:33`) để MassTransit retry/DLQ.
3. **Job xử lý chính** — `JobXuLyChiTieuKhachHang` (`OrderLogisticConsumer.API/Jobs/JobXuLyChiTieuKhachHang.cs:12`) → `NghiepVuJobService.XuLyTatCaNghiepVuPendingAsync` (`NghiepVuJobService.cs:60`): reset stuck (`:64`) → loop từng NghiepVu claim batch (guard 200 loop/nghiệp vụ `:70`).
4. **Claim + dispatch** — `ClaimVaXuLyMotBatchAsync` (`:86`) gọi store `PRC_..._ClaimBatch` (`:99`); `XuLyBatchTheoDonAsync` (`:115`) sort theo `ThoiGianTrangThaiDon` → `handler.XuLyBatchAsync` → `MoveToHistory` (`:292`) / lỗi `RetryOrMoveToHistory` (`:267`, MAX_RETRY=3, delay 5'`).
5. **Handler CHI_TIEU** — `ChiTieuKhachHangService.XuLyBatchAsync` (`OrderLogistic.Service/Generated/ChiTieu/ChiTieuKhachHangService.cs:50`): đọc net từ ChiTieuLog → tính deltaStep/deltaTong → gọi Customer `CapNhatChiTieuLogistic` (`CommonService.cs:3044`) **trước**, ghi log **sau** (`:172`).
6. **Job retry/cleanup** — `JobXuLyNghiepVuLoi` (`OrderLogisticConsumer.API/Jobs/JobXuLyNghiepVuLoi.cs:11`) → `XuLyNghiepVuLoiAsync` (`NghiepVuJobService.cs:151`): cleanup pending(7d)/history(60d)/chiTieuLog(120d) + retry History Loi (<24h, SoLanThuLai<6).

## Stored procedures (DB — không trong repo source)
- `PRC_SCRM_OrderLogistic_DonHangLogisticNghiepVuPending_ClaimBatch` (claim atomic, UPDLOCK+READPAST, OUTPUT inserted.*)
- `..._ResetStuck` (clear token + tăng SoLanThuLai khi IsProcessing>5')
- `..._HaIsMoiNhat` (hạ IsMoiNhat pending cũ cùng IdDonHang+NghiepVu)
- `..._ClearDone` (xóa cứng pending IsDelete quá N ngày)
- `..._DonHangLogisticNghiepVuHistory_ClearCu`, `..._DonHangLogisticChiTieuLog_ClearCu`
- `ThuLaiTuHistory` còn dùng raw SQL `@NghiepVu` để hạ IsMoiNhat (registry node ghi nhận).

## Bug-prone / lưu ý
- **Phạm vi thực tế ≠ premise "tồn kho".** Code hiện CHỈ có handler `CHI_TIEU` (chi tiêu + Nợ/Trả Customer). `TRU_KHO`/`TAO_HOA_DON` chỉ là comment placeholder trong `EnumNghiepVu` — chưa có handler, queue này **không** đụng tồn kho/hóa đơn. Đừng tài liệu/hứa nhầm.
- **Double chi tiêu khi log fail:** handler gọi Customer trước, ghi log sau (`ChiTieuKhachHangService.cs:139-200`). Nếu Customer OK nhưng ghi `ChiTieuLog` fail → vòng retry tính net theo log cũ → có thể **cộng chi tiêu lần 2**. Code chấp nhận rủi ro này (xác suất thấp); khi điều tra lệch chi tiêu, soi `ChiTieuLog` theo `IdPending`.
- **Consumer không catch exception** (`XuLyNghiepVuConsumer.cs:33`) — cố ý cho MassTransit retry/DLQ. Debug "retry mãi không xong" → soi DLQ + `IsProcessing`, không tìm trong log app.
- **Hai đường xử lý song song** (MQ consumer + Quartz job) đi chung store ClaimBatch → idempotent qua `IsProcessing` + `IsMoiNhat`. Sửa logic claim phải giữ tính idempotent (bug C3 cũ: UPDATE cục bộ bị ghi đè token → đã chuyển sang store).
- **`SoTienNo = SoTienTarget`** (không phải `DonHangTienCuoi`) — cố ý để Nợ/Trả nhất quán net=0 khi thu đủ COD; GH1P ghi nợ theo phần đã giao (`NghiepVuPendingService.cs:88-94`). Đừng "sửa" thành TienCuoi.
- **`TaoNghiepVuPendingInTransactionAsync` throw** → nếu gọi trong transaction update đơn, lỗi enqueue làm **rollback cả update đơn**. Bản `TaoNghiepVuPendingAsync` nuốt lỗi (dùng ngoài transaction). Chọn đúng overload theo ngữ cảnh.
- **Pending legacy thiếu `SoTienTarget`** bị skip (`ChiTieuKhachHangService.cs:92`) → tránh rollback nhầm; nếu thấy đơn cũ "không cộng chi tiêu" có thể do thiếu snapshot này.
- **Cấu hình `TrangThaiTang` sai** → đảo Tăng/Giảm: thêm/bớt mã trạng thái (vd quên 35) làm đơn giao một phần bị tính Giảm. Default "31;32".
- **`ThuLaiTuHistory` chặn TrangThaiXuLy=ThanhCong(1)** — chỉ Loi(2)/BoQua(3) tạo lại được (`NghiepVuManagementService`); đừng cho retry bản đã thành công (double count).

## File source (path:line)
- Enqueue: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderLogistic\OrderLogistic.Service\Generated\NghiepVu\NghiepVuPendingService.cs:56`
- Job service (claim/retry/cleanup): `...\OrderLogistic.Service\Generated\NghiepVu\NghiepVuJobService.cs:60` / `:86` / `:115` / `:151` / `:267` / `:292`
- Handler CHI_TIEU: `...\OrderLogistic.Service\Generated\ChiTieu\ChiTieuKhachHangService.cs:50`
- Consumer: `...\OrderLogisticConsumer.API\Consumer\XuLyNghiepVuConsumer.cs:25`
- Jobs: `...\OrderLogisticConsumer.API\Jobs\JobXuLyChiTieuKhachHang.cs:12`, `...\Jobs\JobXuLyNghiepVuLoi.cs:11`
- Controller quản trị: `...\OrderLogistic.API\Controllers\NghiepVuManagementController.cs:12`
- Customer call: `...\OrderLogistic.Service\Generated\Common\CommonService.cs:3044`
- Entities: `...\OrderLogistic.DataContext\Entity\DonHangLogisticNghiepVuPending.cs:11`, `...\DonHangLogisticNghiepVuHistory.cs`, `...\DonHangLogisticNghiepVuCauHinh.cs:11`, `...\DonHangLogisticChiTieuLog.cs`
- Message: `...\OrderLogistic.DataContext\Messages\XuLyNghiepVuMessage.cs:7`
- Enums: `...\Libs\Models.Core\Enum\DonHangLogistic\EnumNghiepVu.cs`, `...\OrderLogistic.Service\Generated\Enums\EnumNghiepVuHistoryTrangThai.cs`, `...\Libs\Models.Core\Enum.cs:340` (GiaoVan_TT_PS)
- Registry node: `projects/scrm/registry/kho/NghiepVuManagementController.md`
