---
controller: DmCongViecMauFieldController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình > Danh mục trường công việc mẫu (định nghĩa field động: mã/tên/kiểu dữ liệu/options; field hệ thống + field tùy biến cho công việc)"
muc_dich: "CRUD danh mục field động của công việc (DmCongViecMauField): Ma/DataType/Options/IsSystem — định nghĩa các cột mở rộng (text/number/date/select/user...) cho công việc + mẫu."
alias: [DmCongViecMauField, "trường công việc mẫu", "field động", "cột mở rộng", custom field, "field tùy biến"]
auth: internal
state_machine: []
deprecated: false
entities: [DmCongViecMauField]
stored_procs: []
depends: [IDmCongViecMauFieldService]
cross_module_in: [CongViecController (resolve field động ext_*, group-by, filter), CongViecMauController (map field vào mẫu)]
cross_module_out: []
---

# DmCongViecMauField — Danh mục trường (field) công việc động

> CRUD định nghĩa field động (EAV). `Ma` + `DataType` (string/int/double/bool/date/array/**select**/**user**) + `Options` (JSON: select source custom|user|node, selectMode multi, user multiple...). CongViec dùng để build filter `ext_<Ma>`, group-by, và slot lưu giá trị (valueText/valueLong/...). Field `IsSystem` cố định, field tùy biến gắn mẫu qua CongViecMauField.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm field (lưới) | Lưới Danh mục trường | `POST api/DmCongViecMauField/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn danh mục | Filter + phân trang; thường lọc IsActive. | EF | — |
| 2 | Chi tiết field | Mở form field | `POST api/DmCongViecMauField/TimTheoId/{id}` | `[SBAuthorize]` | internal | form | Trả bản ghi (Ma/DataType/Options). | EF | — |
| 3 | Lưu field | Nút "Lưu" | `POST api/DmCongViecMauField/Luu` | `[SBAuthorize]` | internal | form | Tạo/sửa field động. | EF | — |
| 4 | Xóa field | Nút "Xóa" | `POST api/DmCongViecMauField/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa field. | EF | — |

## Quan hệ
- **Gọi sang:** `IDmCongViecMauFieldService`.
- **Dữ liệu (entities):** `DmCongViecMauField` (định nghĩa) — giá trị thực lưu ở `CongViecMoRong` / ES customFields.
- **Ripple/đồng bộ:** CongViec build query ext theo Ma+DataType+Options (filter/group-by); đổi DataType/Options ảnh hưởng cách lưu & lọc.

## Lỗi đã biết / lưu ý kỹ thuật
- Đổi `DataType` của field đang có dữ liệu → slot lưu (valueText vs valueLong...) lệch với giá trị cũ trong CongViecMoRong/ES → filter/group-by sai cho bản ghi cũ (không thấy migrate giá trị).
- Xóa field không dọn giá trị CongViecMoRong tương ứng → dữ liệu mồ côi.
