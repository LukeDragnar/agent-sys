---
alias: ["gắn nhãn khách", "nhóm khách hàng", "hạng thành viên", "phân loại khách hàng", "đánh nhãn khách"]
module: khach-hang
trang_thai: đang dùng
goi: cần product xác nhận
quyen: "Gắn nhãn / chia nhóm cho khách: admin tổng, admin chi nhánh, quản lý dịch vụ, nhân viên (tùy thao tác). Cấu hình danh mục nhóm/hạng: admin tổng (DV-AD-TONG). (chi tiết cần product xác nhận)"
phu_thuoc: [Quản lý thông tin khách hàng, Điểm thưởng & đổi thưởng (điều kiện nhóm/hạng theo điểm), Khuyến mãi / chiết khấu theo hạng]
link_flow: ./flow.md
---

# Phân loại & đánh nhãn khách hàng

## Là gì
Bộ công cụ giúp bạn **gắn cờ và phân loại khách hàng theo nhiều cách khác nhau** để dễ lọc, dễ chăm sóc và áp ưu đãi đúng người. Có 3 cách phân loại độc lập nhau:

- **Nhãn khách hàng:** những "cái cờ" tự do bạn đặt ra (ví dụ "Khách VIP", "Hay hủy đơn", "Thích sản phẩm A"). Mỗi nhãn có **tên + màu nền** để nhìn cho dễ. Một khách có thể dán **nhiều nhãn** cùng lúc.
- **Nhóm khách hàng:** chia khách vào từng nhóm có **mức giảm giá riêng** (ví dụ nhóm "Khách sỉ" giảm 10%). Một khách chỉ thuộc **một nhóm** tại một thời điểm. Nhóm có thể **không điều kiện** (gán tay) hoặc **có điều kiện** (đủ điểm mới được vào).
- **Hạng khách hàng:** xếp hạng thành viên theo **điểm tích lũy** (ví dụ Đồng → Bạc → Vàng), mỗi hạng có **ngưỡng điểm** và **mức chiết khấu** riêng; hệ thống tự xét nâng hạng khi khách phát sinh điểm.

Gồm 5 màn hình:
- **Nhãn khách hàng:** tạo / sửa / xóa các nhãn (tên + màu) dùng cho cả đơn vị.
- **Danh sách nhóm khách hàng:** xem khách nào đang thuộc nhóm nào (lọc theo nhóm, theo từ khóa).
- **Danh sách hạng khách hàng:** xem khách nào đang ở hạng nào.
- **Danh mục nhóm KH:** khai báo các nhóm (tên, mức giảm, điều kiện điểm, bật/tắt sử dụng).
- **Danh mục hạng KH:** khai báo các hạng (tên, màu, ngưỡng điểm, mức chiết khấu).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa nhãn khách hàng | ✅ | DV-AD-TONG, DV-QLDV, DV-AD-CN, DV-AD-MCN |
| Xóa nhãn | ⚠️ | DV-AD-TONG/CN/MCN, DV-QLDV; không phải admin tổng chỉ xóa nhãn do mình tạo |
| Gắn nhãn cho khách (nhiều nhãn / khách) | ✅ | làm ở hồ sơ KH360 |
| Thêm / Sửa nhóm khách hàng (danh mục) | ✅ | có key riêng theo từng action; xét theo phân quyền đơn vị |
| **Xóa nhóm khách hàng** | ❌ | endpoint xóa nhóm đã bị comment cứng — không hỗ trợ xóa |
| Bật/tắt sử dụng, bật/tắt tự xét nâng nhóm | ✅ | toggle trên danh mục nhóm |
| Thêm / Sửa hạng khách hàng (danh mục) | ✅ | thực tế giới hạn admin tổng (DV-AD-TONG) |
| Khởi tạo bộ nhóm / hạng mẫu | ✅ | nút "Khởi tạo" lấy bộ mẫu chuẩn |
| Gán khách vào nhóm | ✅ | mỗi khách 1 nhóm; nhóm có điều kiện cần đủ điểm mới gán được |
| Đổi hạng thủ công | ❌ | hạng tự xét theo điểm tích lũy, không gán tay |

## Để làm gì / cho ai
- **Cho sale / CSKH:** lọc nhanh nhóm khách cần chăm (gắn nhãn "cần gọi lại", lọc nhóm "khách sỉ"), nhìn màu nhãn là biết loại khách.
- **Cho chủ shop / quản lý:** thiết lập một lần các nhóm và hạng để cả hệ thống tự áp ưu đãi/chiết khấu đúng đối tượng, không phải nhớ thủ công.
- **Cho marketing:** dựa vào nhãn / nhóm / hạng để lọc danh sách gửi chiến dịch SMS, Zalo.
- **Actor:** nhân viên sale/CSKH (gắn nhãn, gán nhóm); admin tổng (khai báo danh mục nhóm/hạng).

## Cách dùng (bước chính)
1. (Cài đặt một lần) Vào **Danh mục hạng KH** và **Danh mục nhóm KH** để khai báo các hạng/nhóm cho shop. Nếu chưa có gì, có nút **Khởi tạo** để lấy bộ mẫu chuẩn về dùng nhanh.
2. Vào màn **Nhãn khách hàng** tạo các nhãn hay dùng (tên + màu).
3. Ở hồ sơ từng khách: **dán nhãn**, **gán nhóm** cho khách.
4. Hạng thì **không gán tay** — hệ thống tự xét theo điểm tích lũy của khách.
5. Vào **Danh sách nhóm / Danh sách hạng khách hàng** để xem và lọc khách theo nhóm/hạng.

## Cần chú ý (cạm bẫy)
- **3 cách phân loại độc lập nhau:** Nhãn (gắn tự do, nhiều cái) ≠ Nhóm (chỉ 1, có mức giảm giá) ≠ Hạng (theo điểm, có chiết khấu). Đừng nhầm lẫn dùng cái này thay cái kia.
- **Mỗi khách chỉ thuộc 1 nhóm:** khách đã có nhóm rồi thì chỉ **sửa** sang nhóm khác, không "thêm" nhóm thứ hai.
- **Nhóm "có điều kiện" cần đủ điểm:** gán khách chưa đủ điểm vào nhóm có điều kiện sẽ bị chặn ("chưa đủ điều kiện vào nhóm này").
- **Tên nhóm / tên hạng không được trùng** trong cùng đơn vị.
- **Khai báo danh mục nhóm/hạng giới hạn quyền admin tổng** (DV-AD-TONG). Nhân viên thường chỉ gắn nhãn / gán nhóm cho khách, không sửa danh mục.
- **Xóa nhãn có giới hạn quyền:** nếu không phải admin tổng, chỉ xóa được nhãn **do chính mình tạo**.
- **Hạng xét theo điểm tích lũy (chỉ tăng), không phải điểm hiện tại.** Đổi điểm lấy quà không làm tụt hạng.

## Ảnh hưởng tới
- **Giá bán / chiết khấu:** nhóm có "mức giảm giá", hạng có "mức chiết khấu" → ảnh hưởng giá khi lên đơn cho khách thuộc nhóm/hạng đó.
- **Điểm thưởng:** điều kiện vào nhóm và ngưỡng nâng hạng đều dựa trên điểm của khách.
- **Marketing / chăm sóc:** nhãn, nhóm, hạng là tiêu chí lọc danh sách khách để gửi chiến dịch / phân công chăm sóc.
- **Hồ sơ khách hàng 360:** nhãn/nhóm/hạng hiển thị trên hồ sơ khách để nhận diện nhanh.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md)
