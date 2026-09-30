---
controller: ContactImportController
module: sales
service: Contact
man_hinh_ui: "Marketing/Landing > Hồ sơ khách hàng > Import contact (landing-page/ho-so-khach-hang/import-contact, import-contact-thucong) · Sale > Danh sách contact > Import (sale/danhsach-contact/import-contact) · component file-import-history"
muc_dich: "Import contact từ Excel theo lô (file/dòng), quản lý log import + file import, check trùng/kiểu số trước khi đẩy vào Contact, export Excel danh sách contact (kiểu 1/2)"
alias: [ContactImport, "Import contact", "Import data MKT", "File import contact"]
auth: mixed
state_machine: [ContactImport_ChoXuLy, ContactImport_DangXuLy, ContactImport_HoanThanh, FileImport_ChoXuLy, FileImport_HoanThanh]
deprecated: false
entities: [ContactImport, FileImport, ContactImportLog]
stored_procs: ["TimTheoDieuKien/thống kê import đi qua SP (Dapper) — TotalCount/InvalidCount/NoProcessCount đọc từ DataSet"]
depends: [IContactImportService, IFileImportService]
cross_module_in: [OrderLogistic]
cross_module_out: [Contact]
---

# ContactImportController — Import contact từ Excel (theo lô)

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách contact import | Lưới import theo đơn vị | `POST TimTheoDieuKienAsync` | ContactImportController_TimTheoDieuKienAsync | public | — | List ContactImport theo filter | SP | — |
| 2 | Thống kê / log import | Card thống kê + lưới log | `POST GetContactImportLogTotal` · `POST GetContactImportLogByUser` | ContactImportController_GetContactImportLogTotal / _GetContactImportLogByUser | public | — | Tổng + log import theo user | SP | — |
| 3 | Tải file mẫu import | Nút "Tải file mẫu" | `GET DownloadFileTemplateImportContact` | ContactImportController_DownloadFileTemplateImportContact | public | — | Trả `Contact.xlsx` (template có dropdown nguồn/sale/sp...) | GemBox | — |
| 4 | Import contact (file) | Nút "Import" | `POST ImportContact` (IFormFile) | ContactImportController_ImportContact | public | — | Đọc Excel → validate → tạo bản ghi ContactImport (chưa vào Contact) | GemBox/EF | **Mapping cột Excel index cứng, data từ row 7**; có hack `shift`/`shiftEmail` khi đổi template (xem Lỗi đã biết) |
| 5 | Tải file lỗi import | Nút "Tải file lỗi" | `GET DownloadDataWrongContact` (id, userName) | ContactImportController_DownloadDataWrongContact | public | — | Trả `DataImportContactFaild.xlsx` các dòng sai | GemBox | userName truyền raw qua query — nên lấy từ token |
| 6 | Xóa import theo trạng thái | Nút "Xóa theo trạng thái" | `POST DeleteContactImportByMaTrangThai` | ContactImport_DeleteContactImportDonByMaTrangThai | public | theo maTrangThai | Xóa các ContactImport ở trạng thái chỉ định | EF | Mã quyền lệch tên route (Don thừa) |
| 7 | Quản lý file import (CRUD) | Lưới "File import" + lịch sử | `POST GetListFileImport` · `InsertFileImport` · `DeleteFileImportById` · `ImportFile` · `HistoryFileImport` · `DeleteContactByFileImport` | ContactImportController_GetListFileImport / _InsertFileImport / _DeleteFileImportById / _ImportFile / _HistoryFileImport / _DeleteContactByFileImport | public | — | CRUD FileImport, xóa contact theo file → `IFileImportService` | EF | — |
| 8 | Hoàn thành file import | (chuyển trạng thái) | `POST ProcessCompleteFileImport` · `POST CompleteFileImport` | ContactImportController_ProcessCompleteFileImport / _CompleteFileImport | public | state FileImport | Chuyển FileImport sang Hoàn thành | EF | 2 endpoint gần trùng (Process vs Complete) |
| 9 | Export Excel contact (kiểu 1/2) | Nút "Xuất Excel" | `POST ExportExcelContactKieu1` · `POST ExportExcelContactKieu2` | `[SBAuthorize()]` (chỉ đăng nhập) | public | — | Trả `DanhSachContact.xlsx` / `...Kieu2.xlsx`; `Service_Unavailable` → **HTTP 503**, lỗi khác → **500** (raw, không qua ResponseResult) | GemBox/EF | Trả mã HTTP raw 500/503 khác convention; quyền chỉ `[SBAuthorize()]` |
| 10 | Giới hạn số lượng import | (gating UI số dòng) | `POST GetSoLuongDataChoPhepImport` · `POST GetSoLuongContactImportToiDa` | `[SBAuthorize()]` | public | — | Trả số dòng được phép / tối đa | EF | — |

## Endpoint nội bộ (private / job)
- **Job xử lý ngầm (đẩy import → Contact):** `ProcessCheckKieuSoImport` · `TongHopCheckKieuSoImport(countCheckTrenLan=100)` · `ResetCheckKieuSoImport` · `ProcessInsertContact` · `InsertContactImportToContact(countImportTrenLan, optionCheckTrungHeThong, sessionId)` — pipeline: check kiểu số → check trùng → insert vào Contact theo batch. Phần lớn có mã quyền riêng; `ResetCheckKieuSoImport` chỉ `[SBAuthorize()]` (mã/role bị comment).
- **[internal] OrderLogistic → tạo contact từ import đơn:** `[PrivateSBAuthorize] POST ContactImportDonhangLogistic` (List ReqTaoContactTuImportDonHangLogistic) → tạo contact từ đơn logistic import.

## Quy tắc nghiệp vụ (gating / template)
1. **Nhận diện template cũ/mới:** quét 7 dòng header đầu, nếu thấy chuỗi "kết quả tác nghiệp" → `isFileMoi=true`.
2. **Bù cột (shift):** `shiftEmail=1` (file mới chèn cột Email sau SĐT index 5) và `shift=3` (Email + Kết quả tác nghiệp/Thời gian chờ) → các index cột phía sau cộng thêm `shiftEmail`/`shift`. Đây là blueprint trace: cùng 1 field có 2 vị trí cột tùy template.
   - **(mới 07/2026) `genShiftNgaySinh=1`** — template mới thêm cột **Ngày sinh** ngay sau SĐT (index 5): nhận diện bằng header chứa "ngày sinh" (`ContactImportService.cs:1095-1104`); các ô ví dụ (sale/tác nghiệp/SP/DV/combo/nhãn) index +1 (`:1171-1266`). Chồng thêm 1 lớp shift nữa lên `shiftEmail`/`shift`.
3. **Data bắt đầu từ row index 7** (6 dòng header/example).
4. **(mới 07/2026) Cột địa chỉ append CUỐI** (sau "Quốc gia") — `Tinh`/`Huyen`/`Xa` nhập **TÊN**, BE resolve tên→mã theo **phân cấp** (`ChuanHoaTenDiaChi` normalize bỏ tiền tố tỉnh/tp/quận/huyện/phường/xã; dictionary key `"{maCha}|{tenChuan}"`, lookup O(1) trong phạm vi cha — `:1283`, `:1762-1774`, `:2043+`). `DiaChi` = số nhà/đường lưu text thẳng. **Địa chỉ sai KHÔNG loại dòng** (Option 2): bỏ cấp sai + tăng `countDiaChiWarning`, gộp cảnh báo (`:2072`).
5. **(mới 07/2026) Validate ngày sinh import** (`:2105`): `KhachHangNgaySinh` không được > `DateTime.Now.Date` → dòng lỗi `"Ngày sinh không được vượt quá hiện tại"`; sai định dạng dd/MM/yyyy → `"Ngày sinh nhật không hợp lệ"` (`:2114`). Ngày sinh import propagate sang KhachHang khi đẩy import→Contact (đặt tên field `ContactImport.KhachHangNgaySinh` trùng `Contact.KhachHangNgaySinh`).

## Quan hệ
- **Gọi sang:** `IContactImportService` (import + job + export) · `IFileImportService` (CRUD file import).
- **Dữ liệu (entities):** `ContactImport`, `FileImport`, log import.
- **Ripple/đồng bộ:** ContactImport → (job) → **Contact** (InsertContactImportToContact). OrderLogistic → ContactImportDonhangLogistic.

## Luồng xuyên module
1. **Import file → Contact:** Upload `ImportContact` → ContactImport (validate) → job `ProcessCheckKieuSoImport`/`TongHopCheckKieuSoImport` (check kiểu số) → `ProcessInsertContact`/`InsertContactImportToContact` (đẩy vào Contact, có option check trùng hệ thống).
2. **OrderLogistic import đơn:** đơn logistic → `ContactImportDonhangLogistic` (private) → tạo contact tương ứng.

## Lỗi đã biết / lưu ý kỹ thuật
- `ContactImportService.cs:1283-1305` & `:1307-1415+` — **mapping cột Excel theo index cứng**; data đọc từ **row index 7**; có hack `shiftEmail`/`shift` để tương thích template cũ/mới (nhận diện bằng header text "kết quả tác nghiệp") → đổi cấu trúc template ngoài 2 dạng này sẽ mapping sai âm thầm.
- `ContactImportController.cs:233-305` (`ExportExcelContactKieu1/2`) — trả **HTTP raw 500/503** (`Service_Unavailable` → 503), không qua `ResponseResult`; FE phải bắt status code riêng.
- `ContactImportController.cs:348-465` (`FixBugContact`) — **return rỗng ngay dòng đầu**, phần còn lại dead code (hardcode user/ngày debug trong comment).
- `ContactImportController.cs:80-87` (`ResetCheckKieuSoImport`) — mã quyền + role bị comment, hạ về `[SBAuthorize()]`.
- `ContactImportController.cs:206-213` (`DownloadDataWrongContact`) — nhận `userName` qua query string thay vì token → có thể tải file lỗi của user khác.
- Export & một số endpoint chỉ `[SBAuthorize()]` (không mã/role) trong khi list/import có role rõ ràng → phân quyền không nhất quán.
