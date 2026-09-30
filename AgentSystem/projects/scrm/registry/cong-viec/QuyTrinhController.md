---
controller: QuyTrinhController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Quy trình > Danh sách quy trình (thiết kế quy trình/flow; mỗi lần lưu sinh 1 phiên bản QuyTrinhLichSu, xem lịch sử version)"
muc_dich: "CRUD định nghĩa quy trình (QuyTrinh) + flow; mỗi lần lưu sinh phiên bản mới (QuyTrinhLichSu Version+1); xem lịch sử phiên bản."
alias: [QuyTrinh, "quy trình", "mẫu quy trình", "thiết kế quy trình", process, workflow definition, flow]
auth: internal
state_machine: []
deprecated: false
entities: [QuyTrinh, QuyTrinhLichSu]
stored_procs: []
depends: [IQuyTrinhService]
cross_module_in: [QuyTrinhThucThiController (KhoiChay đọc QuyTrinh + QuyTrinhLichSu mới nhất)]
cross_module_out: []
---

# QuyTrinh — Định nghĩa quy trình (versioned flow)

> [KiemTraTienIch] bật. Mỗi `Luu` ghi entity QuyTrinh + **insert 1 bản QuyTrinhLichSu** với `Version = lastVersion + 1` (lưu FlowData snapshot). QuyTrinhThucThi.KhoiChay luôn dùng version mới nhất. Quy trình chưa từng lưu → không có version → không khởi chạy được.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm quy trình (lưới) | Lưới Danh sách quy trình | `POST api/QuyTrinh/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn danh sách | Filter keyword (TenQuyTrinh/MaCode), phân trang. | EF | — |
| 2 | Chi tiết quy trình | Mở editor flow | `POST api/QuyTrinh/TimTheoId/{id}` | `[SBAuthorize]` | internal | editor | Trả định nghĩa + FlowData phiên bản hiện hành. | EF | — |
| 3 | Lưu quy trình (+ version) | Nút "Lưu" | `POST api/QuyTrinh/Luu` | `[SBAuthorize]` | internal | editor | Tạo/sửa QuyTrinh → **insert QuyTrinhLichSu Version+1**. | EF | Mỗi lần lưu đều tạo version mới (kể cả sửa nhỏ) → lịch sử version có thể phình. |
| 4 | Xóa quy trình | Nút "Xóa" | `POST api/QuyTrinh/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa quy trình. | EF | — |
| 5 | Lịch sử phiên bản | Tab "Lịch sử phiên bản" | `POST api/QuyTrinh/LichSuTheoQuyTrinh/{quyTrinhId}` | `[SBAuthorize]` | internal | — | Trả danh sách QuyTrinhLichSu sort Version DESC. | EF | — |

## Quan hệ
- **Gọi sang:** `IQuyTrinhService`.
- **Dữ liệu (entities):** `QuyTrinh` (định nghĩa) + `QuyTrinhLichSu` (version + FlowData).
- **Ripple/đồng bộ:** không trực tiếp; gián tiếp QuyTrinhThucThi.KhoiChay tiêu thụ version mới nhất.

## Lỗi đã biết / lưu ý kỹ thuật
- `QuyTrinhService.cs:166-180` (`Luu`) — mỗi save sinh QuyTrinhLichSu mới (Version+1); không dedupe nếu flow không đổi.
- Lượt thực thi đã khởi chạy giữ `QuyTrinhLichSuId` cố định → sửa quy trình KHÔNG ảnh hưởng lượt đang chạy (version pinning).
