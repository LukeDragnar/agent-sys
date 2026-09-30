# Dev — Cấu hình Elasticsearch

> Service: **ESDocument** (BE .NET) · controller `DonViElasticSearchController`, route `api/DonViElasticSearch`. Reindex/xóa index chạy **nền qua RabbitMQ** (consumer ESDocument). Chỉ dành cho dev.

## Màn hình & route (FE)
- Màn nội bộ: **Hệ thống → Quản lý Elastic search** (HT-AD/HT-CSKH) — đăng ký đơn vị dùng ES + các nút Reindex/Xóa index theo bảng. Route FE cụ thể *cần xác nhận trong scrm_frontend* (chưa trace trong registry node).

## Endpoint / Controller (trace registry)
`DonViElasticSearchController` — `[TienIch("DonViElasticSearchController", "Quản lý Elastic search", TI_HeThong)]`, route `api/DonViElasticSearch`. Hầu hết endpoint `[SBAuthorize]` (HT-AD/HT-CSKH); 2 endpoint test `[AllowAnonymous]` → auth **mixed**.

| MaTinhNang (key) | HTTP path | Mục đích |
|---|---|---|
| `DonViElasticSearch_AddOrUpdateDonVi` | POST `api/DonViElasticSearch/AddOrUpdateDonVi` | Thêm/sửa đơn vị dùng ES (EF) |
| `DonViElasticSearch_TimTheoDieuKien` | POST `.../TimTheoDieuKien` | Danh sách đơn vị dùng ES (filter `DonViElasticSearchFilter`) |
| `DonViElasticSearch_UpdatePhuongThucSearch` | POST `.../UpdatePhuongThucSearch` | Đổi phương thức search của đơn vị |
| `DonViElasticSearch_Reindex{Contact\|KhoSo\|OrderLogistic\|Order\|SanPham}` | POST `.../ReindexContact` … `/ReindexSanPham` | Reindex 1 bảng. Có `DanhSachDonVi` → publish theo đơn vị; rỗng → tất cả đơn vị. ReindexSanPham publish 2 index (sanpham + sanphamcha) |
| `DonViElasticSearch_ReindexAllByDonVi` | POST `.../ReindexAllByDonVi` | Reindex nhiều bảng theo cờ `IsOrderLogistic/IsContact/IsKhoSo/IsSanPham/IsKhoChiTiet` |
| `DonViElasticSearch_DeleteIndexAllByDonVi` | POST `.../DeleteIndexAllByDonVi` | Xóa index nhiều bảng theo cờ (HT-AD) |
| `DonViElasticSearch_ReindexFullDonVi` | POST `.../ReindexFullDonVi` | Reindex toàn bộ — **chặn nếu `CurrentUser.IsSuperUser != true`** |
| (anonymous) | POST `.../Test1`, `.../Test2` | Endpoint test — **không phải nghiệp vụ** |

## Entities / message
- `DonViElasticSearch` — cấu hình tên index từng bảng: `ESContactIndexName`, `ESKhoSoIndexName`, `ESOrderLogisticIndexName`, `ESOrderIndexName` (+ sản phẩm).
- `EventIndexDocument` (action, indexName, TuNgay/DenNgay) — message reindex.
- `EventDeleteIndexDocument` — message xóa index.

## Luồng code
1. Controller resolve tên index theo đơn vị (qua `IDonViElasticSearchService.GetDonViByIds`/`GetAll`).
2. Publish `EventIndexDocument`/`EventDeleteIndexDocument` qua `IPublishEndpoint` (RabbitMQ) — 1 event/đơn vị/loại.
3. Consumer **ESDocument** nhận event → query DB nguồn (Contact/KhoSo/OrderLogistic/SanPham) → ghi/xóa document trên Elasticsearch (background, eventual).
- `Reindex*` rỗng `DanhSachDonVi` → reindex tất cả đơn vị; `ReindexSanPham` → publish 2 index.

## Bug-prone / lưu ý (path:line)
- **`DonViElasticSearchController.cs:571`** — `Test2` (`[AllowAnonymous]`) **hardcode connection string + password DB donhang** → rủi ro bảo mật, nên xóa. (Security session xử lý sau, **không sửa ở đây.**)
- **`DonViElasticSearchController.cs:188-228`** — `ReindexOrder` còn route nhưng index `order` đã bị **comment** ở `ReindexAllByDonVi`/`ReindexFullDonVi` → không đồng bộ: gói "reindex all" có thể bỏ qua đơn hàng. *Cần product/kỹ thuật xác nhận hành vi mong muốn.*
- **Reindex không khử trùng**: bấm nhiều lần → nhiều event vào queue, tốn tài nguyên (không phá dữ liệu).
- **`ReindexFullDonVi`** gate bằng `CurrentUser.IsSuperUser` — đừng nới quyền này khi refactor.

## File source (path:line)
- BE controller: `DonViElasticSearchController.cs` (`Test2` hardcode connstr `:571`; `ReindexOrder` `:188-228`). Đường dẫn gốc theo BE `D:\PROJECT\Git\_SourceForCode\Group2\scrm` — service ESDocument (*đường dẫn file đầy đủ cần trace trong source khi cần chi tiết line*).
- Entities/message: `DonViElasticSearch`, `EventIndexDocument`, `EventDeleteIndexDocument` (service ESDocument).
- Registry node: `projects/scrm/registry/he-thong/DonViElasticSearchController.md`.
