# Module: Hệ thống

> ⚠️ **Khu quản trị nội bộ SaaS (super-admin / admin hệ thống của nhà cung cấp SCRM).** **Phần lớn KHÔNG cho khách hàng cuối (đơn vị/shop) thấy** — đây là nơi đội vận hành SCRM khai báo danh mục gốc dùng chung, quản trị tenant/hợp đồng/gói, phân quyền, duyệt giao dịch, đồng bộ và tra log toàn hệ thống. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Vì module này là nội bộ, `summarize`/`faq` chủ yếu phục vụ team — cân nhắc trước khi cho chatbot khách ăn.

## Danh mục gốc (master data cấp hệ thống)
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Danh mục hành chính | Cây địa giới hành chính dùng chung (quốc gia → tỉnh → quận/huyện → phường/xã) | [danh-muc-hanh-chinh/](danh-muc-hanh-chinh/) |
| Danh mục khách hàng gốc | Nhóm/hạng khách hàng dùng chung cấp hệ thống | [danh-muc-khach-hang-goc/](danh-muc-khach-hang-goc/) |
| Danh mục sản phẩm & kho gốc | Bộ sản phẩm/dịch vụ/nhóm/kho gốc làm dữ liệu mẫu cho các đơn vị | [danh-muc-san-pham-kho-goc/](danh-muc-san-pham-kho-goc/) |
| Danh mục đơn hàng & tác nghiệp | Loại đơn hàng + danh mục tác nghiệp dùng chung | [danh-muc-don-hang-tac-nghiep/](danh-muc-don-hang-tac-nghiep/) |
| Danh mục khác | Các danh mục gốc còn lại: nguồn đặt lịch, ngành nghề/lĩnh vực kinh doanh… | [danh-muc-khac/](danh-muc-khac/) |
| Quản lý đa phương tiện | Danh mục media (ảnh/video/tài liệu/link) gom theo nhóm | [quan-ly-da-phuong-tien/](quan-ly-da-phuong-tien/) |

## Tenant, gói, hợp đồng & phân quyền
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý đơn vị & hợp đồng | Quản trị tenant (mỗi shop = 1 đơn vị) + hợp đồng kèm theo | [quan-ly-don-vi-va-hop-dong/](quan-ly-don-vi-va-hop-dong/) |
| Quản lý gói & tiện ích | Định nghĩa gói cước + chợ tiện ích "khách mua gì" | [quan-ly-goi-va-tien-ich/](quan-ly-goi-va-tien-ich/) |
| Quản lý tài khoản & người dùng | Quản trị tài khoản nhân viên toàn hệ thống: tạo/sửa/khóa/đổi MK/đồng bộ user | [quan-ly-tai-khoan-va-nguoi-dung/](quan-ly-tai-khoan-va-nguoi-dung/) |
| Phân quyền & quản lý quyền | Định nghĩa role/chức danh + quyền chức năng + copy quyền | [phan-quyen-va-quan-ly-quyen/](phan-quyen-va-quan-ly-quyen/) |
| Quản lý trang & giao diện | Khai báo trang web/app, menu và quyền truy cập từng trang | [quan-ly-trang-va-giao-dien/](quan-ly-trang-va-giao-dien/) |
| Quản lý ngôn ngữ | Bản dịch nhãn giao diện (i18n) theo key + sinh file ngôn ngữ | [quan-ly-ngon-ngu/](quan-ly-ngon-ngu/) |
| Mã giới thiệu (đại lý) | Mã referral cho CTV/đại lý: chiết khấu, hoa hồng, duyệt thanh toán | [ma-gioi-thieu-dai-ly/](ma-gioi-thieu-dai-ly/) |

## Duyệt giao dịch & cấu hình kênh
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Phê duyệt giao dịch tổng đài | Duyệt nạp tiền tổng đài IP của đơn vị → cộng số dư | [phe-duyet-giao-dich-tong-dai/](phe-duyet-giao-dich-tong-dai/) |
| Quản lý cấu hình SMS | Phê duyệt đơn vị được dùng tài khoản SMS/đại lý để gửi tin | [quan-ly-cau-hinh-sms/](quan-ly-cau-hinh-sms/) |
| Mẫu quản lý thông báo | Quản lý mã/nội dung mẫu thông báo cấp hệ thống | [mau-quan-ly-thong-bao/](mau-quan-ly-thong-bao/) |

## Cấu hình, đồng bộ & vận hành
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Cấu hình chung hệ thống | Tham số dùng chung toàn hệ thống (code → value), whitelist domain/CORS | [cau-hinh-chung-he-thong/](cau-hinh-chung-he-thong/) |
| Cấu hình Elasticsearch | Khai báo đơn vị dùng ES + reindex/xóa index theo loại dữ liệu | [cau-hinh-elasticsearch/](cau-hinh-elasticsearch/) |
| Đồng bộ dữ liệu | Đăng ký đơn vị dùng Marketing Campaign + sync lại dữ liệu khách hàng theo đơn vị | [dong-bo-du-lieu/](dong-bo-du-lieu/) |
| Quản lý log & nhật ký | Tra log data tích hợp (delivery/ecommerce) + gửi lại (resend) bản ghi lỗi | [quan-ly-log-va-nhat-ky/](quan-ly-log-va-nhat-ky/) |
| Rollback xử lý đơn | Cơ chế nền hoàn tác đơn (hủy/hủy thanh toán/trả khách) — trả ngược tồn kho, điểm, KM, thu/chi… | [rollback-xu-ly-don/](rollback-xu-ly-don/) |
