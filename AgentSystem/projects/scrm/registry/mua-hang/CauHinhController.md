---
controller: CauHinhController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > 'Cấu hình đặt cọc' (màn/khối thiết lập của module: cấu hình khách hàng + cờ gửi tin nhắn tự động khi chưa thanh toán + số phút nhắc). Một phần đọc nội bộ (cấu hình kho/chi nhánh, private config) phục vụ các màn khác."
muc_dich: "Quản lý cấu hình của service đặt cọc theo đơn vị (1 bản ghi CauHinh/đơn vị, lưu JsonSetting): khởi tạo cấu hình khách hàng, cờ + số phút gửi tin nhắn tự động khi chưa thanh toán, cấu hình kho/chi nhánh. Cache theo đơn vị."
alias: ["cấu hình đặt cọc", "cau hinh dat coc", "cấu hình service đặt cọc", "config deposit", "cấu hình gửi tin khi chưa thanh toán", "cấu hình kho chi nhánh đặt cọc"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [CauHinhCacheService/GetOrSetCache_CauHinhSetting_DonVi, CauHinhCacheService/ClearCache_CauHinhSetting_DonVi, MassTransit (consume CauHinhChungMessage — đồng bộ cấu hình chung)]
cross_module_in: [Hệ thống/Cấu hình chung (consume CauHinhChungMessage → SaveCauHinhChung đồng bộ JsonSetting)]
cross_module_out: []
---

# CauHinhController — Cấu hình service đặt cọc (1 bản ghi/đơn vị, JsonSetting + cache)

> Cấu hình của service Deposit: mỗi đơn vị 1 bản ghi `CauHinh` (key `IdDonVi`, IsDelete=false), toàn bộ setting nén trong cột `JsonSetting`. Đọc qua cache theo đơn vị (`GetOrSetCache_CauHinhSetting_DonVi`). Quan trọng cho luồng SMS: cờ `IsGuiTinNhanKhiChuaThanhToan` + `SoPhutGuiTinNhanKhiChuaThanhToan` (≥5 phút) điều khiển job nhắc thanh toán tự động ở DepositSmsSend. Mức auth hỗn hợp: `Get/GetCauHinhInit/GetCauHinhKhoChiNhanh` = `[SBAuthorize]` trống (any login); `SaveCauHinhInit` có key `CauHinh_Deposit_LuuCauHinhKhachHang` (role `DV-AD-TONG`); `PrivateGetConfig` = `[PrivateSBAuthorize]` (service-to-service). `[TienIch]` key = `CauHinh_Deposit_Controller`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình | (load màn cấu hình) | `GET api/CauHinh/Get` | `[SBAuthorize]` (any login) | public | màn cấu hình | Trả `CauHinhSettingDto` của đơn vị (đọc qua cache `GetAsync(idDonVi)`). | **EF**/cache | `[SBAuthorize]` trống. |
| 2 | Lưu cấu hình khách hàng | Nút "Lưu" (khởi tạo cấu hình) | `POST api/CauHinh/SaveCauHinhInit` | `CauHinh_Deposit_LuuCauHinhKhachHang` | public | màn cấu hình | Validate ModelState + `SoPhutGuiTinNhanKhiChuaThanhToan ≥ 5` (lỗi nếu <5). Upsert 1 bản ghi/đơn vị: gộp vào JsonSetting (`Save<T>`), set `IsGuiTinNhanKhiChuaThanhToan`, clear cache. Trả Id. | **EF** + cache | **L207 (insert path) NRE**: gán `cauHinhDb.IsGuiTinNhanKhiChuaThanhToan` TRƯỚC khi `cauHinhDb = new CauHinh()` → khi chưa có bản ghi (lần đầu/đơn vị mới) sẽ NRE. |
| 3 | Lấy cấu hình init | (load form khởi tạo) | `GET api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` (any login) | public | màn cấu hình | Map `GetAsync(idDonVi)` → `CauHinhSettingInitDto`. (Nhánh superuser→Guid.Empty đã comment.) | **EF**/cache | `[SBAuthorize]` trống. |
| 4 | Lấy config (private) | nội bộ service-to-service | `POST api/CauHinh/PrivateGetConfig` | `[PrivateSBAuthorize]` | internal | gọi nội bộ | Trả `CauHinhSettingDto` (như Get) cho service khác qua private auth. | **EF**/cache | — |
| 5 | Lấy cấu hình kho/chi nhánh | (dropdown/cấu hình kho) | `GET api/CauHinh/GetCauHinhKhoChiNhanh` | `[SBAuthorize]` (any login) | public | nơi cần kho/chi nhánh | Đọc `ListKho` từ JsonSetting cache → deserialize `List<CauHinh_Kho_ChiNhanh>`; rỗng → list trống. | cache | `[SBAuthorize]` trống. |

## Endpoint nội bộ (private / service-to-service)
- `[private] PrivateGetConfig` — service khác lấy cấu hình đặt cọc của đơn vị qua `[PrivateSBAuthorize]`.
- `SaveCauHinhChung(CauHinhChungMessage)` (service-level, không qua controller) — consume message cấu hình chung từ Hệ thống → upsert JsonSetting + clear cache.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **1 bản ghi CauHinh / đơn vị** (key IdDonVi, IsDelete=false); mọi setting nén trong `JsonSetting`.
- **SaveCauHinhInit:** `SoPhutGuiTinNhanKhiChuaThanhToan ≥ 5` (vi phạm → lỗi).
- **Cờ điều khiển job SMS:** `IsGuiTinNhanKhiChuaThanhToan` + `SoPhutGuiTinNhanKhiChuaThanhToan` → input cho job nhắc thanh toán ở DepositSmsSend.
- **Cache:** đọc qua `GetOrSetCache_CauHinhSetting_DonVi`; mọi ghi → `ClearCache_CauHinhSetting_DonVi(idDonVi)`.

## Quan hệ
- **Gọi sang:** CauHinhCacheService (get/clear cache theo đơn vị).
- **Dữ liệu (entities):** `CauHinh` (JsonSetting). Đọc/ghi 1 bản ghi/đơn vị.
- **Ripple / đồng bộ:** consume `CauHinhChungMessage` (đồng bộ cấu hình chung từ Hệ thống). Setting (cờ gửi tin/số phút) được DepositSmsSend job đọc; cấu hình kho được DepositReport đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/CauHinhController.cs` — Get/GetCauHinhInit/GetCauHinhKhoChiNhanh `[SBAuthorize]` trống; SaveCauHinhInit key `CauHinh_Deposit_LuuCauHinhKhachHang` role `DV-AD-TONG`; PrivateGetConfig `[PrivateSBAuthorize]`.
- BE `Deposit/Deposit.Service/Generated/CauHinhService.cs`
  - **L207 (`Save<T>`)**: thao tác `cauHinhDb.IsGuiTinNhanKhiChuaThanhToan = ...` đặt TRƯỚC `cauHinhDb = new CauHinh()` trong nhánh insert (cauHinhDb đang null) → **NRE khi đơn vị chưa có cấu hình** (lần lưu đầu tiên). Rủi ro cao.
  - L134-137 (`SaveCauHinhChung`): catch nuốt exception, trả false (đồng bộ cấu hình chung thất bại im lặng).
  - L250-257 (`CauHinhCheSoTheoUserLogin`): catch trả default `IsCheSo=true, IsChoPhepGoiDien=false` (fail-safe chặn — cần lưu ý khi debug che số điện thoại).
- FE: khối cấu hình đặt cọc trong module deposit-receipt (service gọi `api/CauHinh`).
