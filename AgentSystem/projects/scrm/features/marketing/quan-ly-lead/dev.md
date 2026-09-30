# Dev — Quản lý Lead

> Gom 3 controller (đều service **Contact**, module nghiệp vụ marketing): báo cáo hồ sơ KH lead, dashboard MKT (lịch sử thao tác), danh sách số seeding. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Hồ sơ khách hàng (lead MKT) | landing-page/ho-so-khach-hang | lưới hồ sơ KH lead + đếm trùng số + xuất Excel |
| Dashboard Marketing | marketing-dashboard.component / marketing-dashboard-20250609 | lịch sử thao tác dữ liệu user |
| Danh sách số Seeding | danh-sach-seeding-info.component (+ edit) | CRUD số seeding |

## Endpoint / Controller (trace registry)

**HoSoKhachHangController** (service Contact, `TI_Marketing`) — registry `registry/sales/HoSoKhachHangController.md`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Danh sách lead | `POST GetHoSoKhachHang_LeadMKT_Search` | `HoSoKhachHangController_GetHoSoKhachHang_LeadMKT_Search` (AD/MKT/QL-MKT/CHIASO) |
| Đếm trùng số | `POST GetHoSoKhachHang_LeadMKT_TrungSo_Count` | `HoSoKhachHangController_GetHoSoKhachHang_LeadMKT_TrungSo_Count` (AD/MKT/QL-MKT) |
| Xuất Excel kiểu 1/2 | `POST ExportExcelHoSoKhachHangKieu1` · `POST ExportExcelHoSoKhachHangKieu2` | `[SBAuthorize()]` (chỉ login — không key) |

**MarketingDashboardController** (service Contact) — registry `registry/sales/MarketingDashboardController.md`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Lịch sử thao tác dữ liệu | `POST GetUserActivitiesLog` | `MarketingDashboardController_GetUserActivitiesLog` (DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) |
| Danh mục loại hoạt động | `GET GetDMActivityLog` | `[SBAuthorize]` (login) |

**SeedingInfoController** (service Contact) — registry `registry/sales/SeedingInfoController.md`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Thêm mới | `POST ThemMoiAsync` | `SeedingInfoController_ThemMoiAsync` (AD-TONG/CN/MCN, QL-SALE, SALE-LEAD, MKT-LEAD, QL-MKT, MKT) |
| Tìm theo điều kiện | `POST TimTheoDieuKienAsync` | `SeedingInfoController_TimTheoDieuKienAsync` (như trên) |
| Xóa 1 / nhiều | `POST DeleteOneAsync` · `POST DeleteMutipleAsync` | `SeedingInfoController_DeleteOneAsync` / `_DeleteMutipleAsync` |
| Check số là seeding | `GET CheckPhoneNumberSeeding/{phoneNumber}` | `[SBAuthorize]` (login — key gốc đã comment) |

**DataReceiveController** (service **Webhook_Ladi** — `TI_Landingpage`) — registry `registry/marketing/DataReceiveController_Webhook_Ladi.md` _(bổ sung Pha 2 — lead intake tự động)_:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Nhận contact LadiPage (POST/GET) | `POST`/`GET api/DataReceive/ReceiveData/ld-api/lid/{lid}/sc/{sc}` | — **anonymous** (`[AllowAnonymous]`) |
| Nhận contact WooCommerce | `POST api/DataReceive/ReceiveDataWoocommerce/ld-api/lid/{lid}/sc/{sc}` | — **anonymous** (`[DisableFormValueModelBinding]`) |
| Resend từ service log (service→service) | `POST api/DataReceive/ResendDataLogAsync` | — **anonymous** (internal) |

> Đây là **cổng nhận lead công khai** (không UI, không đăng nhập). Lead đổ về qua đây → RabbitMQ → service log-contact tạo contact (chính là lead hiển thị ở `HoSoKhachHangController`). `SanPhamLandingController_Webhook_Ladi` (cùng service) **toàn bộ comment / DEAD** — không expose endpoint, đừng nhầm.

## Entities
- **Contact / HoSoKhachHang** — lead tổng hợp từ Contact; truy vấn báo cáo qua `IHoSoKhachHangService` (SP/EF tổng hợp). Không có entity "Lead" riêng.
- **UserActivitiesLog** — lịch sử thao tác dữ liệu của user (nguồn dashboard MKT).
- **DMActivityLog** — danh mục loại hoạt động để lọc dashboard.
- **SeedingInfo** — số điện thoại seeding theo đơn vị (số nội bộ/test).
- **LogContact / WebhookLadiDataContext** _(bổ sung Pha 2)_ — log toàn bộ request nhận từ landing (DbContext riêng của service Webhook_Ladi); contact thực tế tạo ở service log-contact sau khi consume RabbitMQ. Không có entity "Lead" riêng (lead = Contact).

## Luồng code
- **Lead search/count/export:** `HoSoKhachHangController` → `IHoSoKhachHangService`. RoleCode (`SALE_AND_MKT` cho Search, `DV_MKT` cho Count) **gán cứng trong controller** rồi truyền xuống service. Export build file qua GemBox → `DanhSachContact.xlsx`.
- **Dashboard:** `MarketingDashboardController` → `MarketingDashboardService.GetUserActivitiesLog` (EF, lọc đơn vị/khoảng ngày/loại hoạt động) + `GetDMActivityLog` (combo lọc).
- **Seeding:** `SeedingInfoController` → `SeedingInfoService` (EF CRUD theo đơn vị); `CheckPhoneNumberSeeding` được luồng tạo contact / chia số gọi để loại số seeding.
- **Lead intake tự động** _(bổ sung Pha 2)_: `DataReceiveController.ReceiveData*` (anonymous) → log full request (`WebhookLadiDataContext` / `WriteLogContact`) → parse theo content-type → `IDataReceiveService.SendDataToLogAndWebhook` đẩy RabbitMQ → **service log-contact** consume → tạo/ghép contact SCRM (= lead). Retry: service log gọi `ResendDataLogAsync` → `ReceiveDataWoocommerceAsync` / `ReceiveDataAsync` dựng lại data. Đây là **upstream** của `HoSoKhachHangController` (báo cáo lead chỉ đọc lại contact đã tạo).

## Bug-prone / lưu ý
- **`HoSoKhachHangController.cs:34-40`** — endpoint `GetHoSoKhachHang_LeadMKT_TrungSo_Search` (lấy **danh sách** trùng số) bị **comment toàn bộ**; chỉ còn Count. UI lấy danh sách trùng số qua `ContactController.GetDanhSachTrungSo` (cùng service). Đừng tin endpoint trùng-số ở controller này còn trả list.
- **`HoSoKhachHangController.cs:56-89`** (Export) — chỉ `[SBAuthorize()]` không mã quyền → ai login đều xuất được Excel lead; ngoài ra trả `BadRequest(message)` khi lỗi (lệch convention `ResponseResult`).
- **`HoSoKhachHangController.cs:30` & `:47`** — RoleCode (`SALE_AND_MKT` / `DV_MKT`) gán cứng trong controller → đổi quyền phải sửa code, không cấu hình được.
- **`MarketingDashboardController.cs:17-18`** (service Contact) — DI khai báo `IDataReceiveService`, `IContactStoreService` nhưng **không inject qua constructor và không dùng** → field thừa, dễ nhầm.
- 2 method chart cũ (`TimTheoDieuKien`, `GetStatisticsLast7DayOfLanding`) ở MKT-dashboard service Contact đã bị **comment** → biểu đồ landing/thống kê đã chuyển sang Report-service (xem feature Báo cáo Marketing) / component mới `marketing-dashboard-20250609`. Dễ grep nhầm.
- **`SeedingInfoController.cs:83`** — `CheckPhoneNumberSeeding` key permission gốc đã comment → chỉ còn `[SBAuthorize]` (login), lệch mức quyền so với các action CRUD seeding.

**Lead intake tự động (service Webhook_Ladi)** _(bổ sung Pha 2)_:
- **`DataReceiveController.cs:47-126`** — `ReceiveDataAsync` **anonymous, không verify chữ ký/token nguồn**: ai biết `lid`/`sc` đều POST được lead giả vào hệ thống (spam contact, bơm lead rác, nhiễu báo cáo MKT). Cần secret theo `lid` / signature / IP allowlist LadiPage.
- **`DataReceiveController.cs:120-125`** — catch ở action POST **nuốt lỗi và trả Ok** → khi xử lý hỏng mất dấu vết (chỉ còn log DB), landing vẫn nhận thành công dù lead không vào.
- **`DataReceiveController.cs:420-482`** — `ResendDataLogAsync` anonymous nhưng là endpoint **service→service** → nên giới hạn internal/IP.
- **`DataReceiveController.cs:33-37`** — inject `WebhookLadiDataContext` chỉ để `WriteLogContact`/`SaveChangesAsync`.
- Nhiều private parser trùng logic (Json/Multipart/FormUrlEncoded/QueryString) — cồng kềnh nhưng cần cho đa content-type.
- **`SanPhamLandingController.cs:11-70`** (cùng service Webhook_Ladi) — **toàn bộ class + 5 action CRUD bị comment** → DEAD, không expose endpoint. Đừng tin còn route. Khác `sales/SanPhamLandingController` (service Contact).

## File source (path:line)
- Registry: `projects/scrm/registry/sales/HoSoKhachHangController.md`, `registry/sales/MarketingDashboardController.md`, `registry/sales/SeedingInfoController.md`, `registry/marketing/DataReceiveController_Webhook_Ladi.md`, `registry/marketing/SanPhamLandingController_Webhook_Ladi.md`.
- BE controllers (service Contact): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\HoSoKhachHangController.cs` (`:30`, `:34-40`, `:47`, `:56-89`), `MarketingDashboardController.cs` (`:17-18`), `SeedingInfoController.cs` (`:83`).
- BE controllers (service Webhook_Ladi) _(bổ sung Pha 2)_: `...\DataReceiveController.cs` (`:33-37`, `:47-126`, `:120-125`, `:420-482`), `...\SanPhamLandingController.cs` (`:11-70` — toàn bộ comment/DEAD).
