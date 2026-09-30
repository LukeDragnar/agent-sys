---
controller: ConfigDoiDiemController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/config-doidiem (Cấu hình đổi điểm — tỷ lệ quy đổi điểm thưởng + danh sách đổi điểm chi tiết)"
muc_dich: "Quản lý cấu hình đổi điểm thưởng của đơn vị: cấu hình tổng + CRUD bảng đổi điểm chi tiết"
alias: ["Cấu hình đổi điểm", "Đổi điểm", "Điểm thưởng", "ConfigDoiDiem"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigDoiDiem, ConfigDoiDiemChiTiet]
stored_procs: []
depends: [IConfigDoiDiemService, ILogActivityHelper, BaseController]
cross_module_in: []
cross_module_out: [Log]
---

# ConfigDoiDiemController — Quản lý cấu hình đổi điểm

> Controller cấu hình đổi điểm thưởng. Gắn `[SBAuthorize]` cấp class (yêu cầu đăng nhập) + action siết role DV-AD-TONG/DV-QLDV → auth = **public**. Có ghi `LichSuTacDong` (log thay đổi) khi cập nhật cấu hình tổng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình đổi điểm | Form cấu hình (load) | `GET api/ConfigDoiDiem/GetCauHinhDoiDiem` | ConfigDoiDiem_GetCauHinhDoiDiem | public | — | `GetCauHinhDoiDiem()` | EF | — |
| 2 | Cập nhật cấu hình đổi điểm | Nút "Lưu cấu hình" | `POST api/ConfigDoiDiem/CapNhatThongTin` | ConfigDoiDiem_CapNhatThongTin | public | — | ModelState invalid → lỗi; `GetByIdAsync(model.Id)` null/!Success → `MESS_NotFound`; DeepClone bản cũ → `UpdateAsync` → ghi `EventLichSuTacDong` (UPDATE) | EF | Bắt buộc `model.Id` có giá trị — dùng `model.Id.Value`, nếu null sẽ ném NullReferenceException trước khi vào nhánh NotFound |
| 3 | Khởi tạo cấu hình | Nút "Khởi tạo" | `GET api/ConfigDoiDiem/KhoiTao` | ConfigDoiDiem_KhoiTao | public | — | `KhoiTao(CurrentUser.DonViId)` tạo cấu hình mặc định cho đơn vị | EF | — |
| 4 | Thêm mới đổi điểm chi tiết | Nút "Thêm dòng" | `POST api/ConfigDoiDiem/ThemMoiDoiDiemChiTiet` | ConfigDoiDiem_ThemMoiDoiDiemChiTiet | public | — | `ThemMoiDoiDiemChiTiet(model, CurrentUser)` | EF | Không check ModelState |
| 5 | Cập nhật đổi điểm chi tiết | Nút "Sửa dòng" | `POST api/ConfigDoiDiem/CapNhatDoiDiemChiTiet` | ConfigDoiDiem_CapNhatDoiDiemChiTiet | public | — | `CapNhatDoiDiemChiTiet(model, CurrentUser)` | EF | — |
| 6 | Xóa đổi điểm chi tiết | Nút "Xóa dòng" | `POST api/ConfigDoiDiem/XoaDoiDiemChiTiet/{id}` | ConfigDoiDiem_XoaDoiDiemChiTiet | public | — | `XoaDoiDiemChiTiet(id, CurrentUser)` | EF | — |
| 7 | Tìm đổi điểm chi tiết | Lưới đổi điểm chi tiết | `POST api/ConfigDoiDiem/TimTheoDieuKienDoiDiemChiTiet` | ConfigDoiDiem_TimTheoDieuKienDoiDiemChiTiet | public | — | `TimTheoDieuKienDoiDiemChiTiet(payload, CurrentUser)` | EF | — |

## Quy tắc nghiệp vụ (gating / tier)
- Cập nhật cấu hình tổng (#2): yêu cầu bản ghi tồn tại trước (`GetByIdAsync`) → nếu không có trả `MESS_NotFound`, không tạo mới.
- Cấu hình đổi điểm chi tiet (`ConfigDoiDiemChiTiet`) định nghĩa tỷ lệ/ngưỡng quy đổi điểm → tiêu thụ ở luồng tích/đổi điểm khi giao dịch.

## Quan hệ
- **Gọi sang:** `IConfigDoiDiemService` · `ILogActivityHelper` (ghi lịch sử tác động).
- **Dữ liệu (entities):** `ConfigDoiDiem` (cấu hình tổng) · `ConfigDoiDiemChiTiet` (bảng tỷ lệ đổi điểm).
- **Ripple / đồng bộ:** Cập nhật cấu hình tổng → ghi `EventLichSuTacDong` (TableId `SCRM_CUSTOMER_ConfigDoiDiem`, Activity UPDATE) sang module Log. Cấu hình ảnh hưởng logic tích/đổi điểm KH.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigDoiDiemController.cs:47` (`CapNhatThongTin`) — dùng `model.Id.Value` trực tiếp; nếu client gửi `Id=null` → NullReferenceException trước khi tới nhánh kiểm tra NotFound.
- `ConfigDoiDiemController.cs:79-101` (`ThemMoi/CapNhat/XoaDoiDiemChiTiet`) — không kiểm tra `ModelState.IsValid` (chỉ #2 có), khác convention các action update khác.
