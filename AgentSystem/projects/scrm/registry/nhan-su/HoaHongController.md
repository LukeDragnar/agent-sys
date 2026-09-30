---
controller: HoaHongController
module: nhan-su
service: Branch
man_hinh_ui: "nội bộ — không phân vào Tiện ích (theo NOTE đầu file: API chỉ phục vụ tạo cấu hình hoa hồng cho nhóm nhân viên)"
muc_dich: "CRUD cấu hình/template hoa hồng (entity HoaHong) cho nhóm nhân viên + endpoint internal lấy cấu hình hoa hồng theo nhóm NV (service-to-service)."
alias: [HoaHong, "cấu hình hoa hồng", "template hoa hồng", "hoa hồng nhóm nhân viên"]
auth: mixed
state_machine: []
deprecated: false
entities: [HoaHong]
stored_procs: []
depends: [SBBaseController<HoaHong,HoaHongDto>]
cross_module_in: [Order/Hoa hồng pipeline (gọi LayCauHinhHoaHongByNhomNV để tính hoa hồng theo nhóm NV)]
cross_module_out: []
---

# HoaHong — Cấu hình/template hoa hồng theo nhóm nhân viên

> Kế thừa `SBBaseController<HoaHong, HoaHongDto>` (base `[SBAuthorize]` login-only) → có CRUD base. Controller KHÔNG gắn `[TienIch]` (chú thích trong code: chỉ phục vụ tạo cấu hình cho nhóm NV). 2 action ghi dùng `[SBAuthorize]` rỗng (login-only, không key); 1 endpoint `[PrivateSBAuthorize]` cho service-to-service.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới cấu hình hoa hồng | Lưu | POST `ThemMoi-HoaHong` | `[SBAuthorize]` rỗng | mixed | Tạo cấu hình hoa hồng (`ThemMoiAsync(req, CurrentUser)`). | EF | Login-only, không key phân quyền |
| 2 | Cập nhật cấu hình hoa hồng | Lưu | POST `CapNhap-HoaHong` | `[SBAuthorize]` rỗng | mixed | Update cấu hình hoa hồng. | EF | Login-only; tên route sai chính tả "CapNhap" |
| 3 | Lấy cấu hình HH theo nhóm NV | — (internal) | POST `LayCauHinhHoaHongByNhomNV` | `[PrivateSBAuthorize]` | internal | Service-to-service: trả cấu hình hoa hồng theo nhóm NV (`HoaHongByNhomNVReq`). | EF | — |
| 4 | CRUD base (kế thừa) | — | GET `TimTheoId/{id}`, `LayTatCa`, POST `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` | (base `[SBAuthorize]`, login-only) | mixed | Kế thừa SBBaseController. | EF | Base endpoint không key |

## Endpoint nội bộ (private / service-to-service)
- [internal] Pipeline tính hoa hồng (Order / HoaHongNhanVien logic) gọi `LayCauHinhHoaHongByNhomNV` để nạp cấu hình hoa hồng theo nhóm nhân viên — `[PrivateSBAuthorize]`.

## Quan hệ
- **Gọi sang:** không (EF nội bộ).
- **Dữ liệu (entities):** `HoaHong` (cấu hình/template hoa hồng gắn nhóm NV).
- **Ripple/đồng bộ:** cấu hình là đầu vào cho việc tính hoa hồng nhân viên theo nhóm.

## Lỗi đã biết / lưu ý kỹ thuật
- `HoaHongController.cs:29,36` — `ThemMoi-HoaHong` và `CapNhap-HoaHong` dùng `[SBAuthorize]` rỗng (chỉ login, không key) → ai login cũng tạo/sửa được cấu hình hoa hồng.
- `HoaHongController.cs:37` — route `CapNhap-HoaHong` sai chính tả (Cập **nhập** thay vì Cập **nhật**); giữ nguyên để khớp FE.
- `HoaHongController.cs:14-17` — chủ đích KHÔNG phân Tiện ích (không có UI menu); là cấu hình nền cho HoaHongNhanVien.
