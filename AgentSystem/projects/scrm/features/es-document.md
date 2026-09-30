# ESDocument — Quản Lý Elasticsearch Indexing

> Quản lý việc đánh index dữ liệu vào Elasticsearch cho các đơn vị chọn dùng ES. Hỗ trợ search nhanh cho Contact, Order, OrderLogistic, Kho, Sản phẩm.

---

## Vai trò

- ← **Contact, Order, OrderLogistic, Warehouse, Product**: gửi event khi có thay đổi dữ liệu
- → **Elasticsearch**: index/reindex documents
- → **LogContact**: gửi `LogDataReceive` message sau khi xử lý

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `DonViElasticSearch` | Cấu hình ES theo đơn vị: `ESOrderLogisticIndexName`, `ESContactIndexName`, `ESKhoSoIndexName`, `ESOrderIndexName` |
| `LogDataES` | Log quá trình indexing: `LogName`, `LogData`, `ErrorMessage` |

**ES Document models:**
- `ContactModel` — khách hàng
- `DonHangModel` — đơn hàng tại quầy
- `DonHangLogisticModel` — đơn logistics
- `KhoSoModel` — kho số
- `SanPhamESModel` / `SanPhamChaESModel` — sản phẩm
- `KhoChiTietESModel` — chi tiết kho

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/DonViElasticSearch/AddOrUpdateDonVi` | Thêm/cập nhật unit dùng ES |
| `POST /api/DonViElasticSearch/TimTheoDieuKien` | Danh sách units dùng ES |
| `POST /api/DonViElasticSearch/UpdatePhuongThucSearch` | Đổi phương thức search (ES / SQL) |
| `POST /api/DonViElasticSearch/ReindexContact` | Reindex toàn bộ contact |
| `POST /api/DonViElasticSearch/ReindexKhoSo` | Reindex kho số |
| `POST /api/DonViElasticSearch/ReindexOrderLogistic` | Reindex đơn logistics |
| `POST /api/DonViElasticSearch/ReindexOrder` | Reindex đơn hàng |
| `POST /api/DonViElasticSearch/ReindexSanPham` | Reindex sản phẩm |
| `POST /api/DonViElasticSearch/ReindexAllByDonVi` | Reindex tất cả cho một đơn vị |
| `POST /api/DonViElasticSearch/DeleteIndexAllByDonVi` | Xóa toàn bộ index của đơn vị |

---

## Luồng

```
[Real-time indexing]
Contact/Order/Warehouse service có thay đổi dữ liệu
  ↓ publish event (MassTransit)
ESContactConsumer / ESDonHangConsumer / ESKhoSoConsumer...
  └→ Đọc dữ liệu từ DB → Index vào Elasticsearch

[Batch reindex]
Admin gọi ReindexAllByDonVi
  ↓
DonViElasticSearchService → publish batch reindex message
  ↓
ESContactBatchConsumer / ESDonHangBatchConsumer...
  └→ Đọc toàn bộ từ DB theo IdDonVi → bulk index vào ES

[Search sử dụng ES]
Contact/Order service → check DonViElasticSearch.IsSuDungES
  ├→ True: query Elasticsearch (nhanh hơn)
  └→ False: query SQL Server (fallback)
```

---

## Đặc điểm quan trọng

- **30+ consumers** xử lý các loại sự kiện khác nhau
- Mỗi đơn vị có index name riêng (multi-tenant ES indexes)
- Hỗ trợ switch phương thức search mà không restart service
- Bulk indexing cho reindex hiệu năng cao

---

## Checklist fix bug

- [ ] Search chậm → kiểm tra `DonViElasticSearch.IsSuDungElasticSearch` đã bật chưa
- [ ] Data ES cũ/sai → chạy `ReindexAllByDonVi` để đánh lại index
- [ ] Consumer không index → xem `LogDataES.ErrorMessage`
- [ ] Index name sai → kiểm tra `DonViElasticSearch.ES*IndexName` fields
