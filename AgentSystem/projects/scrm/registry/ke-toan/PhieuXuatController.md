---
controller: PhieuXuatController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > màn 'Phiếu xuất kho' (phía kế toán) — list chứng từ phiếu xuất đã đồng bộ từ kho/CRM sang, cột trạng thái đồng bộ đối tác (MISA). Nút: Tìm kiếm, Export Excel. KHÔNG có nút tạo/sửa/hoàn thành (chứng từ chỉ đến qua đồng bộ)."
muc_dich: "Phía KẾ TOÁN của phiếu xuất kho: chỉ READ — xem/lọc + xuất Excel chứng từ phiếu xuất đã được đồng bộ từ kho (CRM) sang service Accounting. Việc TẠO bản ghi PhieuXuat + sinh bút toán nằm ở consumer DongBoPhieuCrm, KHÔNG ở controller này."
alias: ["phiếu xuất kế toán", "chứng từ xuất kho kế toán", "đồng bộ phiếu xuất sang kế toán", "phiếu xuất misa", "xuất excel phiếu xuất kế toán"]
auth: internal
state_machine: []
deprecated: false
entities: [PhieuXuat, PhieuXuatSanPham, DataReceiveTaoPhieu, ButToan]
stored_procs: []
depends: [GemBox.Spreadsheet (export), ButToanService (sinh bút toán — chạy ở consumer)]
cross_module_in: [WarehouseProduct/PhieuXuat (CRM bắn message đồng bộ), Order/Logistic (xuất bán/xuất hủy)]
cross_module_out: [MISA (đẩy chứng từ qua SendDataPartner), ButToan (hạch toán giá vốn xuất kho)]
---

# PhieuXuatController — Phiếu xuất kho (phía Kế toán, chứng từ đồng bộ từ kho)

> **CHỈ READ.** Controller này (service **Accounting**) chỉ có 3 endpoint: tìm theo điều kiện, tìm theo id, export Excel — đối xứng hệt `PhieuNhapController`. Bản ghi `PhieuXuat` được TẠO qua **`DongBoPhieuCrmConsumer` → `DongBoPhieuCrmService.XuLyPhieuXuat`** khi CRM/kho bắn `DongBoPhieuCrmMessage1` (Type=`PhieuXuat`). Đây là CHỨNG TỪ KẾ TOÁN phản chiếu phiếu xuất bên kho để hạch toán giá vốn + đẩy MISA. **Phân biệt với `WarehouseProduct/.../PhieuXuatController.cs`** — bản kho mới trừ tồn + sinh phiếu; bản kế toán này chỉ nhận chứng từ về.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + lọc (chi nhánh, ngày, keyword, trạng thái đồng bộ) | `POST api/PhieuXuat/TimTheoDieuKien` | `[SBAuthorize()]` (any login) | internal | màn phiếu xuất KT | Lọc theo `IdDonVi`; trả grid phiếu xuất + trạng thái đồng bộ đối tác. (Phân quyền chi nhánh giống cụm phiếu — *cần xác nhận chi tiết filter trong service*.) | **EF** `PhieuXuat` | — |
| 2 | Xem chi tiết | (mở 1 phiếu) | `GET api/PhieuXuat/TimTheoId/{id:Guid}` | `[SBAuthorize()]` | internal | click 1 dòng | Trả full phiếu xuất + `PhieuXuatSanPham`. Lọc theo `IdDonVi`. | **EF** `PhieuXuat` + `PhieuXuatSanPham` | — |
| 3 | Xuất Excel | "Export" → file `PhieuXuatKho.xlsx` | `POST api/PhieuXuat/ExportPhieuXuatKho` | `[SBAuthorize()]` | internal | nút export | Sinh Excel danh sách phiếu xuất; `null` → 404 "Không có dữ liệu để xuất". | **EF** + GemBox template | — |

## Endpoint nội bộ (private / service-to-service)
- Controller KHÔNG có endpoint private. Luồng TẠO chứng từ đi qua **MassTransit consumer**:
  - [internal] WarehouseProduct/Order (CRM) → publish `DongBoPhieuCrmMessage1{Type=PhieuXuat}` → `DongBoPhieuCrmConsumer` → `XuLyPhieuXuat` lưu `PhieuXuat`+`PhieuXuatSanPham` + `ButToan_PhieuXuat_Save`.
- [internal] SendDataPartner đẩy chứng từ phiếu xuất sang MISA (`TypePhieu=PhieuXuatKho`).

## Quan hệ
- **Gọi sang:** GemBox (export); MISA (qua danh mục sync trong consumer).
- **Dữ liệu (entities):** đọc `PhieuXuat`, `PhieuXuatSanPham`; `DataReceiveTaoPhieu` (hàng đợi sync); `ButToan` (consumer sinh).
- **Ripple / đồng bộ:** controller không bắn event. Consumer: tạo phiếu → đồng bộ danh mục MISA (đối tượng/kho `IdTuKho`/sản phẩm) → sinh bút toán giá vốn xuất kho.

## Luồng xuyên module (đồng bộ phiếu xuất kho → kế toán)
```
[WarehouseProduct] PhieuXuat hoàn thành (trừ tồn bên kho)
  → CRM publish DongBoPhieuCrmMessage1 { Type=PhieuXuat, Data=PhieuXuatModel, IdTransaction }
  → [Accounting] DongBoPhieuCrmConsumer → XuLyDuLieuCrmAsync
       → ghi DataReceiveTaoPhieu (nếu đơn vị có cấu hình kết nối kế toán)
       → background ProcessSavePhieuCrmDb → XuLyPhieuXuat:
            • chống trùng theo (IdDonVi, IdTransaction, TypeDoiTac)
            • insert PhieuXuat + PhieuXuatSanPham (transaction)
            • CheckDongBoDoiTuong + CheckDongBoKho(IdTuKho) + CheckDongBoSanPham → MISA
            • ButToan_PhieuXuat_Save → bút toán giá vốn xuất kho
  → controller PhieuXuat (node này) chỉ để xem/lọc/export + theo dõi đồng bộ
```

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuXuatController.cs` — 3 endpoint `[SBAuthorize()]` rỗng, READ-only (không Create/Update/Delete).
- BE `Accounting/Accounting.Service/Generated/DongBoPhieuCrmService.cs:800-911` (`XuLyPhieuXuat`) — nơi TẠO chứng từ thật; chống trùng theo IdTransaction (L818); sinh bút toán L904-907.
- **Trùng tên với kho:** `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuXuatController.cs` là controller khác (trừ tồn + sinh phiếu); node này chỉ phía kế toán.
- Trace "thiếu chứng từ xuất / không hạch toán": đọc `XuLyPhieuXuat` + cấu hình `CauHinhKetNoiKeToan`, không phải controller này.
