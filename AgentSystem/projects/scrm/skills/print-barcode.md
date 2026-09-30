# Recipe — In phiếu / tem / barcode / QR

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
In phiếu vận đơn (barcode mã vận đơn), tem nhãn (IMEI/LoDate), hóa đơn A4/A5, QR thanh toán (VietQR), in nhiệt TSPL.

## Thư viện
- **FE:** `ngx-barcode` (CODE128), `qrcode`/`ng-qrcode` (QR), `print-js` (in HTML). In nhiệt: QZ Tray + TSPL.
- **BE:** `Net.Codecrete.QrCodeGenerator` (`QrCodeBitmapExtensions.ToPng/ToBitmap`) — sinh QR khi cần lưu ảnh.

## Sinh barcode/QR (FE)
```html
<ngx-barcode class="barcode" [bc-value]="donHang?.maDonGiaoVan ?? 'null'"
  [bc-display-value]="false" [bc-height]="40" [bc-width]="coChuMaVach ?? 1"
  [bc-font-size]="17" [bc-format]="'CODE128'" [bc-element-type]="'canvas'"
  [bc-line-color]="'#000000'" [bc-font]="'monospace'"></ngx-barcode>
```
VietQR (QR thanh toán): class `VietQR` trong `src/app/shared/qrcode/qr-pay.ts`.

## Template in + trigger print
```typescript
import es6printJS from 'print-js';
onClickInDon() {
  es6printJS({
    printable: 'pdfTable', type: 'html', targetStyles: ['*'], scanStyles: false,
    css: ['assets/print/font/font.css', 'assets/print/in-don-logistic.css'],
  });
  this._donHangLogisticService.InDon(this.ids);     // ghi log đã in
}
// In nhiệt: await this.qz.printRawTSPL(this.printerName, tspl);
```

## Bẫy thường gặp
- **CSS `@media print`** load qua `css:[...]` của printJS (đừng inline), dùng `scanStyles:false` tránh import CSS DOM lỗi; ngắt trang bằng `.page-break { page-break-before: always; }`.
- **Font tiếng Việt:** embed `@font-face` trong `assets/print/font/font.css`.
- Barcode CODE128 cần font `monospace`, `[bc-element-type]="'canvas'"` render mượt.
- Khổ tem (10x15cm) khác A4 — set margin/padding 0, `[bc-width]` chỉnh độ dày.
- Không dùng `window.print()` thuần — dùng `print-js` để kiểm soát CSS.

## File source mẫu
- FE barcode: `pages/in-don/mau1/phieu-gui-hang-m1/phieu-gui-hang-m1.component.html:24`
- FE print: `pages/in-don/in-don-logistic/in-don-logistic.component.ts:562` · CSS `assets/print/in-don-logistic.css`
- BE QR: `Libs/SB.Core/Helpers/QrCodeBitmapExtensions.cs` · `PreOrder/.../BangQRCodeService.cs`
- VietQR: `src/app/shared/qrcode/qr-pay.ts`
