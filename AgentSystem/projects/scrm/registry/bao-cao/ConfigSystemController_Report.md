---
controller: ConfigSystemController
module: bao-cao
service: Report
man_hinh_ui: "nội bộ — QuanTriArea (SandBoxArea), màn quản trị cấu hình hệ thống Report: thêm/sửa/xóa/tìm/xem chi tiết bản ghi ConfigSystem (mã, tên, kiểu loại, value, sử dụng)."
muc_dich: "CRUD cấu hình hệ thống của Report-service (entity ConfigSystem) — danh mục config key-value (kiểu int/datetime/bool/text) dùng cho các giới hạn báo cáo (số giây giới hạn đọc báo cáo, số user xử lý đồng thời...). Mỗi lần ghi đều refresh Redis cache cấu hình hệ thống."
alias: ["cấu hình hệ thống report", "config system", "cau hinh he thong", "quản trị cấu hình", "configsystem report"]
auth: internal
state_machine: []
deprecated: true
entities: [ConfigSystem]
stored_procs: []
depends: [ConfigSystemService (EF reposConfigSystem), CommonService.LuuRedisCauHinhHeThong (refresh cache), UserPrincipalService (check SuperUser khi xóa)]
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController — Cấu hình hệ thống Report (CRUD config key-value)

> Controller CRUD cấu hình hệ thống đặt trong `Areas/SandBoxArea` nhưng route `api/QuanTriArea/ConfigSystem`. `[TienIch("ConfigSystemController_Report", "Cấu hình hệ thống", MaTienIchSCRM.TI_HeThong, true)]` — cờ `true` = tiện ích **đánh dấu khóa/ẩn (deprecated/nội bộ quản trị)**; controller-level `[SBAuthorize]` + mỗi method `[SBAuthorize()]` → truy cập **nội bộ quản trị** (không phải báo cáo công khai). **Toàn bộ đi EF entity `ConfigSystem`** (LINQ trên `ReportDataContext`), KHÔNG stored proc. Mỗi Insert/Update/Delete đều validate kiểu (Ma regex, KieuLoai 2=int/3=datetime/4=bool) rồi gọi `CommonService.LuuRedisCauHinhHeThong()` nạp lại cache `CauHinhReportHeThong`. Xóa chỉ cho `IsSuperUser`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm cấu hình | Thêm mới | `POST api/QuanTriArea/ConfigSystem/InsertConfig` | (mặc định) | internal | màn quản trị config | Validate Ma (không trùng + regex isMa), KieuLoai/Ten/SuDung/Value bắt buộc, value đúng kiểu (2 int/3 datetime/4 bool), MoTa ≤500. Insert + refresh Redis. | **EF** `ConfigSystem` | — |
| 2 | Sửa cấu hình | Cập nhật | `POST api/QuanTriArea/ConfigSystem/UpdateConfig` | (mặc định) | internal | màn quản trị config | Validate như Insert (Ma không trùng Id khác), MarkDirty các cột → Update + refresh Redis. | **EF** `ConfigSystem` | Update validate int dùng `BigInteger.TryParse` (Insert dùng `int.TryParse`) — không đồng nhất |
| 3 | Tìm theo điều kiện | Lọc danh sách | `POST api/QuanTriArea/ConfigSystem/TimTheoDieuKien` | (mặc định) | internal | lưới config | Lọc theo keyword (Ma/Ten) + suDung; trả list (Id,Ma,Ten,MoTa,Value,NgayCapNhat,SuDung) order theo NgayCapNhat desc. | **EF** `ConfigSystem` | Paging bị comment (L241-242) → luôn trả TẤT CẢ bản ghi, `count` tính nhưng không trả ra |
| 4 | Xem chi tiết | Mở chi tiết | `GET api/QuanTriArea/ConfigSystem/GetDetail?id=` | (mặc định) | internal | mở 1 config | Trả 1 bản ghi (Ma,Ten,MoTa,Value,KieuLoai,SuDung) theo Id; không có → lỗi "Dữ liệu không tồn tại". | **EF** `ConfigSystem` | — |
| 5 | Xóa cấu hình | Xóa | `POST api/QuanTriArea/ConfigSystem/DeleteConfig` | (mặc định) | internal | màn quản trị config | CHỈ `IsSuperUser` mới xóa; không có quyền → lỗi. Tìm theo Id, DeleteAsync + refresh Redis. | **EF** `ConfigSystem` | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **KieuLoai → kiểu Value:** 2=integer, 3=datetime, 4=boolean → `Value` phải parse đúng kiểu, sai → trả lỗi định dạng.
- **Ma:** bắt buộc, không trùng (Insert toàn bảng; Update so Id khác), phải qua `RegexHelpers.isMa` (chỉ ký tự không dấu + `- _ / .`).
- **Quyền xóa:** chỉ `IsSuperUser`; mọi role khác → "Bạn không có quyền xóa cấu hình!".
- **Đồng bộ cache:** mọi ghi (Insert/Update/Delete) gọi `CommonService.LuuRedisCauHinhHeThong()` → cache `CacheKey.CauHinhReportHeThong` được nạp lại; TicketInfoService/CommonService đọc cache này (giới hạn báo cáo).

## Quan hệ
- **Gọi sang:** `CommonService.LuuRedisCauHinhHeThong` (refresh Redis); `UserPrincipalService.GetUserLogin` (check SuperUser).
- **Dữ liệu (entities):** EF entity `ConfigSystem` trên `ReportDataContext`.
- **Ripple / đồng bộ:** thay đổi config → refresh Redis `CauHinhReportHeThong` → ảnh hưởng tới các gate giới hạn báo cáo (CommonService.CheckThoiGianTruyCap/GetTimeToLog, TicketInfoService.DoProcessTicket).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/ConfigSystemService.cs`
  - **L241-242 paging bị comment** trong `TimTheoDieuKien` → `Skip/Take` không chạy, trả toàn bộ bản ghi; `count` (L239) tính xong nhưng không đưa vào kết quả.
  - L168-169 Update kiểm int bằng `BigInteger.TryParse` còn Insert (L92-93) bằng `int.TryParse` → ngưỡng số khác nhau giữa thêm và sửa.
  - L86 message lỗi "không được quá 1000 kí tự" nhưng điều kiện là `> 500` (sai message); Update (L158) message đúng "500".
- BE `Report/Report.API/Areas/SandBoxArea/ConfigSystemController.cs`
  - L14 `TienIch(..., TI_HeThong, true)` → tiện ích cờ deprecated/khóa; route `QuanTriArea` (quản trị nội bộ). Trùng node name module khác → tách `ConfigSystemController_Report.md`.
