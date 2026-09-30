---
controller: SanPhamLoDateController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Xem sản phẩm lô date (route view-sanpham-lodate, view-sanpham-lodate.component) — list tồn lô date theo SP/kho. Dropdown lô date còn nhúng ở các màn: tra-lai-hang-mua, sửa phiếu hoàn thành chọn imei/lô date, thu kho tác nghiệp, phiếu xuất."
muc_dich: "Tra cứu & dropdown sản phẩm theo lô date (số lô + hạn sử dụng) trong kho: tìm theo điều kiện, dropdown lô date theo SP/kho/keyword, chi tiết lô date theo lô sản xuất; cấp tồn lô date cho Order/OrderLogistic để trừ kho FEFO khi bán."
alias: ["lô date", "san pham lo date", "tồn lô date", "lot date", "hạn sử dụng", "FEFO", "lô sản xuất", "SanPhamLoDate", "SanPhamImei lô date"]
auth: mixed
state_machine: []
deprecated: false
entities: [SanPhamLoDate, SanPham, Kho]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Order/DonHang (GetSanPhamLoDateWithKhoMacDinh), OrderLogistic/DonHang (GetSanPhamLoDateByIdKho, TinhToanTruKhoLoDateV2)]
cross_module_out: []
---

# SanPhamLoDateController — Sản phẩm theo lô date (số lô + hạn sử dụng)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | màn view-sanpham-lodate | POST `api/SanPhamLoDate/TimTheoDieuKien` | `SanPhamImei_TimTheoDieuKien` | public | màn list | Trả list tồn theo lô date (SP/kho/lô/HSD) trong scope user, phân trang | EF | Quyền key dùng chung họ `SanPhamImei_*` (imei + lô date chung bộ key) |
| 2 | Dropdown lô date theo SP & kho | ô chọn lô date khi xuất/trả hàng | POST `api/SanPhamLoDate/DropdownLoDateTheoSpAndKho` · `DropdownLoDateTheoSpAndKhoPt` (phân trang) | `SanPhamImei_DropdownLoDateTheoSpAndKho` / `_DropdownLoDateTheoSpAndKhoPt` | public | popup chọn lô | Trả list lô date còn tồn của 1 SP trong 1 kho để người dùng chọn lô khi xuất/trả | EF | - |
| 3 | Dropdown lô date theo keyword + SP | ô search lô | POST `api/SanPhamLoDate/DropdownTheoKeywordAndIdSp` | `SanPhamLoDate_DropdownTheoKeywordAndIdSp` | public | autocomplete | Trả lô date gợi ý theo keyword + IdSp | EF | Key này dùng tiền tố `SanPhamLoDate_` (khác họ `SanPhamImei_` còn lại) — lệch convention |
| 4 | Chi tiết lô date | xem chi tiết tồn 1 lô | POST `api/SanPhamLoDate/TimChiTietLoDate` | `SBAuthorize()` (rỗng — chỉ cần đăng nhập) | public | per-row/popup | Trả chi tiết tồn 1 lô date | EF | Permission key gốc bị comment (`SanPhamImei_TimChiTietLoDate`), còn `[SBAuthorize()]` rỗng — cần xác nhận có chủ ý |
| 5 | Chi tiết lô date theo lô sản xuất | popup theo lô SX | POST `api/SanPhamLoDate/TimChiTietLoDateTheoLoSanXuat` | `SanPhamImei_TimChiTietLoDateTheoLoSanXuat` | public | popup | Trả chi tiết tồn lô date gom theo lô sản xuất | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Order/DonHang** gọi `POST GetSanPhamLoDateWithKhoMacDinh` (`[PrivateSBAuthorize]`) — lấy tồn lô date theo kho mặc định khi dựng đơn (Order.Service/DonHangService.cs, DonHangServicePartial.cs).
- [internal] **OrderLogistic/DonHang** gọi `POST GetSanPhamLoDateByIdKho` (`[PrivateSBAuthorize]`) — lấy tồn lô date theo kho chỉ định.
- [internal/auth] **OrderLogistic/DonHang** gọi `POST TinhToanTruKhoLoDateV2` (`[SBAuthorize]`) — sau khi quét lô date trên đơn, tính toán & trừ kho theo lô date (FEFO) khi hoàn tất đơn (OrderLogistic.Service/DonHangService.cs).

## Quan hệ
- **Gọi sang:** chỉ EF nội bộ WarehouseProduct (`ISanPhamLoDateService`), không gọi service ngoài.
- **Dữ liệu (entities):** `SanPhamLoDate` (tồn theo SP × kho × số lô × hạn sử dụng), liên hệ `SanPham`, `Kho`.
- **Ripple / đồng bộ:** đây là NGUỒN tồn lô date cho bán hàng. `TinhToanTruKhoLoDateV2` được Order/OrderLogistic gọi để trừ kho theo lô khi chốt đơn → ảnh hưởng tồn kho thực; lỗi tại đây làm sai tồn lô date trên đơn.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/SanPhamLoDateController.cs`.
- `public partial class SanPhamLoDateController` (L21) — **controller `partial`**, có thể còn endpoint ở file partial khác; node này cover file Controllers/SanPhamLoDateController.cs. *Cần xác nhận* file partial bổ sung.
- Ctor nhận `IServiceProvider inject` nhưng **không dùng** (L24-31) — dead param.
- Họ permission key **trộn 2 tiền tố**: phần lớn `SanPhamImei_*`, riêng `DropdownTheoKeywordAndIdSp` dùng `SanPhamLoDate_*`; `TimChiTietLoDate` để key rỗng. Khi map quyền dễ sót.
- Nhiều dòng `//[SBAuthorize()]` để lại comment cũ cạnh key thật — nhiễu khi đọc, không ảnh hưởng runtime.
- Lưu ý họ controller liên quan: imei (`SanPhamImei...`) là controller riêng — node này chỉ mô tả lô date.
</content>
