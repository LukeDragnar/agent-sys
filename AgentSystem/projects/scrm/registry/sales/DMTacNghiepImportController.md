---
controller: DMTacNghiepImportController
module: sales
service: Contact
man_hinh_ui: "Sale > Thiết lập tác nghiệp — tải template / import Excel (nội bộ trong màn cấu hình tác nghiệp; cũng có ở Quản trị > Tác nghiệp)"
muc_dich: "Import danh mục tác nghiệp bằng Excel: tải file template (4 sheet: tác nghiệp cần / kết quả / sau bao lâu / cấu hình), upload để nạp đồng loạt DMTacNghiep + DMKetQuaTacNghiep + DMTacNghiepSauBaoLau + DMTacNghiepCauHinh."
alias: [DMTacNghiepImport, "import tác nghiệp", "nhập Excel tác nghiệp", "import danh mục tác nghiệp"]
auth: public
state_machine: []
deprecated: false
entities: [DMTacNghiep, DMKetQuaTacNghiep, DMTacNghiepSauBaoLau, DMTacNghiepCauHinh]
stored_procs: []
depends:
  - "DMTacNghiepImportService (DownloadFileTemplateImport / ImportTemplateDMTacNghiep / TimTheoDieuKien / DeleteTacNghiepImport)"
  - "GemBox.Spreadsheet (ExcelService — đọc/ghi xlsx)"
  - "GetCauHinhInit (IsEnableTacNghiepTheoChiNhanh — gắn IdChiNhanh khi import)"
cross_module_in: []
cross_module_out:
  - "Ghi đồng loạt vào DMTacNghiep / DMKetQuaTacNghiep / DMTacNghiepSauBaoLau / DMTacNghiepCauHinh (insert + update)"
---

# DMTacNghiepImport — Import danh mục tác nghiệp (Excel)

> Controller import Excel (78 dòng) cho toàn bộ họ tác nghiệp. Template gồm **4 sheet**: `tacnghiepcan`, `ketquatacnghiep`, `tacnghiepsaubaolau`, `tacnghiepcauhinh`; mỗi sheet dữ liệu bắt đầu từ **dòng index 3** (tính từ 0). Service đọc theo map cột cố định → nạp vào 4 entity tương ứng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tải file template | Tải template | GET `DownloadFileTemplateImport` | `DMTacNghiepImportController_DownloadFileTemplateImport` (AD-TONG/CN/MCN) | public | Nút Tải template | Trả file `ImportTacNghiep.xlsx` (4 sheet, đổ sẵn dữ liệu hiện có theo cột cố định). | GemBox xlsx | — |
| 2 | Import file | Import | POST `ImportTemplateDMTacNghiep` (IFormFile) | `DMTacNghiepImportController_ImportTemplateDMTacNghiep` (AD-TONG/CN/MCN) | public | Chọn file → Import | Đọc 4 sheet (start row 3), lọc dòng có Ten/GiaTri, **giới hạn 1000 bản ghi/sheet**; insert/update vào 4 entity. Trống cả 4 → "Dữ liệu không tồn tại để import". | GemBox + EF | Map cột index cố định (xem lỗi) |
| 3 | Danh sách kết quả import | Grid kết quả | POST `TimTheoDieuKienAsync` | `DMTacNghiepImportController_TimTheoDieuKienAsync` (AD-TONG/CN/MCN) | public | Sau import | `TimTheoDieuKienAsync(reqData, CurrentUser)` — list trạng thái/log import. | EF | — |
| 4 | Xóa dữ liệu import | Xóa | POST `DeleteTacNghiepImport` (string statusImport) | `DMTacNghiepImportController_DeleteTacNghiepImport` (AD-TONG/CN/MCN) | public | Nút Xóa | `DeleteTacNghiepImport(statusImport, CurrentUser)`. | EF | — |

## Quan hệ
- **Gọi sang:** `IDMTacNghiepImportService`, `GemBox ExcelService`, `GetCauHinhInit`.
- **Dữ liệu (entities):** nạp đồng loạt `DMTacNghiep`, `DMKetQuaTacNghiep`, `DMTacNghiepSauBaoLau`, `DMTacNghiepCauHinh` (insert mới + update theo tên đã tồn tại).
- **Ripple / đồng bộ:** sau import, các danh mục/luật follow-up bị thay đổi → ảnh hưởng cache & cấu hình notification giống như khi sửa tay từng controller.

## Lỗi đã biết / lưu ý kỹ thuật
- **Map cột Excel theo INDEX cố định** (rủi ro lệch cột nếu user chèn/đổi thứ tự cột): sheet `tacnghiepcan` đọc cột 0–7 (Stt, Ten, Color, NhanKhachHang, IsKhoiDau, IsKhoSo, KhoSoThoiGian, IsSuaGio), `ketquatacnghiep` cột 0–4 (Stt, Ten, Color, IsChotDon, IsDatLich), `tacnghiepsaubaolau` cột 0–1 (SoThoiGian, LoaiThoiGian), `tacnghiepcauhinh` cột 0–4 (Stt, Neu, KetQua, Thi, SauBaoLau) — `DMTacNghiepImportService.cs:230-334`.
- `DMTacNghiepImportService.cs:404-413` — dữ liệu bắt đầu từ hardcode row `3` cho cả 4 sheet; sheet được tìm theo **tên cố định** (`tacnghiepcan`/`ketquatacnghiep`/`tacnghiepsaubaolau`/`tacnghiepcauhinh`) → đổi tên sheet trong file = import fail im lặng (không tìm thấy worksheet).
- `DMTacNghiepImportService.cs:425-431` — giới hạn cứng **1000 bản ghi/sheet**; vượt → lỗi, không import 1 phần.
- `DMTacNghiepImportService.cs:434-438` — nếu `GetCauHinhInit` lỗi/null thì chỉ set `idChiNhanh = null` rồi vẫn truy cập `cauHinh.Data.IsEnableTacNghiepTheoChiNhanh` ngay sau (dòng 440) → **NullReferenceException tiềm tàng** khi cấu hình không lấy được.
