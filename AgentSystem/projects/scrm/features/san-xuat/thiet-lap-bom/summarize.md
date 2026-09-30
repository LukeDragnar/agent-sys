---
alias: ["BOM", "định mức", "định mức nguyên vật liệu", "công thức sản phẩm", "thiết lập BOM", "bill of materials", "quy trình sản xuất", "công đoạn sản xuất"]
module: san-xuat
trang_thai: "cần product xác nhận — thuộc module Sản Xuất còn sơ khai. Nút Sao chép BOM hiện chỉ cần đăng nhập (quyền riêng bị comment)."
goi: "cần product xác nhận"
quyen: "cần product xác nhận — Thêm/Sửa/Xóa BOM yêu cầu role AD-TONG/AD-CN/SANXUAT. Sao chép (Clone) BOM hiện chỉ cần đăng nhập. Danh mục Công đoạn (DmCongDoan) toàn bộ quyền riêng bị comment → chỉ cần đăng nhập."
phu_thuoc: ["Sản phẩm (chọn sản phẩm đích + nguyên vật liệu/bán thành phẩm)", "Đơn vị tính (UOM cho định mức)", "Kho (gắn kho cho từng nguyên liệu)", "Danh mục Công đoạn (đặt tên/thứ tự các bước quy trình)"]
link_flow: ./flow.md
---

# Thiết lập BOM (định mức nguyên vật liệu)

## Là gì
**BOM (Bill of Materials — định mức / công thức)** là **bản công thức để sản xuất ra một sản phẩm**: cần những nguyên vật liệu nào, mỗi loại bao nhiêu, qua những công đoạn (bước) nào, mỗi công đoạn cho ra kết quả gì. Đây là khuôn mẫu mà toàn bộ quy trình sản xuất (Work Order) dựa vào để biết phải xuất nguyên liệu gì và nhập thành phẩm gì.

Mỗi BOM gồm:
- **Header**: sản phẩm đích, số lượng theo công thức (1 "mẻ"), kích thước, cờ "mặc định".
- **Quy trình**: danh sách **bước/công đoạn** theo thứ tự (kéo–thả để sắp xếp), mỗi bước có thời gian chờ/hoàn thành.
- Mỗi bước có **Nguyên vật liệu (đầu vào)** và **Kết quả (đầu ra)** kèm định mức, % sai lệch, đánh dấu nguyên liệu chính / bán thành phẩm.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm BOM | ✅ | role AD-TONG/AD-CN/SANXUAT (cần product xác nhận) |
| Sửa BOM (header) | ✅ | chỉ BOM cùng đơn vị + chi nhánh (chặn chéo tenant) |
| Xóa BOM | ✅ | (lưu ý: không chặn chéo tenant — xem Cần chú ý) |
| Sao chép (Clone) BOM | ✅ | hiện chỉ cần đăng nhập (quyền riêng bị tắt) |
| Đặt BOM mặc định cho sản phẩm | ✅ | mỗi sản phẩm chỉ 1 BOM mặc định |
| Tạo / sửa quy trình (bước công đoạn) | ✅ | kéo–thả sắp xếp; lưu là ghi đè toàn bộ quy trình |
| Thêm nguyên vật liệu đầu vào / kết quả đầu ra | ✅ | theo từng bước, có định mức + % sai lệch |
| Quản lý danh mục Công đoạn | ✅ | hiện chỉ cần đăng nhập |

## Để làm gì / cho ai
- **Quản lý sản xuất / kỹ thuật:** chuẩn hóa công thức sản phẩm — sản xuất theo định mức nhất quán, dễ tính nguyên liệu cần.
- **Kế hoạch / mua hàng:** dựa vào BOM để biết cần mua/chuẩn bị bao nhiêu nguyên liệu.
- **Vận hành sản xuất:** mỗi lệnh sản xuất "sao chép" quy trình từ BOM để chạy.

## Cách dùng
1. Vào **Sản xuất → Thiết lập BOM** (`/sanxuat/lenh-bomsetup`).
2. Bên trái là **Danh sách BOM**; bấm **Tạo BOM** để thêm mới (chọn sản phẩm, số lượng theo công thức, kích thước).
3. Chọn một BOM → bên phải hiện **Quy trình**. Bấm **Tạo Bước** để thêm công đoạn; kéo–thả để sắp xếp thứ tự; nhập thời gian chờ/hoàn thành.
4. Với mỗi bước, ở bảng **Nguyên vật liệu** thêm các nguyên liệu đầu vào (chọn sản phẩm, kho, đơn vị, định mức, % sai lệch, đánh dấu nguyên liệu chính / bán thành phẩm). Ở bảng **Kết quả** thêm sản phẩm đầu ra của bước.
5. Bấm **Lưu quy trình** để ghi lại toàn bộ.
6. Có thể **Sao chép** một BOM (tạo bản copy với mã mới) hoặc **Xóa**.

## Cần chú ý
- **Lưu quy trình = ghi đè toàn bộ.** Khi lưu quy trình, hệ thống **xóa hết bước + nguyên liệu cũ rồi tạo lại** → mọi bước/nguyên liệu sẽ có "mã nội bộ" mới. Lệnh sản xuất đã tách trước đó **không tự cập nhật** theo BOM mới.
- **Mỗi sản phẩm chỉ 1 BOM mặc định.** Đặt một BOM làm mặc định sẽ tự bỏ cờ mặc định ở các BOM khác cùng sản phẩm.
- **Sửa BOM bị chặn chéo đơn vị/chi nhánh**, nhưng **Xóa và Sao chép thì không kiểm tra** điều này — cẩn thận thao tác nhầm sang BOM của đơn vị/chi nhánh khác. (cần product xác nhận có chủ ý)
- **Nút Sao chép hiện chỉ cần đăng nhập** (quyền riêng đang bị tắt). Tương tự **danh mục Công đoạn** không yêu cầu quyền riêng.
- **Công đoạn (DmCongDoan)** liên kết với bước BOM theo tên/mã (liên kết "lỏng"), không khóa cứng — đổi tên công đoạn có thể không đồng bộ với bước đã đặt.

## Ảnh hưởng tới
- **Lệnh sản xuất (Work Order):** dựng quy trình + tính nguyên liệu cần đều dựa trên BOM.
- **Yêu cầu sản xuất (PR):** lấy kích thước mẻ (LotSize/KichThuoc) từ BOM.
- **Kho:** định mức trong BOM quyết định số lượng nguyên liệu xuất ra khi chạy lệnh.
