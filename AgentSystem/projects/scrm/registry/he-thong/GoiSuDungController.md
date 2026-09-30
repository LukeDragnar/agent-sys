---
controller: GoiSuDungController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Gói sử dụng — danh sách/CRUD gói + duyệt yêu cầu nâng cấp gói (api/QuanTriArea/GoiSuDung); chủ yếu HT-AD/HT-KT"
muc_dich: "Quản lý gói sử dụng (GoiSuDung) của hệ thống: CRUD chuẩn (kế thừa SBBaseController) + tìm theo điều kiện + luồng duyệt/hủy yêu cầu nâng cấp gói (NangCapGoiSuDung)."
alias: ["gói sử dụng", "GoiSuDung", "package", "nâng cấp gói"]
auth: public
state_machine: [ChoNangCap, DaDuyet, HuyDuyet]
deprecated: false
entities: [GoiSuDung, NangCapGoiSuDung]
stored_procs: []
depends: ["IGoiSuDungService", "INangCapGoiSuDungService"]
cross_module_in: ["FE Quản trị gói sử dụng gọi CRUD + TimTheoDieuKien", "FE duyệt yêu cầu nâng cấp gói gọi DanhSachNangCap/DuyetYeuCauNangCap/HuyDuyetNangCap"]
cross_module_out: ["Duyệt nâng cấp gói → cập nhật gói áp dụng cho đơn vị (qua NangCapGoiSuDungService)"]
---

# GoiSuDungController — Quản lý gói sử dụng

Controller vùng quản trị kế thừa `SBBaseController<GoiSuDung, GoiSuDungDto>` (route `api/QuanTriArea/GoiSuDung/...`), `[SBAuthorize()]` mức controller. Cung cấp CRUD chuẩn (override `XoaAsync` để check tồn tại trước khi xóa) + tìm theo điều kiện + luồng nâng cấp gói. Ủy quyền `IGoiSuDungService` (gói) và `INangCapGoiSuDungService` (yêu cầu nâng cấp).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | CRUD chuẩn (kế thừa) + thêm/sửa gói | Thêm/Sửa/Xem | (kế thừa SBBase) GET TimTheoId/LayTatCa..., POST `ThemMoiGoiSd`, POST `CapNhatGoiSd` | `GoiSuDung_ThemMoiGoiSd` / `GoiSuDung_CapNhatGoiSd` (CRUD base chỉ `[SBAuthorize]`) | public | — | Thêm/sửa gói qua `_service.InsertAsync/UpdateAsync`; CRUD nền từ SBBaseController. | EF `GoiSuDung` | — |
| 2 | Tìm theo điều kiện | Grid/Lọc | POST `api/QuanTriArea/GoiSuDung/TimTheoDieuKien` | `GoiSuDung_TimTheoDieuKien` | public (HT-AD/HT-KT) | — | `TimTheoDieuKienAsync(GoiSuDungFilterDto)`. | EF `GoiSuDung` | — |
| 3 | Xóa gói | Xóa | POST `api/QuanTriArea/GoiSuDung/Xoa/{id}` | `GoiSuDung_Xoa` | public | — | Override `XoaAsync`: check `GetByIdAsync` ≠ null trước, không có thì trả `MESS_NotFound`; có thì `DeleteAsync`. | EF `GoiSuDung` | — |
| 4 | Danh sách yêu cầu nâng cấp | Grid nâng cấp | POST `api/QuanTriArea/GoiSuDung/DanhSachNangCap` | `GoiSuDung_DanhSachNangCap` | public (DV-AD-TONG/HT-KT) | — | `_nangCapService.TimTheoDieuKienAsync(NangCapGoiSuDungFilterDto)`. | EF `NangCapGoiSuDung` | — |
| 5 | Duyệt yêu cầu nâng cấp | Duyệt | POST `api/QuanTriArea/GoiSuDung/DuyetYeuCauNangCap` | `GoiSuDung_DuyetYeuCauNangCap` | public (HT-AD/HT-KT) | chỉ hiện/cho phép khi user là SuperUser HOẶC kế toán hệ thống HOẶC admin hệ thống | Nếu đủ quyền → `DuyetNangCapGoi(request)`; ngược lại trả lỗi "Bạn không có quyền...". | EF `NangCapGoiSuDung` | check quyền nằm trong code (không chỉ ở attribute) — gating runtime. |
| 6 | Hủy duyệt nâng cấp | Hủy duyệt | GET `api/QuanTriArea/GoiSuDung/HuyDuyetNangCap?idNangCap=` | — (`[SBAuthorize()]`) | public | chỉ super/kế-toán-HT/admin-HT mới chạy được | Nếu đủ quyền → `HuyDuyetNangCap(idNangCap)`; ngược lại trả lỗi quyền. | EF `NangCapGoiSuDung` | endpoint không có permission key riêng, chỉ gate bằng code (`GoiSuDungController.cs:98-100`). |
| 7 | Thông tin gói phê duyệt | xem chi tiết duyệt | POST `api/QuanTriArea/GoiSuDung/GoiPheDuyetInfo/{id}` | `GoiSuDung_GoiPheDuyetInfo` | public (DV-AD-TONG/HT-KT) | — | `_nangCapService.GoiPheDuyetInfo(id)`. | EF `NangCapGoiSuDung` | — |

## Quy tắc nghiệp vụ (gating)
- **Duyệt / Hủy duyệt nâng cấp**: điều kiện `CurrentUser.IsSuperUser || IsHeThongKeToan() || IsAdminHeThong()`.
  - true → thực thi duyệt/hủy.
  - false → trả `MethodResult.ResultWithError("Bạn không có quyền sử dụng chức năng này")`.
  - Field quyết định: cờ role của `CurrentUser`.

## Quan hệ
- **Gọi sang:** `IGoiSuDungService` (CRUD/tìm gói), `INangCapGoiSuDungService` (yêu cầu nâng cấp: list/duyệt/hủy/info).
- **Dữ liệu (entities):** `GoiSuDung`, `NangCapGoiSuDung`. DTO: `GoiSuDungDto`, `GoiSuDungFilterDto`, `NangCapGoiSuDungFilterDto`, `RequestDuyetYeuCap`.
- **Ripple / đồng bộ:** duyệt nâng cấp → cập nhật gói áp dụng cho đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `GoiSuDungController.cs:97-99` — `HuyDuyetNangCap` chỉ `[SBAuthorize()]` (permission key bị comment), gate quyền hoàn toàn bằng if trong code.
- Kiểm tra quyền duyệt/hủy lặp 2 lần (DuyetYeuCauNangCap, HuyDuyetNangCap) cùng pattern → nên gom helper.
