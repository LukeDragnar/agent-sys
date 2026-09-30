---
controller: CauHinhChietKhauTheoBillController
module: cua-hang
service: Order
man_hinh_ui: "Cài đặt hệ thống / Cấu hình Order — tab 'Cấu hình chiết khấu theo bill' (config-order/cauhinh-ck-theo-bill, CauhinhCkTheoBillComponent): list cấu hình theo chi nhánh + bảng mức ngưỡng. Áp dụng ở POS thu ngân (thungan/ap-dung-ck-theo-bill, ApDungCkTheoBillComponent)."
muc_dich: "Cấu hình chiết khấu theo bill liên chi nhánh: dùng 1 hóa đơn (bill A) ở chi nhánh nguồn để chiết khấu cho đơn (bill B) ở chi nhánh áp dụng, theo các mức ngưỡng từ-tiền."
alias: ["cấu hình chiết khấu theo bill", "chiết khấu theo hóa đơn", "chiết khấu liên chi nhánh", "giảm giá theo bill nguồn", "mức chiết khấu theo bill"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhChietKhauTheoBill, CauHinhChietKhauTheoBillMuc, DonHang]
stored_procs: []
depends: [Order, Redis/Cache (CauHinhChietKhauTheoBill)]
cross_module_in: [Order/DonHang — DonHangService gọi ICauHinhChietKhauTheoBillService.ComputeChietKhau khi áp chiết khấu theo bill lúc tạo/thanh toán đơn; FE config-order (cấu hình) + thungan/ap-dung-ck-theo-bill (áp dụng)]
cross_module_out: [Clear cache CauHinhChietKhauTheoBill theo ĐơnVị mỗi lần ghi; feed luồng áp KM/giảm giá ở tạo đơn (DonHang) qua ComputeChietKhau]
---

# CauHinhChietKhauTheoBillController — Cấu hình chiết khấu theo bill (liên chi nhánh)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình theo chi nhánh | load list | GET `api/CauHinhChietKhauTheoBill?idChiNhanh=` | (rỗng — login) | public | màn cấu hình | List header cấu hình theo `idChiNhanh` + DonVi user | EF | - |
| 2 | Tạo cấu hình (header) | "Thêm cấu hình" | POST `api/CauHinhChietKhauTheoBill` | `CauHinhChietKhauTheoBill_Create` | public | nút thêm | Validate (chi nhánh áp dụng/nguồn/kiểu CK/sử dụng bắt buộc; không trùng cặp ChiNhánh+ChiNhánhNguồn); insert; gán DonViId; clear cache | EF | - |
| 3 | Cập nhật cấu hình (header) | "Sửa" | PUT `api/CauHinhChietKhauTheoBill/{id}` | `CauHinhChietKhauTheoBill_Update` | public | per-row | Như Create nhưng update theo Id (scope DonVi); guard trùng cặp; clear cache | EF | - |
| 4 | Xóa cấu hình (header) | "Xóa" | DELETE `api/CauHinhChietKhauTheoBill/{id}` | `CauHinhChietKhauTheoBill_Delete` | public | per-row | Xóa header + toàn bộ mức con (transaction + execution strategy); clear cache | EF | - |
| 5 | Lấy mức theo cấu hình | load bảng mức | GET `api/CauHinhChietKhauTheoBill/Muc?idCauHinh=` | (rỗng — login) | public | mở chi tiết | List mức theo `IdCauHinhChietKhauTheoBill` | EF | - |
| 6 | Tạo mức ngưỡng | "Thêm mức" | POST `api/CauHinhChietKhauTheoBill/Muc` | `CauHinhChietKhauTheoBill_CreateMuc` | public | nút thêm mức | Validate (TuTien/TypeGiamGia/ValueGiamGia bắt buộc; %∈[0,100], tiền>0; TuTien không trùng trong cùng cấu hình); insert; clear cache | EF | - |
| 7 | Cập nhật mức ngưỡng | "Sửa mức" | PUT `api/CauHinhChietKhauTheoBill/Muc/{id}` | `CauHinhChietKhauTheoBill_UpdateMuc` | public | per-row | Như CreateMuc, update theo Id; guard TuTien trùng (khác Id, cùng cấu hình); clear cache | EF | - |
| 8 | Xóa mức ngưỡng | "Xóa mức" | DELETE `api/CauHinhChietKhauTheoBill/Muc/{id}` | `CauHinhChietKhauTheoBill_DeleteMuc` | public | per-row | Xóa mức theo Id; clear cache | EF | - |

## Quy tắc nghiệp vụ (gating / ngưỡng — áp dụng khi tạo/thanh toán đơn, `ComputeChietKhau`)
Tham số: `billA` = hóa đơn nguồn (chi nhánh nguồn), `billB` = đơn đang xử lý (chi nhánh áp dụng). Thứ tự (dừng ở điều kiện fail đầu tiên → trả `isHopLe=false` + lý do):
1. Tìm config có `IdChiNhanh == billB.IdChiNhanh` & `IdChiNhanhNguon == billA.IdChiNhanh` & `IsSuDung == true` (load từ cache). Không có → "Chi nhánh này không được cấu hình chiết khấu từ chi nhánh nguồn".
2. `billA.DmDonHangTrangThai` phải = `DaThanhToan`, không thì → "Đơn hàng chưa thanh toán".
3. Nếu `KhoangNgayChoPhep` có giá trị & `billA.NgayThanhToan` < `Now - KhoangNgayChoPhep` ngày → "Đơn hàng đã quá hạn áp dụng chiết khấu".
4. Nếu `billA.CKTheoDonHang_DaApDungChietKhau == true` & đã áp cho đơn khác (`...IdDonHangDaApDung != billB.Id`) → "Hóa đơn này đã được dùng để chiết khấu" (mỗi bill A dùng 1 lần).
5. `refAmount` = `billB.TamTinh` nếu `KieuChietKhau == 2`, ngược lại `billA.TamTinh` (KieuChietKhau=1).
6. Chọn mức: `TuTien` lớn nhất ≤ `refAmount`. Không mức nào đạt → "Số tiền chưa đủ điều kiện".
7. `rawDiscount`: TypeGiamGia=2 (tiền) → `ValueGiamGia`; TypeGiamGia=1 (%) → `ValueGiamGia/100 * refAmount`.
8. Cap: `capAmount` = `billA.TamTinh` nếu KieuChietKhau=2, ngược lại `billB.TamTinh`. `actualDiscount = min(rawDiscount, capAmount)`, thêm chốt `actualDiscount ≤ billB.TamTinh`.
- **KieuChietKhau**: 1 = tính theo bill A, cap theo B; 2 = tính theo bill B, cap theo A (theo comment entity).

## Quan hệ
- **Gọi sang:** Cache (Redis) — `ClearCache_CauHinhChietKhauTheoBill` (mỗi lần ghi), `GetOrSetCache_CauHinhChietKhauTheoBill` (khi compute).
- **Dữ liệu (entities):** `CauHinhChietKhauTheoBill` (IdChiNhanh = nơi áp dụng, IdChiNhanhNguon = nguồn check, KhoangNgayChoPhep, KieuChietKhau, IsSuDung), `CauHinhChietKhauTheoBillMuc` (TuTien, TypeGiamGia, ValueGiamGia), `DonHang`.
- **Ripple / đồng bộ:** mỗi ghi clear cache → lần áp chiết khấu kế tiếp ở tạo/thanh toán đơn lấy config mới.

## Luồng xuyên module (áp chiết khấu theo bill khi tạo đơn)
```
Admin cấu hình header (chi nhánh áp dụng ← nguồn, kiểu CK) + mức ngưỡng → clear cache
  → POS thu ngân: chọn bill A (hóa đơn nguồn) cho đơn B (ap-dung-ck-theo-bill)
      → DonHangService.ComputeChietKhau(billA, billB) đọc config cache + mức
      → trả actualDiscount + đánh dấu billA đã dùng (1 lần) → áp vào giảm giá đơn B
```

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint GET list/Muc (`GetByChiNhanh`, `GetMucByIdCauHinh`) chỉ `[SBAuthorize]` rỗng (login) — không gắn permission key như các verb ghi; có chủ ý cho đọc cấu hình.
- `GetMucByIdCauHinh`/`UpdateMuc`/`DeleteMuc` không kiểm DonVi của mức (chỉ filter theo Id/IdCauHinh) — header có scope DonVi, mức dựa vào FK header; rủi ro thấp nhưng không guard cross-tenant trực tiếp ở tầng mức.
- Code thật: BE `Order/Order.API/Controllers/CauHinhChietKhauTheoBillController.cs`, service `Order/Order.Service/Generated/CauHinhChietKhauTheoBillService.cs` (ComputeChietKhau), entity `Order/Order.DataContext/Entity/CauHinhChietKhauTheoBill.cs` + `CauHinhChietKhauTheoBillMuc.cs`; FE service `services/order/cauhinh-chietkhau-theo-bill.service.ts`, page `pages/caidat-hethong/configsystem/config-order/cauhinh-ck-theo-bill/` + `pages/Order/thungan/ap-dung-ck-theo-bill/`.
