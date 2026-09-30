---
controller: CongViecMauController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình > Mẫu công việc (định nghĩa mẫu + danh sách field động gắn mẫu; dùng khi tạo công việc / import theo mẫu)"
muc_dich: "CRUD mẫu công việc (CongViecMau) + map field theo mẫu (CongViecMauField); cung cấp bản đồ congViecMauId → field hợp lệ để chặn sửa field không thuộc mẫu."
alias: [CongViecMau, "mẫu công việc", task template, "template công việc"]
auth: internal
state_machine: []
deprecated: false
entities: [CongViecMau, CongViecMauField]
stored_procs: []
depends: [ICongViecMauService]
cross_module_in: [CongViecController (Save/Import theo mẫu, validate field thuộc mẫu)]
cross_module_out: []
---

# CongViecMau — Mẫu công việc (template + field động)

> CRUD mẫu + field. `LayBanDoFieldTheoMau` trả `Dictionary<congViecMauId, List<IdDmCongViecMauField>>` — CongViec dùng để **chặn sửa field không thuộc mẫu**.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm mẫu (lưới) | Lưới Mẫu công việc | `POST api/CongViecMau/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn danh sách | Filter keyword(Ten), IsActive, phân trang. | EF | — |
| 2 | Chi tiết mẫu | Mở form mẫu | `POST api/CongViecMau/TimTheoId/{id}` | `[SBAuthorize]` | internal | form | Trả mẫu + danh sách field. | EF | — |
| 3 | Bản đồ field theo mẫu | (nội bộ validate) | `POST api/CongViecMau/LayBanDoFieldTheoMau` | `[SBAuthorize]` | internal | khi tạo/sửa CV | Trả map congViecMauId → field Ids hợp lệ. | EF | — |
| 4 | Lưu mẫu | Nút "Lưu" | `POST api/CongViecMau/Luu` | `[SBAuthorize]` | internal | form | Tạo/sửa mẫu + sync field thuộc mẫu. | EF | — |
| 5 | Xóa mẫu | Nút "Xóa" | `POST api/CongViecMau/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa mẫu. | EF | — |

## Quan hệ
- **Gọi sang:** `ICongViecMauService`.
- **Dữ liệu (entities):** `CongViecMau` + `CongViecMauField` (junction field-mẫu).
- **Ripple/đồng bộ:** CongViec đọc bản đồ field khi tạo/sửa/import theo mẫu.

## Lỗi đã biết / lưu ý kỹ thuật
- Xóa mẫu không kiểm tra CV đang dùng mẫu (`CongViec.CongViecMauId`) → CV cũ giữ id mẫu đã xóa (dangling reference).
