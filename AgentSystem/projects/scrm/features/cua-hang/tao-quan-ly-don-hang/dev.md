# Dev — Tạo & quản lý đơn hàng

> Service: **Order** (BE .NET) · FE module **Order** (`pages/Order/thungan/*`). Controller trung tâm POS, ~110 endpoint active. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Ghi chú |
|---|---|---|
| Thu ngân (POS) | `pages/Order/thungan` | màn bán hàng tại quầy: chọn bàn/khách → thêm món → áp KM/voucher → xếp NV → tính lại → thanh toán → in/phát hành |
| Đang phục vụ | `pages/Order/...` (đang phục vụ) | theo dõi bàn/khu vực/nhân viên |
| Danh sách đơn hàng | `pages/Order/...` | tra cứu/lọc đơn, xuất Excel, đối soát |
| Màn nhân viên (DV-NV) | `pages/Order/...` | thao tác phục vụ + in bếp |

- FE service: `services/order/donhang.service.ts`.

## Endpoint/Controller (trace registry)
Node: `registry/cua-hang/DonHangController.md`. Controller `DonHangController` (service Order), `auth: mixed`, state machine `[ChuaThanhToan, DaThanhToan]`.

Vòng đời chính (permission key riêng `DonHang_*`):
| MaTinhNang / key | Route | Mục đích |
|---|---|---|
| `DonHang_TaoDon` | POST `api/DonHang/TaoDon` | Tạo đơn (ChuaThanhToan) + tab Redis |
| `DonHang_ThemChiTietDon` / `_SuaChiTietDon` / `_XoaChiTietDon` | POST `api/DonHang/ThemChiTietDon`… | CRUD dòng SP/DV/combo/thẻ |
| `DonHang_ApDungVoucher` / `_XoaVoucher` | POST `api/DonHang/ApDungVoucher`… | Áp/xóa voucher (gọi Promotion) |
| `DonHang_CheckChietKhauTheoBill` | GET `api/DonHang/CheckChietKhauTheoBill` | Kiểm tra chiết khấu theo bill |
| `DonHang_ViewXepNv` / `_LuuXepNv` | POST `api/DonHang/ViewXepNv`… | Xếp nhân viên phục vụ |
| `DonHang_TinhToanLai` / `_TinhToanLaiNhieuDon` | POST `api/DonHang/TinhToanLai`… | Tính lại tổng tiền |
| `DonHang_GuiYeuCauThanhToan` | POST `api/DonHang/GuiYeuCauThanhToan` | Chốt yêu cầu thanh toán |
| `DonHang_ThanhToan` | POST `api/DonHang/ThanhToan` | **State → DaThanhToan**, trừ tồn chốt, ripple |
| `DonHang_HuyHoaDon` | POST `api/DonHang/HuyHoaDon` | Rollback thanh toán → ChuaThanhToan |
| `DonHang_Xoa` | POST `api/DonHang/Xoa/{id}` | Xóa đơn (nhiều guard) |
| `DonHang_ChuyenBan` | POST `api/DonHang/ChuyenBan` | Chuyển bàn (+ đèn BI_A) |
| `DonHang_TimTheoDieuKien` | POST `api/DonHang/TimTheoDieuKien` | Danh sách đơn (đi `_storeService` mới) |
| `DonHang_InDon` (+ biến thể) | POST `api/DonHang/InDon`… | In hóa đơn (tự TinhToanLai trước) |

**In bếp** (trace `DonHang_InDonBepNv`): `InDonBepNv`/`InDonBepNvV2`/`InDonBepNvHtml`, `DsInDonBepNv`, `CapNhatTrangThaiDaIn`, `XoaDonInBep`, `LogInDonWeb` — phục vụ in chế biến (bếp/bar).

**Hóa đơn điện tử**: `PhatHanhHoaDon`, `PreviewHoaDonPhatHanh_MeInvoice`/`_SInvoice`, `TaoHoaDonNhap`/`XoaHoaDonNhap`/`DongBoTrangThaiHoaDonNhap`, `LayThongTinThue`… — mọi method check `CheckTienIch(TI_Invocie_Misa || TI_Invocie_Viettel)`. Callback từ Invoice (`[PrivateSBAuthorize]`): `UpdateTrangThaiPhatHanhHoaDon`/`UpdateTrangThaiHoaDonNhap`.

**Cross-service đọc (`[PrivateSBAuthorize]`)**: `GetDonHangByKhachHangPrivate`/`GetDetailByKhachHangPrivate`/`GetLichSuSuDungDichVuPrivate` (Customer gọi), `GetDonHangByIdsSyncBravo` (Bravo retry), `TaoDonTuContact`/`TaoDonTuContactCoTienCoc` (Customer/CTV).

## Entities
`DonHang` (header: `DmDonHangTrangThai`, IdKhachHang, IdChoNgoi, IdChiNhanh, TienCoc, IdPhieuMuaHang, IdContact, IdLichHen…) + chi tiết: `DonHangSanPham`, `DonHangDichVu`, `DonHangNhanVien`, `DonHangThanhToan`, `DonHangPtThanhToan`, `DonHangKhuyenMai`, `DonHangInDon`, `DonHangSanPhamDinhKem`, `DonHangGiaoCa`, `DonHangLichHen`, `TmpDonHangInvoice`.

## Luồng code
- Service chính: `DonHangService`, `DonHangStoreService` (list/store-based mới), `DonHangThanhToanService` (thanh toán/hủy/đối soát), `DonHangNhanVienService`, `DonHangFileService`, `DonHangGiaoCaService`, `DonHangInvoiceService`.
- **Xóa đơn — guard** (DonHangController:805–900): xem chuỗi 6 bước ở `flow.md`. Sau guard: `DeleteAsync` → xóa DonHangInDon/DonHangSanPham → cập nhật Redis Tab + LuuRedisDonHang → `SyncOrderToMarketingCampaign(Type=2)` → rollback ThuChi nếu TienCoc>0 (Branch, fail → lỗi) → tắt đèn BI_A → xóa ảnh KH (Customer, fail → lỗi) → `DongBoContactXoaDonHang`.
- **Thanh toán**: ghi DonHangThanhToan + PtThanhToan → trừ tồn chốt → ripple (MarketingCampaign, Branch ThuChi/báo cáo/hoa hồng, OrderLogistic) → (tùy chọn) phát hành hóa đơn → callback.

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **`TinhToanLaiTongTienTra` KHÔNG có attribute auth** (DonHangController:623) — chỉ `[HttpPost]`, comment "Dùng cho mobile" → public. Rủi ro tính tiền không token; cần xác nhận có chủ ý.
- **`DsDangPhucVuBiaRutGonAllow` là `[AllowAnonymous]`** (DonHangController:1313) — lộ trạng thái bàn cho ẩn danh (màn hiển thị bàn công khai).
- **`GetChiTietDonTheoId/{id}` `[AllowAnonymous]`** (DonHangController:387) — link đánh giá đơn, public theo id đơn → lộ chi tiết đơn nếu biết id.
- **`CapNhatAllChiTietDonHang` chỉ `[SBAuthorize()]`** (login) — ghi hàng loạt chi tiết đơn không gắn permission key riêng.
- **Nhiều endpoint ghi quan trọng chỉ `[SBAuthorize()]`** (PhatHanhHoaDon, TaoHoaDonNhap, ThemBarcode, UpdateThoiGianTongDonHang, UpdateSanPhamDinhKem…) — permission key bị comment, chỉ còn check tiện ích (hóa đơn) hoặc login.
- **Hai service đọc/ghi list song song** — `TimTheoDieuKien`/`DsSpDaXoa` dùng `_storeService` (mới); dòng `_service.TimTheoDieuKienAsync` cũ bị comment (DonHangController:89). Lưu ý khi sửa logic list.
- Guard xóa kiểm cross-tenant qua `IdDonVi`+`IdChiNhanh` — đừng bỏ guard này khi refactor.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Order\Order.API\Controllers\DonHangController.cs` (1711 dòng).
- BE service: `…\Order\Order.Service\Generated\DonHangService.cs`, `DonHangStoreService.cs`, `DonHangThanhToanService.cs`, `DonHangNhanVienService.cs`, `DonHangFileService.cs`, `DonHangGiaoCaService.cs`, `DonHangInvoiceService.cs`.
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Order\thungan\*` + `services/order/donhang.service.ts`.
