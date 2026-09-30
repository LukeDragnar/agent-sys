# Dev — Lịch sử nghiệp vụ logistics

> Chỉ dành cho dev. Mô tả implementation thực tế (source: SCRM BE/FE).

## Màn hình & route (FE — Angular 15 + ng-zorro)
- **Route:** `lich-su-nghiep-vu-logistic` (khai báo trong `landing-page-routing.module.ts`, dòng ~78-81).
- **Component:** `LichSuNghiepVuLogisticComponent` extends `BaseListComponent`.
- **Service FE:** `NghiepVuManagementService` extends `BaseService`, base URL = `${environment.SERVICE_API_OrderLogistic}api/NghiepVuManagement`.
- **UI:** `nz-card` chứa form filter (mã đơn, date-range, select trạng thái giao hàng, multi-select trạng thái nghiệp vụ) + `nz-table` 1 dòng / đơn + `nz-pagination`. Chi tiết hiển thị trong `nz-drawer` (width 780) với 2 bảng: Pending + History.
- **Methods FE chính:**
  - `gridLoadData()` — map `searchModel.date` → `tuNgay/denNgay`, gọi `timDonHang(body)`.
  - `onXemChiTiet(row)` — `Promise.all([timPending({idDonHang}), timHistory({idDonHang})])` rồi đổ vào `detailPending` / `detailHistory`.
  - `getTrangThaiHistoryTag(n)` — 1=Thành công (xanh), 2=Thất bại (đỏ), else Bỏ qua (vàng).
  - `getHuongLabel(n)` — 1=Tăng, else Giảm.
  - `getTenTrangThai(ma)` — lookup tên trạng thái giao hàng từ `listDMTrangThaiGiaoHang`.

## Controller (BE — .NET 6, OrderLogistic.API)
`NghiepVuManagementController : BaseController`, route `api/[controller]`, attribute `[TienIch("NghiepVuManagement_Controller", ..., MaTienIchSCRM.TI_OrderLogistic)]`.

| Endpoint | Method | Permission (SBAuthorize) | Chức năng |
|---|---|---|---|
| `TimDonHang` | POST | `NghiepVuManagement_TimDonHang` | Danh sách đơn + thống kê Pending/Thành công/Thất bại (màn hình chính). |
| `TimPending` | POST | `NghiepVuManagement_TimPending` | Pending của 1 đơn (drawer chi tiết). |
| `TimHistory` | POST | `NghiepVuManagement_TimHistory` | History của 1 đơn (drawer chi tiết). |
| `ThuLaiPending/{id:guid}` | POST | `NghiepVuManagement_ThuLaiPending` | Reset 1 pending lỗi (SoLanThuLai=0, ThoiGianXuLy=now) + publish MQ. |
| `ThuLaiTuHistory/{id:guid}` | POST | `NghiepVuManagement_ThuLaiTuHistory` | Tạo lại pending từ history lỗi/bỏ qua + publish MQ. |

Tất cả lấy `CurrentUser` (UserLogin) để scope theo `DonViId` + `IdChiNhanh`. *Lưu ý: FE hiện chỉ gọi 3 endpoint Tìm; 2 endpoint ThuLai đã có BE nhưng chưa thấy nút trên FE component — cần product xác nhận có expose hay không.*

## Entities (OrderLogistic.DataContext.Entity)
- **`DonHangLogisticNghiepVuPending`** — hàng đợi nghiệp vụ CHƯA xử lý. Khi job xử lý xong → chuyển sang History rồi xóa ở đây (giữ bảng nhỏ). Field đáng chú ý:
  - `NghiepVu` (VARCHAR 100, vd "CHI_TIEU", "COMMISSION"), `TriggerEvent` ("UPDATE_STATUS" | "DOISOAT").
  - `HuongXuLy` (1=Tăng, -1=Giảm), `TrangThaiGiaoHang` (snapshot), `SoTienTarget`, `SoTienNo`, `ThoiGianTrangThaiDon` (snapshot để xử lý tuần tự).
  - `ThoiGianDuDieuKien`, `ThoiGianXuLy` (= DuDieuKien + delay; job quét `ThoiGianXuLy <= now`).
  - `IsMoiNhat` (chỉ pending mới nhất theo cặp IdDonHang+NghiepVu mới được claim), `SoLanThuLai`, `GhiChuLoi`.
  - `IsProcessing` / `ProcessingStartedAt` / `ProcessingToken` — claim batch (store ClaimBatch dùng UPDLOCK + READPAST; store ResetStuck reset record treo).
- **`DonHangLogisticNghiepVuHistory`** — cùng cấu trúc Pending + field kết quả: `TrangThaiXuLy`, `SoLanThuLai`, `GhiChuLoi`, `ThoiGianXuLyThucTe`.
- **`DonHangLogistic`** — đơn giao vận; service join lấy `MaDon`, `KhachHangTen`, `GiaoHangTrangThaiMa`.
- **Enum `EnumNghiepVuHistoryTrangThai`:** 1=ThanhCong, 2=Loi, 3=BoQua, 4=DangRetry, 5=FixDataThanhCong.

## Luồng code (TimDonHang — màn hình chính)
`NghiepVuManagementService.TimDonHangAsync(req, userLogin)`:
1. Xác định filter trạng thái nghiệp vụ (`pending`/`thanh-cong`/`that-bai`); chuẩn hóa `DenNgay` về end-of-day.
2. Nếu có filter đơn (MaDon/TuNgay/DenNgay/GiaoHangTrangThaiMa) → query `DonHangLogistic` (TableWithNolock, scope DonVi+ChiNhanh) lấy ra `filteredDonHangIds`.
3. Group **Pending** theo `IdDonHang` → đếm `Count`. Group **History** theo `IdDonHang` → đếm `SoThanhCong` (TrangThaiXuLy=ThanhCong), `SoThatBai` (=Loi), `Max(ThoiGianXuLyThucTe)`.
4. **Union** id từ 2 nhóm → build `DonHangNghiepVuSummaryDto` (SoPending, SoThanhCong, SoThatBai, ThoiGianGanNhat).
5. Áp filter trạng thái (giữ đơn có ít nhất 1 loại > 0 theo cờ đã chọn), `OrderByDescending(ThoiGianGanNhat)`, `Skip/Take` phân trang.
6. Map `MaDon`/`TenKhachHang`/`GiaoHangTrangThaiMa` cho trang hiện tại qua `GetDonHangMapAsync` (chỉ load đơn của trang → nhẹ).

`TimPendingAsync` / `TimHistoryAsync`: filter theo IdDonHang/IdKhachHang/NghiepVu (+ ChiCoLoi cho pending, TrangThaiXuLy cho history), order desc theo NgayTao / ThoiGianXuLyThucTe, **Take(200)**, map kèm thông tin đơn.

`ThuLaiPendingAsync`: tìm pending theo id+scope; reset `SoLanThuLai=0`, `ThoiGianXuLy=now`, `GhiChuLoi=null`, `MarkDirty` từng field, Update, rồi `Publish(XuLyNghiepVuMessage{IdPending, NghiepVu})`.

`ThuLaiTuHistoryAsync`: chặn nếu history đã ThanhCong; chặn nếu đã tồn tại pending cùng (IdDonHang, NghiepVu, HuongXuLy); tạo `DonHangLogisticNghiepVuPending` mới (copy snapshot từ history, IsMoiNhat=true, SoLanThuLai=0); gọi store `PRC_SCRM_OrderLogistic_DonHangLogisticNghiepVuPending_HaIsMoiNhat` (Dapper) để hạ IsMoiNhat pending cũ; Insert; Publish MQ.

## Cách record được tạo & xử lý (context — ngoài màn hình)
- Pending được sinh khi đơn đổi trạng thái giao hàng / đối soát (TriggerEvent UPDATE_STATUS | DOISOAT).
- **Consumer** `XuLyNghiepVuConsumer : IConsumer<XuLyNghiepVuMessage>` nhận MQ → dispatch sang `INghiepVuJobService.XuLyMotNghiepVuPendingAsync`. Consumer **không catch exception** (để MassTransit retry/DLQ); store ClaimBatch idempotent qua `IsProcessing`.
- Quartz job cũng dùng chung đường store ClaimBatch (chọn đơn đầu đến hạn theo NghiepVu). Xử lý xong → chuyển Pending sang History, set TrangThaiXuLy; vượt ngưỡng retry → History với Loi.

## Bug-prone / lưu ý khi sửa
- **Scope DonVi + ChiNhanh** bắt buộc ở mọi query (3 bảng). Quên `IdChiNhanh` → rò dữ liệu chi nhánh khác. ThuLaiPending/ThuLaiTuHistory cũng kiểm tra scope khi tìm record.
- **Filter ngày lọc theo `NgayTao` của ĐƠN**, không phải thời điểm xử lý nghiệp vụ — dễ nhầm khi tra đơn cũ. `DenNgay` được `.AddDays(1).AddSeconds(-1)` để bao trùm cả ngày.
- **Group + Union + paging làm in-memory** sau khi `ToListAsync` (group đã xuống DB nhưng merge/sort/paging ở C#). Tập đơn lớn của 1 chi nhánh → cân nhắc cost; total = `summaries.Count` sau filter.
- **Take(200)** ở TimPending/TimHistory: đơn có quá nhiều bản ghi sẽ bị cắt — chi tiết không đầy đủ. Không có paging ở drawer.
- **IsMoiNhat:** khi tạo pending mới (ThuLaiTuHistory) phải hạ cờ pending cũ qua store, nếu không job có thể claim nhầm bản cũ. Đừng bỏ bước gọi store.
- **ThuLaiTuHistory** chặn trùng theo (IdDonHang, NghiepVu, **HuongXuLy**) — không chỉ theo NghiepVu; sửa điều kiện này cẩn thận kẻo tạo trùng pending ngược hướng.
- **HuongXuLy** dùng giá trị `-1` cho Giảm (FE `getHuongLabel`: `=== 1 ? Tăng : Giảm`) — đừng giả định 0/1.
- FE `getTenTrangThai` so sánh `x.ma == ma` (loose) — phụ thuộc danh mục `DropdownDMGiaoHangTrangThai` load xong; nếu chưa load thì hiện số thô.

## File source
**BE** (`D:\PROJECT\Git\_SourceForCode\Group2\scrm`):
- `OrderLogistic\OrderLogistic.API\Controllers\NghiepVuManagementController.cs`
- `OrderLogistic\OrderLogistic.Service\Generated\NghiepVu\NghiepVuManagementService.cs` (+ `INghiepVuManagementService.cs`)
- `OrderLogistic\OrderLogistic.DataContext\Entity\DonHangLogisticNghiepVuPending.cs`
- `OrderLogistic\OrderLogistic.DataContext\Entity\DonHangLogisticNghiepVuHistory.cs`
- `OrderLogistic\OrderLogistic.Service\Generated\Enums\EnumNghiepVuHistoryTrangThai.cs`
- `Libs\Models.Core\DTO\OrderLogistic.Client\NghiepVu\NghiepVuManagementDto.cs` (Request/Result/Item DTOs)
- `OrderLogistic\OrderLogisticConsumer.API\Consumer\XuLyNghiepVuConsumer.cs`
- `OrderLogistic\OrderLogistic.DataContext\Messages\XuLyNghiepVuMessage.cs`
- Liên quan xử lý: `OrderLogistic.Service\Generated\NghiepVu\NghiepVuJobService.cs`, `NghiepVuPendingService.cs`

**FE** (`D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`):
- `src\app\pages\landing-page\lich-su-nghiep-vu-logistic\lich-su-nghiep-vu-logistic.component.ts` (+ `.html`, `.scss`)
- `src\app\services\orderLogistic\nghiep-vu-management.service.ts`
- `src\app\pages\landing-page\landing-page-routing.module.ts` (route), `landing-page.module.ts` (declaration)
