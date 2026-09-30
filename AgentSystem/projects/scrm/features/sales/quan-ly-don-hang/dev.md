# Dev — Quản lý đơn hàng

> Service: **OrderLogistic** (BE .NET) · FE màn Đơn hàng logistic. Chỉ dành cho dev.
> Trace registry: `registry/kho/DonHangLogisticController.md` (controller `partial` ~248 marker, file Controllers/DonHangLogisticController.cs 1587 dòng). Lưu ý: KeToan / InDon / CauHinhCod cùng prefix tên là controller ĐỘC LẬP (route khác), KHÔNG phải partial của controller này.

## Màn hình & route (FE)
| Màn hình | Ghi chú |
|---|---|
| Danh sách đơn hàng (đơn bán/giao vận) | màn trung tâm: tạo/sửa/chốt đơn, quét IMEI/lô date, đăng đơn Ahamove, cập nhật trạng thái GH/đối soát hàng loạt, phát hành HĐĐT, import/export |

## Endpoint / Controller (trace registry)
`DonHangLogisticController` (Service `OrderLogistic`). Nhóm hành động chính (bảng đầy đủ + path trong registry node):
| Nhóm | Endpoint chính | Quyền (key) |
|---|---|---|
| Vòng đời đơn | `TaoDonLogisticDto`, `ChotDon`, `HuyChotDon`, `CapNhatDonLogisticDto`, `Xoa/{id}` | `DonHangLogistic_*` |
| Xóa hàng loạt | `XoaDonHangByIds` | `DonHangLogistic_XoaDonHangByIds` (DV-AD-TONG) |
| Danh sách / chi tiết | `TimTheoDieuKien` (qua DonHangStoreService — ES/store), `GetChiTietDon(+ToView/ToViewBasic/ByMa)` | `DonHangLogistic_*` |
| Phí VC | `TinhPhiVc` | `DonHangLogistic_TinhPhiVc` |
| Đăng đơn Ahamove | `Crm_DangDonAhamove`, `Crm_HuyDonAhamove`, `CheckSameWarehouseInOrder` | `[SBAuthorize()]` (mở) |
| Cập nhật trạng thái hàng loạt | `CapNhatTTGH`, `CapNhatTTDoiSoat`, `CapNhatTTCareDon`, `CapNhatTTMaDonHang`, `CapNhatGhiChuKeToan`, `CapNhatDonTheoMa` | `[SBAuthorize()]` + **gate runtime** `KiemTraQuyenUser` |
| Quét IMEI / lô date | `MappingImeiSPIntoOrder`, `MappingLoDateSPIntoOrder`, `SaveDonHangAfterScanImei/LoDate` | một phần `DonHangLogistic_*`, một phần `[SBAuthorize]` mở |
| Hoàn thành SP | `HoanThanhSanPham` | `DonHangLogistic_HoanThanhSanPham` |
| HĐĐT | `PhatHanhHoaDon`, `TaoHoaDonNhap`, `Preview..._MeInvoice/_SInvoice` | `[SBAuthorize()]` + gate tiện ích `TI_Invocie_*` |
| Thanh toán | `LuuPtThanhToan`, `XacNhanPtThanhToan` | `[SBAuthorize()]` (key bị comment) |
| KM / voucher | `TinhToanThanhTienVoucher`, `TinhToanThanhTienListVoucher`, `KiemTraCapNhatKM` | `DonHangLogistic_TinhToanThanhTien*` |
| Import / Export | `ImportDonhangLogistic`, `ExportExcelDonHangSaleKieu1/Kieu2` (dùng `_V2`) | import: `DonHangLogistic_*`; export: `[SBAuthorize()]` mở |
| In đơn | `InDon`, `GetDonHangInfoPrint` | `[SBAuthorize()]` (mở) — KHÁC DonHangLogisticInDonController |
| Lịch sử / ghi chú / nhãn | `LichSu_MuaHang_By_SoDienThoai_Search`, `SaveMessage`, `CapNhatNhanKhachHang`, `GetDonHangByKhachHang` | `DonHangLogistic_*` |

Endpoint private (`[PrivateSBAuthorize]`): Contact ↔ Đơn (`CapNhatLaiContactFromDonHang`, `CapNhatSaleUserId`, `XoaDonHangLogisticFromContact`…), Accounting callback HĐĐT (`UpdateTrangThaiPhatHanhHoaDon`, `UpdateTrangThaiHoaDonNhap`), WarehouseProduct ripple combo (`UpdateFromPhieuNhapTraKhach`/`RollBack...`), Bravo/PartnerConnect sync. Xem registry node.

## Entities
- **`DonHangLogistic`** — header đơn. Field `IsYeuCauXuatHoaDon` mặc định lấy `CauHinhSettingDto.IsYeuCauXuatHoaDonMacDinh` của đơn vị lúc tạo (`OrderLogistic.Service/Generated/DonHangService.cs:1047`); đơn ecom set true/false lúc tạo (null→false). Chi tiết cờ: xem feature `ke-toan/hoa-don-yeu-cau-xuat-logistic`.
- **`DonHangLogisticSanPham` / `Combo` / `SanPhamLoDate` / `ChiTietTMDT`** — line đơn.
- **`DonHangDangDon`** — transaction đăng đơn ĐVVC (lấy `IdTransaction` khi đăng Ahamove).
- **`DonHangLogisticKhuyenMai` / `DonHangLogisticKhuyenMaiChiTiet`** — KM áp lên đơn (KM SP/DV & KM đơn). ChiTiet quà KM đơn mang `SoBoiSo` + `IdDieuKienCapBacApDung` (tier nguồn) làm **chữ ký waterfall** để FE round-trip / bắt chọn lại.
- **`DonHangLogisticChotDonSnapshot`** 🆕 — snapshot 1-1 với `DonHangLogistic` (index unique `IdDonHangLogistic`), lưu JSON `ObjectRolebackLogistic` (Warehouse + Promotion + Customer DiemTamTru) + `IdTrans` + `NgayChot` lúc ChotDon. Không bị job cleanup xóa → phục vụ HuyChotDon không giới hạn thời gian (xem "Luồng code").

## Luồng code
- **Lock tạo/chốt đơn:** Redis `LockTaoDon:{ContactId}` SetNotExists(value=3, ttl=60s); release ở `finally` (controller giữ lock để release kể cả khi service throw). ContactId rỗng → bỏ lock.
- **🆕 Lock 2 lớp HuyChotDon ⟷ đăng đơn** (`DonHangService.cs:7652-7661`, release `:8034`): ngoài lock theo SĐT, `HuyChotDon` lấy thêm lock per-đơn **dùng CHUNG** với `ProcessDonHangDangDon` — key `OL_ProcessDonHangDangDon_{orderId}` (`RedisCacheService.LockTake`, ttl 120s). Fail-fast: lấy không được lock = đơn đang đăng đơn → chặn hủy chốt (*"Đơn đang trong quy trình đăng đơn, không thể hủy chốt..."*). Vào được lock rồi **đọc lại đơn** (`TableWithNolock` theo `Id + IdDonVi`) để guard bên dưới thấy state đã commit. Lock **nhả trong `finally` SAU mọi commit** để luồng đăng đơn vào sau đọc đúng state.
- **🆕 HuyChotDon không giới hạn thời gian** (config `IsKhongGioiHanThoiGianHuyChotDon`, default false): rollback ưu tiên đọc `OrderRollBackLogisticDoiSoat` (qua API). Nếu rỗng (đã bị job cleanup 30 ngày xóa) — chỉ khi đơn vị bật config — thì **fallback đọc `DonHangLogisticChotDonSnapshot`** (`DonHangService.cs:7813-7825`) để rollback (warehouse + promotion + customer DiemTamTru). Snapshot được **upsert cùng transaction ChotDon** (`DonHangService.cs:6847-6906`, atomic với chuyển trạng thái DaChot). Config off → giữ giới hạn 30 ngày như cũ.
- **🆕 Áp KM bội số / waterfall lúc tạo/lưu đơn** (`DonHangService.cs` build `lstDonHangLgtKhuyenMai/ChiTiet`, ~`17300-17422`): KM SP/DV `IsBoiSo` bật + có quà → sau khi add quà, gọi `_commonService.GetPromotionPrivate` để tính lại entitlement, set `newEntity.IsCanChonLai` (SL quà đã chọn > entitlement ở SL hiện tại → bắt chọn lại; lỗi dịch vụ → không khóa). KM **đơn** (waterfall/lũy kế cấp bậc): header `DonHangLogisticKhuyenMai` chỉ áp giảm khi `capBac.IsGiamGiaSanPham`; quà stamp `SoBoiSo` + tier nguồn (`IdDieuKienCapBacNguon`, fallback `capBacMatched.Id`) vào ChiTiet làm chữ ký. Bội số = `floor(SL mua / X)` (X=1 nếu null/≤0), cap bởi `BoiSoGioiHanSoLan` — helper `KhuyenMaiBoiSoHelper.TinhSoBoiSo` (`Libs/Models.Core/DTO/Promotion.Client/KhuyenMai/KhuyenMaiBoiSoHelper.cs`).
- **Gate cập nhật hàng loạt:** cụm `CapNhatTT*` / `CapNhatDonTheoMa` / `GetThongTinTheoDanhSachMa` đều `cauHinhQuanTriService.KiemTraQuyenUser(DonViId, UserId)` trước — quyền ở cấu hình quản trị runtime, KHÔNG ở permission key.
- **Gate HĐĐT:** `CheckTienIch` `TI_Invocie_Misa` HOẶC `TI_Invocie_Viettel`.
- **Đăng đơn Ahamove:** set `GiaoHangHinhThucGiaoHangMa` → lấy `IdTransaction` (DonHangDangDon) → POST delivery_connect.
- Service: `IDonHangService` / `Store` / `Multiple` / `Import` / `GiaoDich` / `Rabit` (OrderLogistic.Service/Generated); `IOrderService` (DeliveryUnitConnect — TinhPhiVc, Ahamove); Accounting; WarehouseProduct; Redis.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này. Để session security xử lý.
- **Quyền không nhất quán:** nhiều endpoint nhạy cảm dùng `[SBAuthorize()]` mở (đăng đơn Ahamove, phương thức thanh toán, phát hành HĐĐT, quét IMEI/lô date, InDon, import IMEI) — permission key bị comment; kiểm soát thực qua `KiemTraQuyenUser` hoặc `CheckTienIch` runtime → khó audit.
- **Dead/FixBug endpoint còn lộ API:** `FixBugOrderLogistic`, `ExportExcelDonHangSaleFixBug` return ngay (DEAD); `FixSaleDisplayName?key=HaiHN@8290` gate bằng key hardcode (`DonHangLogisticController.cs:1236`) — không phải hệ quyền, nên gỡ/khóa ở production.
- **GET có side-effect:** `FixSaleDisplayName` là GET sửa data.
- **Lock release phụ thuộc `finally`:** nếu controller bị kill → lock tự hết hạn sau 60s (không stuck).
- **Export dùng `_V2`** (`ExportExcelDonHangSaleKieu1_V2/Kieu2_V2`); bản non-V2 còn trong code nhưng comment ở call site — đừng sửa nhầm bản cũ.
- **`InDon` ở controller này KHÁC `DonHangLogisticInDonController`** (controller sau chỉ cấu hình in) — đừng nhầm.
- **🆕 Bội số `IsCanChonLai` phụ thuộc dịch vụ ngoài:** proxy khóa chọn-lại quà gọi `GetPromotionPrivate`; `catch { }` nuốt lỗi → **không khóa** khi dịch vụ Promotion lỗi (fail-open) → quà có thể vượt entitlement mà không bị bắt chọn lại.
- **🆕 Snapshot fallback chỉ đúng khi config bật TRƯỚC chốt:** `DonHangLogisticChotDonSnapshot` chỉ được ghi lúc ChotDon; nếu đơn chốt trước khi bật `IsKhongGioiHanThoiGianHuyChotDon` (hoặc code snapshot chưa deploy) → sau 30 ngày vẫn không hủy chốt được (không có snapshot để fallback). Snapshot dùng `TableWithNolock` để tìm existing khi upsert → về lý thuyết có thể chèn trùng dưới race, nhưng thao tác chốt đã serialize qua lock đơn.

## File source
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderLogistic\...\Controllers\DonHangLogisticController.cs` (1587 dòng, partial).
- Registry node: `projects/scrm/registry/kho/DonHangLogisticController.md` (xem thêm `DonHangLogisticInDonController.md`, `DonHangLogisticCauHinhHoTroShipFeeController.md`).
