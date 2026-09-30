# Module: Sản Xuất

> Quản lý sản xuất của phần mềm SCRM: từ đơn đặt hàng → yêu cầu sản xuất (PR) → lệnh công việc (Work Order) → chạy quy trình theo BOM (định mức). Module còn **sơ khai (level 1)** — nhiều nút để quyền rỗng, một số entity nghi legacy; nhiều mục để "cần product xác nhận". Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý sản xuất | Đơn đặt hàng → PR → Work Order → chạy bước (xuất NVL / nhập thành phẩm) | [quan-ly-san-xuat/](quan-ly-san-xuat/) |
| Thiết lập BOM | Định mức NVL + quy trình công đoạn để sản xuất 1 sản phẩm | [thiet-lap-bom/](thiet-lap-bom/) |
| Lịch sử sản xuất | Tra cứu lịch sử Đơn đặt hàng / Yêu cầu sản xuất / Lệnh công việc | [lich-su-san-xuat/](lich-su-san-xuat/) |

> Khung trích từ INDEX.md (phần "Sản Xuất") + quét registry `registry/san-xuat/` (8 node) + source BE `SanXuat` / FE `pages/sanxuat`.
> Registry node liên quan: `ProductionOrderController`, `BomSetupController`, `DmCongDoanController`, `SaleOrderDonHangController`, `SaleOrderDonHangChiTietController`, `SaleOrderProductionController`, `WorkOrderController`, `InPhieuController`.
