# Module Ticket hỗ trợ (WorkFlow) — Cẩm nang Dev

> **Kho là snapshot.** `path:line` có thể lệch — mở source thật verify trước khi sửa. Code là chân lý, kho là bản đồ. Các fact ⓥ đã Claude verify với code Group4 2026-07-15.

## 1. Data model + field quan trọng

### TicketCustomer (SQL, BaseEntity) — phiếu THÔ
`WorkFlow/WorkFlow.DataContext/Entity/TicketCustomer.cs`
- **Định danh:** `Id (Guid)`, `MaTicket` UNIQUE = `T{N}` tăng dần **toàn hệ thống** (không theo tenant).
- **Tenant:** `IdDonVi/IdChiNhanh (Guid?)` = **đơn vị người gửi**. `IdDonViKhachHang/IdChiNhanhKhachHang (Guid?)` = đơn vị khách thật (CSKH gửi hộ → null).
- **Nối THÔ↔GOM:** `SandboxIssueMa VARCHAR(100) NULL`, `SandboxIssueId Guid? (FK)`.
- **Người tạo:** `NguoiTaoUserId (Guid?)` + `NguoiTaoUserName` (BaseEntity) — khóa tự nhiên "sale/người tạo".
- **Nội dung khách:** `Loai` (Bug/YeuCau/ChinhSua/HuongDan/Khac — **chuỗi tự do, không class hằng**), `TieuDe`, `NoiDung (MAX)`, `PhanTich (MAX = MỤC TIÊU)`, `HinhAnhIds (MAX, file id nối ';')`, `NgayYeuCau`.
- **Thông tin khách:** `MaKhachHang`, `TenCongTy`, `Domain`, `TenTaiKhoan`, `SoDienThoai`.
- **SLA/phản hồi:** `PhanHoiSandbox (MAX, chỉ SB team ghi)`, `NgayPhanHoi` (set **1 LẦN** lần đầu SB phản hồi), `ChoSBPhanHoi (bool?)`, `TinCuoiNoiDung/TinCuoiThoiDiem`, `ThoiGianPhanHoiPhut (int?)`.
- **Webhook cũ:** `KhDeXuat`, `TongSoKH`, `TongUser`, `SalesPhuTrach` (chỉ webhook set + chỉ hiển thị — **KHÔNG** dùng lọc).
- **KHÔNG có** cột `IdPhongBan/IdNhomNhanVien`.

### SandboxIssue (SQL, BaseEntity) — phiếu GOM
`Entity/SandboxIssue.cs` — dedup `(IdDonVi, SandboxIssueMa)`. Field: `Loai ('YeuCau'|'Bug')`, `NhomTinhNang`, `MucDo`, `Estimate`, `DuKienXong`, `CapDo`, `ManHinh`, `AnhDemoUrl/DemoLink`, `Data (MAX, full JSON)`, `FileDinhKemIds`, `TrangThai` (riêng). `SandboxIssueMa` **NOT NULL** (default empty).

### DataReceiveTicket (SQL) — nhận webhook thô
`Type ('ticket'|'ticketCustomer')`, `Data (raw JSON)`, `InProcess (bool)`, `CreatedDateTicks (BIGINT)`, `IdTransaction`. Xử lý xong `IsDelete=1`, job xóa vật lý sau.

### TicketCustomerChat (Mongo POCO) — 1 tin chat ⓥ
`CollectionMongoDB/TicketCustomerChat.cs` — `Id [BsonId]`, `IdTicketCustomer`, `NoiDung (string?, dòng 23)`, `NguoiGuiLoai ('SB'|'KHACH', dòng 27)`, `HinhAnhIds (string?, file id nối ';', dòng 31)`, `NgayTao`, `NguoiTaoUserId/UserName`, `NguoiGuiTen (snapshot)`, `IsDelete`. **Không có message-type field** — phân biệt text/ảnh ngầm qua `NoiDung` + `HinhAnhIds`.

### Hằng ⓥ
- `TicketCustomerTrangThai`: `Moi="MOI"`, `DangXuLy="DANG_XU_LY"`, `DaXuLy="DA_XU_LY"`.
- `TicketTenantConstants`: `IdDonViHeThong = b8f8d798-2349-474c-a913-317d8865f0af`; helper `CoTheChonNguoiGui(user)`.

## 2. API (route base `api/TicketCustomer`)

> ⚠️ **List ticket thật nằm ở `TicketCustomerController`, KHÔNG phải `TicketController`.** `TicketController` gần rỗng (chỉ `GetQuyenTicket`); list/detail issue đã chuyển sang `SandboxIssueController`.

| Method | Route | Mục đích | Auth |
|---|---|---|---|
| POST | `TicketCustomer/DanhSach` | List theo scope user + drill-in (lọc `SandboxIssueMa`). Total qua meta. | `[SBAuthorize]` + scope service |
| POST | `TicketCustomer/LayDanhSach` | [AI] list thô **cross-tenant**. | `[PrivateSBAuthorize]` |
| GET | `TicketCustomer/GetByMa/{ma}` | Drill 1 ticket theo `MaTicket`. | `[SBAuthorize]` |
| POST | `TicketCustomer/ThongKe` | Card `Tong/Moi/DangXuLy/DaXuLy` + `DanhSachDonVi`. **KHÔNG ăn filter body.** | `[SBAuthorize]` |
| POST | `TicketCustomer/GuiDeXuat` | CREATE 1 TicketCustomer, sinh `MaTicket`. | `[SBAuthorize]` |
| POST | `TicketCustomer/CapNhat` | UPDATE `TieuDe/NoiDung/Loai/TrangThai/PhanTich/PhanHoiSandbox`. | `[SBAuthorize]` |
| POST | `TicketCustomer/Xoa/{id}` | DELETE — chỉ khi `TrangThai=MOI`. | `[SBAuthorize]` |
| POST | `TicketCustomer/CapNhatMaEnd` | [AI] gắn `SandboxIssueMa + SandboxIssueId`. | `[PrivateSBAuthorize]` |
| POST | `TicketCustomer/LinkIssue` | [WEB] liên kết/gỡ ticket↔issue. **Chỉ SB team.** | `[SBAuthorize]` |
| GET | `Ticket/GetQuyenTicket` | Cờ `CoTheChonNguoiGui` (FE quyết hiện 3 tab). | `[SBAuthorize]` |
| POST | `TicketCustomerChat/GuiTin` | Gửi 1 tin `{IdTicketCustomer, NoiDung?, HinhAnhIds?}`. | `[SBAuthorize]` |
| GET | `TicketCustomerChat/DanhSach/{id}` | List chat 1 ticket, sort NgayTao ASC. | `[SBAuthorize]` |

**Files API (ảnh):** `POST {FILES_API}/Files/UploadFile` (→ data.id); `GET {FILES_API}/Files/GetFile?id={id}&size=o`.

### DTO ⓥ
- `TicketCustomerFilterDto : GridInfo` (`TicketFilterDto.cs:17-27`) — `ChuaGom`, `Keyword`, `TrangThai`, `Loai`, `IdDonVi`, `SandboxIssueId`, `SandboxIssueMa`, `ChuaPhanHoi (tri-state)`, `SapXep`. **KHÔNG có field lọc người tạo/order** ⓥ. `GridInfo`: `Page`(1), `PageSize`(50 nếu ≤0).
- `TicketCustomerListDto` — đã có `NguoiTaoUserName (=Người yêu cầu)`, `TenTaiKhoan`, `TenCongTy` + field SLA/chat denormalize.
- `GuiTinChatDto` (in): `{IdTicketCustomer, NoiDung?, HinhAnhIds?}` — validate ít nhất 1 trong 2.

## 3. Scope multi-tenant / quyền
- `[SBAuthorize]` **trống** = chỉ login+session, **KHÔNG** check role ở attribute. Phân quyền vai/tenant nằm **TRONG service**.
- Tập trung ở **`LaSandboxTeamAsync(user)`** (`TicketCustomerService.cs:69`): SB team = `CoTheChonNguoiGui` (SuperUser|HT_AD|HT_CSKH|`DonViId==IdDonViHeThong`) HOẶC `DonViId==config SANDBOX_GUIDELINE_DON_VI_ID`. SB team → cross-tenant; user thường → `IdDonVi==DonViId` (null→`Guid.Empty` chặn sạch).
- 🔴 **Logic scope COPY-PASTE 6 chỗ:** list(`:191`), thongke(`:535`), getByMa(`:306`), capnhat(`:362`), xoa(`:509`), chat(`:66`). **Sửa 1 quên 1 = rò cross-tenant.**
- `UserLogin` sẵn nguyên liệu phân quyền sale: `IsSale/IsDvSale (DV_SALE)`, `IsQuanLySale (DV_QL_SALE)`, `IsSaleLead (DV_SALE_LEAD)`, `IdPhongBan`, `IdNhomNhanVien`.

## 4. List / filter / sort ⓥ
`LayDanhSachCoreAsync` (`:200-304`): `TableWithNolock` → tenant scope → `ChuaGom` → `TrangThai` → `Loai` → `IdDonVi` → `SandboxIssueId` → `SandboxIssueMa` → `ChuaPhanHoi` → `Keyword` → Count → paging → OrderBy → Select DTO.
- **Keyword** OR trên 5 cột: `TieuDe, NoiDung, TenCongTy, NguoiTaoUserName, MaTicket` — LIKE `%..%` (không sargable).
- **Sort** `SapXep`: `cu_nhat`/`trao_doi`/default `moi_nhat`.
- EF: expression tree không nhận `?.` — đưa scope ra biến ngoài lambda trước.

## 5. Thống kê ⓥ
`ThongKeChoUserAsync` (`:535`) → `ThongKe`: **4× `CountAsync()`** theo TrangThai + `GroupBy(IdDonVi)` cho dropdown. **KHÔNG ăn filter.**
- ⚠️ **BẪY TRÙNG TÊN:** `Core/TicketInfoService.cs` + `Report/TicketInfoService.cs` là **khóa hàng đợi Redis** (import/export), **KHÔNG** phải thống kê support-ticket.

## 6. Chat + ảnh ⓥ (F888)
- BE **đã hỗ trợ ảnh đầy đủ**: model + `GuiTinChatDto` + `TicketChatDto` đều có `HinhAnhIds`. Ảnh = file id nối ';', render `{FILES_API}/Files/GetFile?id={id}&size=o`.
- `GuiTinAsync (:89-160)`: validate NoiDung HOẶC HinhAnhIds → gán `NguoiGuiLoai` (client không override) → `InsertOneAsync` Mongo (`:120`) → denormalize ticket cha (`:122-142`: `ChoSBPhanHoi`, `TinCuoiNoiDung` — chỉ ảnh → literal `'[Hình ảnh]'`).
- **FE:** thread chat ở Tab 'Trao đổi' `ticket-detail-drawer.component.html`. **Render ảnh ĐÃ CÓ** ⓥ: `<app-file-cell-preview [value]="tin.hinhAnhIds">` (`:133-136`). **Gửi ảnh CHƯA có** ⓥ: `guiChat() (:164)` **hardcode `hinhAnhIds:''`**, input chỉ `<textarea>`. Mẫu upload: `ticket-customer-add.component.ts:330` + `FileService.uploadImage`.

## 7. FE list/detail (`ticket-ho-tro`)
- 3 tab: `moi` (server), `issue` (client, tải `PAGE_SIZE_ISSUE=500`), `thongke` (**placeholder**). Cột: `gridColumns[]` thứ tự cứng; `TableColumnKey='TICKET_CUSTOMER_LIST_V3'` (bump khi đổi cột).
- Drill-down (591-6) **đã có**: badge "Gộp N" → drawer `drillInList` (lọc `sandboxIssueMa`, `PAGE_SIZE_DRILL=200`) → click dòng → chi tiết.
- Row list là `any` (không type-safe) — tra đúng chính tả: `phanTich`=MỤC TIÊU, `nguoiTaoUserName`=NGƯỜI YÊU CẦU.

## 8. Gotchas
- Nhầm controller: list/CRUD ở **TicketCustomerController**, không phải TicketController.
- **Comment entity LỆCH:** `TicketCustomer.cs:17` ghi `IdDonVi`="đơn vị hệ thống set cứng" nhưng `GuiDeXuat (:118)` set = đơn vị người gửi. Lọc theo **khách** phải dùng `IdDonViKhachHang`.
- `SalesPhuTrach` KHÔNG đáng tin làm khóa phân quyền → dùng `NguoiTaoUserId`.
- `NgayPhanHoi` set **1 lần** → tính "thời gian phản hồi lần đầu" dùng field này, không lấy `NgayCapNhat`.
- `TrangThai`/`Loai` là chuỗi MÃ → so chuỗi, cẩn thận collation/hoa-thường.
- Mongo chat: query mới phải tự thêm `IsDelete==false`; audit set thủ công.
- **KHÔNG có push notification** khi ticket đổi trạng thái / chat mới (verify 15/07) — chỉ pull qua `ChoSBPhanHoi` badge. Muốn noti = tính năng mới (task **591-9**).

---

## Liên quan task đang làm — HIỆN CÓ vs CẦN THÊM

### 591-6 — Tab thống kê + drill-down
- **HIỆN CÓ:** `ThongKe` (count 4 trạng thái + danh sách đơn vị) + drill-down `DanhSach` + FE badge "Gộp N"→drawer đã chạy.
- **CẦN THÊM:** tab `thongke` FE **chỉ placeholder** — chưa có UI thật. BE `ThongKe` **không ăn filter** → chiều mới (theo ngày/Loai/đơn vị/SLA `ThoiGianPhanHoiPhut`, `ChoSBPhanHoi`) phải thêm param + WHERE trước các `CountAsync`.

### F888 — Ảnh trong Trao đổi
- **HIỆN CÓ:** BE full (model+in+out có `HinhAnhIds`). FE **render ảnh sẵn** (`:133-136`).
- **CẦN THÊM:** đường **GỬI** ảnh — thêm control upload vào ô chat (`:141-147`), sửa `guiChat() (:164)` đang hardcode `hinhAnhIds:''` (mẫu `ticket-customer-add.ts:330`). Nếu ảnh không hiện khi nhận → kiểm `DanhSach` trả đúng field `hinhAnhIds`.

### 926 — Lọc theo người order
- **HIỆN CÓ:** **CHƯA** có filter riêng người tạo (chỉ gián tiếp qua `Keyword`). Output đã có `NguoiTaoUserName` + `TenTaiKhoan`.
- **CẦN THÊM:** (a) thêm property vào `TicketCustomerFilterDto` (vd `Guid? NguoiTaoUserId` hoặc `string? NguoiOrder`); (b) nhánh `Where` trong `LayDanhSachCoreAsync`. **Chốt cột nào** (username audit vs `TenTaiKhoan`) trước khi code. Giữ tenant guard.

### 591-5 — Phân quyền theo sale/đơn vị
- **HIỆN CÓ:** scope **chỉ mức đơn vị**. "Sale chỉ thấy ticket mình tạo" làm ngay bằng `NguoiTaoUserId`.
- **CẦN THÊM:** phân quyền sale/nhóm/phòng. Đề xuất gom **1 helper `TinhScope(user)`** (tránh sửa lệch 6 chỗ). **Chặn:** thiếu cột `IdPhongBan/IdNhomNhanVien` → "sale lead thấy cả nhóm" cần thêm cột (snapshot lúc tạo) hoặc join user service. **Chốt spec với PO.**

### 591-3 — Duyệt yêu cầu
- **HIỆN CÓ:** **KHÔNG có gì** ⓥ — không method Duyet/Approve, không cột `TrangThaiDuyet`, không role gate. `TrangThai` đổi tự do.
- **CẦN THÊM:** luồng MỚI — trạng thái/endpoint "Duyệt" + role gate. Chốt: trạng thái duyệt riêng, ai duyệt, duyệt ở tầng TicketCustomer hay SandboxIssue. Đừng nhầm DANG_XU_LY/DA_XU_LY là "duyệt".

### 591-9 — Thông báo realtime *(task mới)*
- **HIỆN CÓ:** không có push. Chỉ pull (`ChoSBPhanHoi` badge).
- **CẦN THÊM:** hook `GuiTin (:120)` + `CapNhat(TrangThai)` publish event; tái dùng `Notification`/`SocketServer`/`thong-bao-realtime-nen`; scope đúng người (`LaSandboxTeamAsync` + `NguoiTaoUserId`).
