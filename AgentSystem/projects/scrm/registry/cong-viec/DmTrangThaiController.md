---
controller: DmTrangThaiController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình > Danh mục trạng thái công việc (định nghĩa trạng thái: mã/tên/màu/thứ tự; gồm các mã hệ thống CHUA_BAT_DAU, DANG_XU_LY, HOAN_THANH)"
muc_dich: "CRUD danh mục trạng thái công việc (DmTrangThai): mã/tên/màu/thứ tự/active; cấp trạng thái cho CongViec + QuyTrinhThucThi (mã chuẩn CHUA_BAT_DAU/DANG_XU_LY/HOAN_THANH driver vòng đời)."
alias: [DmTrangThai, "trạng thái công việc", "danh mục trạng thái", task status, "tình trạng công việc"]
auth: internal
state_machine: [CHUA_BAT_DAU, DANG_XU_LY, HOAN_THANH]
deprecated: false
entities: [DmTrangThai]
stored_procs: []
depends: [IDmTrangThaiService]
cross_module_in: [CongViecController, QuyTrinhThucThiController, CongViecBaoCaoController (tra trạng thái theo Ma)]
cross_module_out: []
---

# DmTrangThai — Danh mục trạng thái công việc

> CRUD trạng thái. Quan trọng: **mã chuẩn** `CHUA_BAT_DAU` / `DANG_XU_LY` / `HOAN_THANH` được QuyTrinhThucThi + CongViec tra theo `DmTrangThai.Ma` để điều khiển vòng đời — đơn vị thiếu các mã này → IdTrangThai null khi khởi chạy/bắt đầu. Field: Ma, Ten, MauSac, ThuTu, IsActive, IsDefault.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm trạng thái (lưới) | Lưới Danh mục trạng thái | `POST api/DmTrangThai/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn danh mục | Filter + phân trang; thường sort ThuTu. | EF | — |
| 2 | Chi tiết trạng thái | Mở form | `POST api/DmTrangThai/TimTheoId/{id}` | `[SBAuthorize]` | internal | form | Trả bản ghi. | EF | — |
| 3 | Lưu trạng thái | Nút "Lưu" | `POST api/DmTrangThai/Luu` | `[SBAuthorize]` | internal | form | Tạo/sửa (mã/tên/màu/thứ tự/active). | EF | — |
| 4 | Xóa trạng thái | Nút "Xóa" | `POST api/DmTrangThai/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa trạng thái. | EF | — |

## Quan hệ
- **Gọi sang:** `IDmTrangThaiService`.
- **Dữ liệu (entities):** `DmTrangThai`.
- **Ripple/đồng bộ:** CongViec/QuyTrinhThucThi/Báo cáo map IdTrangThai → Ten/MauSac; vòng đời tra theo Ma chuẩn.

## Lỗi đã biết / lưu ý kỹ thuật
- Xóa/đổi `Ma` của trạng thái hệ thống (CHUA_BAT_DAU/DANG_XU_LY/HOAN_THANH) **phá vòng đời** QuyTrinhThucThi (tra theo Ma) — không thấy guard chặn xóa mã hệ thống trong service.
