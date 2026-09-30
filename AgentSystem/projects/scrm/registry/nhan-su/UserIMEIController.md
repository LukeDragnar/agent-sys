---
controller: UserIMEIController
module: nhan-su
service: Branch
man_hinh_ui: "Chấm công > IMEI thiết bị (danh sách IMEI theo chi nhánh + thêm/sửa/xóa/phê duyệt); app NV: đăng ký & kiểm tra IMEI thiết bị khi chấm công"
muc_dich: "Đăng ký & phê duyệt IMEI thiết bị nhân viên để chấm công theo thiết bị: CRUD IMEI, phê duyệt (toggle IsDuyet), NV tự đăng ký IMEI (DangKyIMEI) và kiểm tra tồn tại/trạng thái (CheckExsit / CheckImei)."
alias: [UserIMEI, IMEI, "IMEI thiết bị", "chấm công IMEI", "đăng ký IMEI", "thiết bị chấm công"]
auth: mixed
state_machine: [ChuaDuyet, DaDuyet]
deprecated: false
entities: [UserIMEI]
stored_procs: []
depends: [CauHinhService (RoleDuocPhepThaoTacThietBiChamCong), Core (ChiNhanh), LogActivityHelper]
cross_module_in: [App chấm công NV (DangKyIMEI / CheckExsit / CheckImei khi check-in/out theo thiết bị)]
cross_module_out: [Core (denormalize chi nhánh), Log (LichSuTacDong)]
---

# UserIMEI — Đăng ký & phê duyệt IMEI thiết bị chấm công

> `[TienIch(...TI_ChamCong)]`. Kế thừa `BaseController`. Pattern giống `BSSIDController`: chỉ 2 action đọc (`TimTheoDieuKien`, `TimTheoId`) giữ key `UserIMEI_*`; mọi action ghi + self-service (CapNhat/ThemMoi/Xoa/PheDuyet/CheckExsit/DangKyIMEI/CheckImei) đã **comment key**, để `[SBAuthorize()]` rỗng → login-only, role thực thi inline trong service.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm IMEI theo điều kiện | Danh sách/Lọc | POST `TimTheoDieuKien` | `UserIMEI_TimTheoDieuKien` | public | Quản lý (AD/QLDV/HR) | Lọc IMEI theo chi nhánh user + denormalize tên CN. | EF + Core | — |
| 2 | Cập nhật IMEI | Lưu | POST `CapNhat` | `[SBAuthorize()]` rỗng | mixed | Quản lý | Role inline; sửa bản ghi thuộc CN+đơn vị user; ghi log. | EF | Key comment dòng 34 |
| 3 | Thêm mới IMEI | Lưu | POST `ThemMoi` | `[SBAuthorize()]` rỗng | mixed | Quản lý | Role inline; validate trùng; IsDuyet mặc định false. | EF | Key comment dòng 47 |
| 4 | Xóa IMEI | Xóa | GET `Xoa/{id:Guid}` | `[SBAuthorize()]` rỗng | mixed | Quản lý | Role inline; xóa bản ghi thuộc CN user; ghi log DELETE. | EF | GET cho xóa; key comment dòng 60 |
| 5 | Phê duyệt IMEI | Phê duyệt | GET `PheDuyet/{id:Guid}` | `[SBAuthorize()]` rỗng | mixed | Quản lý, bản ghi thuộc CN | Toggle `IsDuyet` (duyệt/bỏ duyệt); ghi log. | EF | GET; toggle; key comment dòng 73 |
| 6 | Tìm IMEI theo Id | Mở edit | GET `TimTheoId/{id:Guid}` | `UserIMEI_TimTheoId` | public | Quản lý | Trả chi tiết IMEI thuộc CN+đơn vị user. | EF | — |
| 7 | Kiểm tra IMEI tồn tại (app NV) | (nền) | POST `CheckExsit` | `[SBAuthorize()]` rỗng | mixed | NV đăng nhập | Trả bool: IMEI này đã đăng ký cho **chính** NV (`UserId == user.Id`) trong CN chưa. | EF | Key comment dòng 98; typo "CheckExsit"; lọc theo UserId |
| 8 | Đăng ký IMEI (app NV) | Đăng ký thiết bị | POST `DangKyIMEI` | `[SBAuthorize()]` rỗng | mixed | NV đăng nhập | NV tự đăng ký IMEI thiết bị (chờ duyệt). | EF | Key comment dòng 111 |
| 9 | Kiểm tra trạng thái IMEI (app NV) | (nền chấm công) | POST `CheckImei` | `[SBAuthorize()]` rỗng | mixed | NV chấm công | Trả `Response_CheckIMEIDDto` (tồn tại + đã duyệt?) khi check-in/out. | EF | Key comment dòng 125 |

## Quy tắc nghiệp vụ (gating / state)
- **State duyệt:** `IsDuyet` (false=Chưa duyệt, true=Đã duyệt). `PheDuyet` là toggle (giống BSSID). NV `DangKyIMEI` tạo bản ghi chờ quản lý duyệt; chỉ IMEI đã duyệt mới hợp lệ chấm công thiết bị.
- **Role thao tác (inline):** CapNhat/ThemMoi/Xoa/PheDuyet check `CommonService.CheckRoleThaoTac(user, cauHinh.RoleDuocPhepThaoTacThietBiChamCong)` trước khi thao tác — không dựa permission key (đã comment). `PheDuyet` là toggle `IsDuyet = !IsDuyet`.
- **Scope chi nhánh:** ép `IdChiNhanh == user.IdChiNhanh && IdDonVi == user.DonViId` (nhánh admin xem toàn đơn vị đã comment).

## Luồng xuyên module
- App chấm công NV: `DangKyIMEI` (đăng ký thiết bị) → quản lý `PheDuyet` → NV chấm công gọi `CheckExsit`/`CheckImei` để xác minh IMEI đã đăng ký + đã duyệt. Thao tác ghi/xóa/duyệt ghi LichSuTacDong sang Log; đọc danh sách gọi Core lấy tên CN.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserIMEIController.cs:35,48,61,74,99,112,126` — CapNhat/ThemMoi/Xoa/PheDuyet/CheckExsit/DangKyIMEI/CheckImei để `[SBAuthorize()]` rỗng (key gốc comment ngay trên) → login-only + role inline.
- `UserIMEIController.cs:62,75` — `Xoa` và `PheDuyet` dùng HTTP GET.
- `UserIMEIController.cs:100` — action `CheckExsit` sai chính tả ("Exsit") — giữ nguyên để khớp FE/route.
- `UserIMEIService.cs:114,151` — role config dùng `RoleDuocPhepThaoTacThietBiChamCong` (khác BSSID dùng `RoleDuocPhepThaoTacThietBiWifi`).
- `UserIMEIService.cs:141` — LichSuTacDong ghi `TableId = SCRM_MAJOR_BSSID` (copy nhầm từ BSSID; lẽ ra là TableId của IMEI) → log IMEI bị gắn nhầm bảng BSSID.
