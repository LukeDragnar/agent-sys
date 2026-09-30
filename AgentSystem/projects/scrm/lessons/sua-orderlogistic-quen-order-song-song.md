# Lesson: Sửa logic xuất HĐ ở OrderLogistic → QUÊN module Order song song

**Sự cố (2026-07-16, task CCCD + B2 nguồn TT người mua):** thêm chip CCCD + cấu hình B2 (đơn thường không yêu cầu xuất HĐ) chỉ sửa `OrderLogistic\...\DonHangInvoiceService.cs`, **bỏ sót `Order\...\DonHangInvoiceService.cs`** (đơn bán lẻ/offline). Suýt ship thiếu hẳn 1 luồng đơn — user bắt lúc review ("sao thấy mỗi change ở logistic"). Đã commit + push staging bản thiếu trước khi phát hiện.

## Gốc rễ
Order (bán lẻ/offline/POS) và OrderLogistic (giao vận/TMĐT) có **logic xuất hóa đơn SONG SONG** — 2 file `DonHangInvoiceService.cs` RIÊNG, cùng bộ hàm:
- `Fill_InvoiceBuyerInfo_Into_InvoiceDonHang` (dựng người mua)
- `DuThongTinBatBuocTheoDon` / `FillBuyerMotPhanMacDinh` / `GetInfoCustomer`
- dispatch `cachLayThongTinKhachHang`

Config đọc từ CÙNG endpoint `Invoice/CauHinh/GetCustomerInfoDefault`; khách lấy qua CÙNG `Customer/api/KhachHang/GetInfoKhachHangInvoiceByList` (nên phần DTO/service chung sửa 1 lần là đủ — chỉ phần TIÊU THỤ trong 2 `DonHangInvoiceService` là phải sửa 2 nơi).

## Cách xử đúng
Sửa logic **nguồn TT người mua / xuất hóa đơn / auto-invoice** ở 1 module → **BẮT BUỘC rà + sửa module kia**. Khác biệt khi port code giữa 2 module:

| | Order (bán lẻ/offline) | OrderLogistic (giao vận/TMĐT) |
|---|---|---|
| Tên/SĐT trên đơn | `TenKhach` / `SoDienThoaiKhach` | `KhachHangTen` / `KhachSoDienThoai` |
| Địa chỉ trên đơn | KHÔNG có (chỉ từ hồ sơ khách) | `GiaoHangDiaChiFullText` |
| Ecom / case 3 (đơn sàn) | KHÔNG (offline không bao giờ là sàn) | CÓ |
| DTO | `OrderInvoiceDto` | `OrderLogisticInvoiceDto` |
| Guard case 2 | cần `!IdKhachHang.GuidIsNullOrEmpty()` (GetInfoCustomer dùng `.Value` → NRE nếu null) | không cần |

→ Rộng hơn: SCRM có nhiều cặp service **song song Order/OrderLogistic** — sửa 1 luôn tự hỏi *"cái kia có bản song song không?"*. Xem checklist `../qa-code.md` mục 14.
