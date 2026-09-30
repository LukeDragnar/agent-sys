---
controller: PhieuTongHopController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ chủ yếu — không có màn list riêng. Phục vụ luồng tạo phiếu nhập TỪ phiếu xuất sản xuất (chuyển hóa chứng từ) + cấp tra cứu phiếu theo mã cho service khác (private)."
muc_dich: "Xử lý chứng từ tổng hợp xuyên loại phiếu: tạo phiếu nhập từ phiếu xuất sản xuất, và tra cứu phiếu (bất kỳ loại) theo mã chứng từ cho service khác."
alias: ["phiếu tổng hợp", "tạo phiếu nhập từ phiếu xuất sản xuất", "lấy phiếu theo mã", "chứng từ tổng hợp"]
auth: mixed                # 1 endpoint [SBAuthorize()] rỗng (FE), 1 endpoint [PrivateSBAuthorize] (service-to-service)
state_machine: []
deprecated: false
entities: [PhieuNhap, PhieuXuat]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Accounting/Order? (LayPhieuTheoMa qua [PrivateSBAuthorize]) — cần xác nhận caller]
cross_module_out: [PhieuNhap (tạo phiếu nhập mới từ phiếu xuất sản xuất)]
---

# PhieuTongHopController — Phiếu tổng hợp (chuyển hóa chứng từ)

> Controller nhỏ, 2 endpoint. `[TienIchAttribute("PhieuTongHopController", "Phiếu nhập", TI_Kho)]`. Liên quan luồng sản xuất: phiếu xuất sản xuất → sinh phiếu nhập thành phẩm.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo phiếu nhập từ phiếu xuất sản xuất | (flow sản xuất) | POST `api/PhieuTongHop/PhieuNhap_ThemMoiTuPhieuXuatSanXuat` | `[SBAuthorize()]` (rỗng — chỉ đăng nhập) | public | sau khi có phiếu xuất sản xuất | Nhận `CapNhatPhieuXuatDto`, sinh phiếu nhập (thành phẩm) tương ứng phiếu xuất sản xuất | EF | quyền rỗng (chỉ đăng nhập) — *cần xác nhận* có chủ ý |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [private] `GET LayPhieuTheoMa?maChungTu=&ma=&idChiNhanh=` — tra cứu 1 phiếu theo (loại chứng từ + mã + chi nhánh) cho service khác đồng bộ chứng từ. *Cần xác nhận* caller cụ thể (Accounting/Order...).

## Quy tắc nghiệp vụ
- `PhieuNhap_ThemMoiTuPhieuXuatSanXuat`: input là DTO của **phiếu xuất** (`CapNhatPhieuXuatDto`) nhưng action **tạo phiếu nhập** → logic map xuất→nhập nằm trong `IPhieuTongHopService`. Triage sai số lượng/giá thành phẩm → soi service này.

## Quan hệ
- **Gọi sang:** `IPhieuTongHopService` (toàn bộ).
- **Dữ liệu (entities):** `PhieuNhap`, `PhieuXuat` (chuyển hóa giữa 2 loại).
- **Ripple / đồng bộ:** tạo phiếu nhập mới → ảnh hưởng tồn kho thành phẩm; `LayPhieuTheoMa` cấp dữ liệu phiếu cho service ngoài.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuTongHopController.cs` (53 dòng, partial class).
  - L31-37 (`PhieuNhap_ThemMoiTuPhieuXuatSanXuat`): `[SBAuthorize()]` rỗng — chỉ cần đăng nhập, không gắn permission key. *Cần xác nhận* có chủ ý cho luồng nội bộ.
  - L45-51 (`LayPhieuTheoMa`): `[PrivateSBAuthorize]` — service-to-service; tham số `maChungTu` xác định loại phiếu.
  - `partial class` — *cần xác nhận* không có partial bổ sung.
