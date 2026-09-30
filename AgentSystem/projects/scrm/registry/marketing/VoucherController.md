---
controller: VoucherController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Voucher (list + tab lịch sử sử dụng, mã voucher, gia hạn)"
muc_dich: "CRUD voucher + quản lý mã voucher (sinh/xóa/khóa/gia hạn), tra cứu voucher khả dụng theo điều kiện/mã, lịch sử sử dụng, và phục vụ job marketing campaign sinh mã hàng loạt."
alias: [Voucher, "mã giảm giá", "phiếu giảm giá", coupon]
auth: mixed
state_machine: [HoatDong, Khoa]
deprecated: false
entities: [Voucher, VoucherOption, VoucherMa, VoucherLichSu, VoucherDoiTuongApDung, VoucherDoiTuongKhongApDung]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_Voucher — đơn vị phải mua tiện ích Voucher)"
  - "Customer/KhachHang (hạng/nhóm KH để lọc voucher khả dụng)"
  - "WarehouseProduct (quy đổi đối tượng áp dụng: dịch vụ/sản phẩm/thẻ)"
  - "CommonService (GetAllKhuyenMaiGuiSmsZaloDonVi — gửi SMS/Zalo)"
cross_module_in:
  - "MarketingCampaign job (GenVoucherCodeForJobMarketingCampaign* / GenVoucherCodeByBatch / DeleteVoucherCodeByIdNguonTao — sinh & xóa mã voucher cho chiến dịch)"
  - "Order/OrderProcess (GetListWithConditions* / GetListVoucherByCodeAndId — lấy voucher áp được khi thanh toán)"
cross_module_out:
  - "PromotionUpdateWithBillService (áp voucher khi thanh toán: trừ lượt/đánh dấu mã đã dùng + rollback khi hủy đơn)"
  - "VoucherLichSu (ghi lịch sử mỗi lần dùng mã)"
---

# Voucher — Quản lý voucher / mã giảm giá

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách voucher (grid) | Danh sách / Lọc | POST `TimTheoDieuKien` | `Voucher_TimTheoDieuKien` | public | luôn | Non-admin bị ép `IdChiNhanh = CurrentUser.IdChiNhanh`. ModelState invalid → lỗi. | EF | — |
| 2 | Lịch sử sử dụng voucher | Tab lịch sử | POST `LichSuSuDungVoucher` | `Voucher_LichSuSuDungVoucher` | public | luôn | Non-admin ép IdChiNhanh. Trả lịch sử dùng theo voucher. | EF (VoucherLichSu) | — |
| 3 | Thêm mới | Lưu | POST `ThemMoi` | `Voucher_ThemMoi` | public | — | `Create(payload)`. Sinh Voucher + VoucherOption + (mã nếu MotMa/NhieuMa). | EF | — |
| 4 | Chi tiết theo Id | Mở edit | GET `TimTheoId/{id}` | `Voucher_TimTheoId` | public | — | `GetById(id)`. | EF | — |
| 5 | Cập nhật | Lưu | POST `CapNhat` | `Voucher_CapNhat` | public | — | `Update(model)`. | EF | — |
| 6 | Xóa | Xóa | GET `Xoa/{id}` | `Voucher_Xoa` | public | — | `Delete(id)`. | EF | GET cho hành động xóa (không CSRF-safe) |
| 7 | Khóa / Mở khóa | Khóa / Mở khóa | GET `Khoa/{id}` / `MoKhoa/{id}` | `Voucher_Khoa` / `Voucher_MoKhoa` | public | theo state HoatDong/Khoa | `Lock(id)` / `UnLock(id)` — voucher khóa không áp được khi thanh toán. | EF | GET đổi state |
| 8 | Gia hạn voucher | Lấy data / Gia hạn | GET `GetDataGiaHanVoucher/{id}` + POST `GiaHanVoucher` | `Voucher_GetDataGiaHanVoucher` / `Voucher_GiaHanVoucher` | public | — | Lấy data hiện tại → gia hạn ngày hết hạn. | EF | — |
| 9 | Tìm kiếm mã voucher | Tab mã voucher | POST `TimKiemMaVoucher` | `Voucher_TimKiemMaVoucher` | public | — | Phân trang danh sách VoucherMa theo điều kiện. | EF (VoucherMa) | — |
| 10 | Download mã voucher (Excel) | Tải mã | GET `DownloadMaVoucher/{id}` | `Voucher_DownloadMaVoucher` | public | — | Xuất file `MaVoucher.xlsx` (GemBox). | EF | — |
| 11 | Dropdown / autocomplete | (combo) | GET `DropdownVoucher`, POST `VoucherAutoComplete` | `[SBAuthorize()]` (chỉ login) | public | — | Combo voucher của đơn vị hiện tại. | EF | — |
| 12 | Voucher khả dụng theo điều kiện | — (thu ngân) | POST `GetListWithConditions` / `GetListWithConditionsCheckCode` / `GetListVoucherByCodeAndId` / `GetListVoucherInfo` | `Voucher_GetListWithConditions*` / `Voucher_GetListVoucher*` | public | — | Lọc voucher áp được cho giỏ đơn theo mã/đối tượng; *CheckCode kiểm tra mã chính xác tuyệt đối; *Info bỏ check điều kiện. | EF | 4 biến thể tra cứu song song (drift risk) |
| 13 | Xử lý thủ công mã voucher | — | POST `XuLyThuCongMaVoucher/{maVoucher}/{actionId}` | `Voucher_XuLyThuCongMaVoucher` | public | — | Sửa trạng thái 1 mã theo `actionId` (mở/khóa/reset mã đã dùng). | EF | actionId là int "magic" — không enum hóa ở controller |
| 14 | Voucher gửi SMS/Zalo | — | GET `GetAllKhuyenMaiGuiSmsZaloDonVi` | `Voucher_GetAllKhuyenMaiGuiSmsZaloDonVi` | public | — | Qua `_commonService` (KHÔNG qua `_serviceVoucher`). | EF | `_commonService` inject optional `= null` → NRE nếu DI thiếu |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` — **Marketing campaign job** gọi sinh mã hàng loạt:
  - POST `GenVoucherCodeForJobMarketingCampaign` (payload `Dictionary<Guid,int>`: idVoucherOption → số lượng mã)
  - POST `GenVoucherCodeForJobMarketingCampaign_ByMaDinhDanhVoucher/{idDonVi}` (key = mã định danh voucher)
  - POST `GenVoucherCodeByBatch` (payload `GenVoucherCodeByBatchPayload`)
  - POST `DeleteVoucherCodeByIdNguonTao/{idNguonTao}/{idDonVi}` — xóa mã theo nguồn tạo (rollback campaign)
- `[PrivateSBAuthorize]` — service khác lấy voucher: POST `GetLstVoucherByLstDonVi` (list idDonVi), GET `GetVoucherMaByIdVoucherOption/{IdVoucherOption}`, POST `UpdateVoucherMaAndVoucherOption`.

## Quy tắc nghiệp vụ (gating khi áp voucher — logic ở PromotionUpdateWithBillService.ValidateThanhToanDonHang / ApplyVoucher)
Voucher chỉ áp được khi vượt qua chuỗi gate đối tượng (`VoucherDoiTuongApDung`, KieuLoai = enum `EnumVoucher_KieuLoai_ApDung`). Mỗi cờ bật → phải khớp đối tượng HOẶC có bản ghi `IdDichVu = Guid.Empty` (= "tất cả"):
1. **Khách vãng lai** (IdKhachHang rỗng) → chỉ voucher `IsKhongGioiHan` mới áp; các gate hạng/nhóm KH coi như fail.
2. **`IsSuDungChiNhanh`** → cần bản ghi `ChiNhanh` khớp `userLogin.IdChiNhanh` hoặc Guid.Empty; thiếu → loại.
3. **`IsSuDungHangKhachHang`** → cần `HangKhachHang` khớp `rankKhachHang` hoặc Guid.Empty (vãng lai → loại).
4. **`IsSuDungNhomKhachHang`** → cần `NhomKhachHang` thuộc nhóm KH hoặc Guid.Empty (vãng lai → loại).
5. **`IsSuDungLoaiLichHen`** → cần `LoaiLichHen` khớp `IdNguonDatLich` hoặc Guid.Empty.
6. **Voucher theo KhachHang cụ thể** → cần `KhachHang` khớp `payload.IdKhachHang` hoặc Guid.Empty.
7. Đối tượng áp dụng giỏ (dịch vụ/sản phẩm/thẻ): `VoucherDoiTuongApDung` (whitelist) và `VoucherDoiTuongKhongApDung` (blacklist) lọc theo KieuLoai DichVu/SanPham/The.
> Loại mã: `EnumVoucher_Type` — MotMa (1 mã dùng nhiều lần, đếm `SoLanSuDung`), NhieuMa (mỗi mã 1 lần, cờ `IsDaDung`), Deposit/Deposit_Custom (voucher đặt cọc, có `IdThamChieu`/`MaThamChieu`).

## Quan hệ
- **Gọi sang:** Customer (hạng/nhóm KH), WarehouseProduct (đối tượng áp dụng), CommonService (SMS/Zalo, GetAllKhuyenMaiFromRedis).
- **Dữ liệu (entities):** `Voucher` + `VoucherOption` (cấu hình 1 đợt voucher) + `VoucherMa` (từng mã) + `VoucherLichSu` (lịch sử dùng) + `VoucherDoiTuongApDung`/`KhongApDung`.
- **Ripple/đồng bộ:** áp/hủy voucher khi thanh toán đi qua `PromotionUpdateWithBillService` (đánh dấu mã đã dùng / hoàn mã khi rollback). Job marketing campaign sinh mã hàng loạt qua các endpoint Private.

## Luồng xuyên module (áp voucher khi thanh toán → đánh dấu mã → rollback khi hủy)
1. **Thanh toán** — OrderRollBackService → `/api/PromotionUpdateWithBill/ThanhToanDonHang` → trong transaction: NhieuMa set `IsDaDung=true`; MotMa `SoLanSuDung += 1`; option `SoLuongDaDung += n`. Voucher Deposit → publish message Deposit sau commit.
2. **Hủy/rollback** — `/api/PromotionUpdateWithBill/RollBackThanhToanDonHang` theo `IdDonHang`: NhieuMa → `IsDaDung=false`; MotMa → `SoLanSuDung-1`; option `SoLuongDaDung-=n`. Rollback-safe trong transaction.
3. **Job marketing campaign** — sinh mã batch (GenVoucherCode*) khi tạo chiến dịch; `DeleteVoucherCodeByIdNguonTao` để dọn khi hủy chiến dịch.

## Lỗi đã biết / lưu ý kỹ thuật
- `VoucherController.cs:23` — `_commonService` inject `ICommonService commonService = null` (optional default null) → endpoint `GetAllKhuyenMaiGuiSmsZaloDonVi` (`:262`) sẽ NRE nếu DI không đăng ký commonService.
- `VoucherController.cs:140,148,156` — `Xoa`/`Khoa`/`MoKhoa` dùng HTTP **GET** cho hành động đổi/xóa state (không idempotent-safe, dễ trigger ngoài ý muốn qua prefetch/crawler).
- `VoucherController.cs:292` — `XuLyThuCongMaVoucher` nhận `actionId:int` raw từ route → magic number, không validate enum ở tầng controller.
- `VoucherController.cs:57-104` — 4 biến thể tra cứu (`GetListWithConditions`, `GetListWithConditionsCheckCode`, `GetListVoucherByCodeAndId`, `GetListVoucherInfo`) khác nhau ở mức check điều kiện → sửa rule áp voucher phải đồng bộ nhiều nhánh (drift risk).
- `VoucherController.cs:205,247` — `DropdownVoucher`/`VoucherAutoComplete` chỉ `[SBAuthorize()]` (login) trong khi các API khác yêu cầu role AD → khe hở rò danh sách voucher.
