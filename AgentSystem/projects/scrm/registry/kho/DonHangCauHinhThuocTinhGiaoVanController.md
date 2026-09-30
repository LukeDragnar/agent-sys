---
controller: DonHangCauHinhThuocTinhGiaoVanController
module: order-logistic
service: OrderLogistic
man_hinh_ui: "Cấu hình > Giao vận > Cấu hình thuộc tính giao vận theo sản phẩm (màn cấu hình per chi nhánh); auto-fill BE-check khi lên đơn (sales/thu ngân)"
muc_dich: "Cấu hình tự động gán thuộc tính giao vận theo sản phẩm/đối tượng — generic theo ĐVVC + thuộc tính (VD GHTK nhãn đơn = (GHTK, PropValue1))"
alias: ["cấu hình thuộc tính giao vận", "auto gán nhãn đơn theo sản phẩm", "cấu hình nhãn GHTK theo SP", "gán thuộc tính ĐVVC tự động"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonHangCauHinhThuocTinhGiaoVan, DonHangCauHinhThuocTinhGiaoVanChiTiet]
stored_procs: []
depends: [OrderLogistic.Service.DonHangCauHinhThuocTinhGiaoVanService]
cross_module_in: []
cross_module_out: []
---

# DonHangCauHinhThuocTinhGiaoVanController — Cấu hình tự động gán thuộc tính giao vận theo sản phẩm

> ⚠️ **Snapshot — verify với code hiện tại.** Quét tại `origin/release_product` HEAD `e617abcbbf` (short). Path:line có thể lệch khi source đổi — code là chân lý, node là bản đồ.
> Controller MỚI (branch `thiennq/don-hang-cau-hinh-thuoc-tinh-giao-van`). Đăng ký tiện ích `[TienIch(..., MaTienIchSCRM.TI_OrderLogistic)]`.
> Nguồn: `OrderLogistic/OrderLogistic.API/Controllers/DeliveryConnect/DonHangCauHinhThuocTinhGiaoVanController.cs:13-113`, service backing `OrderLogistic.Service/Generated/DonHangCauHinhThuocTinhGiaoVanService.cs`.

## Bối cảnh nghiệp vụ
Cấu hình cho phép ĐVVC (đơn vị vận chuyển) tự động nhận **thuộc tính giao vận** (VD nhãn đơn GHTK) dựa trên **sản phẩm/đối tượng** trong đơn. Mô hình generic: 1 dòng cấu hình = (ĐVVC, thuộc tính, **giá trị**) ↔ danh sách **đối tượng** áp dụng. Khi lên đơn, BE tự đối chiếu đối tượng trong đơn để trả nhãn khớp (auto-fill).

- Controller-level `[SBAuthorize()]` (chỉ cần login) — quyền chi tiết áp per-endpoint.
- **3 endpoint ĐỌC = mở (chỉ login):** auto-fill lúc lên đơn (sales/thu ngân) cũng phải gọi được, chỉ đọc, không nhạy cảm.
- **5 endpoint GHI = chỉ ADMIN đơn vị** (`DV-AD-TONG, DV-AD-CN, DV-AD-MCN`), dùng 3 permission key: `_Save`, `_Xoa`, `_Copy` (Thêm/Xóa đối tượng tái dùng key `_Save`/`_Xoa`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình theo sản phẩm | (màn cấu hình / auto-fill) | POST `api/DonHangCauHinhThuocTinhGiaoVan/GetDonHangCauHinhThuocTinhGiaoVan` | (chỉ login) | public | luôn | Trả IsEnabled + danh sách giá trị↔đối tượng của 1 SP | EF (service) | — |
| 2 | Auto-fill nhãn theo đối tượng | (BE-check lúc lên đơn) | POST `.../GetNhanTheoDoiTuong` | (chỉ login) | public | khi lên đơn | Gửi đối tượng trong đơn → trả nhãn khớp (auto-fill) | EF (service) | — |
| 3 | Lấy gộp cấu hình 1 chi nhánh | Màn xem read-only | POST `.../GetAllCauHinhThuocTinhGiaoVan` | (chỉ login) | public | luôn | Trả GỘP mọi ĐVVC + thuộc tính của 1 chi nhánh | EF (service) | — |
| 4 | Lưu 1 dòng cấu hình | Lưu | POST `.../SaveDonHangCauHinhThuocTinhGiaoVan` | `DonHangCauHinhThuocTinhGiaoVan_Save` | internal (ADMIN đơn vị) | quyền ADMIN | Upsert 1 dòng (1 giá trị + danh sách đối tượng) | EF (service) | — |
| 5 | Xóa 1 dòng cấu hình | Xóa | POST `.../XoaDonHangCauHinhThuocTinhGiaoVan` | `DonHangCauHinhThuocTinhGiaoVan_Xoa` | internal (ADMIN đơn vị) | quyền ADMIN | Xóa 1 dòng theo giá trị | EF (service) | — |
| 6 | Thêm lô đối tượng vào 1 giá trị | Tick chọn + Thêm | POST `.../ThemDoiTuongGiaoVan` | `DonHangCauHinhThuocTinhGiaoVan_Save` | internal (ADMIN đơn vị) | quyền ADMIN | Thêm delta/batch đối tượng (tick 1 = list 1, chọn cả trang = list N) | EF (service) | — |
| 7 | Xóa lô đối tượng khỏi 1 giá trị | Bỏ tick + Xóa | POST `.../XoaDoiTuongGiaoVan` | `DonHangCauHinhThuocTinhGiaoVan_Xoa` | internal (ADMIN đơn vị) | quyền ADMIN | Xóa delta/batch đối tượng (bỏ tick 1 = list 1, bỏ cả trang = list N) | EF (service) | — |
| 8 | Sao chép cấu hình giữa chi nhánh | Copy | POST `.../CopyDonHangCauHinhThuocTinhGiaoVan` | `DonHangCauHinhThuocTinhGiaoVan_Copy` | internal (ADMIN đơn vị) | quyền ADMIN | Chi nhánh đích giống hệt chi nhánh nguồn | EF (service) | — |

## Quy tắc nghiệp vụ (gating / phân quyền)
1. Endpoint **đọc** (1–3) → chỉ cần login. Lý do: auto-fill lúc lên đơn (role sales/thu ngân) cũng gọi endpoint 1 & 2 — nếu siết quyền ADMIN thì auto-fill gãy. Nguồn: `DonHangCauHinhThuocTinhGiaoVanController.cs:30-58`.
2. Endpoint **ghi** (4–8) → `[SBAuthorize("<key>", ..., "DV-AD-TONG, DV-AD-CN, DV-AD-MCN")]` — chỉ 3 role ADMIN đơn vị. Nguồn: `:63-113`.
3. Cặp key tái dùng: Thêm/Xóa **đối tượng** dùng chung key với Lưu/Xóa **dòng** (`_Save`/`_Xoa`), chỉ Copy có key riêng `_Copy`.
4. Mô hình dữ liệu generic: `DonHangCauHinhThuocTinhGiaoVan` (header per ĐVVC + thuộc tính) 1-N `DonHangCauHinhThuocTinhGiaoVanChiTiet` (giá trị ↔ đối tượng áp dụng).

## Quan hệ
- **Gọi sang:** `IDonHangCauHinhThuocTinhGiaoVanService` (namespace `OrderLogistic.Service.Generated.DeliveryUnitConnect`).
- **Dữ liệu (entities):** `DonHangCauHinhThuocTinhGiaoVan`, `DonHangCauHinhThuocTinhGiaoVanChiTiet`.
- **Ripple / đồng bộ:** luồng lên đơn (`DonHangLogisticController` / lên đơn giao vận) gọi `GetNhanTheoDoiTuong` để auto-fill thuộc tính giao vận — VERIFY caller thực tế ở phía lên đơn khi triage (chưa liệt kê cạnh graph vì chưa xác nhận caller cụ thể trong lần quét này).

## Lỗi đã biết / lưu ý kỹ thuật
- DTO request/response nằm ở `Models.Core.DTO.OrderLogistic.Client.DeliveryConnect.DonHangCauHinhThuocTinhGiaoVanModel` (`Req*` classes). Nguồn: `DonHangCauHinhThuocTinhGiaoVanController.cs:5`.
- Endpoint đọc mở (chỉ login) là CHỦ Ý — đừng "siết cho chặt" thành ADMIN, sẽ làm gãy auto-fill lúc lên đơn.

---

## Phụ lục — Delta OrderLogistic khác cần curator xử lý (không thuộc controller này)
> Ghi kèm để người duyệt kho biết còn hạng mục cần tạo/cập nhật node/feature riêng. Đã verify path:line ở HEAD `e617abcbbf`.

1. **ApplyGiaVonOnDonHangLogisticConsumer (Phase 5 giá vốn cross-service)** — `OrderLogistic/OrderLogisticConsumer.API/Consumer/ApplyGiaVonOnDonHangLogisticConsumer.cs:35-162`. Nhận `ApplyGiaVonOnDonHangLogisticMessage`, cross-DB query `KhoChiTietGiaVonTheoKy` (`TrangThaiSnapshot=2, IsDelete=0`) + `SanPham.BangQuyDoiJson` từ conn `SCRM_WarehouseProductStr`. Self-chained paging `PageSize=100` (Skip Offset/Take), chỉ đơn `DonHangTrangThaiChotDon==1` lọc `DonHangNgayChot` trong kỳ. Update `DonHangLogisticSanPham.GiaNhapTrungBinh` + Mongo log collection `GiaVonTheoKyJobUpdateLog` `LoaiBang=3`, idempotent (`Math.Abs(giaVonMoi-giaVonCu) < 0.0001` → skip). Publish `CrossServiceApplyBatchDoneMessage` `LoaiNhanh=5`. V1 lookup snapshot theo (IdSP, Năm/Tháng ngày chốt); nhiều snapshot khác kho cùng tháng → `list.Average()`. → **Đề xuất feature-note** `features/OrderLogistic/apply-gia-von-phase-5/`.

2. **DonHangLogisticChotDonSnapshot — hủy chốt đơn không giới hạn 30 ngày** — `OrderLogistic/OrderLogistic.DataContext/Entity/DonHangLogisticChotDonSnapshot.cs:12-31`. Bảng 1-1 với `DonHangLogistic` (`IdDonHangLogistic` unique). Lúc ChotDon upsert `Data` (JSON `ObjectRolebackLogistic`: Warehouse + Promotion + Customer DiemTamTru) + `IdTrans`. Lúc HuyChotDon, nếu `OrderRollBackLogisticDoiSoat` đã bị job cleanup 30 ngày xóa → fallback đọc bảng này để rollback → **behavior đổi: hủy chốt đơn không còn bị chặn 30 ngày**. Liên quan commit `7cb13d0065`. → **Đề xuất lesson/behavior note**.

3. **DonHangLogisticController — 2 endpoint mới** — `OrderLogistic/OrderLogistic.API/Controllers/DonHangLogisticController.cs`:
   - `TimDonCuaToi` (`:111-123`): `[KPI-CANHAN]` ÉP `gridInfo.IdUserSales = [CurrentUser.Id]` (bỏ giá trị client gửi), key `DonHangLogistic_TimDonCuaToi`, client chỉ truyền IdCuaHangDMS + khoảng ngày. Dùng chung store `TimTheoDieuKienAsync`.
   - `CheckMaDonTonTaiPrivate/{idDonVi:Guid}` (`:1604-1610`): `[PrivateSBAuthorize]` service-to-service, nhận `List<string> maList` → trả list mã tồn tại; **Warranty.Import** gọi validate mã đơn online.
   - Lưu ý: endpoint `ThongKeDonTheoSale` đã GỠ (comment `:125`) — chưa có, bổ sung sau (DB Report).
   → **Cập nhật node** `registry/OrderLogistic/DonHangLogisticController.md`.

4. **CommonController — 7 endpoint nội bộ `[PrivateSBAuthorize]`** (WarehouseProduct job rà soát tồn kho + tái tạo phiếu) — `OrderLogistic/OrderLogistic.API/Controllers/Common/CommonController.cs:93-154`:
   - `GetStateRaSoatByDonIds` (`:94-100`) — luật B/C/D, state batch đơn online.
   - `GetDonChotCanKiemTraPhieu` (`:103-109`) — luật G, bỏ qua `IsChotDonBoQuaKhoChiTiet=true`.
   - `GetDonTaoPhieuChoXuat` (`:112-118`) — CASE 1 phiếu 11 (chờ xuất), keyset theo NGÀY CHỐT.
   - `GetDonTaoPhieuGiaoVan` (`:121-127`) — CASE 2 phiếu 12 (giao vận), keyset theo NGÀY ĐĂNG.
   - `GetDonTaoPhieuHoan` (`:130-136`) — CASE 3 nhập 6 (hoàn), keyset theo NGÀY ĐỐI SOÁT.
   - `GetImeiMovements` (`:139-145`) — replay movement serial IMEI theo lô Imei.
   - `CapNhatGiaVonDonLogistic` (`:148-154`) — lan giá vốn xuống `DonHangLogisticSanPham.GiaNhapTrungBinh` theo (đơn, SP).
   → **Cập nhật node** `registry/OrderLogistic/CommonController.md` (mục Endpoint nội bộ + cạnh `cross_module_in: [WarehouseProduct]`).

5. **Field mới trên `DonHangLogistic`** (`OrderLogistic/OrderLogistic.DataContext/Entity/DonHangLogistic.cs` — verify field khi cập nhật): `IdCuaHangDMS`/`IdKhoDMS`/`IdTuyenDMS` (tham chiếu SaleDMS, KHÔNG FK — tạo phiếu nhập kho đại lý qua RabbitMQ + KPI cá nhân theo tuyến), `IsChotDonBoQuaKhoChiTiet` (job rà soát bỏ qua), `IsYeuCauXuatHoaDon` (thuộc feature YeuCauXuatHoaDon đã fresh 13/7).

6. **Behavior khác đáng ghi (đề xuất lesson/registry behavior):** Lazada nhận diện ngang Shopee/Tiktok (`EnumEcommerce.LAZADA`, map EcommerceId ở DonHangService + `GetEcommerceCode` ReturnsService, `FROM_LAZADA`); tạo PHIẾU BÁN tự động khi cập nhật trạng thái giao hàng theo đơn (map địa chỉ+MST/CCCD+ngày giao, fallback ngày giao theo thời điểm cập nhật); `DonHangHoan.TimTheoDieuKien`+`GetTabCounters` chuyển LINQ→store (fix hiển thị SL hoàn đơn online); KeToanTacNghiep export thêm cột mkt/AdId + UserDisplayMarketing/UserNameMarketing; KhoTacNghiep rà soát/reset tồn kho; ShopeePay vào phương thức thanh toán; Combo tùy chọn + export mã combo; promotion big update (bội số, đơn vị tính); voucher chọn nhóm sản phẩm; tìm kiếm ES v2 DonHangMessage (thêm `where IdDonVi`).

> **Sau khi ghi kho:** cập nhật watermark OrderLogistic → `e617abcbbf` (origin/release_product HEAD lúc quét). Sub-feature `hoa-don-yeu-cau-xuat-logistic` đã fresh 13/7 — không lặp lại.
