# Luồng — Hóa đơn mua bán (Phiếu mua / bán / trả lại hàng mua)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Phiếu mua hàng: cần nhà cung cấp, kho, và danh sách sản phẩm hợp lệ (lô-date đồng nhất nếu áp dụng).
- Trả lại hàng mua: phải có **phiếu mua gốc đã Hoàn thành** và còn sản phẩm chưa trả hết.
- Phiếu bán hàng: phát sinh từ luồng đơn hàng/giao vận (ít thao tác tay).

## Luồng dùng — Phiếu mua hàng
1. Mở **Kế toán → Chứng từ mua hàng → Phiếu mua hàng**, lọc theo loại ngày / kho / NCC / trạng thái / từ khóa. Người thường bị ép xem theo chi nhánh mình; super/CSKH xem tất cả; admin chọn chi nhánh.
2. **Thêm mới**: chọn loại mua (1=vào kho, 2=cũ đổi mới, 3=mua từ khách, 4=nội bộ không nhập kho), NCC, kho, dòng SP, có tạo phiếu chi hay không.
3. Lưu:
   - Trạng thái **Nháp** → chỉ lưu phiếu, không sinh phiếu khác.
   - Trạng thái khác → trong cùng một giao dịch: (a) nếu loại ≠ 4 → sinh **phiếu nhập kho**; (b) nếu tick → sinh **phiếu chi**; (c) nếu có NCC → ghi **công nợ NCC (331)**. Sau khi lưu xong → sinh **bút toán** + cập nhật **đơn đặt hàng** (số lượng đã nhận) nếu phiếu tạo từ đơn đặt.
4. **Hoàn thành**: gọi kho hoàn thành phiếu nhập (cập nhật tồn kho, giá vốn, IMEI, lô-date). Kho callback ngược → đặt phiếu mua = Hoàn thành + sinh bút toán + đẩy MISA.
5. **Hủy**: pre-check phiếu chi (nếu đã xử lý xong → chặn) → hủy phiếu nhập (kho) trước → hủy phiếu chi (Branch) → đặt phiếu mua = Hủy.

## Luồng dùng — Phiếu bán hàng
1. Đơn hàng/giao vận hoàn tất → hệ thống tự tạo **phiếu bán hàng** (luồng nội bộ), sinh bút toán doanh thu/giá vốn, phiếu xuất kho (mua cũ đổi mới), phiếu thu, chứng từ khác (chuyển nợ / giảm doanh thu phí ship), đẩy MISA.
2. Người dùng vào màn phiếu bán để **lọc / xem chi tiết / xem lịch sử (MongoDB) / thống kê tình trạng hóa đơn / xuất Excel**.
3. Khi thu tiền xong (Branch callback) → phiếu bán chuyển **Hoàn thành** khi đủ điều kiện.
4. Khi xuất hóa đơn → đánh dấu "Đã xuất" hoặc "Xuất muộn" (so với ngày tiêu chuẩn xuất hóa đơn).
5. Khách trả hàng → **Tạo phiếu bán trả lại**: sinh tới 3 chứng từ (phiếu bán giảm trừ công nợ, phiếu chi hoàn tiền, phiếu nhập kho).

## Luồng dùng — Trả lại hàng mua
1. Mở **Trả lại hàng mua**, bấm **Tạo**, chọn phiếu mua gốc (autocomplete chỉ hiện phiếu đã Hoàn thành còn SL chưa trả hết).
2. Hệ thống hiện sản phẩm còn được trả (đã trừ số đã trả ở các phiếu trước).
3. Chọn SL trả, hình thức hoàn tiền (Thu lại tiền mặt / Giảm trừ công nợ), có trả hàng về kho (`IsTraLaiHangTrongKho`) hay không.
4. Lưu — nếu trạng thái = **Hoàn thành** mới sinh ripple: phiếu xuất kho (nếu trả về kho), phiếu thu (nếu thu lại tiền mặt), bút toán nghịch + giảm công nợ phải trả + đồng bộ MISA (nếu giảm trừ công nợ).

## Các trường hợp / case biên
- **PMH trạng thái Nháp → bấm Hoàn thành:** bị chặn (phải "đang xử lý" trước). Lưu ý enum trạng thái: ChuaXuLy=1, DangXuLy=2, DaHoanThanh=3, HuyBo=4, **Nhap=5** (Nháp xếp sau Hủy, không phải đầu).
- **PMH Hoàn thành nhưng kho trả Error** (IMEI thiếu): không hoàn thành. Nếu kho đã Completed/AlreadyCompleted (callback đã set) → chỉ đảm bảo phiếu = Hoàn thành, KHÔNG chạy lại body để tránh double bút toán.
- **PMH loại 4 (nội bộ, không nhập kho):** hoàn thành trực tiếp, không qua phiếu nhập.
- **PMH Hủy khi phiếu nhập đã hoàn thành:** bị chặn (tránh sai tồn kho).
- **TLHM trạng thái ChuaXuLy/DangXuLy:** chỉ lưu nháp, KHÔNG tác động kho/tiền/bút toán.
- **TLHM Update sau khi Hoàn thành:** không cho (đóng băng); Update khi chưa hoàn thành chỉ sửa metadata (địa chỉ NCC, thông tin hóa đơn) — không đổi dòng SP/tổng tiền/trạng thái → không ripple.
- **PBH:** phương thức tạo tay `ThemMoiAsync` đánh dấu "code cũ không dùng nữa" → luồng thật là tự sinh từ đơn (`PhieuBanHang_SaveTuDongChoDonHang`).

## Ví dụ cụ thể có số
- **Mua hàng:** PMH loại 1, 100 sản phẩm × 50.000đ = 5.000.000đ, tick tạo phiếu chi. Lưu trạng thái "đang xử lý" → sinh phiếu nhập 100 SP + phiếu chi 5.000.000đ + ghi nợ NCC 5.000.000đ (TK 331). Bấm Hoàn thành → kho cập nhật tồn +100, giá vốn 50.000đ/SP, sinh bút toán Nợ kho / Có 331.
- **Trả lại hàng mua:** phiếu mua gốc đã hoàn thành 100 SP. Tạo TLHM trả 10 SP × 50.000đ = 500.000đ, hình thức "Giảm trừ công nợ", có trả về kho. Đặt trạng thái Hoàn thành → phiếu xuất kho 10 SP (tồn −10) + bút toán nghịch + giảm công nợ phải trả NCC 500.000đ. Lần sau autocomplete chỉ còn 90 SP được trả.

## Hậu điều kiện
- **PMH Thêm (≠ Nháp):** có phiếu nhập + (tùy chọn) phiếu chi + công nợ NCC + bút toán + cập nhật đơn đặt hàng.
- **PMH Hoàn thành:** tồn kho/giá vốn cập nhật, phiếu = Hoàn thành, bút toán + MISA.
- **PMH Hủy:** phiếu nhập + phiếu chi liên kết bị hủy, phiếu = Hủy.
- **PBH tự sinh:** bút toán doanh thu/giá vốn + (tùy) phiếu xuất/thu + chứng từ khác + MISA.
- **TLHM Hoàn thành:** phiếu xuất/thu/bút toán nghịch + giảm công nợ NCC + MISA. Trước Hoàn thành = read-only với kho/tiền.
