---
controller: InvoiceController
module: ke-toan
service: Invoice
man_hinh_ui: "Phát hành/tra cứu HĐĐT từ màn đơn hàng (Order/OrderLogistic): nút Xuất hóa đơn, popup Danh sách hóa đơn điện tử của đơn (lưới phát hành + nháp), Tải file PDF; màn tra cứu HĐĐT toàn đơn vị (tìm theo điều kiện + Xuất Excel). Một số endpoint là service-to-service (Order/OrderLogistic gọi sang)"
muc_dich: "Cổng phát hành & tra cứu hóa đơn điện tử (MeInvoice/MISA + Viettel SInvoice): tạo HĐ thật (qua queue), tạo/xóa/đồng bộ HĐ nháp, lấy file PDF, tìm kiếm + xuất Excel lịch sử phát hành"
alias: ["xuất hóa đơn điện tử", "phát hành HĐĐT", "hóa đơn nháp", "tải hóa đơn PDF", "tra cứu hóa đơn", "CreateInvoice"]
auth: mixed   # CreateInvoice/TaoHoaDonNhap/XoaHoaDonNhap/DongBoTrangThai = internal (PrivateSBAuthorize); còn lại SBAuthorize + gate tiện ích Misa/Viettel
state_machine: [Draft, Created, CreateFail, ValidateFail]   # Status_Nhap (nháp) + Status (publish) trên InvoiceStatus
deprecated: false
entities: [InvoiceStatus, InvoiceInfo, HoaDonDienTuHistory]   # HoaDonDienTuHistory ở MongoDB; InvoiceStatus/InvoiceInfo ở SQL
stored_procs: []
depends:
  - IHoaDonDienTuService     # orchestrator phát hành/tra cứu (Generated)
  - IMeInvocieService        # nhánh MISA (xóa nháp, get file)
  - IViettelInvoiceService   # nhánh SInvoice (đồng bộ, get file) — gọi gián tiếp qua HoaDonDienTuService
  - IMongoDbClient           # đọc HoaDonDienTuHistory
  - RabbitMQ/MassTransit     # publish message phát hành/nháp (bất đồng bộ)
cross_module_in:
  - Order (PhieuXuatKho/Order) gọi CreateInvoice/TaoHoaDonNhap/XoaHoaDonNhap/DongBoTrangThaiHoaDonNhap
  - OrderLogistic gọi tương tự cho đơn online
cross_module_out:
  - MeInvoice/MISA partner API (qua consumer) — phát hành/ký HĐ
  - Viettel SInvoice partner API (qua consumer)
  - SMS/ZNS (đẩy tin báo HĐ sau phát hành — TrangThaiGuiSMS/ZNS trên InvoiceInfo)
---

# InvoiceController — Phát hành & tra cứu hóa đơn điện tử (MeInvoice + Viettel)

> Controller "mỏng" nhưng đứng đầu luồng HĐĐT phức tạp nhất service: phát hành **bất đồng bộ qua RabbitMQ** (không trả kết quả phát hành ngay), nháp/đồng bộ tách NCC (MISA vs Viettel), tra cứu đọc **MongoDB** (`HoaDonDienTuHistory`) + **SQL** (`InvoiceStatus`/`InvoiceInfo`).
> 4 endpoint đầu là **internal** (`PrivateSBAuthorize`) — Order/OrderLogistic gọi sang sau khi tự validate precondition. Các endpoint còn lại có UI nhưng **gate cứng tiện ích** `TI_Invocie_Misa` HOẶC `TI_Invocie_Viettel` (không có → trả lỗi "Bạn không có quyền sử dụng tiện ích này").
> Phát hành trả ngay câu "Đã nhận yêu cầu…" — kết quả thật ghi async vào `InvoiceStatus`/`HoaDonDienTuHistory` bởi consumer; FE phải poll lại (GetHoaDonDienTuByIdDonHang / LayDanhSachPhatHanh).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tải file PDF hóa đơn | Nút "Tải" / icon download trong popup HĐĐT | `GET GetInvoiceFile?idDonHang&no&type` | không key (gate tiện ích Misa/Viettel) | mixed | sau khi đã phát hành (có InvNo) | Gate tiện ích. Tìm `HoaDonDienTuHistory` theo IdDonVi+TypeDonHang+IdDonHang+InvNo. MISA → gọi `_meinvoice.GetInvoiceRepresentationFile_ToClient`; Viettel → `ViettelInvoiceDownloadFile_ToClient`. Trả `File(bytes,"application/pdf",no)` | Mongo + partner API | Bản cũ `GetInvoiceFile` đọc file vật lý wwwroot bị **comment** (controller:81-117); bản hiện tại stream byte từ partner |
| 2 | Lấy trạng thái HĐĐT của đơn | (load popup HĐĐT, badge trạng thái) | `POST GetHoaDonDienTuByIdDonHang/{id:Guid}` | không key (gate tiện ích) | mixed | mọi đơn | Gate tiện ích. Lấy 1 `InvoiceStatus` theo IdDonHang+IdDonVi (NoLock). Không có → "Không tìm thấy hóa đơn điện tử cho đơn hàng này!" | EF `InvoiceStatus` NoLock | — |
| 3 | Danh sách phát hành theo đơn (phân trang) | Lưới popup "Danh sách hóa đơn điện tử" | `POST LayDanhSachPhatHanhTheoIdDonHang` | không key (gate tiện ích) | mixed | popup HĐĐT của 1 đơn | Gate tiện ích. Validate IdDonHang+TypeDonHang. Query **Mongo** `HoaDonDienTuHistory` scope IdDonVi, sort NgayCapNhat desc. Filter tùy chọn: Keyword (regex MaDonHang/InvNo/InvCode/TransactionID), ActionType, IsNhap (true=chỉ nháp / false=chỉ thật), TuNgay/DenNgay, TypeExcute (1=thủ công, 2=tự động+gửi lại) | Mongo (projection 1 phần) | Phân trang offset Mongo + Count riêng — 2 query |
| 4 | Tìm HĐĐT toàn đơn vị theo điều kiện | Lưới màn tra cứu HĐĐT | `POST TimHoaDonDienTuTheoDieuKien` | không key (gate tiện ích) | mixed | màn tra cứu | Gate tiện ích. `BuildQueryHoaDonDienTuTheoDieuKien` trên **SQL** `InvoiceInfo`, phân trang Skip/Take | EF `InvoiceInfo` | Nguồn dữ liệu KHÁC #3 (SQL vs Mongo) — 2 bảng khác nhau, dễ lệch dữ liệu giữa popup-theo-đơn và tra-cứu-tổng |
| 5 | Xuất Excel HĐĐT theo điều kiện | Nút "Xuất Excel" màn tra cứu | `POST XuatExcelHoaDonDienTuTheoDieuKien` | không key (gate tiện ích) | mixed | màn tra cứu | Gate tiện ích (gate fail → `BadRequest` chứ không ResponseResult). result null/!Success/Data null → BadRequest. OK → `File(...,xlsx,"HoaDonDienTu.xlsx")` | EF `InvoiceInfo` → Excel stream | Gate trả `BadRequest` (400) khác convention các action khác (ResponseResult) |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Order/OrderLogistic → `POST CreateInvoice`** (`PrivateSBAuthorize`): phát hành HĐ THẬT. PhieuXuatKho chỉ cho MISA (Viettel bị chặn). Theo `TypeInvoice` publish `InputDataCreateInvoiceViettelRabit` / `InputDataCreateInvoiceMISARabit` lên RabbitMQ (**retry 3 lần, delay 1s**). Luôn trả "Đã nhận yêu cầu xuất hóa đơn điện tử" — phát hành thật do consumer xử lý async.
- [internal] **Order/OrderLogistic → `POST TaoHoaDonNhap`** (`PrivateSBAuthorize`): tạo HĐ NHÁP. Guard null user. Chỉ MISA + Viettel (NCC khác → "Nhà cung cấp hóa đơn không hỗ trợ tạo nháp!"). Set `IsNhap=true`, publish `InputDataCreateDraftViettelRabit`/`InputDataCreateDraftMISARabit` (retry 3 lần).
- [internal] **Order/OrderLogistic → `POST XoaHoaDonNhap`** (`PrivateSBAuthorize`): gọi `_meinvoice.XoaHoaDonNhap` (qua MISA).
- [internal] **Order/OrderLogistic → `POST DongBoTrangThaiHoaDonNhap`** (`PrivateSBAuthorize`): đồng bộ nháp→phát hành. **Orchestrator tách NCC** theo `InvoiceStatus.TypeInvoice`: Viettel → `_viettel.DongBoSInvoice`, MISA → `_meinvoice.DongBoMISA`, **gộp kết quả** (List<SyncDraftResultItem>). Đơn chưa có InvoiceStatus rơi vào nhánh MISA báo "chưa có HĐ".

## Quy tắc nghiệp vụ (gating / dispatch theo NCC)
1. **Gate tiện ích (mọi action UI):** `CheckTienIch(TI_Invocie_Misa) || CheckTienIch(TI_Invocie_Viettel)` — false → chặn ngay. Đây là gate controller-level lặp lại từng method (không phải attribute).
2. **PhieuXuatKho chỉ MISA:** `CreateInvoice` chặn `LoaiDonHang==PhieuXuatKho && TypeInvoice!=MeInovice` → "Phiếu xuất kho chỉ hỗ trợ phát hành hóa đơn qua MeInvoice".
3. **Dispatch NCC theo `TypeInvoice`:** ViettelInvoice → queue Viettel; MeInovice → queue MISA; khác → "Chưa hỗ trợ loại hóa đơn này" (publish) / "Nhà cung cấp hóa đơn không hỗ trợ tạo nháp!" (nháp).
4. **Đồng bộ nháp tách NCC theo `InvoiceStatus.TypeInvoice`** (KHÔNG theo payload) — chống lệch NCC. Lỗi cứng MISA KHÔNG return sớm (giữ kết quả Viettel đã ghi DB), gắn lỗi per-đơn rồi trả gộp.
5. **Phân biệt nháp/thật:** `IsNhap==true` (nháp) vs `IsNhap != true` (null/false = HĐ thật) — filter ở #3.

## Quan hệ
- **Gọi sang:** `IHoaDonDienTuService` (orchestrator) → `IMeInvocieService` / `IViettelInvoiceService` (nhánh NCC), `IMongoDbClient` (`HoaDonDienTuHistory`), `_serviceCache` (cache Account), RabbitMQ publish.
- **Dữ liệu (entities):** `InvoiceStatus` (SQL — trạng thái HĐ/nháp theo đơn, transaction state machine), `InvoiceInfo` (SQL — chi tiết phát hành theo đơn, gồm trạng thái gửi SMS/ZNS), `HoaDonDienTuHistory` (Mongo — lịch sử từng lần gửi NCC, file URL).
- **Ripple / đồng bộ:** phát hành/nháp là **async qua RabbitMQ** → consumer gọi partner (MISA/Viettel), ghi InvoiceStatus + HoaDonDienTuHistory, đẩy SMS/ZNS. FE không nhận kết quả phát hành ngay.

## Luồng xuyên module
- **Phát hành HĐ:** Order/OrderLogistic validate (cấu hình `IsAllowPublishInvoice` + trạng thái cho phép) → `CreateInvoice` → publish queue → consumer gọi MISA/Viettel ký+phát hành → ghi `InvoiceStatus.Status`, append `HoaDonDienTuHistory` → đẩy SMS/ZNS báo khách → FE poll `GetHoaDonDienTuByIdDonHang`.
- **Nháp → phát hành:** `TaoHoaDonNhap` (IsNhap=true, ghi `Status_Nhap=Draft`) → kế toán review → `DongBoTrangThaiHoaDonNhap` (lô ≤50 mỗi nhánh) → consumer phát hành → `Status_Nhap=Created`.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `InvoiceController.cs:73,123,152,165,177` — gate tiện ích copy-paste 5 lần (Misa||Viettel); #5 dùng `BadRequest` thay vì `ResponseResult` → status code lệch (400 vs 200 + body lỗi).
- `InvoiceController.cs:81-117` — bản `GetInvoiceFile` đọc file vật lý wwwroot bị comment; chỉ bản stream-byte (line 119-144) còn sống.
- `HoaDonDienTuService.cs:91-144` (`TaoHoaDonDienTu`) — retry 3 lần publish; nếu publish fail cả 3 lần **vẫn trả "Đã nhận yêu cầu…"** (không báo lỗi cho caller) → đơn có thể "tưởng đã gửi" mà thực chưa publish.
- `HoaDonDienTuService.cs:199-237` (`DongBoTrangThaiHoaDonNhap`) — tách NCC theo `InvoiceStatus.TypeInvoice`; cố ý KHÔNG return sớm khi MISA lỗi để không vứt kết quả Viettel đã ghi DB.
- Hai nguồn dữ liệu tra cứu: `LayDanhSachPhatHanhTheoIdDonHang` đọc **Mongo** (`HoaDonDienTuHistory`), `TimHoaDonDienTuTheoDieuKien` đọc **SQL** (`InvoiceInfo`) → cùng khái niệm "hóa đơn theo đơn" nhưng 2 store khác nhau.
- `TESTSEND` (controller:203-225) đã comment — code test gọi thẳng Viettel API, không phát sinh endpoint.
