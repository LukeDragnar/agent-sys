---
controller: DmMucDoUuTienController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình > Danh mục mức độ ưu tiên (định nghĩa mức độ ưu tiên: tên/màu; gán cho công việc)"
muc_dich: "CRUD danh mục mức độ ưu tiên (DmMucDoUuTien): tên/màu; cấp giá trị ưu tiên cho CongViec + báo cáo theo mức độ ưu tiên."
alias: [DmMucDoUuTien, "mức độ ưu tiên", "độ ưu tiên", priority, "ưu tiên công việc"]
auth: internal
state_machine: []
deprecated: false
entities: [DmMucDoUuTien]
stored_procs: []
depends: [IDmMucDoUuTienService]
cross_module_in: [CongViecController, CongViecBaoCaoController (map mức độ ưu tiên)]
cross_module_out: []
---

# DmMucDoUuTien — Danh mục mức độ ưu tiên

> CRUD mức độ ưu tiên (TenMucDo + MauSac). CongViec gán IdMucDoUuTien; báo cáo gom theo mức độ (null → "Chưa phân loại").

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm mức độ (lưới) | Lưới Danh mục ưu tiên | `POST api/DmMucDoUuTien/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn danh mục | Filter + phân trang. | EF | — |
| 2 | Chi tiết mức độ | Mở form | `POST api/DmMucDoUuTien/TimTheoId/{id}` | `[SBAuthorize]` | internal | form | Trả bản ghi. | EF | — |
| 3 | Lưu mức độ | Nút "Lưu" | `POST api/DmMucDoUuTien/Luu` | `[SBAuthorize]` | internal | form | Tạo/sửa (tên/màu). | EF | — |
| 4 | Xóa mức độ | Nút "Xóa" | `POST api/DmMucDoUuTien/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa mức độ. | EF | — |

## Quan hệ
- **Gọi sang:** `IDmMucDoUuTienService`.
- **Dữ liệu (entities):** `DmMucDoUuTien`.
- **Ripple/đồng bộ:** CongViec/Báo cáo map IdMucDoUuTien → TenMucDo/MauSac.

## Lỗi đã biết / lưu ý kỹ thuật
- `DmMucDoUuTienController.cs:9` — tên hiển thị tiện ích có typo "Danh mục mức dộ ưu tiên" (dộ → độ); chỉ là nhãn.
- Xóa mức độ không kiểm tra CV đang dùng → CV cũ giữ IdMucDoUuTien đã xóa (báo cáo hiện "Chưa phân loại").
