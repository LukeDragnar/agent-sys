---
controller: KhoSoTeamCauHinhController
module: sales
service: Contact
man_hinh_ui: "Sale > Cấu hình kho số theo team (route sale/cau-hinh-kho-so-theo-team — CauhinhKhosoTheoteamComponent; edit: cauhinh-khoso-theoteam-edit)"
muc_dich: "CRUD nhóm team kho số: gom nhiều team (nhóm nhân viên) vào 1 cấu hình để giới hạn sale chỉ thấy/nhặt kho số của các team được khai báo."
alias: [KhoSoTeamCauHinh, "cấu hình team kho số", "team kho số", "nhóm kho số"]
auth: public
state_machine: []
deprecated: false
entities: [KhoSoTeamCauHinh]
stored_procs: []
depends:
  - "KhoSoTeamCauHinhService (ICauHinhService — TimTheoDieuKien/ThemMoi/Edit/GetById/Xoa)"
  - "LogActivityHelper (EventLichSuTacDong — ghi lịch sử tác động ADD/UPDATE/DELETE)"
cross_module_in:
  - "KhoSoController.ChonSo gián tiếp dùng cấu hình team (ChiHienThiKhoSoCuaTeam → GetNhomSaleAllowedPickContact) để lọc kho số được nhặt"
cross_module_out:
  - "LichSuTacDong (EnumTableId.SCRM_CONTACT_KhoSoTeamCauHinh) — audit log mỗi thao tác"
---

# KhoSoTeamCauHinh — Cấu hình team kho số

> CRUD đơn giản (70 dòng, 5 endpoint) cho entity `KhoSoTeamCauHinh` (Ten + TeamIds + TeamTens). Mỗi cấu hình = 1 nhóm các team được gom lại; KhoSo dùng cấu hình này để giới hạn phạm vi kho số sale nhìn/nhặt. Mọi thao tác ghi audit qua `LogActivityHelper`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách / Lọc | POST `TimTheoDieuKien` | `KhoSoTeamCauHinh_TimTheoDieuKien` | public | Mở màn cấu hình | `TimTheoDieuKien(gridInfo, CurrentUser)` — list cấu hình theo đơn vị. | EF | — |
| 2 | Thêm mới | Lưu (thêm) | POST `ThemMoi` | `KhoSoTeamCauHinh_ThemMoi` | public | Nút Thêm | Insert `KhoSoTeamCauHinh` (Id mới, IdDonVi/IdChiNhanh theo user, Ten/TeamIds/TeamTens). Ghi LogActivity ADD. Trả `data.Id`. | EF | — |
| 3 | Cập nhật thông tin | Lưu (sửa) | POST `CapNhatThongTin` | `KhoSoTeamCauHinh_CapNhatThongTin` | public | Form edit | `Edit(model)` — Id bắt buộc; tìm theo Id + đơn vị; deep-clone old; update Ten/TeamIds/TeamTens (MarkDirty); LogActivity UPDATE. | EF | — |
| 4 | Chi tiết theo Id | Mở edit | GET `TimTheoId/{id:Guid}` | `KhoSoTeamCauHinh_TimTheoId` | public | Mở 1 dòng | `GetById(id, CurrentUser)`. | EF | — |
| 5 | Xóa | Xóa | POST `Xoa/{id:Guid}` | `KhoSoTeamCauHinh_Xoa` | public | Nút Xóa | Id bắt buộc; tìm theo Id + đơn vị; `DeleteAsync`; LogActivity DELETE. Trả `true`. | EF | Xóa cứng (hard delete), không soft-delete |

## Quan hệ
- **Gọi sang:** `IKhoSoTeamCauHinhService` (+ LogActivityHelper cho audit).
- **Dữ liệu (entities):** `KhoSoTeamCauHinh` (Ten, TeamIds, TeamTens — gom nhiều team).
- **Ripple / đồng bộ:** mỗi ADD/UPDATE/DELETE ghi `LichSuTacDong` (TableId `SCRM_CONTACT_KhoSoTeamCauHinh`). Cấu hình này được `KhoSoController.ChonSo` (qua `GetNhomSaleAllowedPickContact`) đọc gián tiếp để lọc kho số.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhoSoTeamCauHinhController.cs:62-63` — `Xoa` dùng POST (tốt, idempotent-safe hơn các controller khác xài GET) nhưng là **hard delete** (`DeleteAsync`) — không khôi phục được; cân nhắc soft-delete nếu cần audit dữ liệu.
- `KhoSoTeamCauHinhController.cs:39,47` — `ThemMoi`/`CapNhatThongTin` khai báo `virtual` (partial class) → có thể bị override ở file partial khác; cần kiểm tra không có override ngầm đổi behavior.
- `KhoSoTeamCauHinhService.cs:114` — Edit lọc theo `IdDonVi` (không lọc `IdChiNhanh`) → admin chi nhánh A có thể sửa cấu hình do chi nhánh B trong cùng đơn vị tạo (nếu phân quyền chi nhánh là yêu cầu thì đây là khe hở).
- `KhoSoTeamCauHinhService.cs:151-157` — log DELETE không set `OldObject` (thiếu snapshot dữ liệu bị xóa trong audit) → khó truy vết nội dung đã xóa.
- Constructor inject `IServiceProvider inject` nhưng không dùng (`KhoSoTeamCauHinhController.cs:22`) — dead dependency.
