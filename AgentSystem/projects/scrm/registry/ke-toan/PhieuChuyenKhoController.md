---
controller: PhieuChuyenKhoController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > màn 'Phiếu chuyển kho' (phía kế toán) — list chứng từ phiếu chuyển kho đã đồng bộ từ kho/CRM sang, cột trạng thái đồng bộ đối tác (MISA). Nút: Tìm kiếm, Export Excel. KHÔNG có nút tạo/sửa."
muc_dich: "Phía KẾ TOÁN của phiếu chuyển kho: chỉ READ — xem/lọc + xuất Excel chứng từ phiếu chuyển kho (luân chuyển nội bộ giữa 2 kho) đã đồng bộ từ kho (CRM). Việc TẠO bản ghi PhieuChuyenKho nằm ở consumer DongBoPhieuCrm; chuyển kho KHÔNG sinh bút toán công nợ (chỉ đồng bộ danh mục kho)."
alias: ["phiếu chuyển kho kế toán", "chứng từ chuyển kho kế toán", "đồng bộ phiếu chuyển kho", "luân chuyển kho misa", "xuất excel phiếu chuyển kho"]
auth: internal
state_machine: []
deprecated: false
entities: [PhieuChuyenKho, PhieuChuyenKhoSanPham, DataReceiveTaoPhieu]
stored_procs: []
depends: [GemBox.Spreadsheet (export)]
cross_module_in: [WarehouseProduct/PhieuChuyenKho (CRM bắn message đồng bộ)]
cross_module_out: [MISA (đẩy chứng từ qua SendDataPartner)]
---

# PhieuChuyenKhoController — Phiếu chuyển kho (phía Kế toán, chứng từ đồng bộ từ kho)

> **CHỈ READ.** 3 endpoint: tìm điều kiện, tìm id, export Excel — đối xứng `PhieuNhap/PhieuXuat`. Bản ghi `PhieuChuyenKho` được TẠO qua **`DongBoPhieuCrmConsumer` → `DongBoPhieuCrmService.XuLyPhieuChuyenKho`** khi CRM/kho bắn `DongBoPhieuCrmMessage1` (Type=`PhieuChuyenKho`). Chuyển kho = luân chuyển hàng giữa 2 kho nội bộ → **KHÔNG sinh bút toán công nợ** (khác nhập/xuất), chỉ lưu chứng từ + đồng bộ danh mục 2 kho (từ kho / đến kho) sang MISA. **Phân biệt với `WarehouseProduct/.../PhieuChuyenKhoController.cs`** — bản kho mới trừ tồn kho nguồn + cộng tồn kho đích; bản kế toán này chỉ nhận chứng từ.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + lọc (ngày, keyword, trạng thái đồng bộ) | `POST api/PhieuChuyenKho/TimTheoDieuKien` | `[SBAuthorize()]` (any login) | internal | màn phiếu chuyển kho KT | Lọc theo `IdDonVi` **và `IdNganhNghe`** (khác PhieuNhap — bản này còn lọc ngành nghề). Trả grid phiếu chuyển kho + trạng thái đồng bộ. | **EF** `PhieuChuyenKho` | Filter `IdChiNhanh` bị comment (L38) → không lọc theo chi nhánh dù DTO có field. |
| 2 | Xem chi tiết | (mở 1 phiếu) | `GET api/PhieuChuyenKho/TimTheoId/{id:Guid}` | `[SBAuthorize()]` | internal | click 1 dòng | Trả full phiếu + `PhieuChuyenKhoSanPham`. | **EF** `PhieuChuyenKho` + SanPham | — |
| 3 | Xuất Excel | "Export" → file `PhieuChuyenKho.xlsx` | `POST api/PhieuChuyenKho/ExportPhieuChuyenKho` | `[SBAuthorize()]` | internal | nút export | Sinh Excel danh sách; `null` → 404. | **EF** + GemBox | — |

## Endpoint nội bộ (private / service-to-service)
- Controller KHÔNG có endpoint private. Luồng TẠO chứng từ qua **MassTransit consumer**:
  - [internal] WarehouseProduct (CRM) → publish `DongBoPhieuCrmMessage1{Type=PhieuChuyenKho}` → `DongBoPhieuCrmConsumer` → `XuLyPhieuChuyenKho` lưu `PhieuChuyenKho`+`PhieuChuyenKhoSanPham`. **KHÔNG gọi ButToan**.
- [internal] SendDataPartner đẩy chứng từ phiếu chuyển kho sang MISA (`TypePhieu=PhieuChuyenKho`).

## Quan hệ
- **Gọi sang:** GemBox (export); MISA (qua danh mục sync trong consumer).
- **Dữ liệu (entities):** đọc `PhieuChuyenKho`, `PhieuChuyenKhoSanPham`; `DataReceiveTaoPhieu` (hàng đợi sync).
- **Ripple / đồng bộ:** controller không bắn event. Consumer: tạo phiếu → đồng bộ danh mục **2 kho** (IdTuKho + IdDenKho) + sản phẩm sang MISA. **Không có bút toán** (luân chuyển nội bộ không phát sinh công nợ/giá vốn ngoài).

## Luồng xuyên module (đồng bộ phiếu chuyển kho → kế toán)
```
[WarehouseProduct] PhieuChuyenKho hoàn thành (trừ tồn kho nguồn + cộng tồn kho đích bên kho)
  → CRM publish DongBoPhieuCrmMessage1 { Type=PhieuChuyenKho, Data=PhieuChuyenKhoModel, IdTransaction }
  → [Accounting] DongBoPhieuCrmConsumer → XuLyDuLieuCrmAsync
       → ghi DataReceiveTaoPhieu (nếu đơn vị có cấu hình kết nối kế toán)
       → background ProcessSavePhieuCrmDb → XuLyPhieuChuyenKho:
            • chống trùng theo (IdDonVi, IdTransaction, TypeDoiTac)
            • insert PhieuChuyenKho + PhieuChuyenKhoSanPham (transaction)
            • CheckDongBoDoiTuong + CheckDongBoKho(IdTuKho) + CheckDongBoKho(IdDenKho) + CheckDongBoSanPham → MISA
            • (KHÔNG sinh bút toán)
  → controller PhieuChuyenKho (node này) chỉ để xem/lọc/export + theo dõi đồng bộ
```

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuChuyenKhoController.cs` — 3 endpoint `[SBAuthorize()]` rỗng, READ-only.
- BE `Accounting/Accounting.Service/Generated/PhieuChuyenKhoService.cs:36-38` — query lọc thêm `IdNganhNghe` (khác PhieuNhap); filter `IdChiNhanh` bị comment → không lọc theo chi nhánh.
- BE `Accounting/Accounting.Service/Generated/DongBoPhieuCrmService.cs:919-1035` (`XuLyPhieuChuyenKho`) — TẠO chứng từ thật; chống trùng IdTransaction (L937); đồng bộ 2 kho L994-1013; **KHÔNG gọi ButToan** (xác nhận: chuyển kho nội bộ không sinh bút toán).
- **Trùng tên với kho:** `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuChuyenKhoController.cs` là controller khác (đụng tồn 2 kho); node này chỉ phía kế toán.
