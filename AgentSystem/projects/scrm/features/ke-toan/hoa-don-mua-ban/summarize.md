---
alias: ["phiếu mua hàng", "phiếu bán hàng", "chứng từ mua bán", "trả lại hàng mua", "hóa đơn mua bán", "mua hàng vào kho", "PMH", "PBH", "TLHM", "nhập mua", "trả hàng nhà cung cấp"]
module: ke-toan
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Phiếu mua hàng có phân quyền theo từng thao tác (Tìm kiếm / Thêm / Xem / Sửa); riêng Xóa/Hoàn thành/Hủy/Xuất Excel chỉ cần đăng nhập (không gắn quyền riêng — xem dev.md). Phiếu bán hàng phần lớn sinh tự động từ đơn hàng; chỉ tìm kiếm/xem chi tiết có quyền riêng, các thao tác khác chỉ cần đăng nhập. Trả lại hàng mua: cả 8 thao tác chỉ cần đăng nhập."
phu_thuoc: ["Kho (sinh phiếu nhập / phiếu xuất; giá vốn; IMEI, lô-date)", "Thu chi (sinh phiếu thu / phiếu chi)", "Công nợ nhà cung cấp & khách hàng", "Bút toán kế toán (doanh thu / giá vốn / nghịch mua)", "Đơn đặt hàng (tạo phiếu mua từ đơn đặt; cập nhật số lượng đã nhận)", "Đơn hàng / Logistics (sinh phiếu bán hàng tự động)", "MISA / kết nối kế toán (đồng bộ chứng từ)"]
link_flow: ./flow.md
---

# Hóa đơn mua bán (Phiếu mua hàng · Phiếu bán hàng · Trả lại hàng mua)

## Là gì
Nhóm **chứng từ kế toán cho hoạt động mua – bán hàng**, gồm 3 loại phiếu:
- **Phiếu mua hàng (PMH):** chứng từ khi mua hàng vào (mua vào kho, mua cũ đổi mới, mua từ khách, mua hàng/dịch vụ nội bộ). Là chứng từ gốc **kéo theo** phiếu nhập kho, phiếu chi, công nợ nhà cung cấp và bút toán kế toán.
- **Phiếu bán hàng (PBH):** chứng từ kế toán khi bán hàng, **phần lớn sinh tự động** khi đơn hàng/giao vận hoàn tất. Kéo theo bút toán doanh thu/giá vốn, phiếu xuất kho, phiếu thu, và theo dõi tình trạng xuất hóa đơn.
- **Trả lại hàng mua (TLHM):** chứng từ trả hàng cho nhà cung cấp — nghịch đảo phiếu mua hàng. Kéo theo phiếu xuất kho (hàng rời kho), phiếu thu (NCC hoàn tiền) hoặc giảm công nợ phải trả.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| (PMH) Tìm/lọc danh sách phiếu mua | ✅ | cần quyền xem phiếu mua hàng |
| (PMH) Thêm phiếu mua | ✅ | cần quyền thêm; cần NCC/kho/dòng SP hợp lệ |
| (PMH) Sửa phiếu mua | ✅ | cần quyền sửa; phiếu chưa hoàn thành |
| (PMH) Xóa phiếu mua | ⚠️ | chỉ cần đăng nhập (không gắn quyền riêng — rủi ro phân quyền, xem dev.md) |
| (PMH) Hoàn thành phiếu | ✅ | chỉ cần đăng nhập; phiếu phải đang xử lý/chưa xử lý — KHÔNG hoàn thành từ trạng thái Nháp |
| (PMH) Hủy phiếu | ✅ | chỉ cần đăng nhập; phiếu chưa hoàn thành / chưa hủy |
| (PMH) Nhập / Xuất Excel | ✅ | chỉ cần đăng nhập; xuất rỗng → "Không có dữ liệu" |
| (PBH) Tìm/lọc, xem chi tiết phiếu bán | ✅ | cần quyền xem phiếu bán hàng |
| (PBH) Tạo phiếu bán trả lại | ✅ | chỉ cần đăng nhập; trên phiếu đã bán |
| (PBH) Xem lịch sử / thống kê / xuất Excel phiếu bán | ✅ | chỉ cần đăng nhập |
| (PBH) Tự gõ phiếu bán mới ở màn này | ❌ | phiếu bán **sinh tự động** từ luồng đơn hàng — không nhập tay ở màn kế toán |
| (TLHM) Tạo / sửa / xem / xuất Excel trả lại hàng mua | ✅ | chỉ cần đăng nhập; phải chọn từ phiếu mua gốc đã hoàn thành; sửa chỉ khi chưa hoàn thành |

## Để làm gì / cho ai
- **Kế toán:** có đầy đủ chứng từ mua – bán – trả lại để hạch toán, theo dõi công nợ NCC/khách, và tình trạng xuất hóa đơn.
- **Thủ kho / mua hàng:** lập phiếu mua → tự sinh phiếu nhập kho; trả hàng NCC → tự sinh phiếu xuất.
- **Chủ shop / quản lý:** nắm chi phí mua vào, doanh thu bán ra, hàng trả lại; đối chiếu với MISA.

## Cách dùng

**Phiếu mua hàng:**
1. Vào **Kế toán → Chứng từ mua hàng → Phiếu mua hàng**.
2. Lọc theo loại ngày, khoảng ngày, kho, nhà cung cấp, trạng thái, từ khóa.
3. Bấm **Thêm mới**: chọn nhà cung cấp, kho, nhập các dòng sản phẩm, chọn có tạo phiếu chi hay không. Lưu ở trạng thái Nháp (chưa sinh phiếu khác) hoặc trạng thái khác (tự sinh phiếu nhập + chi + công nợ).
4. Bấm **Hoàn thành** để chốt (đi qua kho); **Hủy** để hủy cả các phiếu liên kết.
5. Có thể **Nhập Excel** (theo file mẫu) hoặc **Xuất Excel**.

**Phiếu bán hàng:** chủ yếu **xem/lọc/xuất Excel**, xem lịch sử thay đổi, thống kê tình trạng hóa đơn (đã xuất / chưa xuất / xuất muộn), và **tạo phiếu bán trả lại** khi khách trả hàng.

**Trả lại hàng mua:**
1. Vào **Kế toán → Chứng từ mua hàng → Trả lại hàng mua**.
2. Bấm **Tạo**, chọn phiếu mua gốc (đã hoàn thành) → hệ thống hiện các sản phẩm còn được trả.
3. Chọn số lượng trả, hình thức hoàn tiền (thu lại tiền mặt / giảm trừ công nợ), có trả hàng về kho hay không.
4. Khi phiếu ở trạng thái **Hoàn thành** mới sinh các phiếu liên quan.

## Cần chú ý
- **Phiếu mua hàng là chứng từ gốc:** một thao tác (thêm/hoàn thành/hủy) có thể kéo theo phiếu nhập kho, phiếu chi, công nợ NCC, bút toán. Hủy phiếu mua sẽ hủy cả các phiếu liên kết — nhưng nếu phiếu nhập đã hoàn thành thì bị chặn.
- **Không hoàn thành được phiếu mua đang ở trạng thái Nháp** — phải chuyển sang "đang xử lý" trước.
- **Phiếu bán hàng phần lớn tự sinh:** đừng tìm nút "tạo phiếu bán" thủ công ở màn kế toán; phiếu phát sinh từ đơn hàng/giao vận.
- **Trả lại hàng mua chỉ tác động khi trạng thái = Hoàn thành.** Lưu nháp (chưa xử lý/đang xử lý) không động đến kho/tiền. Đã hoàn thành thì **không sửa được nữa** (đóng băng).
- **Có lỗi đã biết ở Trả lại hàng mua** (bút toán/đồng bộ MISA có thể chạy nhầm khi lưu phiếu fail — xem dev.md). `cần product xác nhận` về hành vi đúng khi gặp.
- **Phân quyền chưa chặt:** nhiều thao tác quan trọng (xóa/hoàn thành/hủy/tạo trả lại) hiện chỉ cần đăng nhập, không có quyền riêng. `cần product xác nhận`.
- **Chi phí mua hàng (landed cost):** phiếu mua có thể **nhận phân bổ chi phí** từ chứng từ mua dịch vụ (phí vận chuyển/bốc xếp...) → cộng vào giá trị nhập kho & giá vốn. Xem feature [Chứng từ mua dịch vụ & phân bổ chi phí mua hàng](../phan-bo-chi-phi-mua-hang/summarize.md). Phân bổ phải làm **trước khi hoàn thành phiếu** (hoàn thành → khóa giá vốn).
- **Thông báo cho kho:** khi tạo phiếu mua (khác Nháp) hoặc chuyển Nháp→đang xử lý, hệ thống gửi thông báo cho các role đã cấu hình ở "Cấu hình mua bán" (chỉ báo 1 lần, không lặp mỗi lần sửa).

## Ảnh hưởng tới
- **Kho** (phiếu nhập / phiếu xuất, tồn kho, giá vốn, IMEI, lô-date).
- **Thu chi** (phiếu thu / phiếu chi).
- **Công nợ** nhà cung cấp (331) và khách hàng (131).
- **Bút toán & báo cáo kế toán**, **đồng bộ MISA**.
- **Đơn đặt hàng** (số lượng đã nhận) và **đơn hàng** (phiếu bán tự sinh).
