---
alias: ["tính lương", "bảng lương", "lương nhân viên", "bảng lương NV", "trả lương", "chốt lương"]
module: ke-toan
trang_thai: Core (thuộc tiện ích chi nhánh — cần product xác nhận có gate gói riêng không)
goi: cần product xác nhận — tính năng gắn tiện ích chi nhánh
quyen: "admin đơn vị/chi nhánh (DV-AD-TONG, DV-AD-CN, DV-AD-MCN), kế toán (DV-KT, DV-KT-TONG), quản lý đơn vị (DV-QLDV) — áp cho mọi thao tác bảng lương"
phu_thuoc: ["Cài đặt lương cơ bản — định mức lương từng nhân viên", "Chấm công / giờ làm việc đã duyệt — nguồn công để tính lương", "Hồ sơ nhân viên — danh sách NV trong bảng lương"]
link_flow: ./flow.md
---

# Tính lương (Bảng lương nhân viên)

## Là gì
Đây là nơi **lập và quản lý bảng lương** cho nhân viên theo từng kỳ. Mỗi **bảng lương** ứng với 1 kỳ (ví dụ tháng/chi nhánh); trong bảng có **chi tiết lương theo từng nhân viên**. Hệ thống có thể **tạo lương chi tiết tự động** cho cả bảng dựa trên cài đặt lương cơ bản + công đã chấm, sau đó kế toán chỉnh tay từng người và **chốt** bảng lương qua các trạng thái.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem danh sách bảng lương | ✅ | DV-AD-*, DV-KT, DV-KT-TONG, DV-QLDV |
| Thêm bảng lương mới (theo kỳ) | ✅ | như trên |
| Xóa bảng lương | ✅ | như trên |
| Xem chi tiết bảng lương + danh sách NV | ✅ | như trên |
| Xem/lấy lương theo 1 nhân viên | ✅ | như trên |
| Sửa / lưu lương từng nhân viên (chỉnh tay) | ✅ | như trên |
| **Tạo lương chi tiết tự động cho cả bảng** | ✅ | như trên — lấy từ cài đặt lương + công đã duyệt |
| Đổi trạng thái bảng lương (nháp/duyệt/chốt) | ✅ | như trên; trạng thái theo "type" truyền vào |
| In bảng lương | ⚠️ | cần product xác nhận (không thấy endpoint in trong controller này) |
| Xuất Excel bảng lương | ⚠️ | cần product xác nhận (không thấy endpoint export trong controller này) |

## Để làm gì / cho ai
- **Kế toán:** lập bảng lương kỳ, tạo lương tự động, soát và chỉnh từng nhân viên, chốt để trả lương.
- **Admin/quản lý đơn vị:** kiểm soát bảng lương theo chi nhánh, duyệt/chốt.
- **Chủ shop:** nắm chi phí lương từng kỳ.

## Cách dùng
1. Vào **Lương → Bảng lương nhân viên**.
2. Bấm **Thêm mới** để tạo 1 bảng lương cho kỳ cần tính.
3. Mở bảng lương, bấm **Tạo lương tự động** → hệ thống sinh chi tiết lương cho toàn bộ nhân viên dựa trên cài đặt lương cơ bản + công đã duyệt.
4. Mở từng nhân viên để **soát và chỉnh tay** (thưởng/phạt/phụ cấp… nếu cần), rồi **Lưu**.
5. Khi hoàn tất, **đổi trạng thái** bảng lương (duyệt/chốt) để khóa lại.

## Cần chú ý
- **Tạo lương tự động phụ thuộc dữ liệu nền:** nếu cài đặt lương cơ bản hoặc công chấm chưa đủ/chưa duyệt thì kết quả tự động sẽ thiếu hoặc sai — cần kiểm tra chấm công trước.
- **Trạng thái bảng lương theo "type":** nút duyệt/chốt truyền một mã trạng thái; ý nghĩa từng trạng thái do hệ thống quy định (cần product xác nhận danh sách trạng thái cụ thể).
- **Chỉnh tay sau khi tạo tự động:** số liệu tự động chỉ là khởi tạo; kế toán nên soát từng người trước khi chốt.
- **Phân quyền theo kế toán/admin/quản lý** — nhân viên thường không thao tác được bảng lương.

## Ảnh hưởng tới
- **Chấm công (giờ làm việc):** là nguồn công đầu vào; sửa chấm công sau khi đã tạo lương tự động có thể khiến số liệu lệch — nên tạo lại.
- **Cài đặt lương cơ bản:** thay đổi định mức lương ảnh hưởng kết quả tạo tự động lần sau.
- **Báo cáo chi phí/nhân sự:** bảng lương đã chốt là cơ sở chi phí lương.
