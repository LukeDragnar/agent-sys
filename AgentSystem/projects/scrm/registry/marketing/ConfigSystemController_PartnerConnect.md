---
controller: ConfigSystemController
module: marketing
service: PartnerConnect
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Cấu hình hệ thống PartnerConnect (admin HT quản lý config kỹ thuật theo mã/nhóm). Route api/QuanTriArea/ConfigSystem."
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) của service PartnerConnect: thêm/sửa/xóa/lấy config theo mã, theo id, theo nhóm (group), dạng cây. Cấu hình kỹ thuật cấp hệ thống."
alias: ["ConfigSystem PartnerConnect", "cấu hình hệ thống partner connect", "config theo mã partner", "nhóm cấu hình partner"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - IConfigSystemService (PartnerConnect.Service.Generated)
  - "AutoMapperConfiguration.Mapper — map ConfigSystemDto ↔ ConfigSystem"
  - "MaTienIchSCRM.TI_HeThong — gate Area QuanTri (param4=true)"
cross_module_in: []
cross_module_out: []
---

# ConfigSystem — Cấu hình hệ thống PartnerConnect (Area QuanTri)

> CRUD bảng `ConfigSystem` của service PartnerConnect: lấy theo mã/id, lấy tất cả, lấy theo group (phẳng + cây sort), thêm/sửa/xóa, lấy danh sách nhóm. `partial class`, route `api/QuanTriArea/ConfigSystem`, gate `TienIch(..., TI_HeThong, true)`. Thêm/sửa map qua AutoMapper + set audit (NgayTao/NguoiTao...). Cùng khuôn ConfigSystem các service khác (SMS, Core...). Tất cả endpoint chỉ `[SBAuthorize]` trống (key role HT-AD/HT-KT đã comment).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config (theo mã / id / tất cả) | Mở màn cấu hình | `GET .../TimTheoMa?ma` · `GET .../TimTheoId/{id}` · `GET .../LayTatCa` | `[SBAuthorize]` (key đã comment) | public | màn cấu hình HT | Trả config theo mã/id; TimTheoId map sang DTO. | EF | Key `ConfigSystem_Sms_*` đã comment (L27,120). |
| 2 | Lấy theo nhóm (phẳng / cây sort) | Grid nhóm cấu hình | `GET .../LayTatCaByGroup` · `POST .../LayTatCaByGroupSort` · `GET .../LayCacNhomCauHinh` | `[SBAuthorize]` | public | màn cấu hình HT | Trả config gom theo group; LayTatCaByGroupSort theo `FilterCustomDto`. | EF | — |
| 3 | Thêm mới config | Nút "Thêm" | `POST .../ThemMoi` | `[SBAuthorize]` | public | màn cấu hình HT | Validate ModelState; map DTO→entity, set Id mới + audit, Insert. | EF | — |
| 4 | Cập nhật config | Nút "Lưu" | `POST .../CapNhatThongTin` | `[SBAuthorize]` | public | dòng config | Validate ModelState; check tồn tại (404 nếu null), set audit cập nhật, Update. | EF | — |
| 5 | Xóa config | Nút "Xóa" (ONLY_SUPERUSER theo mô tả gốc) | `POST .../Xoa/{id}` | `[SBAuthorize]` | public | dòng config | Check tồn tại (MESS_NotFound), Delete theo id. | EF | Mô tả gốc ghi "ONLY_SUPERUSER" nhưng key bị comment → không còn chặn role. |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` + AutoMapper.
- **Dữ liệu (entities):** `ConfigSystem` (config kỹ thuật cấp service).
- **Ripple/đồng bộ:** config lái hành vi runtime của PartnerConnect (kết nối/đồng bộ đối tác).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:27,86,120` — toàn bộ key quyền `ConfigSystem_Sms_*` (HT-AD,HT-KT) bị comment, thay `[SBAuthorize]` trống → mất giới hạn role cho cả Xóa (mô tả gốc là ONLY_SUPERUSER).
- TienIch attribute label trùng pattern các service khác ("ConfigSystemController_Partner") — phân biệt service qua DI `IConfigSystemService` của PartnerConnect.
