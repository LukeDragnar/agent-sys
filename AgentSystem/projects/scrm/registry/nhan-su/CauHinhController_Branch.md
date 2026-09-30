---
controller: CauHinhController
module: nhan-su
service: Branch
man_hinh_ui: "Cấu hình > Cấu hình nghiệp vụ (service Branch): 3 nhóm setting — chung, hoa hồng, chấm công (check-in). Lưu theo từng nhóm; Get trả toàn bộ JsonSetting của đơn vị."
muc_dich: "Quản lý cấu hình nghiệp vụ của đơn vị (1 bản ghi CauHinh.JsonSetting/đơn vị): Save (toàn bộ), SaveCommission (chỉ field hoa hồng), SaveCheckIn (chỉ field chấm công), Get (đọc, fallback bản ghi default Guid.Empty). Có cache Redis 1 ngày."
alias: ["cấu hình nghiệp vụ", "cấu hình hoa hồng", "cấu hình chấm công", "cấu hình check in", "setting branch", "cau hinh branch"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [Redis cache (CK_CauHinhBranchSetting_ theo đơn vị, TTL 1 ngày), LogActivityHelper (lịch sử tác động)]
cross_module_in: []
cross_module_out: []
---

# CauHinhController (Branch) — Cấu hình nghiệp vụ đơn vị

> Controller cấu hình phía service **Branch** (đừng nhầm với `CauHinhController.md` nhan-su của service khác). Lưu 1 bản ghi `CauHinh` / đơn vị, dữ liệu nằm trong cột `JsonSetting` (serialize `CauHinhSettingDto`). 3 endpoint Save có key (HT/role `DV-AD-TONG`); `Get` chỉ `[SBAuthorize]` trống (login). `SaveCommission`/`SaveCheckIn` đọc setting hiện tại rồi chỉ ghi đè các field thuộc nhóm tương ứng → tránh mất các field khác. Cache Redis `CK_CauHinhBranchSetting_{idDonVi}` (TTL 1 ngày), clear khi Save. Get fallback: đơn vị chưa có setting → lấy bản ghi `IdDonVi=Guid.Empty`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình nghiệp vụ (chung) | Lưu | `POST api/CauHinh/Save` | `CauHinh_Branch_LuuCauHinhNghiepVu` (DV-AD-TONG) | public | tab cấu hình chung | Validate; serialize toàn bộ `CauHinhSettingDto` → JsonSetting; insert (chưa có) hoặc update; clear cache + ghi LichSuTacDong. | **EF** `CauHinh` | `CauHinhService.cs:49` |
| 2 | Lưu cấu hình hoa hồng | Lưu | `POST api/CauHinh/SaveCommission` | `CauHinh_Branch_LuuCauHinhNghiepVuCommission` (DV-AD-TONG) | public | tab hoa hồng | Đọc setting hiện tại; **chỉ ghi đè** field hoa hồng (GiaTriDoiTuongTinhHoaHong, HoaHongThoiDiemTinh, ChiaDeuHhNhanVien, HoaHongKhachCu, HoaHongNguonDatLich, CongDonHoaHongNguonDatLich) rồi SaveAsync. | **EF** | `CauHinhController.cs:34` |
| 3 | Lưu cấu hình chấm công (check-in) | Lưu | `POST api/CauHinh/SaveCheckIn` | `CauHinh_Branch_LuuCauHinhNghiepVuCheckIn` (DV-AD-TONG) | public | tab chấm công | Đọc setting hiện tại; **chỉ ghi đè** field check-in (SoPhutChenhLech, KhoangCachChoPhep, PhuongPhapCheckIn, BatBuoc/KhongDuDieuKien, các Role thao tác chấm công/thiết bị/wifi/phân ca/duyệt phiếu yêu cầu...) rồi SaveAsync. | **EF** | `CauHinhController.cs:57` |
| 4 | Lấy cấu hình nghiệp vụ | (load màn) | `GET api/CauHinh/Get` | `SBAuthorize` (login) | public | mở màn cấu hình | Trả `CauHinhSettingDto` từ cache; cache miss → DB; đơn vị chưa có → fallback bản ghi `Guid.Empty`; vẫn rỗng → DTO mặc định. Cache 1 ngày. | **EF** (cache) | `CauHinhController.cs:88` — key cũ `CauHinh_Branch_LayCauHinhNghiepVu` đã comment, để `[SBAuthorize]` trống. |

## Quy tắc nghiệp vụ (gating)
- **Phân nhóm Save:** `Save` ghi cả DTO; `SaveCommission`/`SaveCheckIn` load DTO hiện tại trước, chỉ ghi đè đúng nhóm field → KHÔNG được phá field nhóm khác (nếu `GetAsync` lỗi → return luôn, không ghi).
- **Scope đơn vị:** ghi/đọc theo `userLogin.DonViId`. Code superuser/HT-AD ép `IdDonVi=Guid.Empty` đã bị **comment** (L67-70, L154-157) → hiện admin cũng lưu theo DonViId của họ, chỉ Get có fallback đọc về bản ghi `Guid.Empty` khi đơn vị chưa có.

## Quan hệ
- **Gọi sang:** không service ngoài; EF `CauHinh` + Redis.
- **Dữ liệu (entities):** `CauHinh` (1 record/đơn vị, `JsonSetting` = `CauHinhSettingDto` serialize).
- **Ripple / đồng bộ:** clear cache `CK_CauHinhBranchSetting_{idDonVi}` sau Save; ghi `EventLichSuTacDong` (TableId SCRM_BRANCH_CauHinh) qua LogActivityHelper.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhService.cs:67-70, 154-157` — logic ép `IdDonVi=Guid.Empty` cho superuser/HT-AD đã comment → admin lưu theo DonViId riêng (có thể không sửa được bản ghi default toàn hệ thống qua màn này).
- `CauHinhService.cs:125` — `ValidationSaveAsync` gần như rỗng (chỉ check null); ràng buộc MaxCustomerInGroup đã comment.
- `CauHinhController.cs:88` (`Get`) — `[SBAuthorize]` trống: chỉ cần login.
- Cache TTL 1 ngày: thay đổi DB ngoài luồng Save sẽ không phản ánh tới khi cache hết hạn / clear.
