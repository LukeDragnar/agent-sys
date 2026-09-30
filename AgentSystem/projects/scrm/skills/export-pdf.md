# Recipe — Export PDF

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Xuất PDF dashboard/báo cáo (client-side), tải PDF hóa đơn điện tử (BE proxy MISA/Viettel), in phiếu (xem [print-barcode.md](print-barcode.md)).

## Thư viện
- **FE:** `html-to-image` (`toPng`, DOM→PNG) + `jsPDF` (PNG→PDF), `print-js` (in HTML).
- **BE:** không tự render PDF — proxy file PDF từ MISA (MongoDB) / Viettel.

## FE — render DOM → PDF (dashboard)
```typescript
import { toPng } from 'html-to-image';
import jsPDF from 'jspdf';
async downloadAllPdf() {
  const dataUrl = await toPng(this.widgetGrid.nativeElement, { pixelRatio: 2, backgroundColor: '#f5f5f5' });
  const img = new Image(); img.src = dataUrl; await new Promise(r => img.onload = r);
  const w = img.width / 2, h = img.height / 2;
  const pdf = new jsPDF({ orientation: 'landscape', unit: 'px', format: [w, h] });
  pdf.addImage(dataUrl, 'PNG', 0, 0, w, h);
  pdf.save(`${this.activePage?.name || 'dashboard'}.pdf`);
}
```

## BE — proxy PDF hóa đơn → tải về
```csharp
[SBAuthorize, HttpGet("GetInvoiceFile")]
public async Task<IActionResult> GetInvoiceFile(Guid idDonHang, string no, int type)
{
    var rs = await _service.GetFileHoaDonDienTu_ToClient(idDonHang, no, type, CurrentUser);
    if (rs.Success) return File(rs.Data, "application/pdf", no);   // byte[] PDF
    return ResponseResult(rs);
}
```
FE gọi `GET /api/invoice/GetInvoiceFile?...` → browser tự tải.

## Bẫy thường gặp
- **jsPDF không parse CSS phức tạp** (grid/flex) → convert DOM→PNG trước rồi `addImage` (cách dashboard làm).
- Font tiếng Việt: `pixelRatio` 2-3 khi `toPng`; in cần embed font.
- PDF lớn: giảm `pixelRatio` / split pages.
- MISA/Viettel API có thể timeout → cache file sau khi tải, tăng timeout HTTP.

## File source mẫu
- FE dashboard: `pages/Report/sandbox-action-dashboard/tab-dashboard-pro/tab-dashboard-pro.component.ts:1601`
- BE hóa đơn: `Invoice/Invoice.API/Controllers/InvoiceController.cs:120` · `HoaDonDienTuService.cs:265`
