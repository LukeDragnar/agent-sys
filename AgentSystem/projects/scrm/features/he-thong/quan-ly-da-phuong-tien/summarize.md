---
alias: ["đa phương tiện", "media", "quản lý media", "tài nguyên đa phương tiện", "đính kèm đa phương tiện", "DaPhuongTien"]
module: he-thong
trang_thai: Core (nội bộ super-admin)
quyen: "Thêm/Sửa yêu cầu quyền nội bộ `HT_Luu_DaPhuongTien`; Xóa yêu cầu `HT_Xoa_DaPhuongTien`. Tìm kiếm/xem chi tiết/lấy nhóm KHÔNG gắn quyền cụ thể (chỉ cần đăng nhập). Là màn quản trị nội bộ — cần product xác nhận có cho khách hàng cuối hay không."
goi: cần product xác nhận
phu_thuoc: []
link_flow: ./flow.md
---

# Quản lý đa phương tiện

## Là gì
Đây là nơi **khai báo và quản lý danh mục "đa phương tiện"** ở cấp hệ thống — mỗi bản ghi là một mục tài nguyên/đường dẫn media (ảnh/video/tài liệu/liên kết) được gom **theo nhóm**, có tên, đường dẫn, thứ tự hiển thị và trạng thái sử dụng. Mục đích là dựng sẵn một danh sách media chuẩn để các màn khác chọn/hiển thị.

Phục vụ **admin hệ thống (super-admin nội bộ SaaS)**, thao tác trên 1 màn hình quản lý có bảng danh sách + form thêm/sửa dạng popup.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm mới mục đa phương tiện | ✅ | quyền `HT_Luu_DaPhuongTien` |
| Sửa mục đa phương tiện | ✅ | quyền `HT_Luu_DaPhuongTien` |
| Xóa mục đa phương tiện | ✅ | quyền `HT_Xoa_DaPhuongTien` (có hộp xác nhận trước khi xóa) |
| Tìm kiếm theo tên / lọc theo nhóm | ✅ | chỉ cần đăng nhập |
| Xem chi tiết một mục | ✅ | chỉ cần đăng nhập |
| Lấy danh sách nhóm (dropdown lọc) | ✅ | chỉ cần đăng nhập |
| Xuất Excel | ⚠️ | nút Export chỉ hiện khi `showExportButton = true` — cần product xác nhận điều kiện bật |

## Để làm gì / cho ai
- **Admin hệ thống:** khai báo, sắp xếp và bật/tắt các mục media dùng chung; gom theo nhóm để dễ chọn ở các màn khác.

## Cách dùng
1. Vào **Quản trị → Đa phương tiện**.
2. Lọc theo **Nhóm** và/hoặc gõ **Tên** vào ô tìm kiếm → bấm **Tìm kiếm**.
3. Bấm **Thêm mới** → điền: **Tên** (bắt buộc), **Nhóm** (bắt buộc, chọn nhóm có sẵn hoặc gõ nhóm mới), **Thứ tự hiển thị** (bắt buộc), **Đường dẫn** (bắt buộc), **Mô tả**, **Sử dụng** (Có/Không) → lưu.
4. Trên mỗi dòng: bấm **Sửa** để chỉnh; mở menu (▾) chọn **Xóa** (có hộp xác nhận).

## Cần chú ý
- **Tên** và **Mô tả** không được có khoảng trắng thừa ở đầu/cuối (hệ thống chặn và báo lỗi).
- **Tên, Nhóm, Thứ tự hiển thị, Đường dẫn** là bắt buộc — thiếu sẽ không lưu được.
- **Xóa** ở backend dùng phương thức GET (lệch convention) — không ảnh hưởng người dùng nhưng là điểm kỹ thuật cần lưu (xem dev.md).
- Một số thao tác (Tìm/Xem/Lấy nhóm) **không kiểm tra quyền chi tiết**, chỉ cần đăng nhập — cần lưu ý về kiểm soát truy cập (ghi ở dev.md).

## Ảnh hưởng tới
- Các màn dùng danh mục media này (chọn theo nhóm) — nếu xóa/đổi một mục, màn đang tham chiếu có thể không còn lựa chọn đó.
