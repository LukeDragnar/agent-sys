---
controller: DonHangController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng / POS thu ngân (route `pages/Order/thungan` — màn bán hàng tại quầy: chọn bàn/khách → thêm SP/DV → áp KM/voucher → xếp nhân viên → tính toán lại → thanh toán → in/phát hành hóa đơn). Cũng phục vụ màn 'Đang phục vụ', 'Lịch sử đơn', 'Danh sách đơn hàng', và màn nhân viên (DV-NV)."
muc_dich: "Controller trung tâm của nghiệp vụ bán hàng/POS: vòng đời 1 đơn hàng end-to-end — tạo đơn, thêm/sửa/xóa chi tiết, áp khuyến mãi/voucher/chiết khấu, xếp nhân viên phục vụ, tính toán lại tiền, thanh toán, hủy hóa đơn, in/phát hành hóa đơn điện tử, chuyển bàn, giao/hoàn, đối soát."
alias: ["bán hàng", "POS", "thu ngân", "tạo đơn", "thanh toán đơn", "đơn hàng", "order", "lên đơn", "in hóa đơn", "phát hành hóa đơn", "đang phục vụ", "chuyển bàn", "xếp nhân viên"]
auth: mixed
state_machine: [ChuaThanhToan, DaThanhToan]
deprecated: false
entities: [DonHang, DonHangChiTiet, DonHangSanPham, DonHangDichVu, DonHangNhanVien, DonHangThanhToan, DonHangPtThanhToan, DonHangKhuyenMai, DonHangInDon, DonHangSanPhamDinhKem, DonHangGiaoCa, DonHangLichHen, TmpDonHangInvoice]
stored_procs: []
depends: [WarehouseProduct (SP/DV/combo/tồn kho), Promotion/MarketingCampaign (KM/voucher), Customer (khách + ghi nợ + ảnh), Branch (ThuChi/báo cáo/chỗ ngồi), Invoice (Misa/Viettel/MeInvoice/SInvoice phát hành), Booking (bàn/chỗ ngồi/đèn), OrderLogistic (giao vận), Redis (Tab đang mở + cache đơn), ElasticSearch (SP), RabbitMQ (publish)]
cross_module_in: [Customer — GetDonHangByKhachHangPrivate/GetDetailByKhachHangPrivate/GetLichSuSuDungDichVuPrivate (PrivateSBAuthorize); Promotion/CTV — TaoDonTuContact / TaoDonTuContactCoTienCoc / UpdateDaLayKhuyenMaiCTV; MarketingCampaign — TaoDonTuWebsite; Booking/lịch hẹn — TaoDonTuLichHen / TaoDonTuPhieuMuaHang; Invoice — UpdateTrangThaiPhatHanhHoaDon / UpdateTrangThaiHoaDonNhap (callback trạng thái); Bravo retry — GetDonHangByIdsSyncBravo; mobile — TinhToanLaiTongTienTra (no auth)]
cross_module_out: [Customer (DongBoContactXoaDonHang, xóa ảnh KH theo đơn, ghi nợ); MarketingCampaign (SyncOrderToMarketingCampaign khi xóa/cập nhật); Branch ThuChi (rollback tiền cọc khi xóa đơn); Invoice service (phát hành/nháp/hủy hóa đơn điện tử Misa/Viettel/MeInvoice/SInvoice); Booking (bật/tắt đèn bàn BI_A); OrderLogistic (giao vận); Redis Tab đang mở + LuuRedisDonHang; Bravo (đồng bộ ERP)]
---

# DonHangController — Đơn hàng / Bán hàng tại quầy (POS thu ngân)

> Controller LỚN (~110 endpoint active). Bảng dưới chỉ giữ **hành động UI chính theo vòng đời đơn**; các nhóm phụ (nhân viên thao tác, in ấn, hóa đơn điện tử, import, báo cáo, private cross-service) gom ở "Endpoint nội bộ / nhóm phụ". Tất cả method nhận `CurrentUser` và lọc theo ĐơnVị+ChiNhánh.

## Hành động (UI / công khai) — vòng đời đơn chính
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách đơn hàng (lọc) | grid "Danh sách đơn hàng" | POST `api/DonHang/TimTheoDieuKien` | `DonHang_TimTheoDieuKien` | public | màn list | List đơn theo bộ lọc; đi qua `_storeService` (store-based, khác `_service` cũ) | EF (store service) | đã chuyển sang `_storeService`, dòng `_service` cũ bị comment (DonHangController:89) |
| 2 | Tạo đơn | "Tạo đơn" / mở tab POS | POST `api/DonHang/TaoDon` | `DonHang_TaoDon` | public | nút Tạo đơn | Tạo `DonHang` trạng thái ChuaThanhToan + tab đang mở; gán bàn/khách/chi nhánh | EF + Redis Tab | - |
| 3 | Cập nhật thông tin đơn | (auto khi sửa header đơn) | POST `api/DonHang/CapNhatDon` | `DonHang_CapNhatDon` | public | tab đang mở | Update thông tin chính (khách, ghi chú, hình thức phục vụ...) | EF | - |
| 4 | Close / Open đơn | "Đóng tab" / "Mở lại" | POST `api/DonHang/CloseDon` · `OpenDon` · `CloseAllDon` | `DonHang_CloseDon` / `_OpenDon` / `_CloseAllDon` | public | tab đang mở | Đóng/mở tab POS (ẩn khỏi danh sách đang mở); CloseAll đóng hết tab user | EF + Redis | - |
| 5 | Thêm/Sửa/Xóa chi tiết đơn | nút +SP/DV, sửa, xóa dòng | POST `api/DonHang/ThemChiTietDon` · `SuaChiTietDon` · `XoaChiTietDon` · `CapNhatAllChiTietDonHang` | `DonHang_ThemChiTietDon` / `_SuaChiTietDon` / `_XoaChiTietDon` (CapNhatAll: login) | public | tab đang mở | CRUD dòng SP/DV/combo/thẻ trong đơn; trừ/cộng tồn theo cấu hình | EF + WarehouseProduct | CapNhatAllChiTietDonHang chỉ `[SBAuthorize()]` (login) |
| 6 | Áp / xóa voucher | "Áp voucher", "Xóa voucher" | POST `api/DonHang/ApDungVoucher` · `XoaVoucher` | `DonHang_ApDungVoucher` / `_XoaVoucher` | public | tab đang mở | Validate + áp voucher (gọi Promotion); tính lại giảm giá; xóa rollback | EF + Promotion | - |
| 7 | Kiểm tra chiết khấu theo bill | (auto trước thanh toán) | GET `api/DonHang/CheckChietKhauTheoBill` | `DonHang_CheckChietKhauTheoBill` | public | trước TT | Kiểm tra đơn đủ điều kiện hưởng chiết khấu theo bill (cấu hình `CauHinhChietKhauTheoBill`) | EF | - |
| 8 | Xếp nhân viên phục vụ | "Xếp nhân viên" (popup) | POST `api/DonHang/ViewXepNv` · `LuuXepNv` · `LayDanhSachXepNhanVien` · `LuuXepNhanVien` | `DonHang_ViewXepNv` / `_LuuXepNv` / `_LayDanhSachXepNhanVien` | public | tab đang mở | View danh sách NV gợi ý + lưu phân công NV cho từng DV/SP có tiền (combo đã mua không tính) | EF + CauHinhXepNhanVien | logic gán ghi ở comment header (DonHangController:151) |
| 9 | Tính toán lại | (auto/nút trước thanh toán) | POST `api/DonHang/TinhToanLai` · `TinhToanLaiNhieuDon` · `TinhToanLaiTongTienTra` | `DonHang_TinhToanLai` / `_TinhToanLaiNhieuDon` (TongTienTra: **no auth**) | mixed | trước TT | Tính lại tổng tiền/giảm giá/KM/thuế; cờ `IsDungKhiAnThanhToan`/`IsDungKhiYcThanhToan` quyết stop dịch vụ tính giờ | EF | `TinhToanLaiTongTienTra` KHÔNG có attribute auth (dùng cho mobile) → public |
| 10 | Gửi yêu cầu thanh toán | "Yêu cầu thanh toán" | POST `api/DonHang/GuiYeuCauThanhToan` | `DonHang_GuiYeuCauThanhToan` | public | đơn ChuaThanhToan | Chốt yêu cầu TT → stop dịch vụ tính giờ, sinh QR/online nếu có | EF | - |
| 11 | Thanh toán | "Thanh toán" | POST `api/DonHang/ThanhToan` | `DonHang_ThanhToan` | public | đơn ChuaThanhToan | Ghi nhận thanh toán → trạng thái **DaThanhToan**; trừ tồn chốt; sinh DonHangThanhToan + PtThanhToan; trigger ripple (KM/báo cáo/giao vận) | EF + nhiều service | mấu chốt state machine |
| 12 | Hủy hóa đơn (hủy TT) | "Hủy hóa đơn" | POST `api/DonHang/HuyHoaDon` | `DonHang_HuyHoaDon` | public | đơn DaThanhToan | `donHangThanhToanService.HuyThanhToanDonHang` — rollback thanh toán, đưa đơn về ChuaThanhToan | EF | - |
| 13 | Phương thức thanh toán | (panel PTTT) | POST `api/DonHang/LuuPtThanhToan` · `LayPtThanhToan` · `GetBalance` | `DonHang_LuuPtThanhToan` / `_LayPtThanhToan` | public | tab đang mở | Lưu/lấy danh sách PTTT của đơn; GetBalance lấy số dư ví/thẻ | EF + Branch | - |
| 14 | Xem / chi tiết đơn | mở đơn | POST `api/DonHang/ViewChiTietDon` · `GetChiTietDon` · GET `GetThongTinDonHang` · `GetDetailDonHang` | `DonHang_ViewChiTietDon` / `_GetChiTietDon` / `_GetThongTinDonHang` | public | - | Lấy chi tiết đơn để hiển thị/sửa | EF | - |
| 15 | Chuyển bàn | "Chuyển bàn" | POST `api/DonHang/ChuyenBan` | `DonHang_ChuyenBan` | public | đơn đang phục vụ | Chuyển đơn từ bàn A → bàn B (cập nhật IdChoNgoi, đèn BI_A) | EF + Booking | - |
| 16 | Đang phục vụ (các biến thể) | grid "Đang phục vụ" | POST `api/DonHang/DsDangPhucVu` · `DsDangPhucVuBia` · `DsDangPhucVuNv` · `DsDangPhucVuBiaRutGon` | `DonHang_DsDangPhucVu` / `_DsDangPhucVuBia` / ... | mixed | màn đang phục vụ | List đơn đang phục vụ theo bàn/khu vực/nhân viên | EF | `DsDangPhucVuBiaRutGonAllow` là **[AllowAnonymous]** (màn hiển thị công khao theo bàn) |
| 17 | Xóa đơn | "Xóa đơn" | POST `api/DonHang/Xoa/{id}` | `DonHang_Xoa` | public | đơn ChuaThanhToan, không từ phiếu mua | **Nhiều guard** (xem Quy tắc nghiệp vụ); xóa + dọn dữ liệu liên quan + rollback cọc + đồng bộ contact/MKT | EF + nhiều service | guard cấu hình `IsKhongChoPhepXoaDonHang` (DonHangController:831) |
| 18 | In / phát hành hóa đơn | "In", "In bán lẻ", "Phát hành HĐĐT" | POST `api/DonHang/InDon` · `InDon2` · `InDonBanLe` · `InDonTrucTiep` · `PhatHanhHoaDon` · `TaoHoaDonNhap` | `DonHang_InDon` / login (PhatHanh) | mixed | - | In tự `TinhToanLai` trước; PhatHanh kiểm tiện ích Misa/Viettel → gọi Invoice service | EF + Invoice | xem nhóm Hóa đơn điện tử |

## Endpoint nội bộ / nhóm phụ (gom theo mục đích)
- **[DV-NV] Nhân viên thao tác** (region "Nhân viên thao tác") — màn của nhân viên phục vụ: `DsKhachNvPv`/`DsKhachNvDx`/`DsDichVuNv`/`DsDichVuDx`/`DsSanPhamNv`/`DsComboNv`/`DsTheNv`, `XuLyNhieuDichVu`/`GetMotDichVu`/`SuaMotDichVu`, `XoaSanPhamNv`/`UpdateSoLuongSanPhamNv`/`SuaGiaNv`, `ThemChiTietDonNv`, `StopStartDv`/`EditStartStopTimeDichVu`, `HoanThanhSanPham`/`HoanThanhTatCaSanPham`, `NhanVienGhiChu`/`GetNhanVienGhiChu`, `NhanVienViewXepNv`/`NhanVienLuuXepNv`/`NhanVien_LayDanhSachXepNhanVien`/`NhanVien_LuuXepNhanVien`, `GetDonHangByNv`, `CheckNvDuocXuLyDonHang`, `LichSuKh`. Tất cả quyền role `DV-NV`.
- **In bếp / in đơn nhân viên** — `InDonBepNv`/`InDonBepNvV2`/`InDonBepNvHtml`, `DsInDonBepNv`, `CapNhatTrangThaiDaIn`, `XoaDonInBep`, `LogInDonWeb`. Phục vụ in chế biến (bếp/bar).
- **Hóa đơn điện tử (Invoice service ripple)** — `PhatHanhHoaDon`, `PreviewHoaDonPhatHanh_MeInvoice`/`_SInvoice`, `TaoHoaDonNhap`/`XoaHoaDonNhap`/`DongBoTrangThaiHoaDonNhap`, `HuyPhatHanhTuDong`, `LayThongTinThue`/`CapNhatThongTinThue`/`XoaThongTinThue`, `LayIdCacPhieuTheoId`. Mọi method check `CheckTienIch(TI_Invocie_Misa||TI_Invocie_Viettel)`; `NguonThucHien=ThuCong`. Callback từ Invoice: `[PrivateSBAuthorize] UpdateTrangThaiPhatHanhHoaDon`/`UpdateTrangThaiHoaDonNhap`.
- **Import Excel đơn hàng** — `ImportOrder` (key `Order_ImportOrder`), `DownloadFileImportOrder`/`DownloadDataWrongOrder/{id}` (xem thêm controller riêng `DonHangImportController`).
- **Export Excel** — `ExportExcelHoaDon`/`ExportExcelHoaDonKieu2`/`ExportDhSp` (trả file .xlsx).
- **Đối soát** — `DoiSoatDonHang`/`HuyDoiSoatDonHang` (`donHangThanhToanService`), `ReportOrderProductGiaoCa` (biên bản bàn giao ca, `donHangGiaoCaService`).
- **Tạo đơn từ nguồn khác** — `TaoDonTuLichHen`, `TaoDonTuWebsite` (DV-AD-TONG), `TaoDonTuPhieuMuaHang`, `[PrivateSBAuthorize] TaoDonTuContact`/`TaoDonTuContactCoTienCoc` (từ Customer/CTV).
- **[PrivateSBAuthorize] cross-service đọc** — `GetDonHangByKhachHangPrivate`/`GetDetailByKhachHangPrivate`/`GetLichSuSuDungDichVuPrivate` (Customer gọi), `CheckKhachHangDaLenDon`, `LayThongTinCoBanById`, `GetChiTietDonHang`, `GetDonHangByIdsSyncBravo` (Bravo ERP retry).
- **Khác** — `ApDung`/voucher, `LayThongTinSpDvSuDung`, `GetChiTietCombo`, `CapNhatSanPhamCbDungTruoc` (combo dùng trước), `ThemImei`/`ThemBarcode`/`ThemLoDate`, `MappingImeiSPIntoOrder`/`SaveDonHangAfterScanImei`/`GetChiTietDonForScanImeiSP` (đơn offline quét imei), `UpdateSanPhamDinhKem`/`GetDonHangSanPhamDinhKem`, `ThayDoiThuNgan`/`UpdateThuNganHoTro`, `MoCacDichVuStopThanhToan`/`MoCacDichVuStopYCThanhToan`, `CapNhatDaGiao`/`CapNhatDaHoan`, `UpdateThoiGianTongDonHang`/`GetThoiGianDichVu`, `LichSuDonHang`, `[AllowAnonymous] GetChiTietDonTheoId/{id}` (link đánh giá).

## Quy tắc nghiệp vụ (guard khi Xóa đơn — `Xoa`)
Thứ tự kiểm tra (dừng ở lỗi đầu tiên), DonHangController:805–900:
1. Không tìm thấy đơn (`GetByIdAsync` null) → `MESS_NotFound`.
2. `IdDonVi != CurrentUser.DonViId` HOẶC `IdChiNhanh != CurrentUser.IdChiNhanh` → `MESS_NotFound` (chặn cross-tenant).
3. `DmDonHangTrangThai == DaThanhToan` → "Đơn hàng đã thanh toán, không được xóa".
4. Có `IdPhieuMuaHang` → "Đơn hàng được tạo từ phiếu mua hàng thu cũ đổi mới, không được xóa".
5. Nếu KHÔNG phải Admin/AdminChiNhánh → lấy `CauHinhSettingDto`; null → "Không tìm thấy cấu hình!"; `IsKhongChoPhepXoaDonHang==true` → "Đơn vị không cho phép xóa đơn hàng!".
6. (BI_A + có IdChoNgoi) lấy thiết bị đèn để tắt sau khi xóa; không tìm thấy chỗ ngồi → lỗi.
- Sau khi qua guard: `DeleteAsync` → xóa DonHangInDon/DonHangSanPham theo đơn → cập nhật Redis Tab + LuuRedisDonHang → `SyncOrderToMarketingCampaign(Type=2)` → rollback ThuChi nếu `TienCoc>0` (gọi Branch, fail → trả lỗi) → tắt đèn (BI_A) → xóa ảnh KH (gọi Customer, fail → trả lỗi) → đồng bộ contact (`DongBoContactXoaDonHang`).

## Quan hệ
- **Gọi sang:** WarehouseProduct (SP/DV/combo, tồn kho), Promotion/MarketingCampaign (voucher/KM/sync), Customer (`CustomerImage`, ghi nợ, contact), Branch (`ThuChi/XoaTheoIdTransaction`, báo cáo, chỗ ngồi/đèn), Invoice (Misa/Viettel/MeInvoice/SInvoice), Booking (bàn/đèn), OrderLogistic (giao vận), Bravo (ERP).
- **Dữ liệu (entities):** `DonHang` (header: trạng thái `DmDonHangTrangThai`, IdKhachHang, IdChoNgoi, IdChiNhanh, TienCoc, IdPhieuMuaHang, IdContact, IdLichHen...), các bảng chi tiết DonHangSanPham/DonHangDichVu/DonHangNhanVien/DonHangThanhToan/DonHangPtThanhToan/DonHangKhuyenMai/DonHangInDon/DonHangSanPhamDinhKem.
- **Ripple / đồng bộ:** mọi thay đổi đơn → Redis Tab đang mở + cache đơn; xóa/cập nhật → MarketingCampaign; thanh toán → báo cáo/ThuChi/giao vận; phát hành → Invoice service (callback cập nhật trạng thái); BI_A → bật/tắt đèn bàn.

## Luồng xuyên module (đơn bán hàng end-to-end)
```
TaoDon (ChuaThanhToan, mở Tab POS)
  → ThemChiTietDon (SP/DV/combo)  ── WarehouseProduct: giữ/trừ tồn theo cấu hình
  → ApDungVoucher / CheckChietKhauTheoBill  ── Promotion + CauHinhChietKhauTheoBill: tính giảm giá
  → ViewXepNv/LuuXepNv  ── CauHinhXepNhanVien: phân công nhân viên hưởng hoa hồng
  → TinhToanLai (stop dịch vụ tính giờ nếu YcThanhToan)  → GuiYeuCauThanhToan (sinh QR/online nếu có)
  → ThanhToan  ⇒ trạng thái DaThanhToan
        ├─ DonHangThanhToan + DonHangPtThanhToan (ghi nhận tiền/PTTT)
        ├─ trừ tồn chốt (WarehouseProduct)
        ├─ SyncOrderToMarketingCampaign (KM/CTV/loyalty)
        ├─ Branch: ThuChi / báo cáo doanh số / hoa hồng nhân viên
        └─ (tùy chọn) PhatHanhHoaDon → Invoice service (Misa/Viettel) ⇄ callback UpdateTrangThaiPhatHanhHoaDon
  → InDon / InDonBepNv (in hóa đơn + in bếp)
  (Hủy: HuyHoaDon rollback về ChuaThanhToan; Xóa: chỉ khi ChuaThanhToan + qua guard, rollback cọc/contact/ảnh)
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`TinhToanLaiTongTienTra` KHÔNG có attribute auth** (DonHangController:623) — chỉ `[HttpPost]`, không SBAuthorize/AllowAnonymous → áp default controller; comment ghi "Dùng cho mobile". Cần xác nhận có chủ ý (rủi ro tính tiền không token).
- **`DsDangPhucVuBiaRutGonAllow` là `[AllowAnonymous]`** (DonHangController:1313) — màn hiển thị bàn công khai theo `DsDangPhucVuBiaRutGonAllowDto`; lộ trạng thái bàn cho ẩn danh.
- **`GetChiTietDonTheoId/{id}` `[AllowAnonymous]`** (DonHangController:387) — link đánh giá đơn, public theo id đơn.
- **Hai service đọc/ghi list song song** — `TimTheoDieuKien`/`DsSpDaXoa` dùng `_storeService` (store-based mới), phần còn lại `_service`; dòng `_service.TimTheoDieuKienAsync` cũ bị comment (DonHangController:89). Lưu ý khi sửa logic list.
- **`CapNhatAllChiTietDonHang` chỉ `[SBAuthorize()]`** (login) — thao tác ghi hàng loạt chi tiết đơn nhưng không gắn permission key riêng.
- **Nhiều endpoint ghi quan trọng chỉ `[SBAuthorize()]`** (PhatHanhHoaDon, TaoHoaDonNhap, ThemBarcode, UpdateThoiGianTongDonHang, UpdateSanPhamDinhKem...) — permission key bị comment, chỉ còn check tiện ích (với hóa đơn) hoặc login.
- Code thật: BE `Order/Order.API/Controllers/DonHangController.cs` (1711 dòng) + service `Order.Service/Generated/DonHangService.cs`, `DonHangStoreService`, `DonHangThanhToanService`, `DonHangNhanVienService`, `DonHangFileService`, `DonHangGiaoCaService`, `DonHangInvoiceService`; FE `pages/Order/thungan/*` + service `services/order/donhang.service.ts`.
