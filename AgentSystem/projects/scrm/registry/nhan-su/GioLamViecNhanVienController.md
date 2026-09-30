---
controller: GioLamViecNhanVienController
module: nhan-su
service: Branch
man_hinh_ui: "Chấm công > Giờ làm việc / Chấm công (xem theo danh sách & lịch, duyệt/từ chối giờ làm, NV tự check-in theo location, báo cáo chấm công + xuất Excel)"
muc_dich: "Quản lý chấm công giờ làm việc của nhân viên: NV tự check-in theo vị trí, quản lý chấm công hộ, duyệt/từ chối giờ làm, thống kê ngày nghỉ và báo cáo chấm công (chi tiết/theo ca/theo ngày) + xuất Excel."
alias: [GioLamViecNhanVien, "chấm công", "giờ làm việc", "check-in", "duyệt công", "báo cáo chấm công"]
auth: mixed
state_machine: [ChoDuyet, DaDuyet, TuChoi]
deprecated: false
entities: [GioLamViecNhanVien]
stored_procs: []
depends: [CaLamViecNhanVien, KhungGioNhanVien, NhanVien/User (Core), BaoCaoChamCongService]
cross_module_in: []
cross_module_out: []
---

# GioLamViecNhanVien — Chấm công giờ làm việc nhân viên

> Controller-level `[SBAuthorize]` (login-only). Action duyệt/từ chối/check-in/quản lý để key SBAuthorize **commented** → fallback login-only; action xem/báo cáo có key thật. Dùng 2 service: `IGioLamViecNhanVienService` (chấm công) + `IBaoCaoChamCongService` (báo cáo/Excel). Có state duyệt: Chờ duyệt → Duyệt/Từ chối.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm giờ làm (danh sách) | Danh sách | POST `TimTheoDieuKienTypeList` | `GioLamViecNhanVien_TimTheoDieuKienTypeList` | public | — | Validate → trả bản ghi chấm công dạng danh sách theo bộ lọc. | EF | — |
| 2 | Tìm giờ làm (lịch) | Lịch | POST `TimTheoDieuKienTypeCalendar` | `GioLamViecNhanVien_TimTheoDieuKienTypeCalendar` | public | — | Trả chấm công dạng calendar. | EF | — |
| 3 | Lấy bản ghi theo NV+khung giờ+ngày | (mở chi tiết ô lịch) | POST `GetByThongTinGioLamViecNhanVien` | `GioLamViecNhanVien_GetByThongTinGioLamViecNhanVien` | public | — | Trả bản ghi giờ làm theo IdNV + IdKhungGio + ngày. | EF | — |
| 4 | Chi tiết theo Id | (mở chi tiết) | GET `TimTheoId/{id}` | `GioLamViecNhanVien_TimTheoId` | public | — | Trả chi tiết 1 bản ghi giờ làm. | EF | — |
| 5 | Xóa bản ghi giờ làm | Xóa | POST `Xoa/{id}` | (key commented → login-only) | public | — | Xóa 1 bản ghi chấm công. | EF | Key commented → login-only |
| 6 | Duyệt giờ làm | Duyệt | GET `DuyetGioLam/{id}` | (key commented → login-only) | public | hiện khi state=Chờ duyệt | Chuyển bản ghi sang Đã duyệt. | EF | Key commented → login-only |
| 7 | Từ chối giờ làm | Từ chối | POST `TuChoiGioLam` | (key commented → login-only) | public | hiện khi state=Chờ duyệt | Chuyển sang Từ chối (kèm lý do). | EF | Key commented → login-only |
| 8 | NV tự check-in theo vị trí | Check-in | POST `ThemMoi_Location` (multipart) | (key commented → login-only) | public | — | Parse JSON `model` + ảnh `file` → chấm công theo location; thiếu/sai model → trả lỗi check-in. | EF + file | Key commented → login-only; multipart, validate model thủ công |
| 9 | Quản lý chấm công hộ NV | Chấm công hộ | POST `ThemMoi_QuanLy` | (key commented → login-only) | public | — | Validate → tạo/sửa bản ghi chấm công cho NV (quản lý nhập hộ). | EF | Key commented → login-only |
| 10 | NV xem lịch hiện tại | Lịch của tôi | POST `ShowCaLamViecNhanVien` | (key commented → login-only) | public | — | Trả lịch ca làm việc hiện tại của NV. | EF | Key commented → login-only |
| 11 | Thống kê ngày nghỉ NV | Thống kê nghỉ | POST `ThongKeNgayNghiNhanVien` | `GioLamViecNhanVien_ThongKeNgayNghiNhanVien` | public | — | Thống kê ngày nghỉ theo Id NV. | EF | — |
| 12 | Báo cáo chấm công chi tiết | Báo cáo chi tiết | POST `BaoCaoChamCongChiTiet` | `GioLamViecNhanVien_BaoCaoChamCongChiTiet` | public | — | Báo cáo chấm công chi tiết theo bộ lọc. | BaoCaoChamCongService | — |
| 13 | Báo cáo chấm công của tôi | Của tôi | POST `BaoCaoChamCongChiTietUser` | (login-only) | public | — | Ép `ListIdChiNhanh=[CurrentUser.IdChiNhanh]` + `IdNhanVien=CurrentUser.Id` → báo cáo của chính NV. | BaoCaoChamCongService | — |
| 14 | Báo cáo chấm công theo ca | Theo ca | POST `BaoCaoChamCongTheoCa` | `GioLamViecNhanVien_BaoCaoChamCongTheoCa` | public | — | Báo cáo chấm công gom theo ca. | BaoCaoChamCongService | — |
| 15 | Báo cáo chấm công theo ngày | Theo ngày | POST `BaoCaoChamCongTheoNgay` | `GioLamViecNhanVien_BaoCaoChamCongTheoNgay` | public | — | Báo cáo chấm công gom theo ngày. | BaoCaoChamCongService | — |
| 16 | Xuất Excel báo cáo chấm công | Xuất Excel | POST `ExportExcelBaoCaoChamCong`, `ExportExcelBaoCaoChamCongV2` | `GioLamViecNhanVien_ExportExcelBaoCaoChamCong(V2)` | public | — | Xuất file `BaoCaoChamCong_{TuNgay}_{DenNgay}.xlsx` (2 kiểu layout) qua GemBox; rs lỗi/null → BadRequest. | BaoCaoChamCongService + GemBox | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- Check-in `ThemMoi_Location`: `model` rỗng → "Thiếu dữ liệu check-in"; JSON parse lỗi → "Dữ liệu check-in không hợp lệ: {ex}"; deserialize null → "Dữ liệu check-in không hợp lệ". Hợp lệ → `ThemMoi_Location(gridInfo, file)` (file ảnh optional).
- Export Excel: `!rs.Success || rs.Data == null` → `BadRequest(rs.Message)`; ngược lại stream xlsx với contentType `SaveOptions.XlsxDefault`.

## Quan hệ
- **Gọi sang:** Core/User (NV), CaLamViecNhanVien/KhungGio (đối chiếu ca), `BaoCaoChamCongService` (báo cáo + Excel).
- **Dữ liệu (entities):** `GioLamViecNhanVien` (bản ghi chấm công, có trạng thái duyệt).
- **Ripple/đồng bộ:** dữ liệu công đã duyệt là đầu vào tính lương (LuongNhanVien).

## Lỗi đã biết / lưu ý kỹ thuật
- `GioLamViecNhanVienController.cs:74,83,92,117,127,154` — loạt action duyệt/từ chối/check-in/chấm-công-hộ/show có key SBAuthorize bị comment → chỉ login-only, mất phân quyền role (rủi ro: NV thường có thể tự duyệt/chấm công hộ nếu chỉ chặn ở UI).
- `GioLamViecNhanVienController.cs:131` — `ThemMoi_Location` nhận `multipart/form-data`, validate model bằng JsonConvert thủ công thay vì ModelState.
