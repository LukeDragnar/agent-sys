---
controller: DonHangHoanController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > màn Đơn hàng hoàn (đơn hoàn/trả hàng TMĐT) — list theo 4 tab (Tất cả / Trả hàng-Hoàn tiền / Hủy / Giao không thành công) + sub-tab trạng thái xử lý; nút 'Lấy đơn hoàn mới', 'Đồng bộ trạng thái', menu per-row 'Tạo HBBTL' / 'Map sản phẩm' / 'Đồng bộ đơn gốc'"
muc_dich: "Quản lý đơn hàng hoàn (return/refund) đồng bộ từ sàn TMĐT (Shopee/TikTok/Lazada): kéo đơn hoàn mới + refresh trạng thái logistics hoàn từ sàn, map SP hoàn ↔ SP đơn bán gốc, chỉnh chiết khấu sàn, re-snapshot thông tin đơn gốc, và pre-fill DTO để FE tạo HBBTL (Hàng bán bị trả lại) bên Accounting."
alias: ["đơn hoàn", "đơn trả hàng", "return order", "hoàn tiền sàn", "đồng bộ đơn hoàn shopee", "tạo HBBTL từ đơn hoàn", "trả hàng TMĐT", "đơn giao không thành công"]
auth: mixed
state_machine: [ChoDuyet, DangXuLy, DangTraHang, DangKhieuNai, DaHoanTien, TuChoi, DaHuy]
deprecated: false
entities: [DonHangHoan, DonHangHoanSanPham, DonHangLogistic, DonHangLogisticSanPham]
stored_procs: []
depends:
  - "IDonHangHoanService (OrderLogistic.Service/Generated/DonHang/DonHangHoanService.cs)"
  - "IEcommerceConnectService (proxy GetReturnFromEcomm / RefreshReturnStatus sang ecommerce_connect)"
  - "Accounting (HBBTL = PhieuBanTraLai — tạo từ DTO prefill phía FE, không gọi trực tiếp ở controller này)"
cross_module_in:
  - "ecommerce_connect / webhook return — sync đơn hoàn vào DonHangHoan (entity được populate bởi consumer/webhook return, không qua controller này)"
cross_module_out:
  - "ecommerce_connect (LayDonHoanMoiTuEcomm → GetReturnFromEcomm; DongBoTrangThaiDonHoan → RefreshReturnStatus)"
  - "Accounting/PhieuBanTraLai (HBBTL_PrefillFromDonHangHoan → FE patch form HBBTL → PhieuBanTraLai persist + IdDonHangHoanSanPham link)"
---

# DonHangHoanController — Đơn hàng hoàn (return/refund TMĐT)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm đơn hoàn theo điều kiện | grid + filter 4 tab/sub-tab | POST `api/DonHangHoan/TimTheoDieuKien` | `[SBAuthorize]` (mở — chỉ cần login) | public | màn list | List đơn hoàn theo date/keyword/branch/sàn/shop + tab cấp 1 (`LoaiDonHoanTab`) + sub-tab trạng thái + action filter (KiemTraHangHoan/CoMaVanDonHoan/UuTienHetHan…), phân trang | EF | Không gắn permission key riêng — controller chỉ gate qua `[TienIch]` TI_OrderLogistic |
| 2 | Đếm counter các tab | badge số trên tab/sub-tab | POST `api/DonHangHoan/GetTabCounters` | `[SBAuthorize]` | public | màn list | Trả `TabCountersDto`: count theo tab cấp 1 + sub-tab xử lý + action + deadline; áp filter common NHƯNG ignore filter tab/action để đếm đúng mọi tab | EF | - |
| 3 | Lấy đơn hoàn mới từ sàn | "Lấy đơn hoàn mới" (nút 1) | POST `api/DonHangHoan/LayDonHoanMoiTuEcomm` | `[SBAuthorize]` | public | chọn sàn + gian hàng + khoảng tg | Proxy sang ecommerce_connect `GetReturnFromEcomm` (đồng bộ thủ công đơn hoàn chưa có trên hệ thống) | proxy ECOMM | Side-effect thực (insert DonHangHoan) nằm ở ecommerce_connect, không ở controller này |
| 4 | Đồng bộ trạng thái đơn hoàn | "Đồng bộ trạng thái" (nút 2) | POST `api/DonHangHoan/DongBoTrangThaiDonHoan` | `[SBAuthorize]` | public | tick ≥1 đơn | Lookup DonHangHoan (filter IdDonVi) → build list (EcommerceCode+shop_id+return_sn) → proxy `RefreshReturnStatus`. Bỏ đơn thiếu mã đơn sàn / sàn chưa hỗ trợ | proxy ECOMM | `MapEcommerceCode` chỉ map 8→SHOPEE / 9→TIKTOK / 10→LAZADA, sàn khác trả null bị bỏ thầm lặng (DonHangHoanService.cs:1352) |
| 5 | Xem chi tiết đơn hoàn | mở row | GET `api/DonHangHoan/GetChiTiet/{id}` | `[SBAuthorize]` | public | - | Trả `DonHangHoanChiTietDto` (header + SP hoàn group theo combo + summary đơn gốc) | EF | - |
| 6 | Chi tiết theo đơn gốc | từ màn đơn bán logistic | GET `api/DonHangHoan/GetChiTietByIdDonHangLogistic/{idDonHangLogistic}` | `[SBAuthorize]` | public | đơn bán có đơn hoàn | Trả chi tiết đơn hoàn map theo IdDonHangLogistic gốc | EF | - |
| 7 | Pre-fill form HBBTL | "Tạo HBBTL" (per-row) | GET `api/DonHangHoan/HBBTL_PrefillFromDonHangHoan/{idDonHangHoan}` | `[SBAuthorize]` | public | đơn hoàn đủ điều kiện logistics (hàng về kho) | Trả `HbbtlPrefillFromDonHangHoanDto` (header KH + listDetail có DonGiaVon/IMEI/MaLoDate snapshot từ line đơn bán) để FE patch form HBBTL; FE chuyển thành ReqPhieuBanTraLaiDto khi Lưu | EF | Gating "hàng đã về kho" tính ở `IsHangVeKho` theo NoiKiemHoan (xem Quy tắc) — nhưng endpoint này KHÔNG tự chặn, FE check qua badge GetTrangThaiHoanLogistics |
| 8 | Map thủ công SP hoàn ↔ SP bán | modal "Map sản phẩm" | POST `api/DonHangHoan/UpdateMapSanPham` | `[SBAuthorize]` | public | auto-map fail (IdDonHangLogisticSanPham null) | Set IdDonHangLogisticSanPham per mapping (đơn cũ trước deploy / SKU đổi tên) | EF | - |
| 9 | Cập nhật chiết khấu sàn | sửa từng dòng màn detail | POST `api/DonHangHoan/UpdateChietKhauSan` | `[SBAuthorize]` | public | - | Cập nhật chiết khấu sàn (TMDT trợ giá) per-SP của đơn hoàn | EF | - |
| 10 | Đồng bộ thông tin đơn gốc | "Đồng bộ đơn gốc" (multi-select) | POST `api/DonHangHoan/DongBoThongTinDonGoc` | `[SBAuthorize]` | public | tick ≥1 đơn | Re-snapshot đơn bán gốc → đơn hoàn: KH, địa chỉ, NV bán, mã đơn, số HĐ, TT giao hàng, mã PBH kế toán + tiền gốc per line + lệch. Trả số đơn đồng bộ thành công | EF | - |
| 11 | Load SP đơn bán gốc (modal map) | mở modal MapSanPham | GET `api/DonHangHoan/GetItemsDonBanByIdDonGoc?idDonGoc=` | `[SBAuthorize]` | public | - | List `DonBanItemDto` (line DonHangLogisticSanPham) theo IdDonHangLogistic, filter IdDonVi — cột "đơn bán" để chọn map | EF | - |

## Quy tắc nghiệp vụ (gating logistics hoàn → cho phép tạo HBBTL)
Logic ở `IsHangVeKho(DonHangHoan)` (DonHangHoanService.cs:1374) — quyết "hàng đã thực về SELLER" theo `NoiKiemHoan`:
1. **NoiKiemHoan = KhoTrungGian** (warehouse_validation, FBS 2 chặng): hàng về kho sàn ở chặng REVERSE chưa tính → phải đợi chặng 2 `LogisticsLoai=="POST_RETURN"` && `LogisticsTrangThaiMa=="POST_RETURN_LOGISTICS_DELIVERY_DONE"`.
2. **NoiKiemHoan = NguoiBan** (seller_validation): hàng về thẳng seller khi `LogisticsLoai=="REVERSE"` && `LogisticsTrangThaiMa=="LOGISTICS_DELIVERY_DONE"`.
3. **NoiKiemHoan null** (đơn cũ): fallback OR cả 2 điều kiện trên (an toàn).
4. Badge UI `GetTrangThaiHoanLogistics`: DA_NHAN / DANG_VE / CHO_GUI / GIAO_FAIL / MAT_HANG — phân biệt qua discriminator `LogisticsLoai` (REVERSE vs POST_RETURN).

## Quan hệ
- **Gọi sang:** `IEcommerceConnectService` (GetReturnFromEcomm, RefreshReturnStatus) — toàn bộ giao tiếp sàn proxy qua ecommerce_connect; `IDonHangHoanService` đọc DonHangLogistic/SanPham gốc để snapshot + prefill HBBTL.
- **Dữ liệu (entities):** `DonHangHoan` (header: TrangThai canonical, LoaiHoan, LoaiYeuCauHoan, NoiKiemHoan, LogisticsLoai/LogisticsTrangThaiMa, MaDonHoanEcomm, EcommerceId, TMDTIdGianHang), `DonHangHoanSanPham` (line + IdDonHangLogisticSanPham map), `DonHangLogistic`/`DonHangLogisticSanPham` (đơn bán gốc).
- **Ripple / đồng bộ:** đơn hoàn KHÔNG tự đụng tồn kho ở controller này. Tồn/kế toán ripple xảy ra KHI user tạo HBBTL (PhieuBanTraLai) bên Accounting từ DTO prefill → PhieuBanTraLai (kiêm nhập kho nếu IsKiemNhapKho=true) mới cộng tồn + giảm công nợ.

## Luồng xuyên module (return order end-to-end)
```
[webhook/ecommerce_connect] sync đơn hoàn sàn → insert DonHangHoan (ngoài controller)
  → user: LayDonHoanMoiTuEcomm (kéo thủ công đơn còn thiếu) / DongBoTrangThaiDonHoan (refresh logistics)
  → đơn đạt logistics "hàng về kho" (IsHangVeKho=true theo NoiKiemHoan)
  → user "Tạo HBBTL": HBBTL_PrefillFromDonHangHoan → FE patch form HBBTL
  → FE Lưu → Accounting PhieuBanTraLai (link IdDonHangHoanSanPham)
       → nếu IsKiemNhapKho=true: kiêm phiếu nhập kho (cộng tồn) + giảm trừ công nợ KH
       → nếu false: chỉ giảm công nợ / hoàn tiền, không có hàng vật lý
```
Triage "không tạo được HBBTL": check `IsHangVeKho` (NoiKiemHoan + LogisticsLoai + LogisticsTrangThaiMa). "Map SP fail": IdDonHangLogisticSanPham null → dùng UpdateMapSanPham.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangHoanService.cs:1352` `MapEcommerceCode` chỉ hỗ trợ Shopee(8)/TikTok(9)/Lazada(10); sàn khác → null → đơn bị **bỏ thầm lặng** khỏi batch đồng bộ trạng thái (không báo riêng đơn nào bị skip).
- `DongBoTrangThaiDonHoan` bỏ qua đơn có `MaDonHoanEcomm` rỗng; nếu cả batch không có đơn hợp lệ → trả lỗi "Không có đơn hợp lệ để đồng bộ".
- Tất cả endpoint dùng `[SBAuthorize]` mở (chỉ cần login) — không phân quyền key per-action; kiểm soát truy cập chỉ ở mức tiện ích `TI_OrderLogistic`.
- Tồn kho/kế toán của đơn hoàn KHÔNG ripple ở service này — đừng tìm logic cộng tồn ở đây; nó nằm ở Accounting PhieuBanTraLai (HBBTL).
- Controller `partial` (`public partial class DonHangHoanController`) — node cover file Controllers/DonHangHoanController.cs; chưa thấy file partial khác cho controller này (grep `class DonHangHoanController` trả 1 file).
