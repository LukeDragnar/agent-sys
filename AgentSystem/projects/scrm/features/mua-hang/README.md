# Module: Mua hàng

> Module mua/nhập hàng của SCRM: theo dõi tồn & dự báo nhập hàng, lập đơn đặt hàng (PO) nhập từ nhà cung cấp, quản lý tiền cọc của khách, và các báo cáo phục vụ mua hàng. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý tồn (dự báo) | Theo dõi tồn kho + dự báo khi nào sắp hết, cấu hình ngưỡng tồn min/max/khẩn cấp | [quan-ly-ton-du-bao/](quan-ly-ton-du-bao/) |
| Quản lý đơn đặt hàng | Lập & theo dõi đơn đặt hàng (PO) nhập từ NCC, qua 2 pha duyệt + xử lý mua | [quan-ly-don-dat-hang/](quan-ly-don-dat-hang/) |
| Quản lý đặt cọc | Vòng đời tiền cọc của khách: phiếu cọc, thanh toán, hoàn/thay SP, đợt trả cọc | [quan-ly-dat-coc/](quan-ly-dat-coc/) |
| Báo cáo mua hàng | Nhóm báo cáo phục vụ mua/nhập hàng (đặt cọc, tồn kho) — chưa có màn tổng hợp riêng | [bao-cao-mua-hang/](bao-cao-mua-hang/) |

> Khung trích từ bảng `Page` (menu thật). "Báo cáo mua hàng" hiện ghi "(chưa gate)" — chưa có controller tổng hợp riêng, chỉ gom báo cáo rời.
