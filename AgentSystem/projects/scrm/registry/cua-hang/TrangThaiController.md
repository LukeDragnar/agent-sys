---
controller: TrangThaiController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Đặt lịch — cấu hình 'Trạng thái đặt lịch' (danh sách trạng thái lịch hẹn: mã, tên, màu nền/chữ, class CSS; có nhánh riêng cho System Admin theo ngành nghề)"
muc_dich: "Quản lý danh mục trạng thái đặt lịch (DatLich) — bản dùng chung theo ngành nghề (IdDonVi=Guid.Empty, do System Admin tạo) và bản riêng cho từng đơn vị (khởi tạo từ bản mẫu ngành nghề)."
alias: ["trạng thái đặt lịch", "trạng thái lịch hẹn", "trạng thái booking", "danh mục trạng thái đặt lịch", "màu trạng thái lịch"]
auth: mixed
state_machine: []
deprecated: false
entities: [TrangThai]
stored_procs: []
depends: [Booking, ICommonService (RequestGetNganhNghe — lấy danh sách ngành nghề)]
cross_module_in: []
cross_module_out: [Màn đặt lịch / calendar (DatLich) dùng TrangThai để hiển thị màu/nhãn trạng thái lịch hẹn]
---

# TrangThaiController — Quản lý trạng thái đặt lịch

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm trạng thái theo điều kiện (đơn vị) | "Tìm kiếm" / load grid | POST `api/TrangThai/TimTheoDieuKien` | `TrangThai_TimTheoDieuKien` | public | màn list của đơn vị | List `TrangThai` của đơn vị hiện tại (`IdDonVi == userLogin.DonViId`); lọc Keyword (`Ten.Contains`) + IdNganhNghe (nếu có); sort theo `NgayTao` desc; phân trang (total + Skip/Take) | EF (nolock) | Sort luôn theo NgayTao desc — `gridInfo.Sorts` chỉ dùng để rẽ nhánh nhưng cả 2 nhánh giống nhau (Service:73-80), client không đổi được cột sort |
| 2 | Khởi tạo trạng thái mặc định cho đơn vị | "Khởi tạo mặc định" | POST `api/TrangThai/InitDataForUnit` | `TrangThai_InitDataForUnit` | public | khi đơn vị chưa có / muốn nạp bộ mặc định | Copy các `TrangThai` mẫu của ngành nghề (`IdDonVi == Guid.Empty`, lọc theo `IdNganhNghe` nếu truyền) sang đơn vị hiện tại; bỏ qua các bản tên đã tồn tại (so theo `Ten.ToLower()`); insert phần còn thiếu | EF (nolock) | So trùng theo `Ten.ToLower()` (Service:241) — chỉ chống trùng theo TÊN, không theo MÃ; 2 mẫu khác mã cùng tên sẽ mất 1 |
| 3 | Thêm mới trạng thái (System Admin) | "Thêm mới" (màn System Admin) | POST `api/TrangThai/ThemMoiAsyncSA` | `TrangThai_ThemMoiAsyncSA` | public (chỉ role HT-AD) | màn quản trị hệ thống (HT-AD) | ModelState valid; chặn trùng `Ma` HOẶC `Ten`; bắt buộc `IdNganhNghe`; chuẩn hoá khoảng trắng `Ten`; set `IdDonVi = Guid.Empty` (bản dùng chung ngành nghề); insert | EF | Check trùng `Ma`/`Ten` quét TOÀN bảng (mọi đơn vị), không scope theo ngành nghề/đơn vị (Service:136) → trùng tên cross-tenant cũng bị chặn |
| 4 | Tìm trạng thái (System Admin, theo ngành nghề) | "Tìm kiếm" (màn System Admin) | POST `api/TrangThai/TimTheoDieuKienSA` | `TrangThai_TimTheoDieuKienSA` | public (chỉ role HT-AD) | màn quản trị hệ thống (HT-AD) | List các bản dùng chung (`IdDonVi == Guid.Empty`); lọc Keyword + IdNganhNghe; trả `ResTrangThai` kèm `TenNganhNghe` (enrich từ `RequestGetNganhNghe`); phân trang | EF (nolock) + Common (ngành nghề) | `lstNganhNghe.FirstOrDefault(...).Ten` (Service:208) — nếu không tìm thấy ngành nghề khớp → NullReferenceException (gọi `.Ten` trên null trước khi `?? ""`) |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint `[PrivateSBAuthorize]`/`[AllowAnonymous]` trong controller này.
- Method `Xoa` (xóa trạng thái) đã **comment toàn bộ** trong controller (TrangThaiController.cs:45-51) — hiện không expose. Service không có method delete riêng (chỉ kế thừa `BaseService`).
- `UpdateAsync` / `InsertAsync` (bản đơn vị) tồn tại trong service nhưng **KHÔNG có action controller riêng** trỏ tới (chỉ là override `BaseService`); cập nhật trạng thái bản đơn vị hiện không có endpoint public rõ ràng.

## Quy tắc nghiệp vụ (gating)
- **Hai phạm vi dữ liệu tách theo `IdDonVi`:**
  - `IdDonVi == Guid.Empty` = bản mẫu dùng chung theo ngành nghề (chỉ HT-AD tạo qua `ThemMoiAsyncSA` / xem qua `TimTheoDieuKienSA`).
  - `IdDonVi == userLogin.DonViId` = bản riêng của đơn vị (xem qua `TimTheoDieuKien`, nạp qua `InitDataForUnit`).
- **InitDataForUnit** = copy mẫu ngành nghề → đơn vị, **idempotent theo tên** (bỏ tên đã có); set `IdDonVi = userLogin.DonViId` cho bản copy.
- **Chặn trùng khi insert** (cả bản SA và bản đơn vị): trùng `Ma` HOẶC `Ten` (toàn bảng) → lỗi "Đã tồn tại mã này, vui lòng thử lại".

## Quan hệ
- **Gọi sang:** `ICommonService.RequestGetNganhNghe()` để lấy tên ngành nghề (chỉ dùng ở `TimTheoDieuKienSA`).
- **Dữ liệu (entities):** `TrangThai` (Ma, Ten, Class, BgColor, Color, SuDung, IdNganhNghe, IdDonVi).
- **Ripple / đồng bộ:** danh mục trạng thái này được màn đặt lịch (DatLich/calendar) dùng để render màu + nhãn trạng thái lịch hẹn. Không thấy clear-cache / event đồng bộ trong service.

## Lỗi đã biết / lưu ý kỹ thuật
- **`TimTheoDieuKienAsyncSA` có thể NRE** — `lstNganhNghe.FirstOrDefault(x => x.Id == item.IdNganhNghe).Ten ?? ""` (Service:208): nếu không có ngành nghề khớp, `FirstOrDefault` trả null → truy cập `.Ten` ném NullReferenceException trước khi `?? ""` kịp chạy.
- **Chống trùng quét toàn bảng** — `InsertAsync`/`InsertAsyncSA`/`UpdateAsync` check `Ma`/`Ten` trên toàn bộ bảng `TrangThai`, không scope theo `IdDonVi`/`IdNganhNghe` (Service:35, 105, 136) → có thể chặn nhầm khi tên trùng giữa các đơn vị/ngành nghề khác nhau.
- **Sort không tùy biến** — `TimTheoDieuKienAsync`/`...SA`: 2 nhánh if/else của `gridInfo.Sorts` cho ra cùng `OrderByDescending(NgayTao)` (Service:73-80, 168-175) → tham số sort từ client vô tác dụng.
- **InitDataForUnit chống trùng theo tên, không theo mã** — `resOwner.RemoveAll(x => resUnit.Contains(x.Ten.ToLower()))` (Service:241): chỉ so tên (lowercase), bỏ qua `Ma`; mẫu khác mã cùng tên sẽ bị loại.
- **Không có endpoint Xóa / Cập nhật bản đơn vị** — `Xoa` đã comment trong controller; `UpdateAsync` chỉ là override base, không có action map tới → màn đơn vị thực tế không sửa/xóa được qua controller này (cần xác nhận chủ đích).
- Code thật: BE controller `Booking/Booking.API/Controllers/TrangThaiController.cs`; service `Booking/Booking.Service/Generated/TrangThaiService.cs`; entity `Booking.DataContext/Entity/TrangThai.cs`.
