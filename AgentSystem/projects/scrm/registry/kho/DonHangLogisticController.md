---
controller: DonHangLogisticController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > Danh sách đơn hàng (đơn bán/giao vận) — màn TRUNG TÂM: tạo/sửa/chốt đơn, quét IMEI/lô date, đăng đơn Ahamove, cập nhật trạng thái giao hàng/đối soát hàng loạt, phát hành HĐĐT, import/export Excel"
muc_dich: "Controller trung tâm của đơn hàng logistic: vòng đời đơn bán (tạo → chốt → cập nhật → xóa), tính phí VC, quét IMEI/lô date khi xuất kho, đăng đơn Ahamove sang ĐVVC, cập nhật hàng loạt trạng thái giao hàng/đối soát/care/kế toán theo mã, phát hành hóa đơn điện tử (Misa/Viettel/SInvoice), import/export. Đăng đơn ĐVVC tổng quát ở controller riêng (DonHangGiaoVanDoiTac)."
alias: ["đơn hàng logistic", "tạo đơn", "chốt đơn", "đơn bán giao vận", "đăng đơn ahamove", "cập nhật trạng thái giao hàng", "quét imei đơn", "phát hành hóa đơn đơn hàng", "import đơn hàng logistic", "đơn sale"]
auth: mixed
state_machine: [Nhap, DaChot, DaDang, DaLayHang, DaGiaoHang, Huy]
deprecated: false
entities: [DonHangLogistic, DonHangLogisticSanPham, DonHangLogisticCombo, DonHangLogisticSanPhamLoDate, DonHangLogisticChiTietTMDT, DonHangDangDon]
stored_procs: []
depends:
  - "IDonHangService / IDonHangStoreService / IDonHangMultipleService / IDonHangImportService / IDonHangGiaoDichService / IDonHangRabitService (OrderLogistic.Service/Generated/...)"
  - "IOrderService (DeliveryUnitConnect — TinhPhiVc, Crm_DangDonAhamove, Crm_HuyDonAhamove)"
  - "IThuKhoTacNghiepService (tin nhắn nội bộ/ghi chú); ICauHinhQuanTriService (KiemTraQuyenUser cập nhật theo mã)"
  - "IRedisCacheService (lock tạo đơn theo ContactId); IAccountingService (HĐĐT/phiếu bán); IWarehouseProductService"
cross_module_in:
  - "Contact (CapNhatLaiContactFromDonHang, CapNhatSaleUserId, CapNhatIdNhomNhanVien, CapNhatPhanBo, XoaDonHangLogisticFromContact… — [PrivateSBAuthorize])"
  - "Accounting (UpdateTrangThaiPhatHanhHoaDon, UpdateTrangThaiHoaDonNhap — callback trạng thái HĐĐT)"
  - "WarehouseProduct (UpdateFromPhieuNhapTraKhach, RollBackUpdateFromPhieuNhapTraKhach — trừ/cộng combo khi nhập trả khách)"
  - "Bravo/PartnerConnect (GetChiTietDonHangLogistic_Bravo, GetDonHangLgtByIdsSyncBravo)"
cross_module_out:
  - "delivery_connect (Crm_DangDonAhamove → /api/order/CreateOrderAhamove; Crm_HuyDonAhamove)"
  - "Accounting (PhatHanhHoaDon/TaoHoaDonNhap/PreviewHoaDon..., LuuPtThanhToan, UpdateKeToanNangCao → chứng từ + HĐĐT)"
  - "Contact (đồng bộ đơn ↔ contact: nhãn KH, tin nhắn nội bộ, xóa đơn)"
  - "WarehouseProduct (quét IMEI/lô date → MappingImeiSPIntoOrder, SaveDonHangAfterScanImei/LoDate; HoanThanhSanPham)"
---

# DonHangLogisticController — Đơn hàng logistic (trung tâm đơn bán/giao vận)

> Controller `partial` rất lớn (~248 attribute/method markers, file Controllers/DonHangLogisticController.cs 1587 dòng). Grep `class DonHangLogisticController` → chỉ 1 file partial này (KeToan/InDon/CauHinhCod là controller riêng cùng prefix tên, KHÔNG phải partial của controller này). Bảng dưới chỉ giữ HÀNH ĐỘNG CÓ UI; phần private/service-to-service gom ở "Endpoint nội bộ".

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo đơn | "Tạo đơn" (chỉ sale) | POST `api/DonHangLogistic/TaoDonLogisticDto` | `DonHangLogistic_TaoDonLogisticDto` (DV-SALE, DV-SALE-LEAD, DV-AD-TONG, DV-AD-CN, DV-CHIASO) | public | sale | Tạo đơn nháp. Nếu có ContactId → lock Redis `LockTaoDon:{ContactId}` (3s/60s) chặn tạo trùng cùng SĐT; release ở finally | EF + Redis | - |
| 2 | Chốt đơn | "Chốt đơn" | POST `api/DonHangLogistic/ChotDon` | `DonHangLogistic_ChotDon` (DV-AD-TONG, DV-AD-CN, DV-SALE, DV-SALE-LEAD, DV-QL-SALE) | public | đơn nháp | Chốt đơn (set DonHangNgayChot, sang DaChot); lock theo ContactId; **upsert `DonHangLogisticChotDonSnapshot` (1-1) cùng transaction** — JSON ObjectRolebackLogistic + IdTrans để HuyChotDon fallback (`OrderLogistic.Service/Generated/DonHangService.cs:6847-6906`) | EF + Redis | - |
| 3 | Hủy chốt đơn | "Hủy chốt" | POST `api/DonHangLogistic/HuyChotDon` | `DonHangLogistic_HuyChotDon` (DV-AD-TONG, DV-AD-CN, DV-SALE, DV-SALE-LEAD, DV-QL-SALE) | public | đơn đã chốt | Revert đơn về nháp. **Lock 2 lớp:** lock SĐT + lock chung `OL_ProcessDonHangDangDon_{orderId}` (ttl 120s) với luồng đăng đơn — đang đăng đơn → chặn ("Đơn đang trong quy trình đăng đơn..."); đọc lại đơn dưới lock. Rollback: ưu tiên `OrderRollBackLogisticDoiSoat`; rỗng + đơn vị bật `IsKhongGioiHanThoiGianHuyChotDon` → fallback snapshot (không giới hạn 30 ngày) (`DonHangService.cs:7652-7661,7813-7825`) | EF + Redis | Fallback snapshot chỉ có khi đơn chốt SAU khi bật config + deploy code snapshot; đơn cũ vẫn kẹt giới hạn 30 ngày |
| 4 | Cập nhật đơn | "Sửa" | POST `api/DonHangLogistic/CapNhatDonLogisticDto` | `DonHangLogistic_CapNhatDonLogisticDto` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-SALE, DV-SALE-LEAD, HT-CSKH, DV-QL-KHO, DV-QL-KHO-TONG, DV-KT) | public | - | Cập nhật thông tin đơn | EF | - |
| 5 | Xóa đơn | "Xóa" | POST `api/DonHangLogistic/Xoa/{id}` | `DonHangLogistic_Xoa` (DV-AD-TONG, DV-AD-CN, DV-SALE, DV-SALE-LEAD, DV-QL-SALE) | public | - | Xóa 1 đơn | EF | - |
| 6 | Xóa nhiều đơn theo filter | "Xóa hàng loạt" | POST `api/DonHangLogistic/XoaDonHangByIds` | `DonHangLogistic_XoaDonHangByIds` (DV-AD-TONG) | public | admin tổng | Xóa nhiều đơn theo filter; ghi log xóa | EF | Chỉ DV-AD-TONG; thao tác phá data hàng loạt |
| 7 | Danh sách đơn | grid + filter | POST `api/DonHangLogistic/TimTheoDieuKien` | `DonHangLogistic_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-SALE, DV-SALE-LEAD, HT-CSKH, DV-QL-CG) | public | màn list | List đơn theo filter (đi qua DonHangStoreService — ES/store) | EF/ES | - |
| 8 | Xem chi tiết đơn | mở đơn | POST `api/DonHangLogistic/GetChiTietDon` (+ `...ToView` / `...ToViewBasic` / `GetChiTietDonByMa`) | `DonHangLogistic_GetChiTietDon` / `_GetChiTietDonByMa` (nhiều role gồm kho/kế toán) | public | - | Trả chi tiết đơn (các biến thể view/basic/by-mã) | EF | - |
| 9 | Tính phí vận chuyển | "Tính phí VC" | POST `api/DonHangLogistic/TinhPhiVc` | `DonHangLogistic_TinhPhiVc` (AD/SALE/CSKH/kho/KT…) | public | - | Tính phí VC qua OrderService; áp tier cấu hình hỗ trợ ship → cập nhật DonHangHoTroShipFee | EF + cache | - |
| 10 | Đăng đơn Ahamove | "Đăng đơn" (Ahamove) | POST `api/DonHangLogistic/Crm_DangDonAhamove` (+ `CheckSameWarehouseInOrder`, `GetThongTinDangDonNguoiGui`) | `[SBAuthorize()]` (mở) | public | đơn đủ điều kiện | Set GiaoHangHinhThucGiaoHangMa, lấy IdTransaction từ DonHangDangDon, POST sang delivery_connect `/api/order/CreateOrderAhamove`; lỗi sàn → trả lỗi | EF + HTTP | Quyền mở `[SBAuthorize()]` — không key riêng; đăng đơn ĐVVC tổng quát ở DonHangGiaoVanDoiTacController |
| 11 | Hủy đơn Ahamove | "Hủy đăng đơn" | POST `api/DonHangLogistic/Crm_HuyDonAhamove` (+ `CheckHuyDonAhamove`) | `[SBAuthorize()]` (mở) | public | đơn đủ điều kiện hủy | Hủy đơn đã đăng Ahamove qua delivery_connect | EF + HTTP | - |
| 12 | Cập nhật trạng thái giao hàng (hàng loạt theo mã) | "Cập nhật TTGH" | POST `api/DonHangLogistic/CapNhatTTGH` (+ `CapNhatDonTheoMa`, `CapNhatTTDoiSoat`, `CapNhatTTCareDon`, `CapNhatTTMaDonHang`, `CapNhatTTHuyDangDonKhongApi`, `CapNhatGhiChuKeToan`) | `[SBAuthorize()]` (mở) + **gate runtime** `KiemTraQuyenUser` | public | có quyền theo cấu hình quản trị | Tất cả chạy qua `cauHinhQuanTriService.KiemTraQuyenUser(DonViId, UserId)`; fail → "Bạn không có quyền". Cập nhật hàng loạt trạng thái GH/đối soát/care/mã/ghi chú | EF | Quyền KHÔNG qua permission key mà qua cấu hình quản trị runtime (KiemTraQuyenUser) → kiểm soát ngoài hệ quyền chuẩn |
| 13 | Cập nhật từng đơn theo mã | "Cập nhật từng đơn" | POST `api/DonHangLogistic/CapNhatTungDon` (+ `LayDonHangCapNhatByMa`) | `DonHangLogistic_CapNhatTungDon` / `_LayDonHangCapNhatByMa` (DV-AD-TONG) | public | admin tổng | Sửa từng đơn theo mã | EF | - |
| 14 | Quét IMEI vào đơn | màn quét IMEI | POST `api/DonHangLogistic/MappingImeiSPIntoOrder` (+ `GetChiTietDonForScanImeiSP`, `CapNhatImei`, `SaveDonHangAfterScanImei`) | `DonHangLogistic_MappingImeiSPIntoOrder` / `_GetChiTietDonForScanImeiSP` (AD/CSKH/kho/KT); một số biến thể `[SBAuthorize]` mở | public | đơn đang xuất | Map IMEI quét được vào SP đơn; lưu đơn sau quét | EF | Vài endpoint scan để `[SBAuthorize]` mở (CapNhatImei, GetChiTietDonForScanLoDateSP) — không key |
| 15 | Quét lô date vào đơn | màn quét lô date | POST `api/DonHangLogistic/MappingLoDateSPIntoOrder` (+ `GetChiTietDonForScanLoDateSP/{maDonHang}`, `SaveDonHangAfterScanLoDate`, `GetLoDateDetailsByIdDonHangLgtSanPham`, `LuuLoDateSauKhiPick`) | `[SBAuthorize]` (mở) | public | đơn SP lô date | Map lô date vào SP đơn; lưu sau pick | EF | Quyền mở |
| 16 | Hoàn thành sản phẩm (xuất kho) | "Hoàn thành SP" | POST `api/DonHangLogistic/HoanThanhSanPham` | `DonHangLogistic_HoanThanhSanPham` (AD/CSKH/kho/KT) | public | - | Đánh dấu SP hoàn thành (xuất kho) | EF | Ripple tồn kho qua WarehouseProduct (phiếu xuất) |
| 17 | Phát hành HĐĐT | "Phát hành hóa đơn" | POST `api/DonHangLogistic/PhatHanhHoaDon` (+ `Preview..._MeInvoice`, `Preview..._SInvoice`, `LayThongTinThue`, `CapNhatThongTinThue`, `HuyPhatHanhTuDong`) | `[SBAuthorize()]` (mở) + gate tiện ích `TI_Invocie_Misa`/`TI_Invocie_Viettel` | public | có tiện ích HĐĐT | Phát hành/preview hóa đơn điện tử (Misa/Viettel/SInvoice); chặn nếu không có tiện ích invoice | EF + Accounting | Quyền mở, gate bằng CheckTienIch invoice runtime |
| 18 | Hóa đơn nhập (HĐĐT đầu vào) | "Tạo HĐ nhập" | POST `api/DonHangLogistic/TaoHoaDonNhap` (+ `XoaHoaDonNhap`, `DongBoTrangThaiHoaDonNhap`) | `[SBAuthorize()]` (mở) + gate tiện ích invoice | public | có tiện ích HĐĐT | Tạo/xóa/đồng bộ hóa đơn nhập | EF + Accounting | - |
| 19 | Phương thức thanh toán | màn thanh toán đơn | POST `api/DonHangLogistic/LuuPtThanhToan` (+ `GetPtThanhToan`, `XoaPtThanhToan`, `XacNhanPtThanhToan`, `XacNhanAllPtThanhToan`) | `[SBAuthorize()]` (mở) | public | - | CRUD + xác nhận phương thức thanh toán đơn | EF | Permission key bị comment, để mở |
| 20 | Tính KM / voucher | màn áp KM | POST `api/DonHangLogistic/TinhToanThanhTienVoucher` (+ `TinhToanThanhTienListVoucher`, `GetChiTietKhuyenMaiDonHangLogistic/{id}`, `KiemTraCapNhatKM`) | `DonHangLogistic_TinhToanThanhTienVoucher` / `_TinhToanThanhTienListVoucher` (AD/SALE/CSKH) | public | - | Tính thành tiền sau voucher/KM cho đơn. **KM bội số/waterfall:** áp KM SP/DV `IsBoiSo` (`floor(SL mua/X)`, cap `BoiSoGioiHanSoLan`, helper `KhuyenMaiBoiSoHelper.TinhSoBoiSo`) + KM đơn lũy kế cấp bậc; stamp `SoBoiSo` + tier nguồn vào `DonHangLogisticKhuyenMaiChiTiet`; proxy `GetPromotionPrivate` set `IsCanChonLai` bắt chọn lại khi quà > entitlement (`DonHangService.cs:~17300-17422`) | EF + HTTP | Proxy `IsCanChonLai` fail-open (`catch{}` nuốt lỗi Promotion → không khóa) |
| 21 | Lịch sử mua hàng theo SĐT | tab lịch sử KH | POST `api/DonHangLogistic/LichSu_MuaHang_By_SoDienThoai_Search` (+ `..._TongTien`) | `DonHangLogistic_LichSu_MuaHang_By_SoDienThoai_Search` / `..._TongTien` (nhiều role) | public | - | List + tổng tiền đơn lịch sử theo SĐT | EF | - |
| 22 | Ghi chú / tin nhắn nội bộ đơn | panel ghi chú | POST `api/DonHangLogistic/SaveMessage` (+ `GetListMessageByIdDonHangLogistic`, `GetListMessageByPhone`) | `DonHangLogistic_SaveMessage` / `_GetListMessage...` (AD/SALE/QL-SALE…) | public | - | Lưu/đọc ghi chú nội bộ theo đơn / SĐT | EF | - |
| 23 | Cập nhật nhãn khách hàng | gán nhãn KH | POST `api/DonHangLogistic/CapNhatNhanKhachHang` | `DonHangLogistic_CapNhatNhanKhachHang` (AD/SALE/MKT/CHIASO) | public | - | Cập nhật nhãn KH của contact gắn đơn | EF | - |
| 24 | Đơn trùng sản phẩm | tab cảnh báo trùng | POST `api/DonHangLogistic/GetDanhSachTrungSanPham` | `DonHangLogistic_GetDanhSachTrungSanPham` (nhiều role) | public | - | List đơn trùng SP (chống đơn ảo/spam) | EF | - |
| 25 | Đơn theo khách hàng | tab KH | POST `api/DonHangLogistic/GetDonHangByKhachHang` | `DonHangLogistic_GetDonHangByKhachHang` (AD/QLDV/KT/SALE/MKT…) | public | - | List đơn 1 KH đã mua | EF | - |
| 26 | Export Excel đơn (kiểu 1/2) | "Export Excel" | POST `api/DonHangLogistic/ExportExcelDonHangSaleKieu1` (+ `Kieu2`) | `[SBAuthorize()]` (mở) | public | - | File `DonHangSale.xlsx` (dùng `_V2`); lỗi → 503 nếu Service_Unavailable, else 500 `EXPORT_ERROR` | EF | Endpoint `ExportExcelDonHangSaleFixBug` = DEAD (return ngay đầu hàm) |
| 27 | Import Excel đơn | "Import To Excel" | POST `api/DonHangLogistic/ImportDonhangLogistic` (+ `DownloadTemplateOrderAsync`, `DownloadTemplateDMTHXAsync`, `GetListDonHangImport`, `InsertDonHangImportToDonHangLogistic`, `XoaDonHangImport`) | `DonHangLogistic_ImportDonhangLogistic` + cụm `_*Import*` (SALE/AD/CHIASO/kho) | public | - | Upload → tmp → insert thật; download template; xóa tmp | EF | - |
| 28 | Import IMEI đơn | "Import IMEI" | POST `api/DonHangLogistic/ImportImeiDonHang` (+ `DownloadTemplateImeiDonHang`) | `[SBAuthorize]` (mở) | public | - | Upload file IMEI gắn đơn; download template | EF | Permission key bị comment |
| 29 | In đơn / phiếu giao | "In đơn" | POST `api/DonHangLogistic/InDon` (+ `GetDonHangInfoPrint`, `GetDonHangInfoPrint`) | `[SBAuthorize()]` (mở) | public | chọn đơn | Trả data in đơn theo list ids (in thực do FE). Lưu ý: KHÁC DonHangLogisticInDonController (chỉ cấu hình) | EF | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- **Contact ↔ Đơn (đồng bộ ngược):** `CapNhatLaiContactFromDonHang`, `CapNhatSaleUserId`, `CapNhatIdNhomNhanVien`, `CapNhatPhanBoIdDonHangLogistic`, `CapNhatIdLanding`, `DongBoTinNhanNoiBoDonHangLogistic`, `CapNhatListIdNhanKhachHang`, `XoaDonHangLogisticFromContact`, `XoaDsDonHangLogisticFromDsContact` — Contact gọi sang để sync sale/nhóm NV/landing/tin nhắn/xóa đơn.
- **Accounting callback HĐĐT:** `UpdateTrangThaiPhatHanhHoaDon`, `UpdateTrangThaiHoaDonNhap` — Accounting báo trạng thái phát hành HĐĐT về đơn.
- **WarehouseProduct ripple combo (nhập trả khách):** `UpdateFromPhieuNhapTraKhach` (trừ SP combo), `RollBackUpdateFromPhieuNhapTraKhach` (cộng lại) — Warehouse gọi khi nhập/hoàn trả lại từ khách.
- **Bravo/PartnerConnect:** `GetChiTietDonHangLogistic`, `GetChiTietDonHangLogistic_Bravo`, `GetDonHangLgtByIdsSyncBravo` — đồng bộ đơn sang Bravo (ERP).
- **Khác (private đọc/đồng bộ):** `ChiTietSanPhamDonHang`, `GetChiTietPhiGiaoDichDonEcomm_Private`, `GetDonHangHoanThanhByFromDateToDate` (cho cập nhật KM), `TimTheoId/{id}`, `LayThongTinCoBanById/{id}`, `TaoDonHangLogisticKhachCu`.
- **FixBug/Dead:** `FixBugOrderLogistic` (return ngay — DEAD), `ExportExcelDonHangSaleFixBug` (return ngay — DEAD), `FixSaleDisplayName?key=HaiHN@8290` (gate bằng key cứng, không phải hệ quyền — dùng vá data thủ công), `PostOrder`, `SyncDonHangToContactForAdmin/{id}` (gate runtime IsCSKH/IsSuperUser).

## Quy tắc nghiệp vụ (gating đáng chú ý)
1. **Lock tạo/chốt đơn theo ContactId:** Redis `LockTaoDon:{ContactId}` SetNotExists(value=3, ttl=60s) — chặn tạo/chốt đơn trùng cùng SĐT; release ở `finally` (controller giữ lock để release kể cả service throw). ContactId rỗng → bỏ qua lock.
2. **Gate "cập nhật hàng loạt theo mã":** cụm `CapNhatTTGH/CapNhatTTDoiSoat/CapNhatTTCareDon/CapNhatTTMaDonHang/CapNhatTTHuyDangDonKhongApi/CapNhatGhiChuKeToan/CapNhatDonTheoMa/GetThongTinTheoDanhSachMa` đều check `cauHinhQuanTriService.KiemTraQuyenUser(DonViId, UserId)` trước → quyền nằm ở **cấu hình quản trị runtime**, KHÔNG ở permission key.
3. **Gate HĐĐT:** PhatHanhHoaDon/TaoHoaDonNhap/Preview... chặn nếu user không có tiện ích `TI_Invocie_Misa` HOẶC `TI_Invocie_Viettel` (CheckTienIch).
4. **SyncDonHangToContactForAdmin:** chỉ chạy nếu `user.IsCSKH==true || user.IsSuperUser`.

## Quan hệ
- **Gọi sang:** delivery_connect (đăng/hủy đơn Ahamove), Accounting (HĐĐT, phương thức thanh toán, kế toán nâng cao), Contact (nhãn KH, tin nhắn, xóa đơn), WarehouseProduct (quét IMEI/lô date, hoàn thành SP → phiếu xuất), Bravo (ERP sync). Redis (lock). 11 service nội bộ inject (DonHangService/Store/Multiple/Import/GiaoDich/Rabit/Order/ThuKho/CauHinhQuanTri…).
- **Dữ liệu (entities):** `DonHangLogistic` (header), `DonHangLogisticSanPham`/`Combo`/`SanPhamLoDate`/`ChiTietTMDT` (line), `DonHangDangDon` (transaction đăng đơn ĐVVC — lấy IdTransaction khi đăng Ahamove).
- **Ripple / đồng bộ:** chốt đơn → kế toán (qua DonHangKeToanController khi đạt trạng thái GH); đăng đơn Ahamove → delivery_connect; hoàn thành SP → phiếu xuất WarehouseProduct (trừ tồn); phát hành HĐĐT → Accounting; mọi thay đổi đơn → đồng bộ Contact + ES store.

## Luồng xuyên module (vòng đời đơn + đăng đơn ĐVVC)
```
TaoDonLogisticDto (nháp, lock theo ContactId)
  → ChotDon (DaChot, set DonHangNgayChot) → đồng bộ Contact
  → TinhPhiVc (áp tier hỗ trợ ship) 
  → [xuất kho] quét IMEI/lô date (MappingImei/LoDate) → HoanThanhSanPham → WarehouseProduct phiếu xuất (trừ tồn)
  → đăng đơn ĐVVC:
       • Ahamove: Crm_DangDonAhamove → set GiaoHangHinhThucGiaoHangMa, lấy IdTransaction (DonHangDangDon)
                  → POST delivery_connect /api/order/CreateOrderAhamove
       • ĐVVC khác (GHN/GHTK/VTP/JNT): ở DonHangGiaoVanDoiTacController (controller riêng)
  → đổi trạng thái giao hàng (DaDang/DaLayHang/DaGiaoHang) hàng loạt: CapNhatTTGH... (gate KiemTraQuyenUser)
       → đạt mốc cấu hình → DonHangKeToan_PhieuChoDonHangOnline_Save → Accounting PhieuBanHang
  → PhatHanhHoaDon (HĐĐT) → Accounting → callback UpdateTrangThaiPhatHanhHoaDon
```
Triage: "tạo đơn trùng" → lock Redis ContactId; "đăng đơn Ahamove lỗi" → DonHangDangDon IdTransaction + response delivery_connect; "không cập nhật được TTGH" → KiemTraQuyenUser (cấu hình quản trị); "đơn không lên kế toán" → xem node DonHangKeToanController.

## Lỗi đã biết / lưu ý kỹ thuật
- **Quyền không nhất quán:** rất nhiều endpoint nhạy cảm dùng `[SBAuthorize()]` mở (đăng đơn Ahamove, phương thức thanh toán, phát hành HĐĐT, quét IMEI/lô date, InDon, import IMEI) — permission key bị comment; kiểm soát thực qua `KiemTraQuyenUser` (cấu hình quản trị) hoặc `CheckTienIch` runtime, không qua hệ quyền chuẩn. Khó audit.
- **Dead/FixBug endpoints còn lộ ra API:** `FixBugOrderLogistic`, `ExportExcelDonHangSaleFixBug` return ngay (DEAD); `FixSaleDisplayName?key=HaiHN@8290` gate bằng key hardcode (DonHangLogisticController.cs:1236) — không phải hệ quyền, nên gỡ/khóa ở production.
- **GET có side-effect** ở các fix endpoint (FixSaleDisplayName là GET sửa data).
- **Lock release phụ thuộc finally:** lock tạo/chốt đơn release ở `finally` — nếu controller bị kill giữa chừng, lock tự hết hạn sau 60s (TTL), không stuck vĩnh viễn.
- **Export dùng `_V2`** (`ExportExcelDonHangSaleKieu1_V2/Kieu2_V2`); bản non-V2 còn trong code nhưng bị comment ở call site — đừng sửa nhầm bản cũ.
- Controller `partial`; chỉ 1 file partial. KeToan/InDon/CauHinhCod cùng prefix là controller ĐỘC LẬP (route khác), không gộp vào node này.
