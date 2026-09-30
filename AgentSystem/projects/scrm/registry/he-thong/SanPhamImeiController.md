---
controller: SanPhamImeiController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. API cho đối tác tra sản phẩm IMEI theo điều kiện (kho/serial)."
muc_dich: "Cổng API đối tác tra danh sách sản phẩm IMEI theo điều kiện — proxy sang WarehouseProduct service; có throttle theo mili-giây cấu hình per-system và gating IsSuDungApiGetData per đơn vị."
alias: ["api imei đối tác", "tra sản phẩm imei partner", "SanPhamImei TimTheoDieuKien", "lấy imei cho đối tác"]
auth: internal
state_machine: []
entities: [SanPhamImei]
stored_procs: [PRC_SCRM_WarehouseProduct_SanPhamImei_Search_Api]
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "Core service (CauHinh/GetCauHinhAPIPrivate — per đơn vị; CauHinh/GetCauHinhAPISystemPrivate — system-wide throttle ms)", "WarehouseProduct service (SanPhamImei/TimTheoDieuKienPrivate)", "Redis (CommonCacheService throttle theo mili-giây)"]
cross_module_in: ["Hệ thống đối tác → POST SanPhamImei/TimTheoDieuKien"]
cross_module_out: ["Core service (cấu hình API đơn vị + system)", "WarehouseProduct service (tra sản phẩm IMEI)"]
---

# SanPhamImeiController — API tra sản phẩm IMEI cho đối tác (AuthenPartner)

Controller AuthenPartner cho đối tác tra **sản phẩm IMEI** (serial trong kho). Kế thừa `BasePartnerController` + `[SBAuthorizePartner]` (mô hình auth đối tác xem node `CommonController_AuthenPartner`). `DonViId` lấy từ token.

Điểm đặc biệt so với các controller partner khác: **throttle theo mili-giây** (không phải 60s cố định). Controller:
1. Lấy cấu hình API per-đơn-vị (`GetCauHinhAPIAsync` → Core `GetCauHinhAPIPrivate`), check `IsSuDungApiGetData` (gating riêng ngoài filter).
2. Lấy cấu hình API system-wide (`GetCauHinhAPISystemAsync` → Core `GetCauHinhAPISystemPrivate`) để lấy `ThoiGianMsLayDuLieuImei`; chưa cài → mặc định **100ms**.
3. Throttle theo ms (`CommonCacheService.GetCache_Time_Call_API_Ms`, key chỉ theo `idDonVi`, không có user).
4. Kẹp `PageSize ≤ 100` → gọi WarehouseProduct `SanPhamImei/TimTheoDieuKienPrivate/{idDonVi}`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra sản phẩm IMEI | (API) | POST `api/SanPhamImei/TimTheoDieuKien` | — | internal `[SBAuthorizePartner]` | — | Lấy CauHinh đơn vị → check `IsSuDungApiGetData`; lấy CauHinh system → throttle theo `ThoiGianMsLayDuLieuImei` (mặc định 100ms); kẹp `PageSize<=100`; gọi WarehouseProduct `SanPhamImei/TimTheoDieuKienPrivate/{idDonVi}` trả list IMEI. | HTTP Core (2 cấu hình) + HTTP WarehouseProduct (SP `..._SanPhamImei_Search_Api`) | Lỗi lấy cấu hình → "Lỗi lấy cấu hình API!"; `IsSuDungApiGetData==false` → "Đơn vị chưa được phép sử dụng API lấy dữ liệu!" |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`) — đây là filter cấp controller.
2. Tầng 2 (trong action): lấy lại CauHinh đơn vị; null/Success=false/Data=null → "Lỗi lấy cấu hình API!"; `Data.IsSuDungApiGetData == false` → "Đơn vị chưa được phép sử dụng API lấy dữ liệu!".
3. Throttle ms: `throttleMs = ThoiGianMsLayDuLieuImei ?? 100`. Nếu >0 và đã gọi trong khoảng đó → "Bạn cần chờ Nms để tiếp tục call API". `throttleMs<=0` → bỏ qua throttle.
4. `PageSize > 100` → ép 100.

## Quan hệ
- **Gọi sang:** Core (`CauHinh/GetCauHinhAPIPrivate/{idDonVi}`, `CauHinh/GetCauHinhAPISystemPrivate`); WarehouseProduct (`SanPhamImei/TimTheoDieuKienPrivate/{idDonVi}`).
- **Dữ liệu (entities):** `SanPhamImei` (read qua WarehouseProduct).
- **Ripple / đồng bộ:** chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `SanPhamImeiController.cs:35-44` — kiểm tra `IsSuDungApiGetData` lặp lại logic của filter `SBAuthorizePartner` (filter cũng đã yêu cầu cờ này = true). Đôi khi dư thừa, nhưng vẫn giữ vì lấy cấu hình mới nhất từ Core (không phải cache filter).
- `CommonCacheService.cs:257-263` — `SetCache_Time_Call_API_Ms` lưu TTL Redis làm tròn lên giây (`Ceiling(ms/1000)`, tối thiểu 1s); nếu `ThoiGianMsLayDuLieuImei < 1000ms` thì key vẫn sống 1 giây → throttle thực tế tối thiểu ~1s dù cấu hình ms nhỏ hơn.
- Throttle key dùng `Guid.Empty` cho user (`KeyCache_Time_Call_API(idDonVi, Guid.Empty, apiName)`) → throttle chung cho toàn đơn vị, không phân biệt từng partner key.
- `GetCauHinhAPISystemAsync` lỗi không chặn cứng — `systemCfgRs?.Data?.ThoiGianMsLayDuLieuImei ?? 100` → nếu Core lỗi vẫn chạy với 100ms mặc định.
