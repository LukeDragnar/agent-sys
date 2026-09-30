# Pushsale — Architecture

> Đọc TRƯỚC khi làm bất kỳ task Pushsale nào. Bản đồ tổng thể hệ thống.
> **Đừng nhầm với SCRM.** SCRM = microservice .NET 6 + Angular. Pushsale = **monolith VB.NET trên DotNetNuke 7.04.02** (WebForms), DAL gọi Stored Procedure, không ORM ở phần legacy.

## 1. Bức tranh tổng thể

Pushsale là nền tảng **CRM + telesale + bán hàng online + giao vận (logistics)** đa tenant. Mỗi tenant = một **đơn vị** (`id_don_vi`). Người dùng đăng nhập qua DNN, được gán **role** (Sale, Marketing, CEO, Admin, UnitAdmin, Accounting, Warehouse, ChiaSo, TaoDonVi...) quyết định màn hình thấy được.

Kiến trúc 3 lớp cổ điển + một nhánh microservice mới đang tách dần:

```
┌──────────────────────────────────────────────────────────────┐
│  PRESENTATION — DotNetNuke WebForms                            │
│  web/DesktopModules/APP/  (542 control .ascx + .ascx.vb)       │
│  Mọi control Inherits PortalModuleBase. Main.ascx là loader    │
│  động: đọc setting "tên control" rồi LoadControl() vào         │
│  placeholder. Phân quyền bằng UserInfo.IsInRole().             │
└───────────────────────────┬──────────────────────────────────┘
                            │ gọi trực tiếp (no service layer)
┌───────────────────────────▼──────────────────────────────────┐
│  HELPER / BLL — lib/base.heplper (project base.helper)         │
│  WebHelper, ObjectHelper, CacheHelper, OfficeHelper (Excel),   │
│  SMSHelper, EncryptionHelper... + lib/base.report              │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  DATA ACCESS — lib/base.dal  (30+ domain)                      │
│  Mỗi domain: {Domain}DAL.vb (entry) + {Domain}Config/Constants │
│  + {Entity}/{Entity}Info.vb (model) + {Entity}CRUD.vb          │
│  Gọi SP qua SqlHelper.Execute*(ConnectionString, "sp_name", …) │
│  Cache theo id_don_vi (multi-tenant isolation).                │
└───────────────────────────┬──────────────────────────────────┘
                            │ Stored Procedure
┌───────────────────────────▼──────────────────────────────────┐
│  SQL SERVER — nhiều database (xem mục 5)                        │
│  *_portal (DNN) · *_data (nghiệp vụ) · *_logs · PS_Webhook_*   │
└──────────────────────────────────────────────────────────────┘

  Bên cạnh (đang tách dần ra):
  ┌──────────────────────────────────────────────┐
  │ MICROSERVICE .NET Core — Services/            │
  │ File.API (MongoDB + gRPC + JWT)               │
  │ Pushsale.CommonV3 (Dapper, gRPC, middleware)  │
  └──────────────────────────────────────────────┘
  Hạ tầng phụ trợ: Elasticsearch (domain ES), Redis (domain Redis).
```

**Điểm mấu chốt cần nhớ:**
- **Không có service/business layer riêng.** Code-behind `.ascx.vb` gọi thẳng DAL (hoặc đôi khi gọi `SqlHelper` trực tiếp với connection string của domain). Logic nghiệp vụ nằm rải trong code-behind + Stored Procedure.
- **Stored Procedure là nơi chứa phần lớn business logic.** SP **không** nằm trong repo (deploy riêng vào DB). File `.sql` ở SVN root chỉ là script sửa/chuẩn hóa **data**, không phải schema versioning.
- **Multi-tenant = `id_don_vi`.** Mọi query và cache key đều mang `id_don_vi`. Đây là ranh giới cô lập dữ liệu giữa các tenant.

## 2. Solution (`pushsale.sln`)

| Project | Lang | Vai trò |
|---|---|---|
| **web** | VB | DNN site .NET 4.8 — chứa module APP, toàn bộ UI nghiệp vụ |
| **base.helper** | VB | Helper/BLL. ⚠️ Folder trên đĩa là `lib/base.heplper/` (sai chính tả), project name vẫn là `base.helper` |
| **base.dal** | VB | Data Access Layer, 30+ domain |
| **base.report** | VB | Helper riêng cho báo cáo |
| **Microsoft.ApplicationBlocks.Data** | C# | `SqlHelper` — wrapper ADO.NET (Enterprise Library Data Access Block cũ) |
| **sb.application.helper.logtime** | C# | Tracking thời gian |
| **sb.invoice.helper** | VB | Helper hóa đơn |
| **sb.test.lib** | C# | Unit test |
| **sb.generator** | C# | Sinh ID phân tán kiểu Snowflake (epoch 2020-01-01, 41-bit time + 10-bit shard + 12-bit seq) |

## 3. Module APP — bản đồ UI (`web/DesktopModules/APP/`)

Entry: **`Main.ascx.vb`** — loader động. Đọc setting `CONTROL_NAME_KEY` (có thể override qua query string `c{ModuleId}` / `cc{ModuleId}` / `ccc`), `LoadControl(controlName)`, ép sang `PortalModuleBase`, add vào `phMain`. → Một instance DNN module render được nhiều màn hình tùy setting.
Phụ trợ: `MainSettings.ascx` (cấu hình module), `ViewSetup.ascx` (setup quyền), `Blank.ascx`.

### Admin/ — quản trị & vận hành
`Monitor/` (50+ tool debug: CacheMonitor, ESMonitor/ES_Clean/ES_Index, BugDetector, JSONViewer, MongoScan, ContactLog, GHLog, AdminScan/ResetKhoLichSu...), `MongoDB/` (init DB Mongo), `QuanLyDanhMuc`, `QuanLyJob`, `QuanLyMenu`, `QuanLyNguoiDung`, `QuanLySystemLog`, `QuanLyThongBao`, `QuanLyUserActivity`, `QuanLyCacheVersion`, `SMS/`.

### Mains/ — nghiệp vụ chính (phần lớn code)
- **Customers/** — CRM khách hàng: `Customers`, `CustomerEdit`, `CustomerContacts` (đơn của KH), `CustomerFacebooks`, `CustomerRank`/`ListRank`, chiến dịch `Events`/`EventCustomers`/`EventEdit`.
- **LD/ (Lâu Đài) — lõi nghiệp vụ**, chia theo bộ phận/role:
  - `Sale/` — bán hàng (lớn nhất): `DanhSachContact` (danh sách lead/đơn), `CapNhatContact2` (form cập nhật contact/đơn — file rất lớn), `BangTongHopBanHang`, `CallHistoryByContact`, khiếu nại (`DanhSachKhieuNai1`, `BieuDoKhieuNai`), `BlacklistLog`.
  - `Marketing/` — `CapNhatContact`, `CapNhatSeeding`, `BieuDoMarketing`.
  - `Warehouse/` — kho: `CapNhatBienBanContact`, `BCNhapXuatTheoNgay`.
  - `Accounting/` — kế toán: `PhieuChi`, `CapNhatCOD`, `CancelInvoice`.
  - `TongDai/` — tổng đài/CSKH: `CapNhatDauSo`, `CapNhatGroup`, `ActiveHistory`.
  - `CEO/` — dashboard cấp cao: `BaoCaoDoanhSo`, `BaoCaoCareDon`, `ChartDashboard`.
  - `PushSale/` — `ThongKeDoanhSoDonVi`, `ThongKeTangTruong`.
  - `Admin/`, `UnitAdmin/`, `Corporations/` (tổng công ty/chi nhánh), `Facebook/` (chat, quảng cáo), `ThongKe/`, `Style/`.
- **Notify/** — `ChatBot/`, `InApp/`, `SMS/`.
- **PAY/** — `Checkout/`, `MuaGoiSuDung/`, `Contact/` (mua/gia hạn gói).
- **PN/** — Partner/đối tác: `Admin/`, `ConfigMember/`, `Token/`.
- **Sample/** — control mẫu.

### Modules/ — component dùng chung
`DashBoard/`, `HTML/` (DisplayHTML/EditHTML), `NguoiDung/` (ChiTietTaiKhoan, DangKy, DoiMatKhau, LichSuDangNhap, QuanLyDangNhap).

### Reports/ — 14 báo cáo/in ấn
`BC_DoanhThu`, `CapNhatSoLieuBaoCao`, in đơn giao vận theo hãng: `vInDonGiaoVan2`, `vInDonGiaoVanGHTK`, `vInDonGiaoVanJNT`, `vInDonGiaoVanSPX`; `vBaoCaoTongHop`, `vBaoCaoDanhSach`, `vBaoCaoExcel`.

## 4. DAL domains (`lib/base.dal/`)

Mỗi domain map một mảng nghiệp vụ; `{Domain}DAL.vb` là entry, `{Domain}Constants/Config.vb` chứa hằng số (vd `LDConstans.vb` ~43KB).

| Domain | Ý nghĩa |
|---|---|
| **APP** | Cấu hình app, user profile (`APP_UserProfile`), quyền |
| **LD** (Lâu Đài) | Lõi: contact/lead/đơn hàng, sản phẩm, nghiệp vụ kho, invoice data |
| **HT** | Hệ thống: log, role, menu, thông báo |
| **CUSTOMERS** | CRM khách hàng |
| **GH** (Giao Hàng) | Logistics — tích hợp 12+ hãng: AhaMove, BEST, BoxMe, DHCL, GHTK, HeyU, HolaShip, JNT, NJV, Ship60, SPX, SUPERSHIP |
| **PAY** | Thanh toán, mua gói sử dụng |
| **RP** | Báo cáo / analytics |
| **ES** | Elasticsearch (index/search contact) |
| **Redis** | Cache phân tán |
| **SMS / CHATBOT / TONGDAI** | Kênh giao tiếp (webhook DB riêng) |
| **Invoice / HDDTDAL** | Hóa đơn / hóa đơn điện tử |
| **Ecommerce** | Tích hợp sàn (Shopee — xem `receive_data_shopee_v2.aspx.vb`) |
| **KS** (Kho Số) | Kho số |
| **LADI** | Landing page (LadiPage) |
| **Notification / HANDLENOTIFICATION / PubSub** | Thông báo, pub/sub |
| **MG** | (MongoDB-related) |
| **Scheduler** | Job định kỳ |
| **APIController / CustomModule / CustomAttribute / Models / Constants / Caching** | Hạ tầng chung |

## 5. Database & connection strings

DB là **SQL Server**, cấu hình ở `web/web.config` (`<connectionStrings>` chỉ có `SiteSqlServer`; phần lớn còn lại nằm trong `<appSettings>` dạng key=connection string). Nhiều môi trường (LOCAL/DEV/STAGING/PROD) để sẵn dạng comment, chỉ 1 dòng active.

Các DB chính (tên DB tùy môi trường, vd `dev_pushsale_*`):
- `*_portal` — DNN portal (`SiteSqlServer`)
- `*_data` — dữ liệu nghiệp vụ (`APP_DATA_ConnectionString`, `HTConnectionString`, `LD_ConnectionString`, `RP_ConnectionString`, `KhoSo_ConnectionString`)
- `*_logs` — log (`LDLog_ConnectionString`)
- `PS_Webhook_*` — webhook các kênh: `SiteSqlServerTongDai`, `SiteSqlServerSms`, `SiteSqlServerChatBot`, `SiteSqlServerCustomers`, `Ecommerce_ConnectionString`

> ⚠️ web.config chứa **credential thật**. KHÔNG copy mật khẩu ra ngoài repo / commit message / doc. Khi đổi môi trường chỉ uncomment dòng tương ứng.

## 6. Microservice (`Services/`)

Hướng tách dần khỏi monolith (tham khảo `micro service.txt` — list bài DDD/microservice).
- **File.API** — ASP.NET Core, MongoDB, gRPC, JWT; `File.DataContext` dùng EF Core (Files/Folders/FsFiles/FsFolders/FsFileShareds).
- **Shared/Pushsale.CommonV3** — thư viện chung C# mới (Dapper, gRPC, middleware, caching, HttpResponses) — dùng cho các service mới, **không** dùng ở phần VB legacy.

## 7. Luồng request điển hình (đọc 1 màn hình)

1. DNN route → tab chứa DNN module APP → `Main.ascx` (loader) đọc setting tên control → `LoadControl()` control nghiệp vụ (vd `DanhSachContact.ascx`).
2. `Page_Load` (`If Not Page.IsPostBack`) → check role (`UserInfo.IsInRole`), nếu không đủ quyền → `Response.Redirect("/")`.
3. `InitData()` đổ dropdown/filter (thường lấy theo `id_don_vi`, có cache); `ShowData()` gọi SP qua `SqlHelper.ExecuteDataset(domain.ConnectionString, "sp_name", params...)` → bind thẳng `DataSet.Tables(0)` vào Repeater/GridView.
4. Filter người dùng chọn được lưu vào `Session("S_...")` để giữ trạng thái giữa các postback.
5. Query chậm (>3000ms) được log qua `HTDAL.HT_Logs_Log(...)` / `WebHelper.TimeTracking(...)`.

→ Chi tiết convention code xem `patterns.md`. Checklist review xem `qa-code.md`.
