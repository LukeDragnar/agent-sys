---
controller: DMTacNghiepCauHinhController
module: sales
service: Contact
man_hinh_ui: "Sale > Thiết lập tác nghiệp (route sale/thiet-lap-tac-nghiep — DanhmucTacnghiepCauhinhComponent)"
muc_dich: "Cấu hình luật follow-up: ánh xạ 'NẾU tác nghiệp cần X có kết quả Y THÌ thực hiện tác nghiệp tiếp Z sau bao lâu T'. Là bộ luật điều khiển quy trình tác nghiệp sale theo đơn vị/chi nhánh; phục vụ cả notification config."
alias: [DMTacNghiepCauHinh, "thiết lập tác nghiệp", "cấu hình tác nghiệp", "luật follow-up", "if-then tác nghiệp"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMTacNghiepCauHinh, DMTacNghiep, DMKetQuaTacNghiep, DMTacNghiepSauBaoLau]
stored_procs: []
depends:
  - "DMTacNghiepCauHinhService (Insert/Update/CheckExist/TimTheoDieuKien/DonViTacNghiepCopy; bản HeThong*)"
  - "GetCauHinhInit (IsEnableTacNghiepTheoChiNhanh — cấu hình theo chi nhánh hay đơn vị)"
  - "DMTacNghiep / DMKetQuaTacNghiep / DMTacNghiepSauBaoLau (tham chiếu Id các vế NẾU/KẾT QUẢ/TIẾP/SAU BAO LÂU)"
cross_module_in:
  - "Notification config gọi GetAllThietLapTacNghiepForNotificationConfig(ByChiNhanh) (PrivateSBAuthorize) để dựng cấu hình nhắc việc"
  - "GetAllByDonViPrivate (PrivateSBAuthorize) — service khác lấy toàn bộ luật theo đơn vị"
cross_module_out:
  - "Cache DMTacNghiepCauHinh theo đơn vị/chi nhánh (clear sau Insert/Update/Delete/Copy)"
---

# DMTacNghiepCauHinh — Cấu hình luật follow-up tác nghiệp

> Controller mang **luật nghiệp vụ** (121 dòng): mỗi bản ghi = 1 quy tắc "NẾU `IdTacNghiepCan` cho ra `IdKetQua` THÌ làm `IdTacNghiepTiepTheo` sau `IdSauBaoLau`". Bộ luật này điều khiển quy trình chăm sóc sale và còn được module notification đọc để dựng nhắc việc. Có cờ `IsEnableTacNghiepTheoChiNhanh` quyết định luật theo chi nhánh hay dùng chung đơn vị.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm luật | Lưu (thêm) | POST `InsertAsync` | `DMTacNghiepCauHinh_InsertAsync` (AD-TONG/CN/MCN) | public | Nút Thêm | `InsertAsync(model, CurrentUser)` — validate (xem Quy tắc) → CheckExist → insert → clear cache. | EF | Validate dùng kết quả bị bỏ (xem lỗi) |
| 2 | Cập nhật luật | Lưu (sửa) | POST `UpdateOneAsync` | `DMTacNghiepCauHinh_UpdateOneAsync` (AD-TONG/CN/MCN) | public | Form edit | `UpdateOneAsync(model, CurrentUser)` — Id bắt buộc; CheckExist; MarkDirty IdSauBaoLau/TenSauBaoLau...; clear cache. | EF | — |
| 3 | Tìm theo điều kiện (grid) | Danh sách / Lọc | POST `TimTheoDieuKienAsync` | `DMTacNghiepCauHinh_TimTheoDieuKienAsync` (AD-TONG/CN/MCN) | public | Mở màn thiết lập | `TimTheoDieuKienAsync(model, CurrentUser)` — join sang SauBaoLau, gắn "(Deleted)" nếu vế tham chiếu đã xóa. | EF | — |
| 4 | Xóa luật | Xóa | POST `DeleteOneItemAsync/{id:guid}` | `DMTacNghiepCauHinh_DeleteOneItemAsync` (AD-TONG/CN/MCN) | public | Nút Xóa | `DeleteOneItemAsync(id, CurrentUser)`. | EF | — |
| 5 | Kiểm tra tồn tại | (validate ngầm) | POST `CheckExistAsync` | `DMTacNghiepCauHinh_CheckExistAsync` (AD-TONG/CN/MCN) | public | Trước khi lưu | `CheckExistAsync(model)` — true nếu đã có luật trùng (cặp NẾU+KẾT QUẢ). | EF | — |
| 6 | Copy cấu hình cho đơn vị | Copy theo ngành nghề | POST `DonViTacNghiepCopy` | `DMTacNghiepCauHinh_DonViTacNghiepCopy` (AD-TONG/CN/MCN) | public | Nút Copy | Controller **ép `IdsDonVi = [CurrentUser.DonViId]`** (bỏ payload) → chỉ copy về đơn vị của user hiện tại; `DonViTacNghiepCopy(model, CurrentUser)`. | EF | Payload IdsDonVi bị override (xem lỗi) |
| 7 | Kiểm tra đã cấu hình chưa | — | GET `CheckDaCauHinhTacNghiep` | `DMTacNghiepCauHinh_CheckDaCauHinhTacNghiep` (AD-TONG/CN/MCN) | public | — | Trả đơn vị/chi nhánh đã thiết lập tác nghiệp hay chưa. | EF | — |
| 8 | Tìm theo ngành nghề (hệ thống) | — | POST `TimTheoDieuKienByNganhNghe` | `DMTacNghiepCauHinh_TimTheoDieuKienByNganhNghe` (AD-TONG/CN/MCN) | public | — | `HeThongTimTheoDieuKienAsync(model, CurrentUser)`. | EF | — |
| 9 | Lấy thông tin thiết lập | — | GET `LayThongTinThietLapTacNghiep` | `[SBAuthorize()]` (chỉ login) | public | — | `LayThongTinThietLapTacNghiep(CurrentUser)` — trả gói thông tin thiết lập. **Authorize rỗng.** | EF | Quyền lỏng hơn các endpoint khác |

## Endpoint nội bộ (private / service-to-service)
- [internal] Notification config gọi `GetAllThietLapTacNghiepForNotificationConfig` và `...ByChiNhanh?idChiNhanh=` (`[PrivateSBAuthorize]`) để dựng cấu hình nhắc việc theo luật follow-up.
- [internal] `GetAllByDonViPrivate` (`[PrivateSBAuthorize]`) — service khác lấy toàn bộ luật cấu hình của đơn vị.

## Quy tắc nghiệp vụ (gating / validate)
1. **Bắt buộc chọn vế NẾU + KẾT QUẢ** — `IdTacNghiepCan` rỗng → lỗi "Id tác nghiệp cần chưa chọn"; `IdKetQua` rỗng → lỗi "Id kết quả tác nghiệp cần chưa chọn". (⚠ thực tế các lỗi này **bị bỏ qua** — xem lỗi đã biết).
2. **Cặp tác nghiệp tiếp + sau bao lâu phải đi cùng nhau** — nếu chỉ chọn 1 trong 2 (`IdTacNghiepTiepTheo` có nhưng `IdSauBaoLau` null, hoặc ngược lại) → lỗi "phải chọn tác nghiệp tiếp và sau bao lâu". (⚠ cũng bị bỏ qua — không `return`).
3. **Gắn chi nhánh theo cấu hình** — nếu `GetCauHinhInit().IsEnableTacNghiepTheoChiNhanh == true` → `entity.IdChiNhanh = userLogin.IdChiNhanh` (luật theo chi nhánh); ngược lại để null (dùng chung đơn vị). `DMTacNghiepCauHinhService.cs:183-187`.
4. **Chống trùng** — trước insert/update gọi `CheckExistAsync`; nếu đã tồn tại luật trùng → "Cấu hình tác nghiệp đã tồn tại, không thể lưu cấu hình." `DMTacNghiepCauHinhService.cs:189-193,327-331`.
5. **Copy chỉ về đơn vị hiện tại** — endpoint #6 ép `IdsDonVi = [CurrentUser.DonViId]` bất kể payload gửi gì → không thể copy chéo sang đơn vị khác qua API public này.

## Quan hệ
- **Gọi sang:** `IDMTacNghiepCauHinhService`, `GetCauHinhInit`; join sang DMTacNghiepSauBaoLau khi list.
- **Dữ liệu (entities):** `DMTacNghiepCauHinh` (IdTacNghiepCan, IdKetQua, IdTacNghiepTiepTheo, IdSauBaoLau + Ten* cache), tham chiếu `DMTacNghiep`, `DMKetQuaTacNghiep`, `DMTacNghiepSauBaoLau`.
- **Ripple / đồng bộ:** mỗi Insert/Update/Delete/Copy clear cache `DMTacNghiepCauHinh` theo đơn vị+chi nhánh; module notification đọc bộ luật này.

## Lỗi đã biết / lưu ý kỹ thuật
- Có **bản sinh đôi Base** `Areas/SanboxArea/DMTacNghiepCauHinhController.cs` route `api/QuanTriArea/DMTacNghiepCauHinh` (TienIch `TI_HeThong`, isAdmin), role `HT-AD, HT-CSKH`, gọi `HeThong*`. Khác biệt: bản Base `DonViTacNghiepCopy` chặn `if (!CurrentUser.IsSuperUser) return BadRequest` (chỉ super-user copy được) thay vì ép IdsDonVi như bản đơn vị.
- `DMTacNghiepCauHinhService.cs:141-148` — các validate vế NẾU/KẾT QUẢ và cặp tiếp/sau-bao-lâu gọi `MethodResult<bool>.ResultWithError(...)` **nhưng KHÔNG `return`** → kết quả validate bị bỏ, luồng vẫn chạy tiếp xuống insert. Luật #1, #2 ở trên thực chất **không có hiệu lực chặn** (chỉ chặn được bởi CheckExist + DB constraint nếu có).
- `DMTacNghiepCauHinhController.cs:113` — `LayThongTinThietLapTacNghiep` dùng `[SBAuthorize()]` rỗng (chỉ cần login) trong khi các endpoint còn lại yêu cầu role AD.
- `DMTacNghiepController`/`CauHinh` được khai báo TienIch khác nhau: DMTacNghiep = `TI_Marketing`, DMTacNghiepCauHinh = `TI_OrderLogistic` (cùng họ tác nghiệp nhưng gắn 2 tiện ích khác nhau → kiểm tra phân quyền tiện ích nếu đơn vị chỉ mua 1 trong 2).
