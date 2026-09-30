---
alias: ["cấu hình công việc", "thiết lập công việc", "mẫu công việc", "quy trình công việc", "thiết kế quy trình", "mẫu quy trình", "field công việc", "thiết lập chung công việc"]
module: cong-viec
trang_thai: "Add-on — cần bật tiện ích (KiemTraTienIch) cho các màn quy trình/mẫu"
goi: "cần product xác nhận — các màn quy trình & thực thi gate qua KiemTraTienIch"
quyen: "cần product xác nhận — phần lớn endpoint cấu hình chỉ yêu cầu đăng nhập (SBAuthorize), scope theo đơn vị; chưa siết role cụ thể"
phu_thuoc: ["Quản lý công việc (mẫu & quy trình ở đây quyết định cách công việc được tạo / sinh tự động)", "Danh mục trạng thái (cần có mã CHUA_BAT_DAU / DANG_XU_LY để lượt thực thi gán đúng trạng thái)", "Cơ cấu tổ chức (người thực hiện gốc, phân công theo flow)"]
link_flow: ./flow.md
---

# Cấu hình công việc

## Là gì
Đây là phần **thiết lập (setup)** cho module Công việc — khác với màn vận hành hằng ngày (Quản lý công việc). Ở đây đơn vị định nghĩa sẵn các "khuôn" để công việc chạy đúng quy trình:

- **Mẫu công việc (template)**: định nghĩa sẵn một loại công việc cùng bộ **trường thông tin (field) động** đi kèm — khi tạo/nhập công việc theo mẫu thì chỉ được dùng đúng các field thuộc mẫu đó.
- **Quy trình (process / flow)**: thiết kế một quy trình gồm nhiều bước (sơ đồ flow). Mỗi lần lưu sinh một **phiên bản** mới; xem được lịch sử phiên bản.
- **Lượt thực thi quy trình**: từ một quy trình đã thiết kế, **khởi chạy** thành một "lượt chạy" → tạo công việc gốc; **bắt đầu** lượt → tự **sinh cây công việc con** theo flow. Đây chính là "luật tự động" sinh việc.
- **Thiết lập chung**: các tham số chung cho module công việc (ví dụ số cấp công việc con cho phép).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo / sửa / xóa mẫu công việc | ✅ | đăng nhập; gán field thuộc mẫu |
| Tạo / sửa / xóa quy trình (flow) | ✅ | đăng nhập |
| Xem lịch sử phiên bản quy trình | ✅ | đăng nhập |
| Khởi chạy quy trình (tạo lượt + công việc gốc) | ✅ | quy trình phải có ít nhất 1 phiên bản đã lưu |
| Bắt đầu lượt (sinh cây công việc con từ flow) | ✅ | lượt đang ở trạng thái chưa bắt đầu |
| Sinh mã / kiểm tra mã lượt | ✅ | đăng nhập |
| Xóa lượt thực thi | ⚠️ | xóa **mềm**, kéo theo công việc gốc + con; khôi phục ở Lịch sử xóa |
| Cấu hình field động cho công việc | ✅ | đăng nhập |
| Đặt số cấp công việc con cho phép | ✅ | đăng nhập (thiết lập chung) |

## Để làm gì / cho ai
- **Quản lý / người thiết kế quy trình:** dựng sẵn quy trình chuẩn (onboarding khách, xử lý đơn, chăm sóc…) để mỗi lần chạy là tự sinh đầy đủ các bước việc.
- **Admin đơn vị:** định nghĩa mẫu công việc + field động để chuẩn hóa thông tin nhập; đặt giới hạn số cấp con.
- **Người vận hành:** khởi chạy quy trình mỗi khi có việc mới phát sinh, hệ thống tự sinh cây công việc con.

## Cách dùng
**Tạo mẫu công việc:**
1. Vào **Công việc → Danh sách mẫu công việc** → Thêm/Lưu.
2. Đặt tên mẫu, chọn các field động thuộc mẫu, đánh dấu kích hoạt.

**Thiết kế quy trình:**
1. Vào **Công việc → Danh sách quy trình** → Thêm/sửa → mở trình thiết kế flow.
2. Vẽ các bước, lưu. Mỗi lần lưu tạo một phiên bản mới (xem ở tab Lịch sử phiên bản).

**Chạy quy trình (luật tự động sinh việc):**
1. Vào **Danh sách thực thi** → **Khởi chạy** một quy trình → hệ thống tạo lượt + công việc gốc (chưa bắt đầu).
2. Bấm **Bắt đầu** → công việc gốc chuyển sang đang xử lý và **tự sinh cây công việc con** theo flow.

**Thiết lập chung:**
1. Vào **Thiết lập chung** → đặt số cấp công việc con cho phép và các tham số khác.

## Cần chú ý
- **Khởi chạy cần quy trình đã có phiên bản**: quy trình chưa từng lưu (chưa có version) sẽ không khởi chạy được — báo "Quy trình chưa có phiên bản".
- **Mỗi lần lưu quy trình đều tạo phiên bản mới** (kể cả sửa nhỏ) → lịch sử phiên bản có thể phình. Lượt đang chạy giữ nguyên phiên bản lúc khởi chạy (sửa quy trình không ảnh hưởng lượt đang chạy).
- **Bắt đầu lượt chỉ thực hiện một lần**: lượt đã bắt đầu rồi sẽ báo "Lượt đã được bắt đầu". Nếu việc sinh cây con lỗi giữa chừng, hệ thống hủy toàn bộ (rollback).
- **Cần có danh mục trạng thái đúng mã** (CHUA_BAT_DAU / DANG_XU_LY): nếu đơn vị thiếu các mã chuẩn này, lượt/công việc có thể không có trạng thái (xem dev.md).
- **Xóa mẫu không kiểm tra mẫu đang được dùng**: công việc cũ vẫn giữ liên kết tới mẫu đã xóa (xem dev.md).
- **Xóa lượt thực thi là xóa mềm và lan rộng** (kéo theo công việc gốc + con) — khôi phục ở Lịch sử xóa.

## Ảnh hưởng tới
- **Quản lý công việc**: mẫu / field động / số cấp con quyết định cách tạo và sửa công việc; lượt thực thi sinh ra công việc trong lưới.
- **Lịch sử xóa**: lượt thực thi xóa mềm nằm ở đây để khôi phục.
- **Tìm kiếm nhanh**: công việc sinh từ lượt được đồng bộ để tìm nhanh.
