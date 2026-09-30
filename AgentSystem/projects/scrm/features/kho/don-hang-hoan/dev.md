# Dev — Đơn hàng hoàn (module Kho / OrderLogistic)

> CHỈ dev. Mã thật từ source `scrm` (BE) + `scrm_frontend` (FE).
> Liên kết chặt với `ecommerce_connect`: dữ liệu đơn hoàn vào SCRM qua backend-to-backend từ ECOMM.
> Ref nghiệp vụ sàn: `projects/ecommerce_connect/shopee_return_refund_flow.md`,
> `projects/ecommerce_connect/tiktok_return_refund_cancel.md`.

## Màn hình & route

| | |
|---|---|
| Route | `don-hang-hoan` (con của `landing-page`) |
| Routing module | `scrm_frontend/src/app/pages/landing-page/landing-page-routing.module.ts` (path `don-hang-hoan` → `DonHangHoanComponent`) |
| Component list | `landing-page/don-hang-hoan/don-hang-hoan.component.ts` + `.html` + `.scss` |
| Component detail | `don-hang-hoan/don-hang-hoan-detail/don-hang-hoan-detail.component.ts` |
| Modals | `don-hang-hoan/modals/`: `khieu-nai`, `cung-cap-bang-chung`, `thuong-luong`, `map-san-pham`, `dispute`, `hoa-don-dieu-chinh`, `lien-ket-sp-don-ban` |
| Timeline | `don-hang-hoan/timeline/reverse-tracking-timeline.component.ts` |
| Service FE | `services/orderLogistic/don-hang-hoan.service.ts`, `services/orderLogistic/returns.service.ts` |
| Enums FE | `shared/Enums/EnumShopeeReturn.ts` (EnumGiaiPhapHoan, EnumLoaiDonHoanTab, EnumHanhDongQuanTrong, EnumSubTabDonHoan, EnumUuTienDeadline, EnumReverseLogisticsStatus, EnumPostReturnLogisticsStatus...) |

`DonHangHoanComponent` extends `BaseListComponentV1`. `maPage = ListMaPage_Filter.Don_Hang_Hoan`,
`TableColumnKey = ListMaPage_GridColumns.Don_Hang_Hoan`. Naming **generic** (không lộ Shopee/TikTok
trên UI — partner-specific chỉ ở ECOMM layer). Tab cấp 1 / sub-tab cấp 2 / action / uuTien deadline
đều set vào `searchModelCore` rồi gọi `applyTabAndActionFilter()` → BE filter theo bộ trạng thái
**canonical CRM** (`listTrangThaiTab: int[]`), KHÔNG dùng raw status sàn.

## Controllers (BE — OrderLogistic.API)

`OrderLogistic/OrderLogistic.API/Controllers/`

| Controller | Vai trò |
|---|---|
| `DonHangHoanController.cs` | API màn hình. `[TienIchAttribute("DonHangHoanController", "Quản lý đơn hàng hoàn", MaTienIchSCRM.TI_OrderLogistic)]`. Endpoints: `TimTheoDieuKien`, `GetTabCounters`, `LayDonHoanMoiTuEcomm` (nút 1), `DongBoTrangThaiDonHoan` (nút 2), `GetChiTiet/{id}`, `GetChiTietByIdDonHangLogistic/{id}`, `HBBTL_PrefillFromDonHangHoan/{id}`, `UpdateMapSanPham`, `UpdateChietKhauSan`, `DongBoThongTinDonGoc`, `GetItemsDonBanByIdDonGoc` |
| `Ecommerce/DonHangHoanEcommController.cs` | Webhook/ingestion từ TMDT (backend-to-backend từ ECOMM). `"Webhook đơn hàng hoàn từ TMDT"` |
| `HoanTra/ReturnsController.cs` | `"Hoàn TMĐT - Xử lý"` — proxy các action xử lý hoàn (upload proof / offer / dispute…) sang ECOMM |
| `Ecommerce/DonHangHoanController.cs` | (cùng nhóm Ecommerce — DonHangHoanService.cs `Generated/Ecommerce`) |

## Entities (OrderLogistic.DataContext / Entity)

### `DonHangHoan.cs` (header đơn hoàn)
Key fields:
- `IdDonVi`, `IdChiNhanh` (multi-tenant), `MaDonHoan` (system), `MaDonHoanEcomm` (return_sn sàn),
  `MaDonGoc` (order_sn), `IdDonHangLogistic` (FK đơn bán gốc), `EcommerceId` (8=Shopee, 9=TikTok, 10=Lazada).
- Trạng thái CRM canonical: `TrangThai` (0=Mới…), `TrangThaiTenGoiTat`; raw sàn: `TrangThaiHoanMaDoiTac` / `TrangThaiHoanTenDoiTac`.
- Tiền: `TienHoan`, `TienChietKhauSan` (sàn trợ giá, cấp đơn), `ChenhLechTienDonGoc` / `IsLechTienDonGoc` (cảnh báo lệch).
- Lý do/giải pháp: `LyDoHoan`/`LyDoHoanTen`, `GiaiPhapHoan`/`GiaiPhapHoanTen` (RETURN_REFUND / REFUND_ONLY), `NoiDungHoan`.
- Phân loại canonical (map từ raw partner):
  - `LoaiHoan` (trục KẾ TOÁN: 0=TruocHoanThanhDon/RRBOC, 1=SauHoanThanhDon/RRAOC, 9=KhongXacDinh) — gate HBBTL.
  - `ThoiDiemYeuCauHoan` (trục GIAO VẬN: 0=SauNhanHang, 1=DangVanChuyen, 2=TaiDiemGiao, 9). Thay dần `LoaiYeuCauHoan` (0/1/2 raw Shopee).
  - `NoiKiemHoan` (0=NguoiBan/seller_validation, 1=KhoTrungGian/warehouse_validation, 9) — quyết mốc "hàng về seller".
- Logistics: `LogisticsLoai` (FORWARD/REVERSE/POST_RETURN), `LogisticsTrangThaiMa`/`Ten`, `LogisticsMaVanDon`, `LogisticsNgayCapNhat`; raw `*DoiTac`.
- `DeadlineKhieuNai`, `CanHoanHangVatLy` (needs_logistics), `NgayTaoDoiTac` (Shopee create_time — dùng filter/sort).
- HBBTL link: `IdPhieuBanTraLai` (1:1 chống double-create), `MaPhieuHBBTL`.
- Snapshot đơn gốc: `KhachHangId/Ma/Ten`, `GiaoHangDiaChi`, `UserIdSale`, `MaDonBanGoc`, `InvNoGoc`,
  `GiaoHangTrangThaiMaGoc/TenGoc`, `MaPhieuBanHangGoc`, `IdPhieuBanHangGoc`.

### `DonHangHoanSanPham.cs` (dòng SP hoàn)
- `IdDonHangHoan` (FK), `IdSanPham` (parse từ CrmSkuId — NULL khi combo), `IdSanPhamCha`, `IdCombo` (combo header khi Loai=2).
- `TenSanPham`, `MaSanPham`, `SoLuong`, `DonGia`, `TienHoan`, `Loai` (1=SanPham, 2=Combo).
- Link dòng đơn gốc: `IdDonHangLogisticSanPham`, `IdDonHangLogisticCombo`.
- Snapshot tiền gốc (đủ để tạo HBBTL không cần join): `DonGiaGoc`, `DonGiaVonGoc`, `SoLuongGoc`,
  `ThanhTienGoc`, `ChietKhauTongGoc`, `SoTienVatTongGoc`, `ValueVatGoc`, `TypeVatGoc`, `ThanhTienPhanBoGoc` (net).
- `ChenhLechTienDonGoc` (lệch), `TienChietKhauSan` + `IsChietKhauSanSuaTay` (cờ user override).

Enum canonical: `SB.Core/EnumCommon/EnumEcommerce.cs`
(`EnumDonHangHoan_LoaiHoan`, `EnumDonHangHoan_ThoiDiemYeuCau`, `EnumDonHangHoan_NoiKiemHoan`,
`EnumNhanDuLieuDonHangHoan_DataType`).

## Luồng code

### Ingestion (ECOMM → SCRM)
`OrderLogistic.Service/Generated/Ecommerce/DonHangHoanService.cs` → `IDonHangHoanService.NhanDuLieuDonHangHoanTMDT(TaoDonHangHoanEcommerceDto)`:
1. Validate (`CrmUnitId`, `ReturnDataDetail`, `PartnerReturnCode`, `PartnerOrderCode`), check đơn vị tồn tại (`coreService.DonVi_GetDonViById`).
2. Lookup existing: `MaDonHoanEcomm == PartnerReturnCode` (scope `IdDonVi`+`IdChiNhanh`).
3. Route theo `DataType`:
   - `CREATE` → `TaoDonHangHoanEcommerce(detail, donViDb, idChiNhanh)`. Idempotent nếu đã tồn tại (trả ref). Bắt buộc có `ProductReturnInfo`.
   - `UPDATE` → `CapNhatTrangThaiDonHangHoan(existed, detail, userLogin)`.
   - `UPDATE_DETAIL` → `CapNhatChiTietDonHangHoan(existed, detail, userLogin)`.
4. `TaoDonHangHoanEcommerce`: resolve `DonHangLogistic` gốc theo `CrmOrderId` (bắt buộc có, else từ chối); snapshot mã/id PBH kế toán (`_accountingService.GetPhieuBanHangByMaDons`, best-effort try/catch); build header + `BuildSanPham`.
5. `BuildSanPham(donHangHoan, item, lstDhlSpGoc, lstComboGoc)`: SP thường → 1 dòng match theo IdSanPham; Combo (Loai=2) → match `DonHangLogisticCombo.IdComboSpa == CrmProductId` → nổ N dòng SP con. `ApplyGoc` snapshot tiền gốc + tính `ChenhLechTienDonGoc = tienHoan − ThanhTienPhanBo×ratio` + `TienChietKhauSan = Max(0,(ThanhTien−ThanhTienPhanBo)×ratio)`.
6. `SyncTrangThaiHoanToLogistic(donHangHoan)` — đẩy trạng thái hoàn ngược về đơn bán gốc.

### List / counters / màn hình
`OrderLogistic.Service/Generated/DonHang/DonHangHoanService.cs` → `IDonHangHoanService`:
- `TimTheoDieuKien` → trả `DonHangHoanListItemDto` (flat, mirror entity + `DanhSachSanPham` + `ShippingOutbound`).
- `GetTabCounters` → `TabCountersDto` (tab cấp 1 + sub-tab + action button + deadline). Áp filter common, ignore filter tab/action để counter đúng mọi tab.
- `LayDonHoanMoiTuEcomm` / `DongBoTrangThaiDonHoan` → proxy sang ECOMM.
- `HBBTL_PrefillFromDonHangHoan` → `HbbtlPrefillFromDonHangHoanDto` (header KH + listDetail có DonGiaVon/IdSanPham/IMEI/MaLoDate; `IsKiemNhapKho = !IsRefundOnly(GiaiPhapHoan)`).
- `UpdateMapSanPhamAsync`, `UpdateChietKhauSanAsync`, `DongBoThongTinDonGoc(Nhieu)`, `GetItemsDonBanByIdDonGocAsync`.

### HBBTL gate (logistics-gated)
- `IsRefundOnly(giaiPhapHoan)` → `REFUND` || `REFUND_ONLY`.
- `IsHangVeKho(DonHangHoan)` (static): theo `NoiKiemHoan`
  - `KhoTrungGian` → `LogisticsLoai=="POST_RETURN" && LogisticsTrangThaiMa=="POST_RETURN_LOGISTICS_DELIVERY_DONE"`.
  - `NguoiBan` → `LogisticsLoai=="REVERSE" && LogisticsTrangThaiMa=="LOGISTICS_DELIVERY_DONE"`.
  - null → OR cả 2 (fallback).
- `GetTrangThaiHoanLogistics(DonHangHoan)` → badge: `DA_NHAN`/`DANG_VE`/`CHO_GUI`/`GIAO_FAIL`/`MAT_HANG`/null.
- `TryAutoCreateHBBTLAsync` (Ecommerce service): hiện **STUB no-op**, guard `AUTO_CREATE_HBBTL = false`
  (chờ inject `IPhieuBanTraLaiService` cross-module). Gate: bỏ qua nếu `IdPhieuBanTraLai != null`,
  nếu `LoaiHoan == TruocHoanThanhDon` (RRBOC), hoặc `!isHangVeKho`. Hiện HBBTL **tạo bằng nút manual** trên FE.

## Bug-prone

- **Idempotency CREATE**: webhook trùng phải trả ref đơn cũ (không tạo trùng) — đã handle, đừng phá khi sửa.
  Dedup key = `MaDonHoanEcomm` (= PartnerReturnCode) trong scope tenant.
- **Bắt buộc có đơn gốc**: `TaoDonHangHoanEcommerce` từ chối nếu không resolve được `DonHangLogistic` theo `CrmOrderId`.
- **2 trục dễ nhầm**: `LoaiHoan` (kế toán: trước/sau ghi doanh thu) ≠ `ThoiDiemYeuCauHoan` (giao vận: hàng ở đâu).
  Gate HBBTL dùng `LoaiHoan` (RRBOC chặn cứng) + `NoiKiemHoan` (mốc hàng về seller).
- **Warehouse validation (FBS)**: REVERSE DELIVERY_DONE chỉ là hàng về kho SÀN, CHƯA về seller — phải đợi
  POST_RETURN DELIVERY_DONE. Nhầm sẽ tạo HBBTL sớm khi hàng chưa thực về.
- **Refund-only**: KHÔNG nhập kho. Quên check `IsRefundOnly` → sai cờ `IsKiemNhapKho`.
- **Combo**: lưu line con của combo gốc với `IdCombo` = Guid nhóm (`DonHangLogisticCombo.Id`), khác id-space với
  `CrmProductId` (master) → match combo qua `IdComboSpa == CrmProductId`, KHÔNG qua `IdDonHangLogisticSanPham`.
- **Map sản phẩm**: `IdDonHangLogisticSanPham` NULL với đơn cũ → HBBTL fallback fuzzy theo IdSanPham; có modal map tay.
- **Filter tab dùng canonical CRM** (`listTrangThaiTab` int[]), KHÔNG raw status sàn → đừng map theo string sàn ở BE filter.
- **Counter ↔ grid phải khớp**: tab TraHangHoanTien/Huy set `excludeGiaoKTC=true` để loại đơn LoaiYC=2 (tab Giao KTC riêng).
- **NgayTaoDoiTac vs NgayTao**: filter/sort theo ngày phải dùng `NgayTaoDoiTac` (create_time sàn); `NgayTao` lệch khi sync trễ.
- **`refund_total` sàn là String / VND 0-decimal** (TikTok) — xem ref ecommerce_connect khi parse precision.

## File source

BE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm`):
- `OrderLogistic/OrderLogistic.DataContext/Entity/DonHangHoan.cs`
- `OrderLogistic/OrderLogistic.DataContext/Entity/DonHangHoanSanPham.cs`
- `OrderLogistic/OrderLogistic.DataContext/Model/Returns/` (ReqTimDonHangHoanDto, ReqGetReturnListSCRM, ReqReturnSCRM, ReqOfferSCRM, ReqDisputeSCRM, ReqUploadProofSCRM, ReqUpdateMapSanPhamDto…)
- `OrderLogistic/OrderLogistic.API/Controllers/DonHangHoanController.cs`
- `OrderLogistic/OrderLogistic.API/Controllers/Ecommerce/DonHangHoanEcommController.cs`
- `OrderLogistic/OrderLogistic.API/Controllers/HoanTra/ReturnsController.cs`
- `OrderLogistic/OrderLogistic.Service/Generated/DonHang/DonHangHoanService.cs` (list/counters/HBBTL prefill/gate)
- `OrderLogistic/OrderLogistic.Service/Generated/Ecommerce/DonHangHoanService.cs` (ingestion ECOMM→SCRM)
- `OrderLogistic/OrderLogistic.Service/Generated/Ecommerce/ReturnsService.cs` + `IReturnsService.cs` (proxy action xử lý hoàn)
- `OrderLogistic/OrderLogistic.Service/Generated/Audit/ReturnsAuditHelper.cs`
- `Libs/SB.Core/EnumCommon/EnumEcommerce.cs` (enum canonical đơn hoàn)
- `Libs/Models.Core/DTO/Ecommerce/OrderReturnModel.cs` (DTO ECOMM↔SCRM: TaoDonHangHoanEcommerceDto, ProductReturnData, ResSendReturnDataToCrm…)

FE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`):
- `src/app/pages/landing-page/don-hang-hoan/don-hang-hoan.component.{ts,html,scss}`
- `src/app/pages/landing-page/don-hang-hoan/don-hang-hoan-detail/don-hang-hoan-detail.component.ts`
- `src/app/pages/landing-page/don-hang-hoan/modals/*`, `.../timeline/reverse-tracking-timeline.component.ts`
- `src/app/services/orderLogistic/don-hang-hoan.service.ts`, `returns.service.ts`
- `src/app/shared/Enums/EnumShopeeReturn.ts`
- `src/app/pages/landing-page/landing-page-routing.module.ts` (route), `landing-page.module.ts` (declare)

## Ghi chú liên kết ecommerce_connect

- Đơn hoàn KHÔNG do SCRM khởi tạo — nguồn là webhook sàn (Shopee push 29 `return_updates_push`;
  TikTok type 12/64/65/67) đổ vào `ecommerce_connect`, được ECOMM chuẩn hóa (map raw status → CRM code,
  resolve `CrmOrderId`/`CrmSkuId`/`CrmProductId` từ OrderItemConnect) rồi gọi b2b sang
  `NhanDuLieuDonHangHoanTMDT`. SCRM chỉ **persist + xử lý kho/kế toán**.
- Các action đẩy ngược lên sàn (confirm / offer / accept_offer / dispute / upload_proof…) đi qua
  `ReturnsController` → ECOMM adapter theo `EcommerceId`. Nhiều action hiện **defer / stub** (xem TODO
  trong `Ecommerce/DonHangHoanService.cs` header + comment FE component).
- Mapping nghiệp vụ chi tiết partner: `projects/ecommerce_connect/shopee_return_refund_flow.md`
  (7 states, return_refund_request_type 0/1/2, validation_type seller/warehouse) và
  `projects/ecommerce_connect/tiktok_return_refund_cancel.md` (Cancel/Refund/Return/Replacement, webhook 12/64/65/67).
