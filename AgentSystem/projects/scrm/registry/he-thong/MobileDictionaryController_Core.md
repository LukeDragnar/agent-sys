---
controller: MobileDictionaryController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý Dictionary — khu vực backup/template/import: nút Download backup, Download file mẫu, Import từ Excel (route api/QuanTriArea/MobileDictionary)"
muc_dich: "Biến thể QUẢN TRỊ của MobileDictionary: backup ra Excel, tải file template, import từ điển từ Excel. Khác hẳn bản Controllers (chỉ CRUD/đọc) — bản này chuyên thao tác file Excel và CÓ gating quyền đầy đủ (HT-AD, HT-MB)."
alias: ["import dictionary", "backup dictionary", "template dictionary", "quản trị dictionary", "MobileDictionary QuanTriArea"]
auth: public
state_machine: []
deprecated: false
entities: [MobileDictionary]
stored_procs: []
depends: ["IMobileDictionaryService (DownloadDataBackUp/DownloadFileTemplate/ImportDictionary)", "ExcelService + GemBox.Spreadsheet"]
cross_module_in: ["Trang quản trị Dictionary gọi backup/import qua route QuanTriArea"]
cross_module_out: ["Import ghi đè/bổ sung MobileDictionary → ảnh hưởng từ điển app mobile đọc"]
---

# MobileDictionaryController (QuanTriArea) — Backup / Template / Import từ điển mobile

Biến thể **SanboxArea** (route `api/QuanTriArea/MobileDictionary`), TienIch `MobileDictionaryController` nhóm `TI_HeThong` (tham số thứ 4 = `true`). Khác bản Controllers ở chỗ: **chỉ 3 endpoint thao tác Excel** (backup, template, import) và **mỗi endpoint đều có `[SBAuthorize]` với key + role HT-AD/HT-MB** (gating đầy đủ, không bị comment như bản kia). Import đọc Excel theo cột cố định (Ma, Value, MoTa, IsNotImport).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tải backup | Download backup | GET `api/QuanTriArea/MobileDictionary/DownloadDictionaryBackup` | `MobileDictionaryController_DownloadDictionaryBackup` (HT-AD, HT-MB) | public | — | `DownloadDataBackUp()` → file `MobileDictionary_Backup.xlsx`. | EF → Excel | Dùng `.Result` (block sync) trên stream |
| 2 | Tải file mẫu | Download template | GET `api/QuanTriArea/MobileDictionary/DownloadFileTemplate` | `MobileDictionaryController_DownloadFileTemplate` (HT-AD, HT-MB) | public | — | `DownloadFileTemplate()` → trả file Excel. | Excel | Tên file trả về cũng là `MobileDictionary_Backup.xlsx` (lệch: file mẫu nhưng tên backup) |
| 3 | Import từ Excel | Import | POST `api/QuanTriArea/MobileDictionary/ImportDictionary` | `MobileDictionaryController_ImportDictionary` (HT-AD, HT-MB) | public | File != null & có dòng | Đọc Excel theo 4 cột (Ma, Value, MoTa, IsNotImport) → `ImportDictionary(datas)`. File null → "File không hợp lệ"; 0 dòng → "Không có bản ghi nào được import". | Excel → EF | Cột import (Ma/Value/MoTa/IsNotImport) thiếu DefaultValue so với entity |

## Quan hệ
- **Gọi sang:** `IMobileDictionaryService.DownloadDataBackUp / DownloadFileTemplate / ImportDictionary`, `ExcelService` (GemBox).
- **Dữ liệu (entities):** `MobileDictionary`.
- **Ripple / đồng bộ:** Import ghi dữ liệu từ điển → app mobile đọc qua bản `MobileDictionaryController` (Controllers); thay đổi lan tới label/cấu hình app.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas/SanboxArea/MobileDictionaryController.cs:30,39` — `excelStream.Result` (block `.Result` trên Task) trong method `async` → nguy cơ deadlock/lãng phí, nên `await`.
- `Areas/SanboxArea/MobileDictionaryController.cs:39` `DownloadFileTemplate` trả file đặt tên `MobileDictionary_Backup.xlsx` (giống endpoint backup) → tên file mẫu sai, dễ nhầm.
- `Areas/SanboxArea/MobileDictionaryController.cs:51-78` cột import cố định theo index (Ma=0, Value=1, MoTa=2, IsNotImport=3), không có `DefaultValue` → import không set được DefaultValue.
- Khác bản Controllers: bản này **có gating quyền đầy đủ** (HT-AD/HT-MB), trong khi bản Controllers đã comment hết `[SBAuthorize]`.
