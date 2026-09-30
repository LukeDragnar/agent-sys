---
controller: CaiDatLuongCoBanController
module: nhan-su
service: Branch
man_hinh_ui: "Lương > Cài đặt lương cơ bản (danh sách nhân viên chi nhánh + mức lương cơ bản theo cài đặt; nút Lưu)"
muc_dich: "Cấu hình lương cơ bản theo nhân viên của chi nhánh: lấy danh sách NV kèm mức lương theo cài đặt hiện tại và lưu lại cài đặt lương cơ bản."
alias: [CaiDatLuongCoBan, "cài đặt lương", "lương cơ bản", "thiết lập lương cơ bản"]
auth: public
state_machine: []
deprecated: false
entities: [CaiDatLuongCoBan]
stored_procs: []
depends: [NhanVien/User (Core)]
cross_module_in: []
cross_module_out: []
---

# CaiDatLuongCoBan — Cài đặt lương cơ bản nhân viên

> Không có `[SBAuthorize]` controller-level; cả 2 action có key SBAuthorize đầy đủ. Controller gọn: 1 đọc + 1 ghi, là đầu vào cấu hình cho tính lương (LuongNhanVien).

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lấy NV + lương theo cài đặt | (tải danh sách) | GET `GetAllNhanVienCaiDat` | `CaiDatLuongCoBan_GetAllNhanVienCaiDat` | public | Trả tất cả NV của chi nhánh kèm mức lương cơ bản theo cài đặt hiện tại. | EF | — |
| 2 | Lưu cài đặt lương cơ bản | Lưu | POST `SaveCaiDatCoBan` | `CaiDatLuongCoBan_SaveCaiDatCoBan` | public | Lưu/cập nhật cài đặt lương cơ bản theo model. | EF | — |

## Quan hệ
- **Gọi sang:** Core/User (danh sách NV chi nhánh).
- **Dữ liệu (entities):** `CaiDatLuongCoBan` (mức lương cơ bản theo NV/chi nhánh).
- **Ripple/đồng bộ:** không phát event; cài đặt này là đầu vào cho `LuongNhanVien.TaoLuongChiTietNhanVienTuDong`.

## Lỗi đã biết / lưu ý kỹ thuật
- (không phát hiện) — controller mỏng, không validate ModelState; dựa hoàn toàn vào service.
