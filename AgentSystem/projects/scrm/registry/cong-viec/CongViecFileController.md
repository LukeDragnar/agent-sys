---
controller: CongViecFileController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc — tab/khu 'Tệp đính kèm' trong popup chi tiết công việc (upload/liệt kê/xóa file gắn 1 công việc)"
muc_dich: "Quản lý tệp đính kèm công việc (CongViecFile): lấy theo công việc, lưu (đính kèm), xóa."
alias: [CongViecFile, "tệp đính kèm", "file công việc", attachment, "đính kèm công việc"]
auth: internal
state_machine: []
deprecated: false
entities: [CongViecFile]
stored_procs: []
depends: [ICongViecFileService]
cross_module_in: [CongViecController (xóa CV cascade xóa file qua snapshot)]
cross_module_out: []
---

# CongViecFile — Tệp đính kèm công việc

> CRUD đơn giản file gắn 1 công việc (CongViecId). Khi xóa CV, file bị soft-delete cùng subtree và đưa vào snapshot LichSuXoa (khôi phục được).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy file theo công việc | Khu "Tệp đính kèm" | `POST api/CongViecFile/LayTheoCongViecId/{congViecId}` | `[SBAuthorize]` | internal | popup | Trả danh sách file của 1 công việc. | EF | — |
| 2 | Lưu file (đính kèm) | Nút "Tải lên" / "Lưu" | `POST api/CongViecFile/Save` | `[SBAuthorize]` | internal | — | Tạo/cập nhật bản ghi file (metadata đường dẫn). | EF | — |
| 3 | Xóa file | Nút "Xóa" (file) | `POST api/CongViecFile/Xoa/{id}` | `[SBAuthorize]` | internal | — | Xóa 1 file. | EF | — |

## Quan hệ
- **Gọi sang:** `ICongViecFileService`.
- **Dữ liệu (entities):** `CongViecFile` (FK CongViecId).
- **Ripple/đồng bộ:** xóa CV cha → file vào snapshot LichSuXoa (khôi phục cùng CV).

## Lỗi đã biết / lưu ý kỹ thuật
- (không phát hiện lỗi đáng chú ý.)
