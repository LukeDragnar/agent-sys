---
controller: ConfigSystemController
module: marketing
service: SMS
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Cấu hình hệ thống SMS (admin/kế toán quản lý các config theo mã/nhóm). Route api/QuanTriArea/ConfigSystem."
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) của service SMS: thêm/sửa/xóa/lấy config theo mã, theo id, theo nhóm (group), sắp xếp dạng cây. Cấu hình kỹ thuật cấp hệ thống, chỉ HT-AD/HT-KT."
alias: ["ConfigSystem SMS", "cấu hình hệ thống sms", "config theo mã", "nhóm cấu hình sms"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "IConfigSystemService (SMS.Services.Generated)"
  - "AutoMapperConfiguration.Mapper — map ConfigSystemDto ↔ ConfigSystem"
  - "MaTienIchSCRM.TI_HeThong — tiện ích gate (Area QuanTri, param4=true)"
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController — Cấu hình hệ thống SMS (Area QuanTri)

> Route `api/QuanTriArea/ConfigSystem`. Controller cấu hình kỹ thuật cấp hệ thống, mọi endpoint chỉ HT-AD/HT-KT.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (tra cứu) | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_Sms_TimTheoMa` (HT-AD, HT-KT) | public | — | Lấy config theo mã. | EF | — |
| 2 | Lấy tất cả / theo id | (load) | GET `.../LayTatCa` · `.../TimTheoId/{id}` | `ConfigSystem_Sms_LayTatCa` · `_TimTheoId` (HT-AD, HT-KT) | public | — | Lấy toàn bộ config / 1 config theo id (map sang Dto). | EF | — |
| 3 | Lấy theo nhóm | (cây nhóm) | GET `.../LayTatCaByGroup` · `.../LayCacNhomCauHinh` · POST `.../LayTatCaByGroupSort` | `ConfigSystem_Sms_LayTatCaTheoGroup` · `_LayCacGroupConfigSystem` · `_LayTatCaTheoGroupVaSort` (HT-AD, HT-KT) | public | — | Lấy config nhóm theo group, dạng cây, có sắp xếp. | EF | — |
| 4 | Thêm config | Thêm mới | POST `api/QuanTriArea/ConfigSystem/ThemMoi` | `ConfigSystem_Sms_ThemMoi` (HT-AD, HT-KT) | public | ModelState hợp lệ | Map Dto → entity, set NgayTao/NguoiTao/Id, insert. | EF | — |
| 5 | Sửa config | Cập nhật | POST `api/QuanTriArea/ConfigSystem/CapNhatThongTin` | `ConfigSystem_Sms_CapNhatThongTin` (HT-AD, HT-KT) | public | Tồn tại theo id | Map Dto → entity, set NguoiCapNhat/NgayCapNhat, update; không tồn tại → 404. | EF | — |
| 6 | Xóa config | Xóa | POST `api/QuanTriArea/ConfigSystem/Xoa/{id}` | `ConfigSystem_Sms_XoaConfigSystem` (HT-AD, HT-KT) | public | Tồn tại theo id (mô tả: ONLY_SUPERUSER) | Xóa 1 config theo id. | EF | — |

## Quan hệ
- **Gọi sang:** `IConfigSystemService`; AutoMapper.
- **Dữ liệu (entities):** `ConfigSystem` (cặp mã-giá trị cấu hình hệ thống, có group/sort).

## Lỗi đã biết / lưu ý kỹ thuật
- Audit fields (NgayTao/NguoiTao/NgayCapNhat/NguoiCapNhat) set thủ công ở controller thay vì service (`ConfigSystemController.cs:68-71,100-102`).
- `TimTheoDieuKien` đã comment (`cs:133-138`).
- Mô tả quyền Xóa ghi "ONLY_SUPERUSER" nhưng key cho cả HT-AD/HT-KT (`cs:77`) — kiểm tra lại nếu thực sự cần chặn về SuperUser.
