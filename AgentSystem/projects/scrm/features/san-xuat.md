# SanXuat — Sản Xuất / Manufacturing

> Quản lý quy trình sản xuất: tạo lệnh sản xuất từ đơn hàng, định nghĩa BOM (Bill of Materials), theo dõi công đoạn, quản lý xuất hàng.

---

## Vai trò

- ← **Order**: tạo SaleOrder → trigger lệnh sản xuất
- → **WarehouseProduct**: lấy thông tin sản phẩm, nguyên vật liệu
- → **Report**: cung cấp dữ liệu sản xuất cho báo cáo
- → **Core**: lấy thông tin đơn vị, chi nhánh

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `ProductionOrder` | Lệnh sản xuất: `Ma`, `Ten`, `SoLuongYeuCau`, `SoLuongSanXuat`, `HanXuLy`, `IsTachDon`, `IdSanPham` |
| `WorkOrder` | Lệnh công việc: `Ma`, `TenSanPham`, `SoLuong`, `HanXuLy`, `TrangThai`, `IsPlan`, `ThuTu`, `BuocIndex`, `NgayBatDau`, `NgayHoanThanh` |
| `WorkOrderChiTiet` | Chi tiết bước công việc |
| `WorkOrderStep` / `WorkOrderStepProduct` | Công đoạn và vật liệu của từng bước |
| `SaleOrderDonHang` | Đơn hàng bán: `Ma`, `TongSoLuong`, `TrangThaiPR`, `HanXuLyDuKien`, `NgayDatHang` |
| `SaleOrderDonHangChiTiet` | Chi tiết dòng hàng của đơn |
| `SaleOrderProduction` | Liên kết đơn hàng → lệnh sản xuất |
| `SaleOrderShipment` | Lô xuất hàng từ sản xuất |
| `BomSetup` | BOM: `Ma`, `Ten`, `IdSanPham`, `KichThuoc` |
| `BomProduct` / `BomProductBatchSize` | Nguyên vật liệu trong BOM theo lô |
| `BomStep` | Công đoạn sản xuất trong BOM |
| `DmCongDoan` | Danh mục công đoạn: quy trình sản xuất chuẩn |
| `SanPhamLock` | Khoá sản phẩm trong quá trình sản xuất |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| CRUD `/api/ProductionOrder/...` | Quản lý lệnh sản xuất |
| CRUD `/api/WorkOrder/...` | Quản lý lệnh công việc |
| CRUD `/api/SaleOrderDonHang/...` | Quản lý đơn hàng bán |
| `POST /SaleOrderDonHang/TimTheoDieuKien` | Tìm đơn hàng |
| CRUD `/api/SaleOrderProduction/...` | Liên kết đơn → sản xuất |
| CRUD `/api/SaleOrderShipment/...` | Quản lý xuất hàng |
| CRUD `/api/BomSetup/...` | Quản lý BOM |
| CRUD `/api/DmCongDoan/...` | Danh mục công đoạn |
| `POST /InPhieu/...` | In phiếu sản xuất |

---

## Luồng

```
[Từ đơn hàng đến sản xuất]
SaleOrder được tạo (từ Order service hoặc trực tiếp)
  ↓
SaleOrderProduction liên kết → tạo ProductionOrder
  ├→ IsTachDon=true: tách thành nhiều lệnh nhỏ
  └→ Gán HanXuLy theo HanXuLyDuKien của SaleOrder

[Lập kế hoạch công việc]
ProductionOrder → tạo WorkOrder theo BomSetup
  ├→ BomStep → WorkOrderStep (các công đoạn)
  ├→ BomProduct → vật liệu cần chuẩn bị
  └→ Xếp ThuTu, BuocIndex cho từng bước

[Thực hiện & theo dõi]
WorkOrder.TrangThai: Chờ → Đang làm → Hoàn thành
  ├→ Cập nhật NgayBatDau, NgayHoanThanh thực tế
  └→ So sánh với HanXuLy (deadline)

[Xuất hàng]
SaleOrderShipment → lô xuất từng phần hoặc toàn bộ
  └→ Cập nhật SaleOrderDonHang.TrangThaiPR
```

---

## Đặc điểm quan trọng

- **GenCodeService**: tự động sinh mã `Ma` cho ProductionOrder, WorkOrder, SaleOrder
- **BomSetup theo KichThuoc**: cùng sản phẩm nhưng kích thước khác có BOM khác nhau
- **BomProductBatchSize**: BOM có thể thay đổi tỷ lệ vật liệu theo số lượng lô
- **SanPhamLock**: khoá sản phẩm tránh thay đổi trong khi sản xuất
- **IsPlan**: WorkOrder có thể là kế hoạch (chưa thực hiện) hoặc đã thực hiện

---

## Checklist fix bug

- [ ] Lệnh sản xuất không tạo → kiểm tra `SaleOrderProduction` liên kết, `BomSetup` có tồn tại
- [ ] Vật liệu sai → kiểm tra `BomProduct` theo `BomSetup.IdSanPham + KichThuoc`
- [ ] WorkOrder không đúng thứ tự → kiểm tra `ThuTu`, `BuocIndex` trong `WorkOrderStep`
- [ ] Xuất hàng trùng → kiểm tra `SaleOrderShipment`, trạng thái `TrangThaiPR`
