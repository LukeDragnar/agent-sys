---
controller: DonViElasticSearchController
module: he-thong
service: ESDocument
man_hinh_ui: "Hệ thống > Quản lý Elastic search (HT-AD/HT-CSKH) — đăng ký đơn vị dùng ES + nút reindex/xóa index theo bảng + đổi version index (new_)"
muc_dich: "Quản lý đơn vị sử dụng Elasticsearch, phát lệnh đánh lại index (reindex)/xóa index theo từng bảng qua RabbitMQ, và quản lý VERSION index bằng tiền tố \"new_\" (ES Search v2 — mapping đọc từ file JSON)."
alias: ["quản lý elastic search", "reindex ES", "đánh index", "đồng bộ ES", "reindex đơn vị", "index version new_", "ES v2"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonViElasticSearch, EventIndexDocument, EventDeleteIndexDocument, EventCreateIndexName, RequestIndexVersion, ElasticIndexConfig, ElasticFieldConfig]
stored_procs: []
depends: [IDonViElasticSearchService, IPublishEndpoint (RabbitMQ), IElasticSearch (Nest client)]
cross_module_in: ["FE màn Quản lý Elastic search (HT-AD/HT-CSKH) gọi reindex/xóa index/đổi version", "Publisher EventCreateIndexName (đăng ký đơn vị mới) → ESCreateIndexNameConsumer"]
cross_module_out: ["Publish EventIndexDocument/EventDeleteIndexDocument → consumer ESDocument đánh/xóa index trên Elasticsearch (contact, khoso, orderlogistic, order, sanpham, sanphamcha, khochitiet)", "Ghi/khởi tạo index Elasticsearch trực tiếp (CreateOrUpdateIndex*AsyncV2) đọc mapping từ MappingModel/*_mapping.json"]
---

# DonViElasticSearchController — Quản lý Elasticsearch, reindex & versioning index theo đơn vị

> ⚠️ **Snapshot — verify lại với code thật.** Node quét từ `origin/release_product` @ `e617abcbbf`. `path:line` có thể lệch theo commit sau; code là chân lý, node là bản đồ.

`[TienIch("DonViElasticSearchController", "Quản lý Elastic search", TI_HeThong)]`, route `api/DonViElasticSearch`. Hầu hết endpoint dùng `[SBAuthorize]` key (HT-AD/HT-CSKH); 2 endpoint test `[AllowAnonymous]` đã comment lại (Test1/Test2) → controller-level vẫn coi là **mixed**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm/sửa đơn vị dùng ES | Lưu | POST `api/DonViElasticSearch/AddOrUpdateDonVi` | DonViElasticSearch_AddOrUpdateDonVi | internal (HT-AD/HT-CSKH) | — | Đăng ký/cập nhật đơn vị sử dụng ES | EF | — |
| 2 | Danh sách đơn vị dùng ES | Tìm | POST `api/DonViElasticSearch/TimTheoDieuKien` | DonViElasticSearch_TimTheoDieuKien | internal | — | Lọc danh sách theo `DonViElasticSearchFilter` | EF | — |
| 3 | Cập nhật phương thức search | Lưu | POST `api/DonViElasticSearch/UpdatePhuongThucSearch` | DonViElasticSearch_UpdatePhuongThucSearch | internal | — | Đổi phương thức search của đơn vị (truyền CurrentUser) | EF | — |
| 4 | Reindex 1 bảng | Reindex | POST `.../ReindexContact`, `.../ReindexKhoSo`, `.../ReindexOrderLogistic`, `.../ReindexOrder`, `.../ReindexSanPham` | DonViElasticSearch_Reindex{Contact\|KhoSo\|OrderLogistic\|Order\|SanPham} | internal | — | Có `DanhSachDonVi` → publish EventIndexDocument cho từng đơn vị; rỗng → reindex tất cả. ReindexSanPham publish 2 index (sanpham + sanphamcha) | RabbitMQ publish | — |
| 5 | Reindex nhiều bảng theo đơn vị | Reindex | POST `api/DonViElasticSearch/ReindexAllByDonVi` | DonViElasticSearch_ReindexAllByDonVi | internal | — | Theo cờ `IsOrderLogistic/IsContact/IsKhoSo/IsSanPham/IsKhoChiTiet` publish event tương ứng cho danh sách đơn vị | RabbitMQ publish | Bỏ index `order` (đã comment) |
| 6 | Xóa index nhiều bảng | Xóa index | POST `api/DonViElasticSearch/DeleteIndexAllByDonVi` | DonViElasticSearch_DeleteIndexAllByDonVi | internal (HT-AD) | — | Publish EventDeleteIndexDocument theo cờ cho danh sách đơn vị | RabbitMQ publish | — |
| 7 | Reindex toàn bộ đơn vị | Reindex full | POST `api/DonViElasticSearch/ReindexFullDonVi` | DonViElasticSearch_ReindexFullDonVi | internal (HT-AD) | Chỉ SuperUser | Chặn nếu `CurrentUser.IsSuperUser != true`; còn lại reindex orderlogistic+contact+khoso+sanpham+sanphamcha cho tất cả đơn vị | RabbitMQ publish | — |
| 8 🆕 | Đổi version index (bật/tắt `new_`) | Cập nhật version | POST `api/DonViElasticSearch/UpdateIndexVersion` | DonViElasticSearch_UpdateIndexVersion | internal (HT-AD) | — | Set `IsIndexNew` cho các đơn vị (`RequestIndexVersion.ListIds`), chuẩn hóa tiền tố `new_` cho **cả 4** index name (contact/orderlogistic/order/khoso) qua `ApplyNewIndexPrefix`; nếu `IsIndexNew==true` thì tạo/cập nhật index V2 (contact/orderlogistic/khoso) đọc mapping từ JSON; đồng bộ cờ sang `DonViModel.IsIndexNew` + xóa cache index name | EF + Nest (Indices) | Bật `new_` chỉ CreateOrUpdate 3 index — **thiếu `order`** (svc `:580-582`); order chỉ đổi tên tiền tố chứ không tạo index V2 |
| 9 🆕 | Xóa index bản cũ theo đơn vị | Xóa index cũ | POST `api/DonViElasticSearch/DeleteIndexOldVersionByDonVi` | DonViElasticSearch_DeleteIndexOldVersionByDonVi | internal (HT-AD) | Dùng sau khi đã chuyển sang `new_` | Với các đơn vị (`RequestIndexVersion.ListIds`) publish EventDeleteIndexDocument xóa index bản cũ (bỏ tiền tố `new_`) | RabbitMQ publish | **Chỉ xóa index `contact` cũ** (`ESContactIndexName.Replace("new_","")`) — KHÔNG xóa orderlogistic/khoso/order cũ (ctrl `:604-622`) |

## Endpoint nội bộ (private / service-to-service)
- `Test1`, `Test2` (`[AllowAnonymous]`) — **đã comment toàn bộ** (ctrl `:515-591`); `Test2` từng hardcode connection string + password DB donhang → nếu bỏ comment là rủi ro bảo mật, không phải endpoint nghiệp vụ.

## Cơ chế versioning index "new_" (ES Search v2) — delta 🆕
Mục tiêu: chuyển đơn vị sang bộ index thế hệ mới song song với index cũ, rồi xóa index cũ khi ổn định (zero-downtime reindex).

- **Cờ nguồn:** `DonViElasticSearch.IsIndexNew` (`bool?`, entity `DonViElasticSearch.cs:109`) + đồng bộ `DonViModel.IsIndexNew`. `true` = đơn vị đang dùng bộ index `new_*`.
- **Helper chuẩn hóa tên:** `ApplyNewIndexPrefix(indexName, isIndexNew)` (svc `DonViElasticSearchService.cs:601-609`, `private static`, null-safe): `isIndexNew==true` → prepend `"new_"` (idempotent — bỏ qua nếu đã có); ngược lại → `Replace("new_","")`. Áp cho cả 4 index (contact/orderlogistic/order/khoso).
- **Mapping từ file JSON (thay hardcode):** `CreateOrUpdateIndexAsyncV2` (contact, svc `:700`), `CreateOrUpdateIndexDonHangLogisticAsyncV2` (orderlogistic, ~`:809`), `CreateOrUpdateIndexKhoSoAsyncV2` (khoso) đọc `MappingModel/{contact,orderlogistic,khoso}_mapping.json` (thư mục `ESDocument.API/MappingModel/`), deserialize sang `ElasticIndexConfig`/`ElasticFieldConfig` (`ESDocument.DataContext/Models/ElasticFieldConfig.cs`) rồi build Nest `PropertiesDescriptor` theo `field.Type` (keyword/text/boolean/date/number). Index tồn tại → update mapping; chưa có → create.
- **Đăng ký đơn vị mới → dùng ngay index `new_`:** consumer `ESCreateIndexNameConsumer` (`ESDocument.API/Consumer/ESCreateIndexNameConsumer.cs`) nhận `EventCreateIndexName` → gọi `EsCreateIndexName(model)` (svc `:611-698`): nếu đơn vị chưa có bản ghi `DonViElasticSearch` thì insert với `IsIndexNew = true` và toàn bộ index name mang tiền tố `new_`; sau đó với từng index orderlogistic/contact/khoso **kiểm tra tồn tại trên ES**, chưa có mới `CreateOrUpdateIndex*AsyncV2`. Consumer nuốt exception (`catch {}` rỗng) → lỗi khởi tạo im lặng.
- **Hotfix kèm cụm này:** tăng batch đánh index lên 5000/lần; "tạo đơn vị không tự tạo index"; "đăng ký mới dùng index mới".

## Quan hệ
- **Gọi sang:** `IDonViElasticSearchService` (GetAll/GetDonViByIds/AddOrUpdate/UpdatePhuongThucSearch/**UpdateIndexVersion**/**EsCreateIndexName**/**CreateOrUpdateIndex\*AsyncV2**), `IPublishEndpoint` (RabbitMQ), `IElasticSearch` → Nest client (Indices.ExistsAsync/Create/PutMapping).
- **Dữ liệu (entities):** `DonViElasticSearch` (tên index từng bảng: ESContactIndexName, ESKhoSoIndexName, ESOrderLogisticIndexName, ESOrderIndexName + `IsIndexNew`), `EventIndexDocument`, `EventDeleteIndexDocument`, `EventCreateIndexName` (Models.Core.MessageBroker), `RequestIndexVersion` (ListIds + IsIndexNew), `ElasticIndexConfig`/`ElasticFieldConfig` (cấu hình mapping từ JSON).
- **Ripple / đồng bộ:** publish event → consumer đánh/xóa index (background, eventual); ngoài ra svc ghi/khởi tạo index Elasticsearch **trực tiếp** (không qua queue) khi bật version mới.

## Luồng xuyên module
- **Reindex:** HT bấm Reindex → resolve tên index theo đơn vị → publish EventIndexDocument(Action, IndexName, TuNgay/DenNgay) → consumer query DB nguồn → ghi document lên ES.
- **Đăng ký đơn vị mới (v2):** publisher gửi `EventCreateIndexName(IdDonVi, TenDonVi, IndexName*)` → `ESCreateIndexNameConsumer` → `EsCreateIndexName` → insert `DonViElasticSearch(IsIndexNew=true)` + tạo index `new_contact/new_orderlogistic/new_khoso` từ mapping JSON nếu chưa tồn tại.
- **Chuyển version thủ công:** HT gọi `UpdateIndexVersion(ListIds, IsIndexNew=true)` → đổi tên 4 index sang `new_` + tạo/cập nhật 3 index V2 → (chạy reindex) → `DeleteIndexOldVersionByDonVi` xóa index cũ.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonViElasticSearchService.cs:580-582` — bật `IsIndexNew==true` chỉ CreateOrUpdate contact/orderlogistic/khoso, **không tạo index `order` V2** dù vẫn đổi tên `new_order_*`.
- `DonViElasticSearchController.cs:604-622` — `DeleteIndexOldVersionByDonVi` **chỉ publish xóa index `contact` cũ**, bỏ sót orderlogistic/khoso/order → index cũ 3 bảng còn tồn dư.
- `ESCreateIndexNameConsumer.cs:25-27` — `catch (Exception) {}` rỗng: lỗi khởi tạo index đơn vị mới bị nuốt, không log.
- `EsCreateIndexName` mặc định `IndexName* ?? 1` (svc `:613-615`) — thiếu version trong event thì rơi về version 1.
- `DonViElasticSearchController.cs:515-591` — Test1/Test2 đã comment; Test2 từng hardcode connection string/password DB donhang — không khôi phục.
- `ReindexOrder` (`:188-228`) còn route nhưng index `order` đã comment ở ReindexAllByDonVi/ReindexFullDonVi (không đồng bộ).

## Ghi chú entity/field mới (delta — thuộc index model, ngoài endpoint controller này)
> Không phải endpoint mới; là cột index bổ sung trên các model được ESDocument đọc lên (SqlSugar tự SELECT, AutoMapper map sang *ES). Ghi ở đây để không mất tri thức; xem thêm `domain.md`/feature liên quan.
- **Yêu cầu xuất hóa đơn:** `DonHangModel.IsYeuCauXuatHoaDon` (`bool?`, `DonHangModel.cs:244`, đơn offline/POS) và `DonHangLogisticModel.IsYeuCauXuatHoaDon` (`bool?`, `DonHangLogisticModel.cs:614`, đơn logistic). `null` = đơn cũ. Index để hiển thị/lọc yêu cầu xuất HĐ ở list thu ngân. DTO `TimTheoDieuKienResponse` + SP `PRC_SCRM_Order_SearchDanhSachDon` nằm bên module Order (ngoài scope ESDocument).
- **KPI cá nhân DMS:** `DonHangLogisticModel.IdCuaHangDMS` (`:25`), `IdTuyenDMS` (`:29`), `IdKhoDMS` (`:33`) — `Guid?`, cột cùng tên bảng DonHangLogistic; index để tính KPI theo điểm bán/tuyến/kho DMS.
- **Nguồn quảng cáo Facebook:** `ContactModel.AdId` (`string?`, `ContactModel.cs:674`) — id quảng cáo FB khi contact đến từ comment/tin nhắn bài quảng cáo; `DonHangLogisticModel.AdId` (`:795`) cũng có trường tương ứng.
