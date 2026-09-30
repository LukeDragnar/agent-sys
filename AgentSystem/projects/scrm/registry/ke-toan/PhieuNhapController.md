---
controller: PhieuNhapController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > màn 'Phiếu nhập kho' (phía kế toán) — list chứng từ phiếu nhập đã đồng bộ từ kho/CRM sang, cột trạng thái đồng bộ đối tác (MISA) + trạng thái sinh bút toán. Nút: Tìm kiếm, Export Excel (mẫu nhập kế toán). KHÔNG có nút tạo/sửa/hoàn thành phiếu (chứng từ chỉ đến qua đồng bộ)."
muc_dich: "Phía KẾ TOÁN của phiếu nhập kho: chỉ READ — xem/lọc + xuất Excel chứng từ phiếu nhập đã được đồng bộ từ kho (CRM) sang service Accounting. Việc TẠO bản ghi PhieuNhap + sinh bút toán nằm ở consumer DongBoPhieuCrm, KHÔNG ở controller này."
alias: ["phiếu nhập kế toán", "chứng từ nhập kho kế toán", "đồng bộ phiếu nhập sang kế toán", "phiếu nhập misa", "xuất excel phiếu nhập kế toán"]
auth: internal
state_machine: []
deprecated: false
entities: [PhieuNhap, PhieuNhapSanPham, DataReceiveTaoPhieu, ButToan]
stored_procs: []
depends: [WarehouseProduct/Common (DropdownSanPham, DropdownDsKho), GemBox.Spreadsheet (export), MongoDB (PhieuNhapInfo), ButToanService (sinh bút toán — chạy ở consumer)]
cross_module_in: [WarehouseProduct/PhieuNhap (CRM bắn message đồng bộ), Order/Logistic (phiếu nhập trả lại), PhieuMuaHang]
cross_module_out: [MISA (đẩy chứng từ qua SendDataPartner), ButToan (hạch toán công nợ NCC / giá vốn)]
---

# PhieuNhapController — Phiếu nhập kho (phía Kế toán, chứng từ đồng bộ từ kho)

> **CHỈ READ.** Controller này (service **Accounting**) chỉ có 3 endpoint: tìm theo điều kiện, tìm theo id, export Excel. Bản ghi `PhieuNhap` được TẠO ở nơi khác — qua **`DongBoPhieuCrmConsumer` → `DongBoPhieuCrmService.XuLyPhieuNhap`** khi CRM/kho bắn message `DongBoPhieuCrmMessage1` (Type=`PhieuNhap`). Đây là CHỨNG TỪ KẾ TOÁN phản chiếu phiếu nhập bên kho, để hạch toán bút toán + đẩy sang đối tác MISA. **Phân biệt với `WarehouseProduct/.../PhieuNhapController.cs` (cùng tên, service kho)** — bản kho mới là nơi cộng tồn/giá vốn + sinh phiếu; bản kế toán này chỉ nhận chứng từ về để hạch toán & báo cáo.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + lọc (chi nhánh, phân loại, trạng thái đồng bộ, ngày, keyword mã) | `POST api/PhieuNhap/TimTheoDieuKien` | `[SBAuthorize()]` (any login) | internal | màn phiếu nhập KT | Non-admin & không phải admin-nhiều-chi-nhánh → ép `IdChiNhanh = userLogin.IdChiNhanh`. Lọc theo `IdDonVi`, phân loại, keyword (`Ma.Contains`), `TrangThaiDongBo` (ChuaDongBo→statusSendPartner null; còn lại so khớp), khoảng ngày `NgayCapNhat`. Trả grid + cờ `statusSendPartner`/`SinhButToanSuccess`/`SinhButToanMessErr`. | **EF** `PhieuNhap` (TableWithNolock) | Filter `MaTinhTrang` bị comment (L89-92) → không lọc được theo tình trạng phiếu. |
| 2 | Xem chi tiết | (mở 1 phiếu) | `GET api/PhieuNhap/TimTheoId/{id:Guid}` | `[SBAuthorize()]` | internal | click 1 dòng | Trả full phiếu + `ListPhieuNhapSanPham` (giá nhập, SL, TK kho/công nợ/thuế) + enrich `PhieuNhapInfo` từ MongoDB. Lọc theo `IdDonVi`. | **EF** `PhieuNhap` + `PhieuNhapSanPham` + **MongoDB** `PhieuNhapInfo` | — |
| 3 | Xuất Excel | "Export" → file `PhieuNhapKho.xlsx` | `POST api/PhieuNhap/ExportPhieuNhapKho` | `[SBAuthorize()]` | internal | nút export | Load template `ExcelTemplates/phieu_nhap_kho.xlsx`, gọi sang **WarehouseProduct** `Common/DropdownSanPham` + `Common/DropdownDsKho` để map mã SP/kho, đổ từng dòng chi tiết từ dòng 4. Quy đổi ĐVT theo `BangQuyDoiJson` của SP. | **EF** `GetData` (PhieuNhap+ChiTiet) + HTTP WarehouseProduct | Cột mã NV bán hàng / người giao / diễn giải bỏ trống (TODO L382-387). Thiếu template → trả lỗi `CODE_Error`. |

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint private. Luồng TẠO chứng từ đi qua **MassTransit consumer** (không phải HTTP):
  - [internal] WarehouseProduct/Order/PhieuMuaHang (CRM) → publish `DongBoPhieuCrmMessage1{Type=PhieuNhap}` → `DongBoPhieuCrmConsumer.Consume` → `DongBoPhieuCrmService.XuLyPhieuNhap` lưu `PhieuNhap`+`PhieuNhapSanPham` + sinh bút toán `ButToan_PhieuNhap_Save`.
- [internal] SendDataPartner controller (riêng) đẩy chứng từ phiếu nhập đã lưu sang MISA (`TypePhieu=PhieuNhapKho`).

## Quan hệ
- **Gọi sang:** WarehouseProduct `Common/DropdownSanPham`, `Common/DropdownDsKho` (export); MongoDB `PhieuNhapInfo` (chi tiết).
- **Dữ liệu (entities):** đọc `PhieuNhap`, `PhieuNhapSanPham` (chứa TkKho/TkCongNo/TkThue — tài khoản hạch toán); `DataReceiveTaoPhieu` (hàng đợi nhận sync, do consumer ghi); `ButToan` (do consumer sinh).
- **Ripple / đồng bộ:** controller này KHÔNG bắn event. Ripple thực nằm ở consumer: tạo phiếu → đồng bộ danh mục MISA (đối tượng NCC, kho, sản phẩm) → sinh bút toán công nợ NCC/giá vốn.

## Luồng xuyên module (đồng bộ phiếu nhập kho → kế toán)
```
[WarehouseProduct] PhieuNhap hoàn thành (cộng tồn + giá vốn bên kho)
  → CRM publish DongBoPhieuCrmMessage1 { Type=PhieuNhap, Data=PhieuNhapModel, IdTransaction }
  → [Accounting] DongBoPhieuCrmConsumer.Consume → XuLyDuLieuCrmAsync
       → lưu log Mongo + ghi DataReceiveTaoPhieu (chỉ khi đơn vị/chi nhánh CÓ cấu hình kết nối kế toán)
       → enqueue background: ProcessSavePhieuCrmDb → XuLyPhieuNhap:
            • chống trùng theo (IdDonVi, IdTransaction, TypeDoiTac) → đã có thì bỏ qua
            • nếu IdPhieuGoc + MaChungTuGoc∈{HBBTL, MH}: phiếu nhập "ăn theo" trạng thái đồng bộ của phiếu gốc (PartnerSyncByPhieuGoc=true) → KHÔNG tự đẩy partner
            • tính lại TongTienHang/ChietKhau/Thue cho từng SP nếu CRM chưa tính
            • insert PhieuNhap + PhieuNhapSanPham (transaction)
            • CheckDongBoDoiTuong (NCC) + CheckDongBoKho + CheckDongBoSanPham sang MISA
            • ButToan_PhieuNhap_Save → sinh bút toán công nợ NCC / nhập kho
  → controller PhieuNhap (node này) chỉ để KẾ TOÁN xem/lọc/export + theo dõi statusSendPartner
```
Triage "phiếu nhập không lên kế toán": (1) đơn vị có `CauHinhKetNoiKeToan` mặc định không (không có → không ghi DataReceiveTaoPhieu, L228-239); (2) trùng IdTransaction → bị bỏ qua; (3) `SinhButToanMessErr` trên lưới; (4) phiếu sinh từ PMH/HBBTL → đồng bộ theo phiếu gốc.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuNhapController.cs` — cả 3 endpoint chỉ `[SBAuthorize()]` rỗng (chỉ cần đăng nhập, không gắn permission key). Controller READ-only, không có Create/Update/Delete.
- BE `Accounting/Accounting.Service/Generated/PhieuNhapService.cs:89-92` — đoạn lọc theo `MaTinhTrang` bị comment → không filter được theo tình trạng.
- BE `...PhieuNhapService.cs:382-387` (Export) — nhiều cột để trống (người giao, diễn giải, mã NV bán hàng) đang TODO.
- BE `Accounting/Accounting.Service/Generated/DongBoPhieuCrmService.cs:620-792` (`XuLyPhieuNhap`) — nơi TẠO chứng từ thật. Chống trùng theo IdTransaction (L639). Phiếu từ PMH/HBBTL set `PartnerSyncByPhieuGoc` để không đẩy partner trùng (L645-682).
- **Trùng tên với kho:** `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuNhapController.cs` là controller khác (sinh phiếu + cộng tồn + giá vốn); node này CHỈ là phía kế toán. Đừng nhầm khi sửa logic tồn/giá vốn — đó là bên kho.
- Việc TẠO `PhieuNhap` Accounting hoàn toàn không qua HTTP của controller này → muốn trace bug "thiếu/sai chứng từ nhập" phải đọc consumer + `XuLyPhieuNhap`, không phải controller.
