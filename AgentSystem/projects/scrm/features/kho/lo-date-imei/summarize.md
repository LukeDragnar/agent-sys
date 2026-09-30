---
alias: ["quản lý lô date", "quản lý hạn sử dụng", "imei", "seri sản phẩm", "quản lý số seri", "lô sản xuất", "barcode hàng hóa"]
module: kho
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận (cần quyền nhóm kho — DV-QL-KHO, DV-KT, DV-AD...)
phu_thuoc: [Quản lý sản phẩm (bật cờ "SP quản lý IMEI" / "SP quản lý lô date"), Phiếu nhập kho, Phiếu xuất kho, Phiếu chuyển kho]
link_flow: ./flow.md
---

# Quản lý lô date & Imei

## Là gì
Hai cách **theo dõi hàng tới từng đơn vị cụ thể** thay vì chỉ đếm số lượng:

- **IMEI / số seri**: mỗi sản phẩm là **một mã riêng biệt** (máy, thiết bị điện tử...). Phần mềm biết chính xác *cái máy mã X này* đang ở kho nào, đã bán cho ai.
- **Lô date**: hàng cùng loại được gom theo **lô sản xuất** kèm **ngày sản xuất – hạn sử dụng** (mỹ phẩm, thực phẩm chức năng, thuốc, hàng có "date"). Phần mềm biết *lô này còn bao nhiêu, hết hạn ngày nào*.

Nhóm tính năng gồm các màn: Quản lý/Báo cáo sản phẩm lô date, Sinh lô date, Quản lý/Danh sách IMEI, Sinh IMEI.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Bật cờ "quản lý IMEI / lô date" cho sản phẩm | ✅ | làm ở Quản lý sản phẩm; IMEI và lô date không chọn cả hai |
| Nhập tay mã IMEI / thông tin lô khi nhập kho | ✅ | SP đã bật cờ; quyền nhóm kho |
| Sinh tự động mã IMEI (SinhImeiTuDong) | ✅ | tiền tố chỉ là số, tối đa 2 ký tự |
| Sinh tự động mã lô date (SinhLoDateTuDong) | ✅ | hạn dùng > ngày SX; mã lô không trùng trong đơn vị |
| Tra cứu / lọc danh sách IMEI, lô date | ✅ | quyền xem nhóm kho |
| In tem / xuất Excel IMEI, lô date | ✅ | quyền xem |
| Xóa lô date đã sinh | ⚠️ | không xóa được nếu phiếu nhập đã hoàn thành (phải hủy/sửa phiếu trước) |
| Nhập lại IMEI đã "đã xuất kho" / "đang chuyển kho" | ❌ | trạng thái IMEI chặn; kiểm tra trạng thái trước |

## Để làm gì / cho ai
- **Cho chủ shop/spa bán hàng có date** (mỹ phẩm, TPCN, thuốc): biết lô nào sắp hết hạn để đẩy bán trước, tránh tồn hàng quá date.
- **Cho shop bán hàng có số seri** (điện thoại, đồ điện tử, hàng bảo hành): tra cứu nhanh một mã IMEI đang ở đâu, đã bán chưa, để **bảo hành / đổi trả** chính xác.
- **Actor:** nhân viên kho, kế toán kho, quản lý kho, admin chi nhánh.

## Cách dùng (tóm tắt)
1. **Bật cờ cho sản phẩm trước**: trong Quản lý sản phẩm, tick "Sản phẩm quản lý IMEI" hoặc "Sản phẩm quản lý lô date".
2. Khi **nhập kho** sản phẩm đó: nhập tay mã IMEI / thông tin lô (lô SX, ngày SX, hạn dùng), **hoặc** để hệ thống **sinh tự động** mã IMEI / mã lô theo tiền tố cấu hình.
3. Vào các màn quản lý để **tra cứu, lọc, in tem, xuất Excel** danh sách IMEI / lô date.
4. Khi **bán / xuất / chuyển kho**: chọn đúng IMEI hoặc đúng lô để trừ kho.

## Cần chú ý (cạm bẫy)
- **Phải bật cờ "quản lý IMEI/lô date" trên sản phẩm trước** — không bật thì nhập/xuất kho sẽ không yêu cầu IMEI/lô và không có dữ liệu để tra cứu.
- **Tiền tố sinh IMEI/lô date chỉ được là số, tối đa 2 ký tự** — nhập chữ hoặc dài hơn sẽ báo lỗi.
- **Hạn sử dụng phải lớn hơn ngày sản xuất**, nếu không hệ thống chặn.
- **Mã lô sản xuất không được trùng** trong cùng đơn vị (trừ khi cố tình bỏ qua check).
- **Đã sinh mã rồi mà phiếu nhập đã hoàn thành thì không xóa được lô date** — phải hủy/sửa phiếu trước.
- IMEI đã ở trạng thái **"đã xuất kho"/"đang chuyển kho"** thì không nhập lại bình thường được — kiểm tra trạng thái trước khi thao tác.

## Ảnh hưởng tới
- **Quản lý sản phẩm** — nơi bật/tắt cờ IMEI & lô date.
- **Phiếu nhập / xuất / chuyển kho** — nơi mã IMEI và lô date thực sự được tạo, trừ, và di chuyển.
- **Bán hàng (Đơn hàng)** — khi bán SP có IMEI/lô phải chọn đúng mã để xuất.
- **Bảo hành** — tra IMEI để xác định máy còn bảo hành.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md) · Kỹ thuật: [./dev.md](./dev.md)
