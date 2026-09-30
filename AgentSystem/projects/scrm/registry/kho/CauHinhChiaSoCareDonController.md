---
controller: CauHinhChiaSoCareDonController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > Cấu hình chia số care đơn (theo chi nhánh): chọn kiểu chia — Kiểu1 'chia về nhóm' (cấu hình cây Nhóm user → User) hoặc Kiểu2 'user liên kết nhóm sale' (gán trực tiếp tài khoản care đơn vào nhiều nhóm sale, bật/tắt nhận đơn từng nhóm); popup lịch sử thay đổi cấu hình"
muc_dich: "Cấu hình quy tắc chia số care đơn cho 1 chi nhánh: chọn kiểu chia (Kiểu1 = chia về nhóm user care đơn; Kiểu2/Option2 = tài khoản care đơn liên kết trực tiếp với (nhiều) nhóm sale + bật/tắt nhận đơn), CRUD danh sách user liên kết, đổi kiểu chia, và xem lịch sử thay đổi cấu hình."
alias: ["cấu hình chia số care đơn", "thiết lập chia care đơn", "cấu hình care đơn theo chi nhánh", "kiểu chia care đơn", "user liên kết nhóm sale care đơn"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhChiaSoCareDon, CauHinhChiaSoDoiTuongCareDon, LichSuThayDoiCHCS]
stored_procs: []
depends:
  - "ICauHinhChiaSoCareDonService (OrderLogistic.Service/Generated/CauHinhChiaSoCareDonService.cs)"
  - "ICommonCacheService (CHCSCareDon_GetBy/SetBy_IdCHCSAsync — cache cấu hình theo IdCHCS)"
cross_module_in:
  - "FE màn Cấu hình chia số care đơn (tác nghiệp kho) — cấu hình ở đây là input cho ChiaSoCareDonController khi chia/phân bổ đơn"
cross_module_out:
  - "ChiaSoCareDonController (OrderLogistic) — cấu hình kiểu chia + danh sách nhóm/user care đơn được dùng khi phân bổ đơn"
---

# CauHinhChiaSoCareDonController — Cấu hình chia số care đơn (theo chi nhánh)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình Kiểu1 (chia về nhóm) | "Lưu" (tab Chia về nhóm) | POST `api/CauHinhChiaSoCareDon/Save_Kieu1` | `Save_Kieu1` | public | kiểu chia = về nhóm | Tạo/cập nhật cấu hình + cây Nhóm user (`LoaiDoiTuong=NhomUser`) và User con (`LoaiDoiTuong=User`); check `IsApDungCSCareDonChoChiNhanh` trước; refresh cache CHCS | EF | Pre-check qua `IsApDungCSCareDonChoChiNhanhAsync` — nếu chi nhánh không bật care đơn → trả lỗi |
| 2 | Lấy chi tiết cấu hình | mở màn cấu hình | GET `api/CauHinhChiaSoCareDon/GetDetail` | `CauHinhChiaSoCareDon_GetDetail` | public | màn cấu hình | Resolve `IdCHCS` theo (DonViId, IdChiNhanh) của user → trả chi tiết cấu hình (cây nhóm/user + danh sách user liên kết Option2); đọc qua cache | EF (cache) | Nếu chưa có cấu hình → trả template rỗng |
| 3 | Check chi nhánh được dùng care đơn | gating ẩn/hiện màn | POST `api/CauHinhChiaSoCareDon/CheckApDungCSCareDonChoChiNhanh` | `[SBAuthorize]` (mở) | public | - | Trả bool chi nhánh user login có được phép dùng tính năng care đơn | EF | - |
| 4 | Lưu Kiểu2 (user liên kết nhóm sale) | "Thêm" (tab user liên kết) | POST `api/CauHinhChiaSoCareDon/Save_Kieu2_CareDonLienKet` | `CauHinhChiaSoCareDon_Save_Kieu2_CareDonLienKet` | public | kiểu chia = user liên kết | Thêm tài khoản care đơn liên kết trực tiếp với (nhiều) nhóm sale (`CauHinhChiaSoCareDonType=Option2`); trả list tên lỗi/trùng nếu có | EF | Trả `List<string>` các trường hợp không thêm được |
| 5 | Cập nhật grid user liên kết | "Lưu grid" | POST `api/CauHinhChiaSoCareDon/Update_Kieu2_CareDonLienKet` | `CauHinhChiaSoCareDon_Update_Kieu2_CareDonLienKet` | public | có user liên kết | Cập nhật cấu hình grid user care đơn liên kết nhóm sale | EF | - |
| 6 | Bật/tắt nhận đơn — ALL nhóm sale | toggle tổng | POST `api/CauHinhChiaSoCareDon/BatTatUserCareDonAllNhomSale` | `CauHinhChiaSoCareDon_BatTatUserCareDonAllNhomSale` | public | user liên kết ≥1 nhóm | Bật/tắt nhận đơn cho 1 tài khoản care đơn trên TẤT CẢ nhóm sale liên kết | EF | - |
| 7 | Bật/tắt nhận đơn — 1 nhóm sale | toggle per-nhóm | POST `api/CauHinhChiaSoCareDon/BatTatUserCareDonNhomSale` | `CauHinhChiaSoCareDon_BatTatUserCareDonNhomSale` | public | per liên kết user-nhóm | Bật/tắt nhận đơn cho 1 tài khoản trên 1 nhóm sale cụ thể | EF | - |
| 8 | DS user care đơn liên kết nhóm sale | grid tab user liên kết | POST `api/CauHinhChiaSoCareDon/GetDSUserCareDonLienKetNhomSale` | `CauHinhChiaSoCareDon_GetDSUserCareDonLienKetNhomSale` | public | - | List tài khoản care đơn liên kết nhóm sale (phân trang/filter) | EF | - |
| 9 | Đổi kiểu chia care đơn | radio Kiểu1/Kiểu2 | POST `api/CauHinhChiaSoCareDon/ChangeTypeCHCSCareDon` | `CauHinhChiaSoCareDon_ChangeTypeCHCSCareDon` | public | - | Chuyển kiểu chia của chi nhánh (về nhóm ⇄ user liên kết); trả IdCHCS | EF | Đổi kiểu có thể đổi cách `GetDetail` build cây/grid |
| 10 | Xóa nhiều user liên kết | "Xóa" multi-select | POST `api/CauHinhChiaSoCareDon/DeleteMultiUserCareDonNhomSale` | `CauHinhChiaSoCareDon_DeleteMultiUserCareDonNhomSale` | public | tick ≥1 user | Xóa cấu hình các tài khoản care đơn liên kết nhóm sale đã chọn | EF | - |
| 11 | DS nhóm sale/CSKH liên kết | dropdown chọn nhóm | GET `api/CauHinhChiaSoCareDon/GetDSNhomSaleOrCSKHLienKet` | `[SBAuthorize]` (mở) | public | - | List nhóm sale / CSKH để liên kết user care đơn | EF | Không gắn permission key riêng |
| 12 | Lịch sử thay đổi cấu hình | popup "Lịch sử" | POST `api/CauHinhChiaSoCareDon/LichSuThayDoiCauHinhChiaSo` | (KHÔNG có) | anonymous | - | List lịch sử thay đổi cấu hình chia số (filter) | EF | **Không có attribute auth nào** — endpoint mở hoàn toàn (permission attr đã comment, không có `[SBAuthorize]`) (CauHinhChiaSoCareDonController.cs:151-156) |

## Quy tắc nghiệp vụ (2 kiểu chia)
- **Kiểu1 — chia về nhóm** (`Save_Kieu1`): cấu hình cây 2 cấp trong `CauHinhChiaSoDoiTuongCareDon`: cấp `LoaiDoiTuong=NhomUser` (nhóm care đơn) và cấp con `LoaiDoiTuong=User` (`IdParent`/`IdDoiTuongParent` trỏ về nhóm). Dùng cho luồng ChiaSoCareDon phân bổ về nhóm rồi xuống user.
- **Kiểu2 — user liên kết nhóm sale** (`CauHinhChiaSoCareDonType=Option2`): tài khoản care đơn gán trực tiếp tới (nhiều) nhóm sale, có cờ bật/tắt nhận đơn theo từng nhóm hoặc all. Dùng cho mô hình care đơn theo nhóm sale.
- Cấu hình cache theo `IdCHCS` (`CHCSCareDon_GetBy/SetBy_IdCHCSAsync`); Save_Kieu1 refresh cache sau khi lưu (CauHinhChiaSoCareDonService.cs:437).
- `IsApDungCSCareDonChoChiNhanhAsync` là gating: chi nhánh không bật tính năng care đơn → không lưu/không hiện cấu hình.

## Quan hệ
- **Gọi sang:** `ICauHinhChiaSoCareDonService` (CRUD cấu hình + cây đối tượng); `ICommonCacheService` cache cấu hình theo IdCHCS.
- **Dữ liệu (entities):** `CauHinhChiaSoCareDon` (header cấu hình theo DonViId+IdChiNhanh, có Type kiểu chia); `CauHinhChiaSoDoiTuongCareDon` (cây nhóm/user, enum `CHCSCareDonEnum.NhomUser`/`.User`, type `Option2` cho user liên kết); lịch sử thay đổi cấu hình.
- **Ripple / đồng bộ:** cấu hình này là **input** cho `ChiaSoCareDonController` khi phân bổ đơn (nhóm/user care đơn). Không tự đụng đơn hàng/tồn kho.

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuThayDoiCauHinhChiaSo` (CauHinhChiaSoCareDonController.cs:151-156): permission attr bị comment và KHÔNG có `[SBAuthorize]` → endpoint mở (anonymous) — bất kỳ ai gọi được endpoint đều xem lịch sử cấu hình.
- `Save_Kieu1` permission key chỉ là `Save_Kieu1` (không prefix tên controller như các action khác) — đặt tên không nhất quán, dễ trùng/khó query.
- Controller gate `[TienIchAttribute(..., TI_OrderLogistic)]` ở controller-level; các action có `[SBAuthorize(key,...)]` map role kho/CSKH.
- IdCHCS resolve từ (DonViId, IdChiNhanh) của CurrentUser ở `GetDetail` — mỗi chi nhánh 1 cấu hình.
