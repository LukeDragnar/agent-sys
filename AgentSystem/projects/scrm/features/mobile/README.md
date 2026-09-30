# Module: Mobile (app di động)

> **Mobile = bản app di động MIRROR (lặp lại) các tính năng đã có trên web** — Marketing, Sales, Kho, Khách hàng, Kế toán, Cửa hàng, Nhân sự, Báo cáo... App di động chỉ là **một cửa sổ khác** vào cùng nghiệp vụ; hành vi/quyền/luồng **giống feature web tương ứng**.
>
> **Vì vậy KHÔNG viết lại 4 file (`summarize`/`faq`/`flow`/`dev`) cho Mobile.** Mỗi dòng feature Mobile chỉ **trỏ về feature web** đã có. Khi khách/chatbot hỏi "app làm được gì?" → đọc đúng feature web tương ứng trong bảng dưới.
>
> Khác biệt giữa app và web (nếu có — ví dụ: UI rút gọn, vài thao tác mobile-only như quét wifi chấm công, ghi hình đóng hàng) sẽ ghi bổ sung tại **feature web đó** (mục "Cần chú ý"), không tách tài liệu riêng cho mobile.

## Bảng mapping: feature Mobile → feature web tương ứng

| Feature Mobile (INDEX) | Là gì | Trỏ về feature web |
|---|---|---|
| Quản lý Leads & Marketing | Theo dõi/phân bổ lead từ kênh marketing, dashboard MKT, voucher | [../marketing/quan-ly-lead/](../marketing/quan-ly-lead/) · [../marketing/bao-cao-marketing/](../marketing/bao-cao-marketing/) · [../marketing/quan-ly-nguon-du-lieu/](../marketing/quan-ly-nguon-du-lieu/) · [../marketing/quan-ly-voucher/](../marketing/quan-ly-voucher/) |
| Danh sách khách hàng | Tra thông tin KH + lịch sử giao dịch (CRM 360°) | [../khach-hang/quan-ly-thong-tin-kh/](../khach-hang/quan-ly-thong-tin-kh/) · [../sales/quan-ly-contact/](../sales/quan-ly-contact/) |
| Đơn hàng & Bán hàng | Tạo/xử lý đơn + thanh toán tại quầy (POS) | [../sales/quan-ly-don-hang/](../sales/quan-ly-don-hang/) · [../cua-hang/tao-quan-ly-don-hang/](../cua-hang/tao-quan-ly-don-hang/) |
| Lịch hẹn & Booking | Lên lịch phục vụ + nhắc, duyệt lịch hẹn | [../sales/lich-hen-gap-khach/](../sales/lich-hen-gap-khach/) · [../cua-hang/lich-hen-khach-hang/](../cua-hang/lich-hen-khach-hang/) |
| ChatHub & tương tác | Quản lý tin nhắn Facebook/messenger | [../cong-viec/quan-ly-chat/](../cong-viec/quan-ly-chat/) · [../sales/chathub-sale/](../sales/chathub-sale/) · [../marketing/chathub-marketing/](../marketing/chathub-marketing/) |
| Kiểm tra tồn kho | Xem tồn theo chi nhánh | [../kho/quan-ly-ton-kho/](../kho/quan-ly-ton-kho/) |
| Kho vận | Phiếu nhập xuất + vận đơn + ghi hình đóng hàng (mobile-only) | [../kho/quan-ly-van-don/](../kho/quan-ly-van-don/) · [../kho/quan-ly-ton-kho/](../kho/quan-ly-ton-kho/) |
| Sản phẩm & Combo | Danh mục SP/DV/combo/thẻ | [../kho/quan-ly-san-pham/](../kho/quan-ly-san-pham/) |
| Báo cáo kho | Thống kê nhập xuất, dự báo hết hàng | [../kho/bao-cao-kho/](../kho/bao-cao-kho/) |
| Thu ngân & bàn giao ca | Giao dịch + bàn giao ca + doanh số | [../cua-hang/quan-ly-ca-lam-viec/](../cua-hang/quan-ly-ca-lam-viec/) · [../cua-hang/bao-cao-thong-ke/](../cua-hang/bao-cao-thong-ke/) |
| Kế toán & đối soát | Thu chi + đối soát đơn | [../ke-toan/thu-chi/](../ke-toan/thu-chi/) · [../ke-toan/doi-soat-don-online/](../ke-toan/doi-soat-don-online/) |
| Nhân viên & chấm công | Hồ sơ NV + chấm công + quét wifi (mobile-only) | [../nhan-su/quan-ly-ho-so-nhan-vien/](../nhan-su/quan-ly-ho-so-nhan-vien/) · [../nhan-su/quan-ly-cham-cong/](../nhan-su/quan-ly-cham-cong/) |
| Báo cáo (chấm công/hoa hồng/cửa hàng/online/booking/CEO) | Bộ báo cáo bản mobile (nhiều loại) | [../bao-cao/](../bao-cao/) (chọn báo cáo tương ứng) · [../cua-hang/bao-cao-thong-ke/](../cua-hang/bao-cao-thong-ke/) · [../nhan-su/thuong-phat-hoa-hong/](../nhan-su/thuong-phat-hoa-hong/) |
| Trợ lý AI (G.R.A.I.N) | **Placeholder — chưa phát triển.** Trợ lý/chatbot AI dự kiến; hiện chưa có tính năng thật. | — (chưa có) |
| Quyền User & Admin | Cấu hình quyền theo user/cấp | [../he-thong/phan-quyen-va-quan-ly-quyen/](../he-thong/phan-quyen-va-quan-ly-quyen/) · [../cai-dat/quan-ly-quyen-tai-khoan/](../cai-dat/quan-ly-quyen-tai-khoan/) |

## Ghi chú
- Mobile **mirror** web → mọi câu hỏi "app làm được gì / thêm-sửa-xóa được không" trả lời bằng cách đọc feature web tương ứng (bảng trên).
- **Trợ lý AI (G.R.A.I.N)** = placeholder, chưa phát triển → không có tài liệu, không hứa hẹn tính năng.
- Thao tác **mobile-only** (quét wifi chấm công, ghi hình đóng hàng...) ghi bổ sung tại mục "Cần chú ý" của feature web liên quan, không tách folder riêng.

> Khung trích từ bảng `Page` (menu thật) — xem [../INDEX.md](../INDEX.md) phần "Mobile".
