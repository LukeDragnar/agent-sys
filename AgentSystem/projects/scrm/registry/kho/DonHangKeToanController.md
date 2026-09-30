---
controller: DonHangKeToanController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng (gọi nội bộ khi đồng bộ đơn online sang kế toán; route trùng base api/DonHangLogisticController)"
muc_dich: "Đồng bộ 1 đơn hàng logistic online sang kế toán: tự sinh PhieuBanHang (chứng từ bán hàng) bên Accounting khi trạng thái giao hàng của đơn đạt ngưỡng cấu hình (DaDang/DaLayHang/DaGiaoHang), build chi tiết SP + chiết khấu + VAT + chuyển nợ COD/sàn."
alias: ["đồng bộ kế toán đơn hàng", "tạo phiếu bán hàng tự động từ đơn online", "phiếu chờ đơn hàng online", "sync donhang sang accounting", "DonHangKeToan"]
auth: internal
state_machine: []
deprecated: false
entities: [DonHangLogistic, DonHangLogisticSanPham, DonHangLogisticCombo, DonHangLogisticSanPhamLoDate, DonHangLogisticChiTietTMDT]
stored_procs: []
depends:
  - "IDonHangKeToanService (OrderLogistic.Service/Generated/DonHang/DonHangKeToanService.cs)"
  - "IAccountingService (GetCauHinhAccounting + PhieuBanHang_SaveTuDongChoDonHang)"
  - "IWarehouseProductService (PhieuXuat_GetByIdDonHang — reference phiếu xuất cho chi tiết PBH)"
  - "ICommonService / ICauHinhService / IUserPrincipalService"
cross_module_in:
  - "DonHangLogisticController / consumer đơn online — gọi DonHangKeToan_PhieuChoDonHangOnline_Save khi đổi trạng thái giao hàng"
cross_module_out:
  - "Accounting/PhieuBanHang (PhieuBanHang_SaveTuDongChoDonHang → tạo chứng từ bán hàng + công nợ KH + chuyển nợ COD cho NCC/sàn)"
  - "WarehouseProduct/PhieuXuat (đọc PhieuXuat_GetByIdDonHang để reference)"
---

# DonHangKeToanController — Đồng bộ đơn hàng online sang kế toán

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo/đồng bộ phiếu bán hàng kế toán cho đơn online | (gọi nội bộ — không có nút riêng) | GET `api/DonHangKeToan/DonHangKeToan_PhieuChoDonHangOnline_Save?idDonHang=` | `[SBAuthorize()]` (mở — chỉ cần login) | internal | đơn online đạt trạng thái giao hàng theo cấu hình | Load đơn theo id → check cấu hình kế toán → nếu đủ điều kiện trạng thái → build `ReqPhieuBanHang_Save` (header + chi tiết SP + chiết khấu + VAT + COD/chuyển nợ) → gọi Accounting tạo PhieuBanHang. Không đủ điều kiện → trả message (không lỗi) | EF + Accounting | Verb GET nhưng có side-effect tạo chứng từ kế toán (non-idempotent) — bất thường REST |

## Endpoint nội bộ (private / service-to-service)
- Service `IDonHangKeToanService` có 2 overload: `DonHangKeToan_PhieuChoDonHangOnline_Save(DonHangLogistic, Guid? userId)` (gọi từ luồng đổi trạng thái nội bộ, truyền sẵn entity) và overload `(Guid? idDonHang, UserLogin?)` (controller dùng — tự load entity theo id). Controller chỉ expose overload theo id.
- Endpoint này là điểm "đồng bộ đơn online → kế toán" tương ứng với guard `IsSuDungPhieuXuatNghiepVuKeToan` + `TrangThaiTaoPhieuBanHang` trong cấu hình Accounting.

## Quy tắc nghiệp vụ (gating tạo PhieuBanHang)
1. **Bật kế toán:** `cauHinhKeToan.IsSuDungPhieuXuatNghiepVuKeToan == true`, nếu không → "Không sử dụng kế toán" (không tạo).
2. **Có cấu hình mốc tạo phiếu:** `TrangThaiTaoPhieuBanHang != null`, nếu null → "Không cấu hình tạo phiếu bán hàng".
3. **Gating theo ngưỡng trạng thái giao hàng** (DonHangKeToanService.cs:93-132) — đơn chỉ tạo PBH khi `GiaoHangTrangThaiMa` ĐẠT mốc cấu hình:
   - Cấu hình = **DaDang** → đơn ở DaDang / DaLayHang / DaGiaoHang đều OK.
   - Cấu hình = **DaLayHang** → đơn ở DaLayHang / DaGiaoHang OK.
   - Cấu hình = **DaGiaoHang** → chỉ đơn DaGiaoHang OK.
   - Trạng thái thấp hơn mốc → "Không cấu hình tạo phiếu bán hàng" (skip, không lỗi).
4. **Loại bán hàng theo nguồn đơn:** GiaoHangDoiTacMa==GiaoHangThuCong → BanHangOnline "giao thủ công"; EcommerceId>0 → BanHangSanTMDT; còn lại (GHKT/JNT/VTP) → BanHangOnline "qua ĐVVC".
5. **TongTienThu (doanh thu) khác nguồn:**
   - Ecomm (EcommerceId>0): `TienSP − ChietKhauTong + VATSauPhanBo` (GỒM phần sàn trả hộ — vì PBH ghi nhận full doanh thu, phần sàn thành công nợ NCC). KHÔNG dùng DonHangTienCuoi vì đã trừ TMDTSanHoTroTong.
   - Non-ecomm: `DonHangTienCuoi` (đã gồm ship + km).
6. **Tình trạng thanh toán:** TongTienThu − TienDatCoc ≥ 0 → CongNo; ngược lại → DaThanhToan.
7. **Chuyển nợ COD/sàn:** đơn ecomm → IsChuyenNo + MaDoiTuongThuCod = `EcommerceId_TMDTIdGianHang`; cờ IsChuyenNo / IsGiamDoanhThuPhiGiaoHang AND thêm `isTaoChungTuBuTru` / `isTaoChungTuGiamDoanhThu`.

## Quan hệ
- **Gọi sang:** `accountingService.GetCauHinhAccounting` (đọc cấu hình), `accountingService.PhieuBanHang_SaveTuDongChoDonHang` (persist PBH, DonHangKeToanService.cs:691), `warehouseProductService.PhieuXuat_GetByIdDonHang` (reference phiếu xuất loại 2), `commonService.GetDonHangSanPhamByIdsDonHang` / `GetDonHangComboByIdsDonHang` / `LayDungTichListSanPham`.
- **Dữ liệu (entities):** `DonHangLogistic` (nguồn header), `DonHangLogisticSanPham` (line → PhieuBanHangSanPham), `DonHangLogisticCombo` (gán TenCombo), `DonHangLogisticSanPhamLoDate`, `DonHangLogisticChiTietTMDT`.
- **Ripple / đồng bộ:** tạo `PhieuBanHang` bên Accounting → công nợ khách hàng (131), chuyển nợ COD sang NCC/sàn, ghi nhận doanh thu. Không tự cộng/trừ tồn (tồn do PhieuXuat lo).

## Luồng xuyên module (đồng bộ đơn online → kế toán)
```
Đổi trạng thái giao hàng đơn (DaDang/DaLayHang/DaGiaoHang)
  → trigger DonHangKeToan_PhieuChoDonHangOnline_Save(idDonHang)
  → GetCauHinhAccounting: check IsSuDungPhieuXuatNghiepVuKeToan + TrangThaiTaoPhieuBanHang
  → so trạng thái đơn với mốc cấu hình (gating bước 3)
  → build ReqPhieuBanHang_Save: header KH/vãng lai + TongTienThu (công thức theo ecomm/non-ecomm)
       + chi tiết SP (chiết khấu, VAT, làm tròn) + COD chuyển nợ
  → accountingService.PhieuBanHang_SaveTuDongChoDonHang → tạo chứng từ bán hàng
```
Triage "đơn online không lên kế toán": (1) cấu hình IsSuDungPhieuXuatNghiepVuKeToan; (2) TrangThaiTaoPhieuBanHang vs GiaoHangTrangThaiMa đơn; (3) doanh thu lệch → kiểm công thức TongTienThu ecomm vs non-ecomm.

## Lỗi đã biết / lưu ý kỹ thuật
- **Verb GET có side-effect tạo chứng từ** (`[HttpGet("DonHangKeToan_PhieuChoDonHangOnline_Save")]`, DonHangKeToanController.cs:35) — non-idempotent, dễ tạo trùng nếu gọi lại; cần guard idempotency ở Accounting.
- Khách không có mã (`KhachHangMa` rỗng) → fallback "KH-VANG-LAI-000" / "Khách vãng lai" (DonHangKeToanService.cs:180).
- Công thức `TongTienThu` ecomm GỒM phần sàn trả hộ (DonHangKeToanService.cs:201-207) — KHÁC DonHangTienCuoi; sửa nhầm sẽ lệch doanh thu HĐĐT.
- Controller `[TienIch("DonHangLogisticController", ...)]` dùng tên tiện ích trùng DonHangLogisticController (cùng route base `api/[controller]` nhưng controller name là DonHangKeToan → route thật `api/DonHangKeToan/...`). Lưu ý không nhầm với DonHangLogisticController.
- Nhiều block chi tiết PBH (MapPhieuBanHangSanPham, phương thức thanh toán) đang **comment** trong service — chi tiết SP build inline ở vòng foreach lstDonHangLogisticSanPham (DonHangKeToanService.cs:343+), combo/dịch vụ phần lớn chưa đẩy.
