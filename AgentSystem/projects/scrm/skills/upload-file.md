# Recipe — Upload / Import file

> **Trạng thái: ✅ Đã verify từ source SCRM thật** (quét 2026-06-19).

## Khi nào dùng
Nhận file từ user: (A) **upload file/ảnh đính kèm**, (B) **import Excel** (đơn hàng, phiếu mua hàng...).

## A. Upload file/ảnh

**BE** — `IFormFile`, validate phần mở rộng, lưu disk + record DB:
```csharp
[SBAuthorize, HttpPost("UploadFile"), DisableRequestSizeLimit]
public async Task<IActionResult> UploadFile(IFormFile file)
{
    if (!ImageExtensions.Contains(extension.ToUpperInvariant()))      // whitelist đuôi file
        return ResponseResult(MethodResult<object>.ResultWithError("File không được phép upload"));
    await file.CopyToAsync(memoryStream);                            // lưu wwwroot/Files (+ resize ảnh)
    // ... insert entity Files
}
```
- Whitelist: `.JPG .JPEG .PNG .PDF .RAR .ZIP .DOC .DOCX .XLS .XLSX .CSV ...` (ảnh resize Small/Medium/Large **on-demand**, Original lưu lúc upload)
- Mẫu: `File/File.API/Controllers/FilesController.cs:39`

**FE** — `nz-upload` dùng `nzAction` (tự POST FormData):
```html
<nz-upload [nzAction]="urlApi" [nzBeforeUpload]="beforeUpload"
           (nzChange)="handleChange($event)" [(nzFileList)]="fileList"
           nzListType="picture-card" [nzMultiple]="isMultiple"></nz-upload>
```
```typescript
this.urlApi = `${environment.SERVICE_API_FILE}Files/UploadFile`;
beforeUpload = (file: NzUploadFile): boolean => {
  if (file.size > 1024*1024*10) return false;                 // giới hạn 10MB
  if (!this.filterImageType.includes(file.type)) return false;
  return true;
};
handleChange(info: NzUploadChangeParam) {
  if (info.file.status === 'done') { this.fileIds.push(info.file.response.data.id);
    this.onChange(this.fileIds.join(';')); }
}
```
- Mẫu: `src/app/components/file-upload/file-upload.component.ts`

## B. Import Excel

**BE** — `[FromForm]`, parse bằng `ExcelService` (GemBox), map cột → DTO, lưu **bảng TMP**:
```csharp
[Consumes("multipart/form-data"), HttpPost("ImportFileDonHang")]
public async Task<IActionResult> ImportFileDonHang([FromForm] ReqImportDonHang reqData)
    => ResponseResult(await _service.ImportFileDonHang(reqData, CurrentUser));

// trong service:
var excel = new ExcelService(file.OpenReadStream());
var cols  = ReadExcelCol();                          // định nghĩa ánh xạ cột
var rows  = excel.GetData<DonHangImportDTO>(cols, 4);  // data bắt đầu dòng 4
```
- `ExcelService` (GemBox, license key trong code): `Libs/SB.Core/Excel/ExcelService.cs`
- Map cột tự động qua Reflection theo `CellValueType` (String/DateTime/...)
- **Ngày từ Excel** thường là serial (vd 46141) → `DateTime.FromOADate(serial)`. Helper xử lý 3 dạng (chuỗi dd/MM/yyyy, serial, US locale): `Accounting/.../Common/ImportExcelHelper.cs:19`
- Giới hạn: max **5000 dòng/lần** (config DB `COUNT_MAX_DATA_IMPORT_EXCEL_ORDER`)
- Mẫu: `Order/Order.API/Controllers/DonHangImportController.cs:65` · `Order/.../DonHangImport/DonHangImportService.cs:354`

**FE** — upload Excel → rồi **batch loop** gọi BE insert dần từ TMP:
```typescript
this.apiUpload = `${environment.SERVICE_API_ACCOUNTING}api/PhieuMuaHang/ImportTemplatePhieuMuaHang`;
handleChange(info: NzUploadChangeParam) {
  if (info.file.status === 'done' && info.file.response?.success) {
    this.msg.success('Đã nạp file'); this.gridLoadData();   // dữ liệu vào TMP, hiển thị review
  }
}
private async runBatchLoop() {                                // chốt dữ liệu theo lô
  while (loopCount < this.MAX_LOOP) {
    const res = await this._service.InsertTmp...(this.BATCH_SIZE);  // BATCH_SIZE ~50
    if (res.data.daHet) break;                               // BE báo hết → dừng
  }
}
```
- Mẫu: `src/app/pages/major/phieu-mua-hang/import-phieu-mua-hang/import-phieu-mua-hang.component.ts:160`

## Bẫy thường gặp
- **Ngày Excel = serial number**, không phải chuỗi → phải `FromOADate`.
- **Giới hạn dòng** (5000) + **size** (10MB ảnh) — validate cả FE (`beforeUpload`) lẫn BE.
- **Import 2 pha:** parse → TMP table → FE **batch loop** chốt vào bảng thật (tránh timeout 1 request lớn). Đừng insert thẳng toàn bộ trong 1 call.
- `nzAction` tự gửi FormData — không cần tự build `FormData` thủ công cho luồng chuẩn.

## File source mẫu
- Upload: `File/File.API/Controllers/FilesController.cs:39` · `src/app/components/file-upload/file-upload.component.ts`
- Import Excel: `Order/Order.API/Controllers/DonHangImportController.cs:65` · `Libs/SB.Core/Excel/ExcelService.cs` · FE `pages/major/phieu-mua-hang/import-phieu-mua-hang/`
