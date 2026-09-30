---
alias: ["voucher", "mã giảm giá", "phiếu giảm giá", "coupon", "mã ưu đãi"]
module: marketing
trang_thai: cần product xác nhận — cần bật tiện ích Voucher
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD/khóa/gia hạn voucher có quyền riêng theo từng thao tác; non-admin chỉ thấy voucher chi nhánh mình; một số thao tác tra cứu nhanh (dropdown/gợi ý mã) chỉ yêu cầu đăng nhập (khe hở — xem dev)"
phu_thuoc: ["Tiện ích Voucher đã mua của đơn vị", "Danh sách khách hàng (hạng/nhóm KH để lọc đối tượng áp dụng)", "Sản phẩm/dịch vụ/thẻ (đối tượng áp dụng)", "Đơn hàng (áp/đánh dấu mã khi thanh toán + hoàn khi hủy)", "Chiến dịch khách hàng (job sinh mã voucher hàng loạt)"]
link_flow: ./flow.md
---

# Quản lý voucher

## Là gì
Đây là nơi **phát hành và quản lý voucher / mã giảm giá** cho khách hàng: tạo đợt voucher, sinh mã (một mã dùng nhiều lần hoặc nhiều mã mỗi mã một lần), giới hạn đối tượng được dùng (theo chi nhánh, hạng/nhóm khách, sản phẩm/dịch vụ/thẻ), gia hạn, khóa/mở khóa, và theo dõi lịch sử sử dụng từng mã.

Khi khách thanh toán và nhập mã hợp lệ, hệ thống **áp giảm giá, đánh dấu mã đã dùng**; nếu hủy đơn thì hoàn lại mã.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm đợt voucher | ✅ | cần quyền tạo voucher |
| Sửa voucher | ✅ | cần quyền sửa voucher |
| Xóa voucher | ✅ | cần quyền xóa voucher |
| Xem danh sách / lọc voucher | ✅ | non-admin chỉ thấy voucher chi nhánh mình |
| Khóa / Mở khóa voucher | ✅ | cần quyền khóa/mở khóa |
| Gia hạn voucher | ✅ | cần quyền gia hạn |
| Tìm kiếm mã voucher | ✅ | tùy quyền |
| Tải danh sách mã (Excel) | ✅ | cần quyền tải mã |
| Xem lịch sử sử dụng | ✅ | cần quyền xem lịch sử |
| Xử lý thủ công 1 mã (mở/khóa/reset) | ✅ | cần quyền xử lý thủ công mã |
| Sinh mã voucher hàng loạt | ✅ | tự động qua job chiến dịch khách hàng (nội bộ) |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** phát voucher để kích cầu, tri ân khách, chạy chiến dịch.
- **Quản lý chi nhánh:** xem voucher đang áp tại chi nhánh.
- **Thu ngân / bán hàng:** áp mã voucher khách đưa khi tính tiền.
- **Khách hàng:** dùng mã giảm giá khi mua hàng.

## Cách dùng
1. Vào **Marketing → Voucher → Thêm mới**.
2. Cấu hình đợt voucher: mức giảm, thời gian, **loại mã** (một mã dùng nhiều lần / nhiều mã mỗi mã một lần), số lượng mã, đối tượng áp dụng (chi nhánh, hạng/nhóm khách, sản phẩm/dịch vụ/thẻ).
3. Lưu → hệ thống sinh mã. Có thể **tải danh sách mã (Excel)** để phát cho khách.
4. Theo dõi **lịch sử sử dụng**, **gia hạn**, **khóa/mở khóa**, hoặc **xử lý thủ công** từng mã khi cần.
5. Khi khách thanh toán nhập mã → hệ thống kiểm tra điều kiện → áp giảm + đánh dấu mã đã dùng.

## Cần chú ý
- **Non-admin chỉ thấy voucher chi nhánh mình** — phân quyền, không phải lỗi.
- **Voucher bị khóa không áp được** khi thanh toán; mở khóa lại mới dùng được.
- **Hai loại mã khác nhau:** "một mã" dùng nhiều lần (đếm số lần dùng), "nhiều mã" mỗi mã chỉ dùng một lần. Chọn đúng loại khi tạo, vì cách đếm/hoàn khác nhau.
- **Khách vãng lai** chỉ dùng được voucher đặt là "không giới hạn"; các điều kiện theo hạng/nhóm khách sẽ loại khách vãng lai.
- **Hủy đơn đã dùng voucher** → mã được hoàn (mã nhiều lần giảm số lần, mã một lần bỏ đánh dấu đã dùng). Nếu rollback lỗi, mã có thể kẹt trạng thái "đã dùng".
- **Voucher đặt cọc (Deposit)** là loại đặc biệt gắn với phiếu cọc — không phải voucher giảm giá thường.

## Ảnh hưởng tới
- **Đơn hàng / thanh toán** — voucher quyết định giảm giá; áp/hoàn gắn vòng đời đơn.
- **Khuyến mãi & Hoa hồng giới thiệu** — xử lý chung trong bước áp ưu đãi khi thanh toán.
- **Chiến dịch khách hàng** — sinh mã voucher hàng loạt cho chiến dịch; hủy chiến dịch sẽ dọn mã.
- **Lịch sử sử dụng voucher** — ghi nhận mỗi lần dùng để báo cáo.
