# Module: Kế toán

> Nghiệp vụ kế toán của SCRM: chứng từ mua–bán, thu chi quỹ, công nợ, hóa đơn điện tử, đối soát đơn online, tính lương và báo cáo quỹ — kèm các job chạy nền tự phát hành hóa đơn & đối soát. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Báo cáo quỹ | Sổ quỹ theo tài khoản (tiền mặt/ví/ngân hàng): xem số dư & phát sinh theo đối tượng, drill chi tiết theo mã phiếu; chỉ xem | [bao-cao-quy/](bao-cao-quy/summarize.md) |
| Hóa đơn mua bán | Nhóm chứng từ mua–bán: phiếu mua (nhập kho/chi/công nợ), phiếu bán (tự sinh từ đơn hàng), trả lại hàng mua | [hoa-don-mua-ban/](hoa-don-mua-ban/summarize.md) |
| Thu chi | Dòng tiền vào–ra quỹ (phiếu thu/chi, chuyển nội bộ, tồn quỹ) và khai báo số dư ban đầu; chủ yếu xem/lọc/xuất Excel | [thu-chi/](thu-chi/summarize.md) |
| Chứng từ khác | Chứng từ hạch toán thủ công đa dòng (cặp TK Nợ–Có tự chọn) cho nghiệp vụ không có phiếu chuẩn — bút toán tự do, đồng bộ MISA | [chung-tu-khac/](chung-tu-khac/summarize.md) |
| Công nợ | Quản lý công nợ: lập phiếu bù trừ (cấn trừ phải thu/phải trả cùng đối tượng) và báo cáo công nợ tổng hợp 131/331 | [cong-no/](cong-no/summarize.md) |
| Hóa đơn điện tử | Phát hành & tra cứu HĐĐT qua 2 NCC MISA/MeInvoice và Viettel/SInvoice — khai báo tài khoản, đồng bộ mẫu, xuất HĐ từ màn đơn | [hoa-don-dien-tu/](hoa-don-dien-tu/summarize.md) |
| Tạo hóa đơn tự động | Tự phát sinh HĐĐT (nháp/phát hành thật) cho đơn theo trạng thái giao hàng cấu hình, có độ trễ & chống trùng — không cần bấm tay | [tao-hoa-don-tu-dong/](tao-hoa-don-tu-dong/summarize.md) |
| Đối soát đơn online | Màn tác nghiệp kế toán trên đơn online/logistic: tra soát, đăng/hủy đăng đơn, cập trạng thái giao, ghi chú, blacklist, xuất Excel | [doi-soat-don-online/](doi-soat-don-online/summarize.md) |
| Tính lương | Lập & quản lý bảng lương theo kỳ: tạo lương chi tiết tự động từ cài đặt + công đã chấm, chỉnh tay từng NV, chốt qua các trạng thái | [tinh-luong/](tinh-luong/summarize.md) |
| Tự động hóa đơn & đối soát chạy nền | Phần chạy ngầm (consumer/job): tự phát hành/nháp HĐĐT, tự đối soát đơn hoàn sinh phiếu bán trả lại, đối soát giao dịch ngân hàng MB | [tu-dong-hoa-don-doi-soat-nen/](tu-dong-hoa-don-doi-soat-nen/summarize.md) |
| Chứng từ mua dịch vụ & phân bổ chi phí mua hàng | Chứng từ mua dịch vụ (MDV) tick "Là chi phí mua hàng" → phân bổ vào phiếu mua hàng → cộng phí (vận chuyển/bốc xếp...) vào giá trị nhập kho & giá vốn (landed cost) | [phan-bo-chi-phi-mua-hang/](phan-bo-chi-phi-mua-hang/summarize.md) |

> Khung trích từ bảng `Page` (menu thật). Chi tiết kỹ thuật cũ: [../accounting.md](../accounting.md).
