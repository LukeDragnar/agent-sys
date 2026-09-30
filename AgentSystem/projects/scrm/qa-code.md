# SCRM — QA Code Checklist

Dùng khi review code mới (PR, session AI, v.v.).
Gọi bằng: "QA code đi" → Claude đọc file này và kiểm tra toàn bộ code được thêm/sửa trong session.

---

## 1. Repository / LINQ

- [ ] **LUÔN dùng `TableWithNolock()` cho MỌI query** — không có ngoại lệ, kể cả khi sẽ update (update đi qua `GetByIdAsync` + `MarkDirty` + `UpdateAsync`, không query bằng `.Table`). Chú ý chính tả: `Nolock` chữ `l` thường
- [ ] **KHÔNG** thêm `x.IsDelete == false` vào Where — đã có global query filter trong `OnModelCreating`
- [ ] Mọi query phải có điều kiện lọc rõ ràng (không fetch all rồi filter in-memory)
- [ ] **Multi-tenant filter** — nếu entity có `IdDonVi` / `IdChiNhanh` → query PHẢI có `.Where(x => x.IdDonVi == ... && x.IdChiNhanh == ...)` để ăn index + tránh rò data cross-tenant. **Áp cả query tay/debug** (sqlcmd/SSMS/Dapper raw) — không scan toàn bảng → lag prod (IdDonVi staging hay dùng: `CB720173-CC95-4EBD-96B0-FAF550D8531E`)
- [ ] Nguồn data filter ưu tiên từ `userLogin` trước — `userLogin.DonViId` (prop là `DonViId`, không phải `IdDonVi`) và `userLogin.IdChiNhanh`
- [ ] Mapping đúng: `x.IdDonVi == userLogin.DonViId` (entity dùng `IdDonVi`, UserLogin dùng `DonViId`)
- [ ] Chỉ dùng param `donViId` / `chiNhanhId` thay userLogin khi: job/consumer (không có userLogin), admin cross-đơn-vị, hoặc method internal đã xác định scope — ghi rõ lý do tại call-site
- [ ] Khi **INSERT** entity có `IdDonVi` / `IdChiNhanh` → PHẢI set giá trị từ userLogin (hoặc param), không để null nếu không có lý do
- [ ] Dùng `InsertAsync(entity)` — **không** dùng `AddAsync`, **không** dùng `InsertAsync(entity, userLogin)` (overload đó `throw NotImplementedException`)
- [ ] Dùng `UpdateAsync(entity)` — **không** dùng `UpdateAsync(entity, userLogin)` (tương tự)
- [ ] Dùng `DeleteAsync(entity)` — không xóa cứng trừ khi có yêu cầu
- [ ] **Queue/Pending table**: dùng `DeleteAsync(entity, removeInDatabase: true)` — bảng queue không dùng soft delete, IsDelete=1 tích lũy làm nặng bảng
- [ ] **BẮT BUỘC trước `UpdateAsync`**: gọi `entity.MarkDirty(nameof(entity.Field))` cho **MỌI** field đã thay đổi. Thiếu MarkDirty → field không được update dù đã gán giá trị mới. Đây là lỗi phổ biến nhất, đặc biệt khi thêm logic update entity lấy từ DB (ví dụ `existingNo.SoTien = newValue` → **phải** `existingNo.MarkDirty(nameof(existingNo.SoTien))` trước `UpdateAsync`)
- [ ] **Đếm bản ghi bảng dự kiến LỚN** (log, webhook, history, queue persistence…) — KHÔNG dùng `COUNT(*)` / `COUNT(1)` kể cả `WITH (NOLOCK)` (vài giây đến vài chục giây). Thay bằng `SELECT @count = SUM(p.rows) FROM sys.partitions p WHERE p.object_id = OBJECT_ID('dbo.TableName') AND p.index_id IN (0, 1)` — vài ms. Áp dụng cho count toàn bảng (không filter); cần count có WHERE thì vẫn dùng COUNT bình thường. Chi tiết: [memory/feedback_sql_count_large_table.md](../../memory/feedback_sql_count_large_table.md)
- [ ] 🔴 **SQL/LINQ đụng bảng LỚN → phải "ăn" được index** (sự cố 14/07/2026: script Claude đưa cho dev chạy trên product → **disk read spike 6000**, dev bị claim):
  - [ ] Lọc bằng **cột CÓ index** (`Id`/`IdDonVi`/`IdDonHang`/`NgayTao`/`EcommerceId`), KHÔNG lấy cột nghiệp vụ phụ (vd `TMDTSanChietKhau > 0`) làm điều kiện lọc chính
  - [ ] Cột index để **TRẦN** trong `WHERE`/`JOIN ON` — **cấm bọc hàm**: `ISNULL(col,0) > 0` ❌ · `CONVERT(date, NgayTao) = ...` ❌ → index chết, full scan
  - [ ] Lọc **bảng CHA trước** (ít dòng) rồi mới join xuống **bảng chi tiết** — đừng bắt đầu từ bảng chi tiết to nhất
  - [ ] Script đưa cho user chạy **trên PRODUCT**: kèm `OPTION (MAXDOP 1)` + **chia nhỏ khoảng ngày** + cảnh báo **giờ cao điểm**. Chi tiết: [rules/db.md](../../rules/db.md)

---

## 2. Service / Interface

- [ ] Class phải khai báo implement interface: `public class FooService : IFooService`
- [ ] Return type dùng `IMethodResult<T>` / `MethodResult<T>.ResultWithData(...)` / `MethodResult<T>.ResultWithError(...)`
- [ ] Method naming: nhất quán với pattern trong cùng service (xem method lân cận — một số service không dùng Async suffix cho method phức tạp, e.g. `GetCauHinhInit` thay vì `GetCauHinhInitAsync`)
- [ ] Wrap logic trong try-catch nếu các method cùng service đều có (nhất quán)
- [ ] Không inject `IRepository<T>` trực tiếp vào service nếu chỉ cần cache — dùng `ICommonCacheService`
- [ ] **Field lấy từ sàn** (Shopee/TikTok/Lazada): mỗi trạng thái/phân loại phải đi **cặp** canonical + raw (suffix `DoiTac`) — thiếu raw là sai convention (xem patterns.md)
- [ ] **Form/DTO prefill dùng-chung** (HBBTL, Phiếu thu/chi, Nhập/Xuất): DTO chỉ chứa flag **generic** (`bool?`/`string?`), form patch blindly — KHÔNG để form đọc enum/code source (vd `REFUND_ONLY`); caller compute flag

---

## 3. Cache

- [ ] Key cache theo pattern: `OrderLogistic_{Name}:{idDonVi}:{qualifier}` (xem `CommonKeyCache.cs`)
- [ ] Get: Redis → miss → DB (`ICommonRetrieveDataForCacheService`) → set cache
- [ ] Sau khi save: gọi `ClearCache_*` để invalidate
- [ ] Thêm method vào cả interface và implementation của `ICommonCacheService`
- [ ] Thêm method retrieve vào cả interface và implementation của `ICommonRetrieveDataForCacheService`

---

## 4. Controller

- [ ] Kế thừa `BaseController`
- [ ] Có `[SBAuthorize]` hoặc `[PrivateSBAuthorize]` trên từng action
- [ ] Route: `[Route("api/[controller]")]` + `[HttpGet/Post(...)]`
- [ ] Trả về `ResponseResult(rs)` — không trả trực tiếp object

---

## 5. DI Registration

- [ ] Service mới được đăng ký trong `DependencyInjectionService.cs`
- [ ] `IRepository<T>` mới KHÔNG cần đăng ký riêng — đã có generic `IRepository<>` → `EfRepository<>`
- [ ] Entity mới phải có `DbSet<T>` trong `OrderLogisticDataContext.cs`

---

## 6. MassTransit / Quartz

- [ ] Consumer mới phải đăng ký `ReceiveEndpoint` trong `MassTransitExtensions.cs`
- [ ] Consumer chạy production: đặt **trong** `if (!IsDebug)` block
- [ ] Consumer chạy mọi môi trường (kể cả debug): đặt **ngoài** `if (!IsDebug)` — **không** thêm `// TODO: ForDebug` nếu đây là behavior production mong muốn
- [ ] Job mới phải có cả `AddJob` + `AddTrigger` trong cùng `AddQuartz` block
- [ ] `AddQuartzHostedService` và `AddQuartz` phải đặt **sau** `if (IsDebug) return` — debug mode không cần Quartz infrastructure
- [ ] Không duplicate cùng `JobKey` ở nhiều `AddQuartz` block
- [ ] Không tạo nhiều `AddQuartz` block riêng lẻ — tất cả job gom vào một block duy nhất

---

## 7. Using / Namespace

- [ ] `LogHelper` cần `using SB.Core.Logger;` (không phải `SB.Core.Helpers`)
- [ ] `ThanhToan_DonHangPtThanhToanDto` trong `Order.Client.Dto`
- [ ] Khi có namespace trùng tên với type (e.g. `DonHang` vừa là namespace vừa là nested class): dùng fully qualified name
- [ ] Sau khi refactor (chuyển logic ra extension method, đổi cách gọi), kiểm tra lại các `using` trong file gốc — xóa những `using` không còn được dùng trực tiếp trong file đó

---

## 8. Entity

- [ ] Kế thừa `BaseEntity` — fields chung (KHÔNG tự tạo lại, KHÔNG đặt tên khác): `Id`, `IsDelete`, `NgayTao`, `NgayCapNhat`, `NgayXoa`, `NguoiTaoUserId`, `NguoiTaoUserName`, `NguoiCapNhatUserId`, `NguoiCapNhatUserName`, `NguoiXoaUserId`, `NguoiXoaUserName`. Khi viết raw SQL phải dùng đúng tên này (ví dụ `NgayCapNhat` không phải `NgaySua`)
- [ ] `IdDonVi` kiểu `Guid?` — multi-tenant
- [ ] String fields có default `= string.Empty` nếu không nullable
- [ ] **🔴 CRITICAL: String Property Column Type**
  - [ ] **MỌI** string property phải có `[Column(TypeName = "...")]` attribute — không được để trống hay thiếu
  - [ ] `VARCHAR(n)` — cho ASCII-only: `MaXXX`, `MaStatus`, `EventName`, `TenDangNhap`, `StatusCode` (ví dụ: `[Column(TypeName = "VARCHAR(50)")]`)
  - [ ] `NVARCHAR(n)` — cho tiếng Việt/Unicode: `TenXXX`, `MoTa`, `GhiChu`, `TieuDe`, tất cả text user-friendly (ví dụ: `[Column(TypeName = "NVARCHAR(200)")]`)
  - [ ] **Lỗi phổ biến**: Missing `[Column(...)]` → EF sinh `nvarchar(max)` default (gây performance issue) → **QA phải catch**
  - [ ] Check: grep `public string` trong entity file → mỗi dòng phải có `[Column(TypeName = "...")]` ở dòng trước
- [ ] **KHÔNG** dùng khóa ngoại (Foreign Key constraint) — chỉ dùng field Guid reference, không tạo FK relationship trong DB
- [ ] Field mới trên entity đặt snapshot (lưu tại thời điểm tạo) phải nhất quán với các field snapshot khác trong cùng entity (ví dụ: `TenSanPham` trong `DonHangLogisticSanPham` là snapshot → `TenVietTat`/`SuDungTenVietTat` cũng phải là snapshot, lưu cùng lúc)
- [ ] Logic tính toán tên hiển thị theo flag nghiệp vụ (e.g. `GetTenHienThi`) đặt trong **extension method** trên entity/DTO, không inline tại điểm gọi — để tái sử dụng nhiều nghiệp vụ mà không lặp lại code

- [ ] **Cột thuộc nghiệp vụ sàn TMĐT phải prefix `TMDT`** (xem `patterns.md` › Naming) — `TMDTKhachHangYeuCau`, KHÔNG phải `KhachHangYeuCauSan`
- [ ] **Thêm cột DB-first**: có script ALTER **để ngoài repo** (`memory/local/sql/`, KHÔNG commit `.sql` vào repo code) + ghi rõ **DB đích** + **chạy TRƯỚC khi deploy BE** (BE đọc cột mới ngay → thiếu cột là `Invalid column name`)
- [ ] **Backfill cột mới: kiểm chứng giả định "dữ liệu cũ do AI/cái gì ghi"** trước khi copy hàng loạt
  - Sai lầm thật (B318): giả định *"cờ của đơn sàn chỉ do webhook sàn ghi"* → thực tế record do user tự tạo cũng có giá trị đó (BE cũ `?? true`) → suýt backfill sai, khóa oan hàng loạt đơn + hiện thông báo sai sự thật
  - Cách kiểm: tìm **dấu hiệu phân biệt nguồn ghi** (vd webhook ghi ĐỒNG THỜI 2 cột bằng nhau ⇒ `WHERE cotA = cotB` lọc đúng nhóm do webhook ghi), đừng backfill "cả cụm cho nhanh"

---

## 8b. Ghi cùng một trường ở NHIỀU nhánh (INSERT / UPDATE / webhook)

- [ ] **Luật đặt ở nhánh INSERT thì PHẢI đặt cả nhánh UPDATE** (và ngược lại) — nếu không, **cùng một thao tác của user cho 2 kết quả khác nhau** tùy record đã tồn tại hay chưa
  - Lỗi thật (B318, trace bắt được): "khai thông tin thuế ⇒ coi như có yêu cầu" chỉ cài ở nhánh tạo mới → đơn đã có record thì cờ giữ nguyên → hóa đơn ra sai nguồn thông tin
- [ ] Grep hết **mọi điểm ghi** của trường đó (`rg "\.TenTruong\s*="`) rồi đối chiếu từng điểm có tuân cùng một luật không — đừng chỉ sửa chỗ đang đọc
- [ ] Trường có **cột phái sinh / cột song song** (cờ trên bảng con vs cờ trên bảng cha) → sau mỗi nhánh ghi phải hỏi *"2 cột còn khớp nhau không?"*, lệch là màn danh sách và màn chi tiết hiển thị khác nhau

---

## 9. Pre-push Checklist — TODO / Debug / Hardcode

Trước khi đẩy code lên git, grep toàn bộ các file đã thay đổi trong session và confirm từng mục:

- [ ] **TODO do Claude sinh ra**: grep `// TODO` trong các file đã sửa — từng TODO phải được giải quyết hoặc user xác nhận tạm chấp nhận
  - Ví dụ điển hình: `// TODO: remove after debug`, `// TODO: ForDebug`, `// TODO: comment for debug`
- [ ] **Code debug tạm thời**: tìm `appsettings.StagingDebug.json` — kiểm tra có override URL nào dùng `localhost` hay giá trị debug không (ví dụ `CustomerServiceUrl = "http://localhost:..."`)
- [ ] **Hardcode URL / config**: grep `localhost`, `http://127.0.0.1`, IP cứng trong file `.cs` và `appsettings.*.json` các file đã sửa
- [ ] **Hardcode giá trị nghiệp vụ**: magic number không có constant/enum (ví dụ: `if (status == 31)` thay vì `GiaoVan_TT_PS.DaGiaoHang`)
- [ ] **Console.log / Debug.WriteLine còn sót**: grep trong file `.ts` và `.cs` đã sửa
- [ ] **Comment-out code lớn**: code bị comment `//` dài hơn 5 dòng liên tiếp — xác nhận có giữ lại không hay xóa hẳn

**Cách thực hiện**: Với mỗi file đã tạo/sửa trong session, grep các pattern trên và báo cáo danh sách — user confirm từng mục trước khi push.

---

## 11. NghiepVu Framework

- [ ] **Khi thay đổi `INghiepVuHandler` interface** (ví dụ: thêm/đổi tham số `XuLyAsync`): grep toàn bộ project tìm tất cả implementors — mỗi class implement interface phải cập nhật signature ngay trong cùng session, không để compile error tồn tại
- [ ] `XuLyAsync(pending, int huongNet)`: `huongNet` luôn là `+1` hoặc `-1` (từ `Math.Sign(net)`) — handler không cần tự tính hướng từ `TrangThaiGiaoHang` nữa
- [ ] `TrangThaiGiam` đã bị xóa khỏi entity `DonHangLogisticNghiepVuCauHinh` — mọi status không trong `TrangThaiTang` đều là Giảm theo quy tắc ngầm định, không cần cấu hình riêng
- [ ] Khi thêm/xóa field khỏi entity, kiểm tra tất cả service đang dùng entity đó — đặc biệt là `new Entity { ... }` initialization có thể đang set field đã xóa

---

## 12. Angular / Frontend

- [ ] **KHÔNG** dùng `formXxx = this.fb.group(...)` là class field initializer — dự án dùng `useDefineForClassFields: true`, field initializer chạy TRƯỚC khi constructor params được gán → `this.fb` undefined. Khai báo `formXxx: FormGroup` rồi init trong constructor body
- [ ] Service method mới phải thêm vào cả `.service.ts` lẫn gọi trong component
- [ ] `patchValue` sau khi get data từ API
- [ ] **ng-zorro disable**: `nz-button` dùng `[disabled]` **native**, KHÔNG có `[nzDisabled]` — đoán sai → compile `NG8002` fail
- [ ] **`nzOnCancel` trong `NzModalService.confirm()`**: nếu cancel = không làm gì thì để **trống** — KHÔNG gọi lại API với `confirmStatus = false` vì BE sẽ xử lý như một lần submit bình thường
- [ ] 🔴 **Loading phải dùng spinner CHUNG `SpinnerManagerService.show()/hide()`** — ❌ KHÔNG `<nz-spin [nzSpinning]="loading">` bọc panel/bảng/danh sách (spinner nhỏ khác kiểu overlay chung → màn nào một kiểu). Chỉ được `nz-spin nzSimple nzSize="small"` cho indicator **từng item** và `[nzLoading]` cho **nút submit**. Và vì service **đếm ref**: `show()` phải khớp cặp `hide()` ở **then + catch + return sớm**; hàm A `show()` rồi gọi hàm B cũng `show()` thì cần **2** `hide()` — **thiếu 1 hide = overlay treo, khoá cả trang**. Lỗi lặp lại nhiều lần (chốt 30/07/2026). Xem `patterns.md` mục Loading.
- [ ] 🔴 **Thay danh sách HARDCODE bằng danh mục động**: grep **TOÀN BỘ** nơi dùng danh sách cũ TRƯỚC khi đổi, và giữ **ĐỦ tập giá trị** — kể cả giá trị tưởng "không ai chọn". **Ẩn/hiện là quyền của DANH MỤC (`IsActive`), FE KHÔNG được `filter` thêm**; fallback lúc danh mục chưa seed cũng phải trả **đủ** bộ mặc định. Bug thật 30/07/2026 (591-3): seed `Khac` `IsActive=0` **+** FE `filter(x => x.value !== 'Khac')` → form "Tạo ticket hỗ trợ" **mất hẳn loại "Khác"** vốn đang dùng; lỗi im lặng, chỉ user phát hiện.
- [ ] 🔴 **1 màn có NHIỀU BẢNG → thêm/sửa cột phải rà HẾT các bảng**, mỗi bảng có bộ cột + `TableColumnKey` + `TableSettingComponent` RIÊNG (vd `ticket-ho-tro`: `gridColumns`/`TICKET_CUSTOMER_LIST`/`tblSetMain` **và** `drillGridColumns`/`TICKET_DRILL_TK`/`tblSetDrill`). Kèm 3 thứ không tự lan: **quy tắc ẩn/hiện** (`adminCols` chỉ chạy trên 1 mảng — bảng kia phải lập luận riêng, cấm copy mù), **cách render cùng 1 field** (bảng này in nhãn, bảng kia in mã thô = 2 bảng nói khác nhau), và **user đã lưu layout thì KHÔNG tự thấy cột mới** (phải clear cấu hình cột sau deploy). Xem `skills/table-list.md` › "1 màn nhiều bảng".
- [ ] 🔴 **Đụng mảng cột config (`GridColumn[]` / columns / list có `field`/`key`)**: grep **TOÀN mảng** tìm `field` TRÙNG (không chỉ đọc dòng thêm trong diff) — 2 cột cùng `field` → **lưu cấu hình cột CHẾT** (save config key theo `field` → collision). Trace BẮT BUỘC cover case **"bấm Lưu cấu hình cột"**, không chỉ hiển thị/bind giá trị. Bug thật 24/07 (1046): `giaoHangNgayGiaoHang` khai 2 lần trong `don-hang-sale.component.ts`. Xem `lessons/grid-column-field-trung-save-config-die.md`.

---

## 13. Angular Lazy Module — Tách module

- [ ] `antdModule` (từ `src/app/constants/import.ts`) **không bao gồm tất cả** NgZorro — thiếu ít nhất: `NzTreeSelectModule`. Khi tạo lazy module mới, kiểm tra từng `nz-*` tag trong HTML và thêm module còn thiếu vào `imports[]`
- [ ] Component chỉ được declare trong **một NgModule duy nhất** — nếu component A (trong module X) bị reference bởi component B (trong module Y), phải copy A vào folder của module Y và declare lại, không dùng cross-module import
- [ ] Khi copy component từ folder sâu hơn sang folder nông hơn (ví dụ `WarehouseProduct/delivery/` → `pages/delivery/`), tất cả relative import (`../../..`) bị lệch một cấp — phải đổi sang `src/app/...` absolute path để tránh lỗi
- [ ] `ComponentsModule` import một số NgZorro module (NzDropDownModule, NzCheckboxModule, NzPaginationModule) nhưng **không export** ra ngoài — lazy module import `ComponentsModule` vẫn phải tự import trực tiếp các NgZorro module cần dùng
- [ ] Khi xóa component khỏi module cũ: phải xóa cả (1) dòng `import { ... } from '...'` **và** (2) dòng trong `declarations[]` — thiếu một trong hai sẽ lỗi compile
- [ ] Khi đổi FE route (lazy-loaded path trong `app-routing.module.ts`): phải UPDATE bảng `Page` trong DB để đồng bộ URL menu — không có script này thì link menu vẫn trỏ URL cũ
- [ ] Sau khi chuyển module, **grep toàn bộ folder mới** tìm hardcode path cũ (`/old-module-path/`) trong cả `.ts` lẫn `.html` — đặc biệt nguy hiểm là `router.navigate()` trong TypeScript (tự redirect khi load) và `routerLink` trong template (sai link khi user click)
- [ ] `router.navigate()` hardcode path cũ bên trong `queryParamMap.subscribe()` hoặc `ngOnInit` sẽ gây **redirect vô hình** ngay khi component load — khó debug hơn `routerLink` vì không cần user click
- [ ] **Thông báo (notification) phải dùng service chung**: `_notification.success/error/warning(MESSAGE.X, body)` — title = hằng `MESSAGE` (`src/app/constants/message`), vị trí mặc định (topRight). ❌ KHÔNG tự chế helper `toast()` gọi `_notification.create(..., { nzPlacement: 'bottomRight' })` (thông báo nhảy góc khác hệ thống → trông như "dùng thư viện khác"); ❌ KHÔNG hiện `HttpErrorResponse.message` thô ("Http failure response for..."). Xem `patterns.md` mục Notification.

## 14. Order ↔ OrderLogistic — logic SONG SONG (rà cả 2)

- [ ] 🔴 **Sửa logic xuất hóa đơn / nguồn TT người mua / auto-invoice ở `OrderLogistic\...\DonHangInvoiceService.cs` → PHẢI rà + sửa `Order\...\DonHangInvoiceService.cs`** (và ngược lại). 2 module có bộ hàm SONG SONG (`Fill_InvoiceBuyerInfo_Into_InvoiceDonHang`, `DuThongTinBatBuocTheoDon`, `FillBuyerMotPhanMacDinh`, dispatch `cachLayThongTinKhachHang`). Order = bán lẻ/offline (`TenKhach`/`SoDienThoaiKhach`, KHÔNG địa chỉ trên đơn, KHÔNG ecom, cần guard `!IdKhachHang.GuidIsNullOrEmpty()`); OrderLogistic = giao vận/TMĐT (`KhachHangTen`/`GiaoHangDiaChiFullText`/`KhachSoDienThoai` + có ecom case 3). Rộng hơn: SCRM có nhiều cặp service song song Order/OrderLogistic — sửa 1 luôn hỏi "cái kia có bản song song không?". Xem `lessons/sua-orderlogistic-quen-order-song-song.md`.
- [ ] **Thêm biến thể vào dispatch kiểu int** (vd `cachLayThongTinKhachHang` 0/1/2/3/4): KHÔNG đẻ magic number mới (`41`...) + nhân bản helper `_B2`. Đúng: tái dùng giá trị dispatch có sẵn + **tham số hóa helper** (truyền config/list qua tham số hoặc biến local), giữ 1 helper dùng chung. Xem `convention/them-config-object-long-opt-in.md`.
