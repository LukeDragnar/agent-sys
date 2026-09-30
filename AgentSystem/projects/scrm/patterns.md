# SCRM — Code Patterns & Conventions

## Backend (.NET 6)

### Cấu trúc 1 module
```
{Module}/
├── {Module}.API/
│   ├── Controllers/          ← BaseController, [SBAuthorize], route: api/[controller]
│   ├── Consumer/             ← MassTransit consumers (nếu có)
│   └── Program.cs
├── {Module}.DataContext/
│   ├── Entity/               ← EF Core entities kế thừa BaseEntity
│   ├── Generated/
│   │   ├── Configs/          ← IEntityTypeConfiguration
│   │   └── DbContexts/       ← DbContext
│   └── EfRepository.cs
└── {Module}.Service/
    └── Generated/
        ├── {Entity}Service.cs ← partial interface + partial class
        └── BaseService.cs
```

### BaseEntity (SB.Core)
```csharp
// Mọi entity đều kế thừa BaseEntity
public class CongViec : BaseEntity
{
    public Guid? IdDonVi { get; set; }    // BẮTBUỘC — multi-tenant
    public Guid? IdChiNhanh { get; set; } // BẮTBUỘC — chi nhánh
    
    // String properties — BẮT BUỘC định nghĩa column type
    [Column(TypeName = "VARCHAR(50)")]
    public string MaCongViec { get; set; } = "";  // ASCII-only: code, enum, event name
    
    [Column(TypeName = "NVARCHAR(200)")]
    public string TieuDe { get; set; } = "";      // Tiếng Việt: name, title, description
    
    [Column(TypeName = "NVARCHAR(500)")]
    public string? GhiChu { get; set; }           // Tiếng Việt: note, comment (nullable)
}
// BaseEntity có: Id (Guid), NgayTao, NgayCapNhat, IsDeleted (soft delete)
// Update dùng MarkDirty() để chỉ update các field thay đổi
entity.MarkDirty(nameof(entity.TieuDe));
```

**String Property Rule:**
- **NVARCHAR(n)** — string chứa tiếng Việt hoặc Unicode (name, title, description, note, comment): `[Column(TypeName = "NVARCHAR(n)")]`
- **VARCHAR(n)** — ASCII-only (code, enum, event name, username, status): `[Column(TypeName = "VARCHAR(n)")]`
- **BẮT BUỘC** đặt `[Column(TypeName = "...")]` — không để EF tự map thành `nvarchar(max)` mặc định
- **Mặc định string property**: `public string Field { get; set; } = "";` (không nullable, empty string default)

### IMethodResult<T> — Response pattern
```csharp
// Thành công có data
return MethodResult<T>.ResultWithData(data);
return MethodResult<List<T>>.ResultWithData(data, "", total); // có total cho paging

// Thất bại
return MethodResult<T>.ResultWithError(Contants.MESS_NotFound, Contants.CODE_NotFound);
return MethodResult<T>.ResultWithError(errorMessage);
```

### Service pattern
```csharp
public partial interface ICongViecService : IBaseService<CongViec>
{
    Task<IMethodResult<List<CongViecDto>>> TimTheoDieuKienAsync(CongViecFilterDto filter, UserLogin? userLogin);
    Task<IMethodResult<CongViecDetailDto>> TimTheoIdAsync(Guid id, UserLogin? userLogin);
    Task<IMethodResult<Guid>> LuuAsync(CongViecSaveDto dto, UserLogin? userLogin);  // create + update chung 1 method
    Task<IMethodResult<object>> XoaTheoIdAsync(Guid id, UserLogin? userLogin);
}
// UserLogin luôn là param cuối — inject từ CurrentUser ở controller
```

### Repository pattern
```csharp
_repository.TableWithNolock()   // LUÔN dùng cho mọi SELECT (read-only)
_repository.GetByIdAsync(id)    // load entity để update — đã có tracking sẵn
_repository.InsertAsync(entity)
_repository.UpdateAsync(entity) // nhớ MarkDirty() trước khi gọi
_repository.DeleteAsync(entity)
_repository.InsertAsync(list)   // bulk insert
_repository.DeleteAsync(list)   // bulk delete
```

**Rule:** query LUÔN `TableWithNolock()`. Update theo luồng `GetByIdAsync` → sửa field → `MarkDirty` → `UpdateAsync`. `.Table` (có tracking) gần như không cần dùng trực tiếp.

### NghiepVu async queue pattern (race-condition safe)

Dùng khi cần xử lý nghiệp vụ async với nhiều instance chạy song song.

```csharp
// 1. Enqueue — gọi từ API/consumer, KHÔNG xử lý trực tiếp
await TaoNghiepVuPendingAsync(donHangId, triggerEvent, trangThaiGiaoHang);

// 2. Claim batch (trong job) — atomic 2 bước
var token = Guid.NewGuid();
await _context.Database.ExecuteSqlRawAsync(@"
    UPDATE TOP(100) p
    SET p.IsProcessing=1, p.ProcessingStartedAt=GETDATE(), p.ProcessingToken={0}
    FROM dbo.DonHangLogisticNghiepVuPending p
    WHERE p.IsProcessing=0 AND p.IsDelete=0 AND p.ThoiGianXuLy<={1}", token, now);
var pendingList = await _reposPending.TableWithNolock()
    .Where(x => x.ProcessingToken == token).ToListAsync();

// 3. Xử lý từng pending
foreach (var pending in pendingList)
{
    await XuLyMotPendingAsync(pending);
    await MoveToHistoryAsync(pending); // hard delete + insert history
}

// 4. Hard delete queue record
await _reposPending.DeleteAsync(pending, removeInDatabase: true);

// 5. Stuck cleanup (chạy đầu mỗi job cycle)
// IsProcessing=1 && ProcessingStartedAt < now-5min → retry hoặc move to history lỗi (max 3 lần)
```

**Rule:** Queue/Pending table dùng `removeInDatabase: true` — không soft delete, tránh tích lũy rác.

### Transaction pattern
```csharp
var strategy = _context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    using var trans = await _context.Database.BeginTransactionAsync();
    try
    {
        // ... operations
        await trans.CommitAsync();
    }
    catch (Exception ex)
    {
        await trans.RollbackAsync();
        errorMessage = ex.Message;
    }
});
```

### Controller pattern
```csharp
[Route("api/[controller]")]
[ApiController]
public partial class CongViecController : BaseController
{
    [SBAuthorize]
    [HttpPost("TimTheoDieuKien")]
    public async Task<IActionResult> TimTheoDieuKienAsync(FilterDto filter)
    {
        var result = await _service.TimTheoDieuKienAsync(filter, CurrentUser);
        return ResponseResult(result);
    }
    // Tất cả action đều HttpPost (kể cả get list/detail)
    // Route tên tiếng Việt: TimTheoDieuKien, TimTheoId, Save, Xoa
    // CurrentUser inject từ BaseController
}
```

### Naming conventions
- Method: **tiếng Việt** — `TimTheoDieuKienAsync`, `LuuAsync`, `XoaTheoIdAsync`
- Entity field: **tiếng Việt** — `TieuDe`, `MoTa`, `TrangThai`, `NgayTao`
- DTO: `{Entity}Dto` (list), `{Entity}DetailDto` (detail), `{Entity}SaveDto` (create/update), `{Entity}FilterDto` (search)
- Enum: PascalCase tiếng Việt — `ChuaLam`, `DangLam`, `HoanThanh`, `Huy`
- 🔴 **Field thuộc nghiệp vụ SÀN TMĐT → prefix `TMDT`** (prefix ở ĐẦU tên, không phải hậu tố):
  `TMDTKhachHangYeuCau`, `TMDTSanChietKhau`, `TMDTShopChietKhau`, `TMDTIdGianHang`, `TMDTPhiNguoiMuaTra`, `TMDTDonHangTienShopNhan`…
  → Đặt kiểu `KhachHangYeuCauSan` / `...Ecom` là **lạc quy ước** (T800/B318 đã đặt sai, phải rename cả code + script trước khi MR).
  Áp cho cột DB/entity; tên method/biến nội bộ không bắt buộc.

### Multi-tenant — filter `IdDonVi` / `IdChiNhanh` (BẮT BUỘC)

Đa phần bảng có `IdDonVi` + `IdChiNhanh`. Quy tắc:

1. **Query**: nếu entity có `IdDonVi` / `IdChiNhanh` → PHẢI filter, để SQL ăn index và không rò data cross-tenant.
2. **Nguồn data ưu tiên**: lấy từ `userLogin` trước khi dùng bất cứ nguồn nào khác.
3. **Lưu ý tên property** — dễ confuse:
   - Trên `UserLogin`: `userLogin.DonViId` (Guid), `userLogin.IdChiNhanh` (Guid)
   - Trên entity DB: `x.IdDonVi`, `x.IdChiNhanh`
   - Map: `x.IdDonVi == userLogin.DonViId` (KHÔNG phải `userLogin.IdDonVi`)

**Pattern chuẩn (query):**
```csharp
var query = _repos.TableWithNolock()
    .Where(x => x.IdDonVi == userLogin.DonViId
        && x.IdChiNhanh == userLogin.IdChiNhanh);
```

**Pattern chuẩn (insert):**
```csharp
var entity = new DmMucDoUuTien
{
    IdDonVi = userLogin.DonViId,
    IdChiNhanh = userLogin.IdChiNhanh,
    // ... fields khác
};
await _repos.InsertAsync(entity);
```

**Ngoại lệ — khi dùng param thay vì userLogin:**
- Job / MassTransit consumer / scheduled task → không có `userLogin`, truyền `donViId` / `chiNhanhId` qua param.
- Admin tổng xử lý cross-đơn-vị → truyền `donViId` từ input request.
- Method internal nhận `donViId` từ caller đã xác định scope.

```csharp
// Luồng cross-đơn-vị — donViId từ tham số
.Where(x => childQuyTrinhIds.Contains(x.Id) && x.IdDonVi == donViId)
```

**Chống sót filter:**
- Nếu entity chỉ có `IdDonVi` (không có `IdChiNhanh`) → chỉ filter `IdDonVi`.
- Nếu entity "riêng của user" (log, notification, ghim) → thêm `x.IdUser == userLogin.Id`.
- KHÔNG tự thêm `x.IsDeleted == false` — đã có global query filter.

### Field lấy từ sàn — luôn đi CẶP canonical + raw

Mỗi trạng thái/phân loại có nguồn từ sàn (Shopee/TikTok/Lazada) lưu trong SCRM phải có **2 field đồng hành**:
1. **CRM canonical** — giá trị chuẩn dùng chung mọi sàn (ECOMM map về, vd int enum).
2. **Đối tác raw** — giá trị gốc partner gửi, suffix `DoiTac` — view phụ + audit + phòng partner có value chưa map.

VD: `LogisticsTrangThaiMa` + `LogisticsTrangThaiMaDoiTac`; `ThoiDiemYeuCauHoan` (canonical int) + `ThoiDiemYeuCauHoanDoiTac` (raw, vd Shopee `request_type`). ECOMM map raw→canonical **trước** khi gửi, gửi sang SCRM **cả 2**; SCRM chỉ lưu. Mất raw → không trace được / không xử lý value sàn mới.

### Form dùng-chung phải domain-agnostic — caller compute, form patch generic

Form dùng chung nhiều nguồn (HBBTL, Phiếu thu/chi, Nhập/Xuất kho) chỉ nhận DTO prefill shape **generic** (`bool?`/`string?`/`number`). Form **KHÔNG biết** domain nguồn (đơn hoàn ecomm code `REFUND_ONLY`, manual flag, retur receipt status…).

- ✅ Caller (BE service) compute flag → expose generic: `bool? IsKiemNhapKho`, `bool? IsAutoCalculate`, `string? Title`. Form patch blindly: `if (data.isKiemNhapKho != null) form.isKiemNhapKho = data.isKiemNhapKho`.
- ❌ `if (data.giaiPhapHoan === 'REFUND_ONLY') form.isKiemNhapKho = false` ← leak ecomm code vào accounting domain.

1 form dùng cho nhiều source (đơn hoàn ecomm, manual, từ đơn gốc, retur receipt) → nhồi logic 1 source = phải copy khi thêm source khác. Liên quan: ranh giới ecomm vs CRM (xem `ecommerce_connect/patterns.md`).

---

## Frontend (Angular 15 + ng-zorro)

### Service pattern
```typescript
@Injectable({ providedIn: 'root' })
export class CongViecService extends BaseService {
  constructor(public httpClient: HttpClient) {
    super(httpClient, `${environment.SERVICE_API_WORK}api/CongViec`);
  }

  timTheoDieuKien(body: any): Promise<ResponseData> {
    return this.PostRequestV2(`${this.table}/TimTheoDieuKien`, body).toPromise();
  }

  timTheoId(id: string): Promise<ResponseData> {
    return this.PostRequestV2(`${this.table}/TimTheoId/${id}`, null).toPromise();
  }

  luu(body: any): Promise<ResponseData> {
    return this.PostRequestV2(`${this.table}/Save`, body).toPromise();
  }

  xoa(id: string): Promise<ResponseData> {
    return this.PostRequestV2(`${this.table}/Xoa/${id}`, null).toPromise();
  }
}
```

### Component list pattern
```typescript
export class XxxComponent extends BaseListComponent implements OnInit {
  listData: any[] = [];

  constructor(
    private _modalService: NzModalService,
    private _notification: NzNotificationService,
    private _spinner: NgxSpinnerService,
    private _xxxService: XxxService,
    public httpClient: HttpClient
  ) { super(httpClient); }

  getData() {
    this._spinner.show();
    this._xxxService.timTheoDieuKien({ ...filter, pageInfo: { page: this.page, pageSize: this.pageSize } })
      .then((res: ResponseData) => {
        this._spinner.hide();
        if (!res?.success) { this._notification.error('Lỗi', res?.message); return; }
        this.listData = res.data;
        this.totalRecord = res.total;
      });
  }
}
```

### Response model
```typescript
export interface ResponseData {
  success: boolean;
  message: string;
  data: any;
  total: number;
}
```

### UI Components hay dùng (ng-zorro)
- List/Table: `nz-table` với `nzBordered`, `nzSize="small"`
- Form: `nz-form`, `nz-form-item`, `nz-form-control`
- Modal: `NzModalService.create()` hoặc `nz-modal`
- Notification (BẮT BUỘC theo chuẩn — service thông báo chung của hệ thống):
  - Dùng **`NzNotificationService`** inject là `_notification`, gọi **convenience method** `.success/.error/.warning/.info(title, body)`.
  - **Title = hằng `MESSAGE`** (`import { MESSAGE } from 'src/app/constants/message'`): `MESSAGE.SUCCESS` / `MESSAGE.ERROR` / `MESSAGE.WARNING`. Body = câu thông báo cụ thể (hoặc `rs?.message` từ BE).
    → `this._notification.error(MESSAGE.ERROR, rs?.message)` · `this._notification.success(MESSAGE.SUCCESS, 'Đã lưu...')`
  - ❌ **KHÔNG** tự chế helper `toast()` gọi `_notification.create(type, title, content, { nzPlacement: 'bottomRight' })` — ép `bottomRight` làm thông báo nhảy góc khác hệ thống (mặc định topRight), trông như "dùng thư viện khác". KHÔNG set `nzPlacement`.
  - ❌ **KHÔNG** hiện message thô `HttpErrorResponse.message` ("Http failure response for..."); dùng câu tiếng Việt gọn.
- Loading: **spinner CHUNG `SpinnerManagerService.show()/hide()`**, ❌ KHÔNG `<nz-spin [nzSpinning]>` bọc panel/bảng. Service **đếm ref** ⇒ `show()` phải khớp cặp `hide()` trên **mọi** nhánh, thiếu 1 `hide()` là treo overlay khoá cả trang. Chi tiết + 2 bẫy lệch cặp + mẫu helper: **[skills/loading.md](skills/loading.md)** (đọc khi đụng bất kỳ màn nào có loading).
- Select: `nz-select` với `nzShowSearch`
- DatePicker: `nz-date-picker` với format `dd/MM/yyyy`
- Button: `nz-button` với `nzType="primary|default|danger"` — **disable dùng `[disabled]` native, KHÔNG có `[nzDisabled]`** (đoán sai → compile `NG8002`). Quy tắc: element HTML native (`button`/`input`/`textarea`) → `[disabled]`; component `nz-*` (`nz-checkbox`/`nz-select`/`nz-switch`…) → `[nzDisabled]`

---

## DB — Bảng `Page` (menu & phân quyền)

Bảng `Page` trong SCRM Core DB quản lý **menu hiển thị + phân quyền** cho từng màn hình.

### Khi đổi FE route phải đồng bộ DB

Khi đổi URL lazy-loaded route (ví dụ `/warehouse-product/shopee-*` → `/ecommerce/shopee-*`):
1. Tìm record trong `Page` theo `Url LIKE '/warehouse-product/...'`
2. Chạy `UPDATE Page SET Url = '/new-path/...' WHERE MaPage = '...'`
3. Áp dụng cho **tất cả môi trường** có chạy DB riêng (staging, production)

```sql
-- Kiểm tra records bị ảnh hưởng
SELECT MaPage, Ten, Url, MaTinhNang, IsDelete
FROM [dbo].[Page]
WHERE Url LIKE '/warehouse-product/%'
  AND IsDelete = 0
ORDER BY Url
```

### Phân quyền log pages

Log pages (log delivery, log ecommerce) cần có `MaTinhNang` riêng để kiểm soát quyền truy cập (chỉ super/CSKH). Khi tạo log page mới, update `MaTinhNang` vào record `Page` tương ứng.

---

## Ecommerce OAuth — Redirect URL

### Luồng redirect khi kết nối Shopee/TikTok

1. FE gửi `callBackUrlAuthorization = window.location.origin + window.location.pathname` lên BE
2. BE build OAuth URL, truyền callback URL qua `state` parameter
3. Sau khi OAuth xong, provider redirect về BE endpoint
4. BE redirect về `callBackUrlAuthorization` (FE URL) kèm query params `isSuccess`, `message`

**`callBackUrlAuthorization` là dynamic** — tự động đúng khi FE URL thay đổi, không cần sửa BE.

### TikTok — `AUTHORIZATION_URL_REDIRECT`

Khác với `callBackUrlAuthorization`, `AUTHORIZATION_URL_REDIRECT` là URI đăng ký với TikTok OAuth app, lưu trong bảng `ConfigApp` (DB TikTok) với key `AUTHORIZATION_URL_REDIRECT`. Đây là **BE endpoint**, không phải FE URL — không cần đổi khi đổi FE route.

---

## Git

### Naming convention

- **Branch:** `{author}/{ten-chuc-nang}` — ví dụ: `thiennq/san-pham-ten-viet-tat`
- **Commit message:** `{branch-name}: {mô tả ngắn}` — ví dụ: `thiennq/san-pham-ten-viet-tat: thêm tên viết tắt sản phẩm, áp dụng khi in phiếu và đăng đơn giao hàng`
- BE (`scrm`) và FE (`scrm_frontend`) dùng **cùng tên branch** cho cùng một task
- Chỉ stage đúng file của task — không commit file không liên quan (`.vscode/`, `angular.json`, `package.json`, v.v.)

### Quy trình đưa feature vào `release_staging`

Dùng **cherry-pick** (không merge) — chỉ lấy đúng commit của nhánh hiện tại:

1. `git checkout release_staging`
2. `git pull origin release_staging` ← **bắt buộc trước khi cherry-pick**
3. `git cherry-pick <commit-hash>`
4. Resolve conflict nếu có
5. `git pull origin release_staging` ← pull lần 2 nếu có người push trong lúc đó
6. `git push origin release_staging`

> Nếu working tree dirty (có file chưa commit): `git stash push <file>` trước bước 1, `git stash pop` sau khi xong.
