---
controller: KhungGioController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Đặt lịch — cấu hình 'Khung giờ làm việc' theo chi nhánh (danh sách khung giờ: tên, giờ bắt đầu/kết thúc, trạng thái sử dụng; có nhánh AD-DV thao tác chéo chi nhánh trong đơn vị)"
muc_dich: "Quản lý khung giờ làm việc (KhungGio) của chi nhánh: thêm/sửa/đổi trạng thái sử dụng, khởi tạo bộ mặc định (Sáng/Chiều/Tối), và sinh các mốc giờ (slot) cho lịch đặt — phục vụ ngày làm việc + đặt lịch."
alias: ["khung giờ làm việc", "khung giờ chi nhánh", "ca làm việc theo giờ", "slot giờ đặt lịch", "giờ làm việc cửa hàng", "khung giờ Sáng Chiều Tối"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhungGio, NgayLamViec, KhungGioNgayLamViec, DatLich]
stored_procs: []
depends: [Booking, CauHinhService (RoleDuocPhepThaoTacNgayLamViecVaKhungGio), Branch (CaLamViecNhanVien — check/xóa ca làm việc khi tắt khung giờ), Redis/Cache (KhungGio_ChiNhanh), Core (CoCau đơn vị — tên chi nhánh)]
cross_module_in: [NgayLamViecService gọi nội bộ `KhungGio/KhoiTao` & `KhungGio/KhoiTaoByIdChiNhanh` khi khởi tạo ngày làm việc; service nội bộ gọi `GetListKhungGioByChiNhanh_Private`/`GetListKhungGioByDonVi_Private`]
cross_module_out: [Branch — DeleteUserCaLamViec khi tắt khung giờ; Cache KhungGio_ChiNhanh (clear mỗi lần ghi); slot giờ đặt lịch (calendar) lấy từ GetKhungGioLamViec15Minute]
---

# KhungGioController — Quản lý khung giờ làm việc chi nhánh

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy khung giờ theo chi nhánh hiện tại | load grid | GET `api/KhungGio/TimTheoChiNhanh` | (không key — `[SBAuthorize]` rỗng) | public | màn cấu hình khung giờ | List `KhungGio` theo `DonVi+ChiNhánh`; nếu rỗng → tự `KhoiTao` bộ mặc định; sort `GioBatDau` rồi `GioKetThuc` | EF (nolock) | - |
| 2 | Lấy khung giờ đang sử dụng (chi nhánh) | dropdown / list active | GET `api/KhungGio/TimTheoChiNhanhIsSuDung` | `KhungGio_TimTheoChiNhanhIsSuDung` | public | nơi cần khung giờ active | Như (1) nhưng lọc `IsSuDung == true` | EF (nolock) | - |
| 3 | Lấy tất cả khung giờ theo đơn vị | list toàn đơn vị | GET `api/KhungGio/LayTatCaTheoDonVi` | (không key — `[SBAuthorize]` rỗng) | public | màn tổng theo đơn vị | List `KhungGio` theo `DonVi` (mọi chi nhánh); enrich `TenChiNhanh` từ cache cơ cấu đơn vị; nếu rỗng → `KhoiTao` (chi nhánh user) | EF + Cache CoCau | `KhoiTao` chỉ tạo cho chi nhánh user hiện tại, không phủ toàn đơn vị (Service:500) |
| 4 | Thêm mới khung giờ (chi nhánh user) | "Thêm mới" | POST `api/KhungGio/ThemMoi` | (không key — `[SBAuthorize]` rỗng) | public | nút Thêm | Check role `RoleDuocPhepThaoTacNgayLamViecVaKhungGio`; validate (tên bắt buộc, không trùng tên trong chi nhánh); set `IdDonVi/IdChiNhanh` = user; `IsCaDem = GioBatDau > GioKetThuc`; insert; clear cache; ghi log tác động | EF + Cache | Permission key gốc đã bị comment (Controller:53), giờ chỉ yêu cầu đăng nhập |
| 5 | Cập nhật khung giờ (chi nhánh user) | "Sửa" | POST `api/KhungGio/CapNhatThongTin` | (không key — `[SBAuthorize]` rỗng) | public | nút Sửa per-row | Check role; validate; tìm bản theo `DonVi+ChiNhánh+Id` (not found → lỗi); cập nhật + `IsCaDem`; clear cache; log | EF + Cache | Permission key gốc đã comment (Controller:39) |
| 6 | Đổi trạng thái sử dụng (chi nhánh user) | toggle "Sử dụng" | POST `api/KhungGio/ChangeIsSuDung/{id}` | (không key — `[SBAuthorize]` rỗng) | public | per-row | Check role; tìm bản theo `DonVi+ChiNhánh+Id`; đảo bit `IsSuDung`; gọi Branch `DeleteUserCaLamViec` (xóa ca làm việc gắn khung giờ); update; clear cache; log | EF + Branch + Cache | Gọi `DeleteUserCaLamViec` cả khi đang BẬT lại khung giờ (không chỉ khi tắt) — Service:155-163 đảo bit rồi luôn xóa ca |
| 7 | Khởi tạo khung giờ mặc định (chi nhánh user) | "Khởi tạo" | GET `api/KhungGio/KhoiTao` | `KhungGio_KhoiTao` | public | khi chi nhánh chưa có | Nếu chi nhánh chưa có khung giờ → tạo Sáng(7-12)/Chiều(12-18)/Tối(18-23), `IsSuDung=true`; trả bool | EF | - |
| 8 | Khởi tạo khung giờ theo chi nhánh (AD-Tổng) | (thao tác AD tổng) | GET `api/KhungGio/KhoiTaoByIdChiNhanh/{id}` | `KhungGio_KhoiTaoByIdChiNhanh` | public (DV-AD-TONG) | - | Như (7) nhưng cho `idChiNhanh` truyền vào | EF | - |
| 9 | Kiểm tra có nhân viên gắn khung giờ không (trước khi tắt) | confirm trước toggle | GET `api/KhungGio/CheckUserCaLamViecToChangeIsSuDung_CN/{id}` | `KhungGio_CheckUserCaLamViecToChangeIsSuDung_CN` | public | trước khi tắt khung giờ | Tìm khung giờ theo `DonVi+Id`; gọi Branch `CheckUserCaLamViecToChangeIsSuDungKhungGio`; nếu đang `IsSuDung=true` và có user gắn ca → trả `false` (cảnh báo); ngược lại `true` | EF + Branch | - |
| 10 | Tìm/Thêm/Sửa/Đổi trạng thái — nhánh AD-DV (chéo chi nhánh) | thao tác AD-DV | GET `api/KhungGio/TimTheoChiNhanh_AD_DV/{id}` · POST `ThemMoi_AD_DV` · POST `CapNhatThongTin_AD_DV` · GET `ChangeIsSuDung_AD_DV/{id}/{idChiNhanh}` | `KhungGio_TimTheoChiNhanh_AD_DV` · `KhungGio_ThemMoi_AD_DV` · `KhungGio_CapNhatThongTin_AD_DV` · `KhungGio_ChangeIsSuDung_AD_DV` | public (DV-AD-TONG, DV-HR) | màn AD-DV quản lý chi nhánh khác | Như nhóm (4-6) nhưng thao tác trên `idChiNhanh` truyền vào (không bắt buộc trùng chi nhánh user); Thêm/Sửa AD-DV bắt buộc `IdChiNhanh != null`. **AD-DV Thêm/Sửa KHÔNG check role**, không ghi log | EF + Branch + Cache | `ThemMoi_AD_DV`/`Update_AD_DV` bỏ check `CheckRoleThaoTac` (khác bản thường) — Service:246, 446; `ChangeIsSuDung_AD_DV` cũng không check role |
| 11 | TimKiemTheoDieuKien | (stub) | POST `api/KhungGio/TimKiemTheoDieuKien` | `KhungGio_TimKiemTheoDieuKien` | public | - | **Trả `Ok()` rỗng** — chưa implement, không gọi service | (none) | Endpoint rỗng (Controller:33-36), giữ permission key nhưng vô tác dụng |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`KhoiTaoByIdChiNhanhPrivate`** (POST, `NgayLamViecReq`) — khởi tạo khung giờ mặc định cho `IdDonVi+IdChiNhanh` truyền qua body (service-to-service, không token user). Lưu ý: trong `KhoiTaoByIdChiNhanhPrivate` của service có nhánh check `QueryLayDanhSachKhungGio` nhưng `queryOrder` cuối cùng bị bỏ (không dùng) — Service:388.
- [internal — `[PrivateSBAuthorize]`] **`GetKhungGioLamViec15Minute_Private`** (POST, `NgayLamViecReq`) — sinh slot giờ theo phút cho `IdDonVi+IdChiNhanh+DateWork`.
- [internal — `[PrivateSBAuthorize]`] **`GetListKhungGioByChiNhanh_Private`** (GET, `idChiNhanh`) — đọc cache `KhungGio_ChiNhanh`; miss → retrieve + set (TTL 1 tháng). Dùng cho service khác lấy khung giờ.
- [internal — `[PrivateSBAuthorize]`] **`GetListKhungGioByDonVi_Private`** (GET, `idDonVi`) — list khung giờ toàn đơn vị (EF trực tiếp, không cache).
- [public — `[SBAuthorize]` rỗng] **`GetKhungGioLamViec15Minute`** (GET, `date`) — sinh slot giờ theo phút cho chi nhánh user theo ngày (dùng cho calendar đặt lịch); bước nhảy = `DistanceTwoTimeFrame` (cấu hình, mặc định 10', ≤0 → 10'); bỏ slot quá khứ nếu là hôm nay; đếm `SoKhach` từ `DatLich`.
- [public — `[SBAuthorize]` rỗng] **`GetListKhungGioByChiNhanh`** (GET) — bản có token: đọc/đổ cache khung giờ theo chi nhánh user.

## Quy tắc nghiệp vụ (gating)
1. **Quyền thao tác (bản thường):** `ThemMoi`/`CapNhatThongTin`/`ChangeIsSuDung` gọi `CheckRoleThaoTac(userLogin, cauHinh.RoleDuocPhepThaoTacNgayLamViecVaKhungGio)` — không thuộc role cấu hình → "Bạn không có quyền thực hiện thao tác này".
2. **Validate khung giờ** (`ValidationKhungGio_Common`): bắt buộc đối tượng/`Ten`/`GioBatDau`/`GioKetThuc` (không null); **không** ép GioBatDau < GioKetThuc (cho phép ca đêm); trùng `Ten` trong cùng `DonVi+ChiNhánh` (khác Id) → lỗi tồn tại.
3. **Ca đêm:** `IsCaDem = GioBatDau > GioKetThuc` (tự suy ra khi thêm/sửa).
4. **Bộ mặc định khởi tạo:** Sáng 07:00-12:00, Chiều 12:00-18:00, Tối 18:00-23:00 — chỉ tạo khi chi nhánh CHƯA có khung giờ.
5. **Tắt khung giờ liên đới ca làm việc:** `ChangeIsSuDung` luôn gọi Branch `DeleteUserCaLamViec` (xóa các ca nhân viên gắn khung giờ này); `CheckUserCaLamViecToChangeIsSuDung_CN` cảnh báo trước nếu đang dùng và có nhân viên gắn.

## Quan hệ
- **Gọi sang:**
  - `CauHinhService.GetAsync(DonViId)` — lấy `RoleDuocPhepThaoTacNgayLamViecVaKhungGio` + `DistanceTwoTimeFrame`.
  - `Branch` — `CaLamViecNhanVien/CheckUserCaLamViecToChangeIsSuDungKhungGio` (kiểm tra), `CaLamViecNhanVien/DeleteUserCaLamViec` (xóa ca khi đổi trạng thái).
  - `CommonService.GetCoCauDonViTuCache` — enrich tên chi nhánh.
  - Cache (Redis) — `Get/Set/ClearCache_KhungGio_ChiNhanh` (TTL 1 tháng).
- **Dữ liệu (entities):** `KhungGio` (Ten, GioBatDau, GioKetThuc, IsSuDung, IsCaDem, IdDonVi, IdChiNhanh); join `KhungGioNgayLamViec` ↔ `NgayLamViec`; `DatLich` (đếm số khách theo slot).
- **Ripple / đồng bộ:** mỗi lần Thêm/Sửa/Đổi trạng thái → `ClearCache_KhungGio_ChiNhanh` → các consumer (ngày làm việc, đặt lịch) lần sau lấy lại; đổi trạng thái còn xóa ca làm việc bên Branch.

## Luồng xuyên module (khởi tạo ngày làm việc kéo theo khung giờ)
```
NgayLamViecService.KhoiTao/KhoiTaoByIdChiNhanh: nếu chi nhánh chưa có KhungGio
  → HTTP nội bộ gọi Booking `api/KhungGio/KhoiTao` (hoặc KhoiTaoByIdChiNhanh)
      → tạo Sáng/Chiều/Tối
  → NgayLamViec map mỗi ngày-trong-tuần × các KhungGio (KhungGioNgayLamViec)
Calendar đặt lịch: GetKhungGioLamViec15Minute → slot giờ (bước = DistanceTwoTimeFrame) + đếm khách từ DatLich
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`ThemMoi_AD_DV` / `Update_AD_DV` / `ChangeIsSuDung_AD_DV` KHÔNG check `CheckRoleThaoTac`** (Service:246, 446; 179) — khác bản thường (có check role theo cấu hình). Quyền chỉ dựa vào permission key `*_AD_DV` (DV-AD-TONG/DV-HR), không qua `RoleDuocPhepThaoTacNgayLamViecVaKhungGio`.
- **`ChangeIsSuDung` luôn gọi `DeleteUserCaLamViec`** (Service:155-163) — kể cả khi đang BẬT lại khung giờ (đảo bit rồi vô điều kiện gọi xóa ca); rủi ro xóa nhầm ca khi bật lại khung giờ.
- **`TimKiemTheoDieuKien` là stub** — `return Ok();` (Controller:33-36), không gọi service; giữ permission key nhưng không có dữ liệu.
- **Nhiều endpoint mất permission key** — `ThemMoi`/`CapNhatThongTin`/`ChangeIsSuDung`/`TimTheoChiNhanh`/`LayTatCaTheoDonVi`/`GetListKhungGioByChiNhanh` dùng `[SBAuthorize]` rỗng (key gốc đã comment, Controller:39,53,84,141) → chỉ cần đăng nhập, không phân quyền chi tiết.
- **`LayTatCaTheoDonVi` fallback `KhoiTao` lệch phạm vi** — khi đơn vị chưa có khung giờ, gọi `KhoiTao()` chỉ tạo cho chi nhánh user, không phủ mọi chi nhánh (Service:498-501).
- **`KhoiTaoByIdChiNhanhPrivate` có nhánh chết** — `queryOrder = QueryOrderDanhSachKhungGio(tmpQuery)` cuối hàm không được dùng (Service:388); chỉ trả `true`.
- **`Xoa` đã comment** — không có endpoint xóa khung giờ (Controller:76-82; Service:100-109); chỉ bật/tắt `IsSuDung`.
- Code thật: BE controller `Booking/Booking.API/Controllers/KhungGioController.cs`; service `Booking/Booking.Service/Generated/KhungGioService.cs`; entity `Booking.DataContext/Entity/KhungGio.cs`.
