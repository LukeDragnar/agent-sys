---
controller: ThuongPhatNhanVienController
module: nhan-su
service: Branch
man_hinh_ui: "Nhân sự > Hồ sơ nhân viên > tab Thưởng phạt (danh sách thưởng/phạt + thêm/xóa); bảng tổng hợp thưởng phạt theo nhân viên"
muc_dich: "Quản lý bản ghi thưởng/phạt áp cho từng nhân viên (thêm/xóa/tra cứu) + tổng hợp tiền thưởng/phạt/khấu trừ theo nhân viên."
alias: [ThuongPhatNhanVien, "thưởng phạt nhân viên", "thưởng phạt NV", "phạt nhân viên", "thưởng nhân viên"]
auth: public
state_machine: []
deprecated: false
entities: [ThuongPhatNhanVien, DMThuongPhat]
stored_procs: []
depends: [DMThuongPhat (loại thưởng/phạt), SBBaseController<ThuongPhatNhanVien,ThuongPhatNhanVienDto>]
cross_module_in: [Bảng lương / tổng hợp công (đọc tổng thưởng phạt NV)]
cross_module_out: []
---

# ThuongPhatNhanVien — Thưởng phạt áp cho nhân viên

> Kế thừa `SBBaseController<ThuongPhatNhanVien, ThuongPhatNhanVienDto>` (controller-level `[SBAuthorize]` → login-only mặc định). CRUD base (`TimTheoId`, `LayTatCa`, `ThemMoi`, `CapNhatThongTin`, `Xoa`) có sẵn nhưng controller này KHÔNG dùng để áp thưởng phạt — dùng action riêng `ThemMoiThuongPhatNv` / `XoaTheoId` có key phân quyền.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `TimTheoDieuKien` | `ThuongPhatNhanVien_TimTheoDieuKien` | public | Lọc thưởng phạt NV theo filter (ép theo `CurrentUser`). | EF | — |
| 2 | Thưởng phạt theo nhân viên | Xem chi tiết | GET `GetThuongPhatByNhanVien?idNhanVien` | `ThuongPhatNhanVien_GetThuongPhatByNhanVien` | public | Danh sách bản ghi thưởng/phạt của 1 NV. | EF | — |
| 3 | Tổng thưởng/phạt/khấu trừ theo NV | (tổng) | GET `GetSumThuongPhatByNhanVien?idNhanVien` | `ThuongPhatNhanVien_GetSumThuongPhatByNhanVien` | public | Tính tổng tiền thưởng / phạt / khấu trừ theo NV (theo `CurrentUser`). | EF | — |
| 4 | Thêm mới thưởng phạt NV | Lưu | POST `ThemMoiThuongPhatNv` | `ThuongPhatNhanVien_ThemMoiThuongPhatNv` | public | Insert 1 bản ghi thưởng/phạt cho NV. | EF | — |
| 5 | Xóa theo Id | Xóa | POST `XoaTheoId/{id:Guid}` | `ThuongPhatNhanVien_XoaTheoId` | public | Xóa bản ghi thưởng/phạt theo id. | EF | — |
| 6 | CRUD base (kế thừa) | — | GET `TimTheoId/{id}`, GET `LayTatCa`, POST `ThemMoi`, POST `CapNhatThongTin`, POST `Xoa/{id}` | (chỉ `[SBAuthorize]` base, login-only) | mixed | Endpoint base kế thừa từ `SBBaseController`; không dùng cho luồng nghiệp vụ chính. | EF | Base endpoint chỉ login, không key — tránh dùng để ghi |

## Quan hệ
- **Gọi sang:** không (EF nội bộ Branch).
- **Dữ liệu (entities):** `ThuongPhatNhanVien` (gắn `IdNhanVien`, loại = `DMThuongPhat`, số tiền, là thưởng/phạt/khấu trừ).
- **Ripple/đồng bộ:** tổng thưởng/phạt là đầu vào cho bảng lương / tổng hợp công của nhân viên.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuongPhatNhanVienController.cs:14` — kế thừa `SBBaseController` nên có sẵn 5 endpoint base login-only không key; nghiệp vụ thật dùng `ThemMoiThuongPhatNv`/`XoaTheoId` (có key). Khi audit quyền cần soi cả endpoint base.
- `ThuongPhatNhanVienController.cs:64` — `XoaTheoId` dùng HTTP POST (không phải DELETE) theo convention SCRM.
