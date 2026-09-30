# Recipe — Xuất Excel

> **Trạng thái: ✅ Đã verify từ source SCRM thật** (quét 2026-06-19).

## Khi nào dùng
Màn hình có nút "Xuất Excel" → tải file `.xlsx`.

## Thư viện
- **BE:** `ClosedXML` v0.102.3 (build động) + `GemBox.Spreadsheet` (khi cần template). Ví dụ `OrderLogistic/OrderLogistic.Service/OrderLogistic.Service.csproj`.
- **FE:** `file-saver` v2.0.5 (`saveAs`).

## BE — sinh file, trả `File(...)`

**Controller** trả `FileContentResult`:
```csharp
[HttpPost("ExportExcelDonHangSaleKieu1")]
public async Task<IActionResult> ExportExcelDonHangSaleKieu1(DonHangLogisticFilterDto gridInfo)
{
    var result = await _service.ExportExcelDonHangSaleKieu1_V2(gridInfo, CurrentUser);
    if (!result.Success)
        return StatusCode(result.Message == "Service_Unavailable" ? 503 : 500,
            new { Status = "Error", Message = result?.Message, Code = "EXPORT_ERROR" });
    var excelStream = result.Data;                     // MemoryStream
    return File(excelStream.ToArray(), SaveOptions.XlsxDefault.ContentType, "DonHangSale.xlsx");
}
```
- Mẫu: `OrderLogistic/OrderLogistic.API/Controllers/DonHangLogisticController.cs:506`

**Service** dùng ClosedXML, trả `IMethodResult<MemoryStream>`:
```csharp
var wb = new XLWorkbook();
var ws = wb.AddWorksheet("SanPhamTongHop");
// header: cell.Style.Font.Bold = true; cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
// số: var (v, fmt) = ExcelHelper.Money(sp.DoanhSo); cell.Value = v; cell.Style.NumberFormat = fmt;
var stream = new MemoryStream();
wb.SaveAs(stream); stream.Position = 0;
return MethodResult<MemoryStream>.ResultWithData(stream);
```
- Format số chuẩn: `Libs/SB.Core/ExcelHelper.cs` (`ExcelHelper.Money/Format` → `#,##0.##`, tự loại trailing zero)
- Template: `Path.Combine(Directory.GetCurrentDirectory(), "Template/TemplateExcelSB_V3.xlsx")`
- Mẫu: `OrderLogistic/OrderLogistic.Service/Generated/DonHang/DonHangExportService.cs:675`

## FE — gọi blob + `saveAs`

**Service** dùng base method `postDownloadWithExcel` (`responseType: 'blob'`):
```typescript
ExportExcelDonHangSaleKieu1(body) {
  const url = `${environment.SERVICE_API_OrderLogistic}api/DonHangLogistic/ExportExcelDonHangSaleKieu1`;
  return this.postDownloadWithExcel(url, body);   // src/app/services/base.service.ts:116
}
```
> `postDownloadWithExcel` POST + `responseType: 'blob'`, **bắt lỗi blob** → parse JSON `error.error.text()` để lấy `Message` (vì body lỗi cũng là blob).

**Component** trigger download:
```typescript
import { saveAs } from 'file-saver';
ExportExcelKieu1() {
  const filter = { ...this.searchModelCore, ...this.getGridOption() };
  this.spinner.show();
  this._service.ExportExcelDonHangSaleKieu1(filter).subscribe(
    (res: Blob) => { this.spinner.hide();
      saveAs(res, `DonHangSaleKieu1_${moment(this.searchModelCore.tuNgay).format('DD_MM_YYYY')}.xlsx`); },
    (err) => { this.spinner.hide();
      if (err.status === 503) this.msg.warning('Bạn chưa được xuất excel lúc này. Thử lại sau'); });
}
```
- Mẫu: `src/app/pages/sale/don-hang-sale/don-hang-sale.component.ts:1317`

## Bẫy thường gặp
- **Lỗi BE trả về blob** — FE phải parse `error.error.text()` mới đọc được message (đã có sẵn trong `postDownloadWithExcel`).
- **Phần trăm:** giá trị `0.105` muốn hiện `10.5%` → set `NumberFormat = "0.00%"` (Excel tự ×100); nếu data đã là `10.5` thì format `"0.00\%"`/×100 khác nhau — kiểm tra nguồn.
- **Quá tải:** service check quyền + trả `"Service_Unavailable"` (503) khi không cho export lúc cao điểm — FE bắt 503 báo riêng.
- **Dữ liệu cực lớn** (>~1M dòng): `MemoryStream` có thể nặng RAM, cân nhắc `FileStreamResult`.
- Tiếng Việt UTF-8: ClosedXML + file-saver xử lý native, an toàn.

## File source mẫu
- BE controller: `OrderLogistic/OrderLogistic.API/Controllers/DonHangLogisticController.cs:506`
- BE service: `OrderLogistic/OrderLogistic.Service/Generated/DonHang/DonHangExportService.cs:675`
- FE base: `src/app/services/base.service.ts:116` · component: `pages/sale/don-hang-sale/don-hang-sale.component.ts:1317`
