---
controller: CongViecESController
module: he-thong
service: ESExtended
man_hinh_ui: "nội bộ — không có UI (gọi để reindex domain CongViec lên Elasticsearch)"
muc_dich: "Phát lệnh reindex dữ liệu Công việc (domain CongViec) lên Elasticsearch theo 1/nhiều/tất cả đơn vị đã đăng ký index, qua RabbitMQ."
alias: ["reindex công việc", "index công việc ES", "đánh index CongViec"]
auth: public
state_machine: []
deprecated: false
entities: [DonViIndex, CongViecReindexCommand]
stored_procs: []
depends: [IDonViIndexService, IESCongViecIndexService, IPublishEndpoint (RabbitMQ), IElasticSearch]
cross_module_in: ["Tool/job HT gọi Reindex domain CongViec"]
cross_module_out: ["Publish CongViecReindexCommand → consumer ESExtended đánh index CongViec trên Elasticsearch"]
---

# CongViecESController — Reindex Công việc lên Elasticsearch

Route `api/CongViecES`, không `[TienIch]`/`[SBAuthorize]` → **public**. Endpoint test (PingES/DropIndex/EnsureIndex/RunReindexNow/UpsertOne/DeleteByDonVi) đã comment.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Reindex Công việc | — (không UI) | POST `api/CongViecES/Reindex` | — | public | — | Có `DanhSachDonVi` → reindex các đơn vị đó; rỗng → lấy tất cả đơn vị đăng ký domain CongViec (SuDung=true). Mỗi đơn vị: EnsureRow → publish CongViecReindexCommand(IndexName, TuNgay, DenNgay, RecreateIndex). Trả số đơn vị đã queue | EF (DonViIndex) + RabbitMQ publish | Public, không phân quyền |

## Quan hệ
- **Gọi sang:** `IDonViIndexService` (TimTheoDieuKien/EnsureRow — lookup IndexName theo đơn vị+domain), `IPublishEndpoint` (RabbitMQ). `IESCongViecIndexService`/`IElasticSearch` inject nhưng chỉ dùng ở endpoint test (đã comment).
- **Dữ liệu (entities):** `DonViIndex` (map đơn vị+domain → IndexName), `CongViecReindexCommand` (command).
- **Ripple / đồng bộ:** publish command → consumer ESExtended query DB Công việc → ghi document lên Elasticsearch (background, eventual). `RecreateIndex=true` → drop+tạo lại index.

## Luồng xuyên module
- Gọi Reindex → resolve IndexName từ DonViIndex (EnsureRow tạo nếu chưa có) → publish CongViecReindexCommand → consumer reindex CongViec lên ES theo khoảng TuNgay/DenNgay.

## Lỗi đã biết / lưu ý kỹ thuật
- `CongViecESController.cs:37-72` — endpoint Reindex public, không `[SBAuthorize]`; ai gọi cũng trigger reindex toàn bộ đơn vị.
