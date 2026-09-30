---
controller: FilesController
module: he-thong
service: File
man_hinh_ui: "nội bộ — hạ tầng upload/serve file dùng chung toàn hệ thống (gọi từ nhiều màn hình qua URL ảnh/tài liệu)"
muc_dich: "Upload/serve/download/xóa file & ảnh dùng chung: lưu Original lúc upload, lazy-resize Small/Medium/Large on-demand, lưu metadata vào bảng Files."
alias: ["upload file", "upload ảnh", "GetFile", "Download", "tải ảnh", "image server", "UploadFile", "suploadbase64"]
auth: mixed
state_machine: []
deprecated: false
entities: [Files]
stored_procs: []
depends: ["IFilesService", "ImageProcess (SixLabors.ImageSharp)", "System.Drawing.Image", "ImageSettings (Quality/Comporessed)", "HttpClient (UploadFilesFromLinks)"]
cross_module_in: ["Mọi module FE/BE nhúng ảnh-tài liệu gọi GET /Files/GetFile?id=... & /Download", "Service khác gọi [Private] DeleteFilesByIds để cleanup file theo Id"]
cross_module_out: ["Lưu vật lý wwwroot/Images (Original + biến thể resize) & wwwroot/Files (document); ghi bảng Files"]
---

# FilesController — Hạ tầng upload & phục vụ file/ảnh dùng chung

Controller hạ tầng lưu trữ file của toàn hệ thống. Lúc upload chỉ sinh ảnh `Original` (giới hạn 1920px); các size `Small/Medium/Large` được lazy-generate khi `GetFile`/`Download` lần đầu rồi cache trên đĩa (`EnsureResizedExistsAsync`). Metadata lưu bảng `Files` (Id, Type, FileName, Extention, Path, Size). Whitelist phần mở rộng cố định trong `ImageExtensions` (ảnh + pdf/office/zip/rar/csv/txt).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Upload file (multipart) | (input file) | POST `/UploadFile` | — (chỉ `[SBAuthorize()]`, không key) | internal | — | Validate extension; thử mở ảnh → là ảnh: lưu `Original` resize, Type=IMAGE; không phải ảnh: ghi nguyên vào wwwroot/Files, Type=DOCUMENT. Insert `Files`, trả record. | EF Insert Files | Dùng `System.Drawing` (Windows-only / GDI+); `file.FileName=="blob"` → ép .png |
| 2 | Upload base64 (nhiều file) | (paste/clipboard) | POST `/suploadbase64` | — | internal | — | Decode base64, xử lý EXIF orientation (prop 274 → xoay 90/270), lưu Original, Type=1(image), trả list `FileResponse`. | EF Insert Files | Type hardcode `1`; `CreatedByUser=""` (bỏ UserInfo) |
| 3 | Lấy metadata theo Ids | (load preview) | POST `/GetsByIds` | — (không attr) | anonymous | Ids rỗng GUID → trả ảnh `image-not-found.png` | Tách Ids theo `;`/`,`, lọc GUID hợp lệ, trả list `Files`. | EF GetsByIds | Không auth → ai cũng query metadata file |
| 4 | Xem file / ảnh theo size | `<img src=.../GetFile?id=&size=>` | GET `/GetFile?id=&size=m` | — (không attr) | anonymous | id=empty GUID → trả ảnh mặc định | Map size (a/m/l/o/s → Avatar/Medium/Large/Original/Small), ảnh: lazy-resize từ Original; document: trả file gốc. Stream với `Cache-Control: max-age=31536000`. | EF GetFileById + đĩa | Không auth → URL đoán Id là tải được; Content-Disposition=attachment cả khi xem |
| 5 | Download file/ảnh | (nút tải) | GET `/Download?id=&size=m` | — (không attr) | anonymous | id=empty GUID → ảnh mặc định | Như GetFile nhưng header `no-store/no-cache`. | EF GetFileById + đĩa | Không auth (xem cột Known-issue chung) |
| 6 | Upload file từ link URL | (nhập link) | POST `/UploadFilesFromLinks` | — (không attr) | anonymous | Links ngăn `;` | Tải từng link qua HttpClient, validate mime→ext, lưu (ảnh→Original / khác→file), trả Id file đầu tiên. | EF Insert Files | Không auth → SSRF risk (server tải URL tùy ý) |
| 7 | Upload không cần đăng nhập | (form công khai) | POST `/NoAuThen-UploadFile` | — (không attr) | anonymous | — | Giống UploadFile nhưng mở công khai (route đặt tên NoAuThen). | EF Insert Files | Chủ ý anonymous → cần rate-limit/whitelist nếu lộ ra ngoài |
| 8 | Upload tài liệu mọi loại | (input file) | POST `/UploadDocuments` | — (`[SBAuthorize()]`) | internal | file null/0 byte → lỗi | Validate extension, ghi thẳng wwwroot/Files, Type=30, insert Files. | EF Insert Files | Type=30 (document) |
| 9 | Download tài liệu theo Id | (nút tải) | GET `/DownloadFile/{id}` | — (`[SBAuthorize()]`) | internal | `[Consumes(multipart/form-data)]` | Đọc file từ wwwroot/Files, trả `File(stream,...)`. | EF GetByIdAsync | `[Consumes(multipart/form-data)]` trên GET — bất thường |
| 10 | Xóa file của mình | (nút xóa) | POST `/Xoa/{id}` | — (`[SBAuthorize()]`) | internal | Chỉ xóa nếu là SuperUser hoặc chính người tạo | Kiểm chủ sở hữu rồi `DeleteByIdsAsync([id])`. | EF Delete | Người không phải chủ → trả "File không tồn tại" (che giấu) |

## Endpoint nội bộ (private / service-to-service)
- [internal] `[PrivateSBAuthorize]` POST `/DeleteFilesByIds` — service khác gửi list Id để **hard-delete** hàng loạt: xóa record DB + file vật lý (mọi biến thể ảnh / document) + entry lịch sử thùng rác khớp. Dùng khi cleanup file theo nghiệp vụ (xóa đơn/sản phẩm...).

## Quan hệ
- **Gọi sang:** `IFilesService` (Insert/GetById/GetsByIds/DeleteByIds), `ImageProcess.Save2/Save2Atomic` (resize), `System.Drawing.Image` (đọc ảnh + EXIF).
- **Dữ liệu (entities):** `Files` (Id, Type, FileName, Extention, Path, Size, IdFolderUnit, NguoiTaoUserId).
- **Ripple / đồng bộ:** ghi đĩa `wwwroot/Images/{Original|Small|Medium|Large}/...` (lazy) và `wwwroot/Files/...` (document); là nguồn URL ảnh cho toàn bộ FE.

## Lỗi đã biết / lưu ý kỹ thuật
- `FilesController.cs:275-390,393-467,469-596,613` — `GetsByIds`, `GetFile`, `Download`, `UploadFilesFromLinks`, `NoAuThen-UploadFile` **không có `[SBAuthorize]`** → anonymous: đoán Id GUID là xem/tải file được, không kiểm quyền sở hữu.
- `FilesController.cs:469-596` `UploadFilesFromLinks` tải URL do client truyền → **SSRF**; nên whitelist host.
- `FilesController.cs:81,197,642` dùng `System.Drawing.Image` (GDI+) → **Windows-only**, không an toàn trên Linux container; xử lý ảnh chính đã chuyển sang SixLabors qua `ImageProcess` nhưng đọc/orientation vẫn dùng System.Drawing.
- `FilesController.cs:938-939` `DownloadFile` là `[HttpGet]` nhưng `[Consumes("multipart/form-data")]` — mâu thuẫn, GET không có body.
- `FilesController.cs:35,162,863` action `[SBAuthorize()]` **không truyền key** → chỉ check đăng nhập, không phân quyền chi tiết.
- Lazy-resize: `EnsureResizedExistsAsync` (cs:821) ghi atomic `.tmp→Move`, touch LastWriteTime để gia hạn TTL (có cleanup job ngoài controller xóa biến thể cũ).
