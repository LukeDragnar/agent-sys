---
alias: ["sản phẩm chi nhánh", "dịch vụ chi nhánh", "combo chi nhánh", "thẻ chi nhánh", "danh mục riêng chi nhánh"]
module: kho
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận
phu_thuoc: [Core (Chi nhánh / phân quyền), WarehouseProduct (danh mục SP/DV/Combo/Thẻ cấp đơn vị)]
link_flow: ./flow.md
---

# Quản lý sản phẩm chi nhánh

## Là gì
Cho phép **mỗi chi nhánh tự cấu hình danh mục sản phẩm / dịch vụ / combo / thẻ dịch vụ riêng của chi nhánh mình**, thay vì dùng chung một danh mục cho toàn đơn vị.

Gồm các màn hình:
- **Quản lý sản phẩm chi nhánh** — danh mục sản phẩm của chi nhánh.
- **Quản lý dịch vụ chi nhánh** — danh mục dịch vụ của chi nhánh.
- **Quản lý Combo chi nhánh** — gói combo của chi nhánh.
- **Quản lý thẻ chi nhánh** — thẻ dịch vụ do chi nhánh tự tạo.
- **Gói mở rộng** — phần mở rộng danh mục dùng riêng cho chi nhánh *(tên hiển thị cần product xác nhận)*.

> Khác với **"Quản lý sản phẩm"** (cấp **đơn vị** — danh mục gốc dùng chung). Cái này là cấp **chi nhánh**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh mục SP/DV/Combo/Thẻ của chi nhánh | ✅ | bắt buộc chọn nhóm; chỉ thấy món của chi nhánh đang đăng nhập |
| Thêm món (SP/DV/Combo/Thẻ) cho chi nhánh | ✅ | Admin CN / Quản lý DV / Quản lý kho (cần product xác nhận) |
| Sửa món của chi nhánh | ✅ | theo phân quyền chi nhánh |
| Xóa món của chi nhánh | ✅ | theo phân quyền chi nhánh |
| Bật / tắt "Sử dụng" từng món | ✅ | theo phân quyền |
| Import Excel | ✅ | tùy màn |
| Xem / sửa món của chi nhánh khác | ❌ | mỗi phiên đăng nhập gắn 1 chi nhánh; kể cả admin cũng không thấy món CN khác |
| Đặt giá khác nhau cho cùng 1 SP theo chi nhánh | ❌ | đây là danh mục riêng, không phải "1 SP chung giá khác nhau" |

## Để làm gì / cho ai
- **Cho chuỗi nhiều chi nhánh:** mỗi chi nhánh chỉ thấy & bán đúng những món của chi nhánh mình, không bị lẫn hàng/dịch vụ của chi nhánh khác.
- **Cho quản lý chi nhánh / quản lý kho chi nhánh:** tự thêm/sửa/ẩn món của chi nhánh mà không đụng danh mục chung.
- **Actor:** Admin chi nhánh, Quản lý dịch vụ, Quản lý kho (theo phân quyền — *cần product xác nhận danh sách quyền*).

## Cách dùng
1. Vào module **Kho** → chọn màn hình tương ứng (sản phẩm / dịch vụ / combo / thẻ chi nhánh).
2. Chọn **nhóm** (nhóm sản phẩm / nhóm dịch vụ) ở cột bên trái.
3. Danh sách hiển thị các món **thuộc chi nhánh đang đăng nhập**.
4. Thêm mới / sửa / xóa / bật-tắt sử dụng từng món.
5. (Tùy màn) chọn **chi nhánh** để lọc, tìm theo từ khóa, import Excel.

## Cần chú ý (khác gì SP cấp đơn vị)
- **Phạm vi nhìn thấy theo chi nhánh:** màn chi nhánh chỉ hiện món **do chính chi nhánh đó tạo** và **được gán cho chi nhánh đó**. Đứng ở chi nhánh A sẽ **không thấy** món của chi nhánh B — kể cả admin (mỗi phiên đăng nhập gắn 1 chi nhánh).
- **Không phải đổi giá theo chi nhánh:** đây là **danh mục riêng** (mỗi chi nhánh có bộ món riêng), không phải "một SP chung nhưng giá khác nhau từng chi nhánh". (Combo/thẻ có thêm cấu hình riêng như **số lượng giới hạn theo chi nhánh** — xem flow.)
- **Phải gán nhóm:** mở danh sách bắt buộc có **nhóm sản phẩm / nhóm dịch vụ**, không truyền nhóm sẽ không ra dữ liệu.
- **Thẻ chi nhánh là thẻ do chi nhánh tự tạo** ("thẻ dịch vụ chi nhánh tạo"), phân biệt với thẻ dịch vụ cấp đơn vị.

## Ảnh hưởng tới
- **Bán hàng / Đặt lịch** — món của chi nhánh là nguồn để chọn khi lên đơn / đặt lịch tại chi nhánh đó.
- **Tồn kho** — sản phẩm chi nhánh gắn với tồn kho của kho thuộc chi nhánh.
- **Danh mục cấp đơn vị (Quản lý sản phẩm)** — là lớp gốc; danh mục chi nhánh là lớp cấu hình riêng bên trên.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md) · Kỹ thuật: [./dev.md](./dev.md)
