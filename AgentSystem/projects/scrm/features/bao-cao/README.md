# Module: Báo cáo

> Module báo cáo tổng của SCRM: gom các bộ báo cáo phân tích doanh số, khách hàng, sản phẩm, sale/marketing, giao vận, tài chính, KPI và dashboard lãnh đạo. Hầu hết chỉ đọc, lọc theo chi nhánh + khoảng ngày, đa số có Xuất Excel. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.
>
> ⚠️ **Lưu ý trùng lặp:** module Báo cáo gom góc nhìn "đọc/báo cáo"; nhiều báo cáo ở đây cũng xuất hiện trong module gốc (Khách hàng, Cửa hàng, Sales, Kế toán, Nhân sự...) vì cùng đọc một nguồn dữ liệu. Mỗi feature có mục "Ảnh hưởng tới / trùng lặp" trỏ về tài liệu gốc.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Dashboard tổng quan / CEO | Bảng điều khiển toàn cảnh kinh doanh cho lãnh đạo, gom nhiều nhóm báo cáo vào 1 màn | [dashboard-ceo/](dashboard-ceo/) |
| Báo cáo doanh số | Doanh thu theo ngày/chi nhánh/khách/sản phẩm/nhân viên/nguồn/giao vận | [bao-cao-doanh-so/](bao-cao-doanh-so/) |
| Báo cáo tổng hợp bán hàng | Gộp nhiều chỉ số bán hàng + tác nghiệp vào một bảng tổng quát | [bao-cao-tong-hop-ban-hang/](bao-cao-tong-hop-ban-hang/) |
| Báo cáo khách hàng | Phân tích hành vi mua, khách cũ/mới, khách quay lại theo tháng (trùng module Khách hàng) | [bao-cao-khach-hang/](bao-cao-khach-hang/) |
| Báo cáo sản phẩm | Tỉ lệ chốt đơn SP, combo, số ngày SP chưa bán được | [bao-cao-san-pham/](bao-cao-san-pham/) |
| Báo cáo sale & lead | Công việc sale + tối ưu sale, đánh giá hiệu suất đội Sale | [bao-cao-sale-lead/](bao-cao-sale-lead/) |
| Báo cáo marketing | Công việc MKT (view tác nghiệp/sale) + MKT theo nguồn landing | [bao-cao-marketing/](bao-cao-marketing/) |
| Báo cáo bậc thang & KPI | Bảng xếp hạng (BXH) + phong thần bảng Sale/MKT/nhóm theo thành tích | [bao-cao-bac-thang-kpi/](bao-cao-bac-thang-kpi/) |
| Báo cáo giao vận | Doanh số & hiệu suất bán hàng theo từng đơn vị vận chuyển (ĐVVC) | [bao-cao-giao-van/](bao-cao-giao-van/) |
| Báo cáo theo nhãn / phí sàn | Doanh số theo nhãn contact + phí sàn TMĐT (Shopee/TikTok/Lazada) | [bao-cao-theo-nhan-phi-san/](bao-cao-theo-nhan-phi-san/) |
| Báo cáo tài chính (công nợ) | Công nợ phải thu (131) / phải trả (331) — góc xem báo cáo, gốc ở Kế toán | [bao-cao-tai-chinh-cong-no/](bao-cao-tai-chinh-cong-no/) |

> Khung trích từ bảng `Page` (menu thật) + registry node `BaoCaoOnlineController` / `BaoCaoCuaHangController` / `BaoCaoCeoController` / `BaoCaoPhiSanController` / `BaoCaoKeToanController`.
