---
alias: ["chứng từ mua dịch vụ", "phân bổ chi phí mua hàng", "chi phí mua hàng", "chi phí vận chuyển nhập hàng", "landed cost", "MDV", "cộng phí vào giá vốn", "mua dịch vụ"]
module: ke-toan
trang_thai: mới (release_product 2026-06-26) — cần product xác nhận gói/quyền
goi: "cần product xác nhận — controller Chứng từ mua dịch vụ gắn TienIchAttribute gói Kế toán; controller Phân bổ (ChiPhiMuaHang) KHÔNG gate gói"
quyen: "cần product xác nhận — toàn bộ endpoint (Lưu/Xóa/Hủy chứng từ; Phân bổ/Xóa phân bổ) chỉ cần đăng nhập, KHÔNG có quyền key riêng (rủi ro phân quyền, xem dev.md)"
phu_thuoc: ["Nhà cung cấp (đối tượng chứng từ + công nợ 331)", "Thu chi (sinh phiếu chi khi thanh toán ngay)", "Bút toán kế toán", "Phiếu mua hàng (nơi nhận phân bổ chi phí)", "Kho / Phiếu nhập (chi phí cộng vào giá trị nhập kho + giá vốn)", "MISA / kết nối kế toán (đồng bộ chứng từ)"]
link_flow: ./flow.md
---

# Chứng từ mua dịch vụ & Phân bổ chi phí mua hàng (landed cost)

## Là gì
Hai phần liên kết, giải bài toán **cộng chi phí mua vào giá vốn hàng nhập** (landed cost):
- **Chứng từ mua dịch vụ (MDV):** chứng từ ghi nhận **mua dịch vụ** phục vụ nhập hàng — phí vận chuyển, bốc xếp, kiểm định, thuê ngoài... Cấu trúc giống phiếu mua hàng (nhà cung cấp, nhân viên mua, thanh toán ngay hoặc công nợ), kéo theo **phiếu chi / công nợ NCC / bút toán** và đồng bộ MISA. Mỗi chứng từ có thể tick **"Là chi phí mua hàng"**.
- **Phân bổ chi phí mua hàng:** với chứng từ đã tick "Là chi phí mua hàng" và **đã hạch toán**, kế toán vào **form Phiếu mua hàng** chọn chứng từ chi phí khả dụng và **phân bổ** số tiền chi phí xuống các dòng sản phẩm của phiếu → chi phí cộng vào **giá trị nhập kho và giá vốn** sản phẩm.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Lập chứng từ mua dịch vụ | ✅ | chỉ cần đăng nhập; ≥1 dòng dịch vụ hợp lệ; mã tự sinh (prefix MDV) nếu để trống |
| Đánh dấu "Là chi phí mua hàng" | ✅ | tick trên form → chứng từ mới được đem đi phân bổ |
| Sửa chứng từ | ⚠️ | chỉ khi **chưa phân bổ** và **chưa hạch toán / chưa hủy**; đã phân bổ/hạch toán → chặn sửa |
| Xóa chứng từ | ⚠️ | chỉ khi **chưa hạch toán**; đã hạch toán → chặn xóa (dùng Hủy) |
| Hủy chứng từ | ✅ | set trạng thái Đã hủy + thu hồi bút toán |
| Phân bổ chi phí vào phiếu mua hàng | ✅ | chứng từ phải **đã hạch toán** + còn dư; **không phân bổ vượt** tổng tiền chứng từ; phiếu mua **chưa hoàn thành** |
| Xóa phân bổ | ✅ | phiếu mua **chưa hoàn thành**; hoàn lại số dư chứng từ + tính lại chi phí dòng SP |
| Phân bổ lại chứng từ đã hoàn thành phiếu | ❌ | phiếu mua đã **Hoàn thành** (khóa kho/giá vốn) → không đổi phân bổ được (Rule E) |

## Để làm gì / cho ai
- **Kế toán / kế toán kho:** phản ánh **đúng giá vốn** — phí vận chuyển & dịch vụ nhập hàng được cộng vào giá trị hàng tồn thay vì hạch toán rời, giúp báo cáo lãi/lỗ chính xác.
- **Mua hàng:** một hóa đơn vận chuyển có thể chia cho **nhiều phiếu mua**; một phiếu mua có thể gánh chi phí từ **nhiều chứng từ dịch vụ**.

## Cách dùng
1. **Lập chứng từ mua dịch vụ** (Kế toán → Chứng từ mua dịch vụ): chọn NCC, nhập các dòng dịch vụ (tiền hàng, chiết khấu, thuế GTGT), chọn thanh toán ngay (sinh phiếu chi) hay công nợ. **Tick "Là chi phí mua hàng"**. Hạch toán chứng từ.
2. **Phân bổ** (trong form Phiếu mua hàng): mở popup phân bổ chi phí → chọn các chứng từ chi phí khả dụng (đã hạch toán, còn dư) → nhập số tiền phân bổ cho phiếu này.
3. Chọn **phương thức trải xuống dòng SP**: theo đơn giá (mặc định), theo số lượng, hoặc tự nhập từng dòng.
4. Lưu — chi phí cộng vào `Tổng chi phí mua hàng` của phiếu + từng dòng sản phẩm (`Chi phí mua hàng` + `% chi phí`), và vào **giá trị nhập kho + giá vốn** khi phiếu nhập được tạo/cập nhật.

## Cần chú ý
- **Chỉ phân bổ được chứng từ ĐÃ HẠCH TOÁN + còn dư** (còn `Còn lại phân bổ` > 0). Chứng từ nháp không hiện trong danh sách khả dụng.
- **Không phân bổ vượt tổng tiền chứng từ** — tổng phân bổ trên mọi phiếu ≤ tổng tiền chứng từ; báo lỗi kèm "còn lại {số}" nếu vượt.
- **Phiếu mua đã Hoàn thành thì đóng băng chi phí** — không phân bổ thêm/xóa phân bổ (vì kho + giá vốn đã khóa). Phải phân bổ TRƯỚC khi hoàn thành phiếu (hoặc ngay trong lúc lưu phiếu).
- **Sửa chứng từ đã phân bổ:** phải **xóa phân bổ trước** rồi mới sửa được header chứng từ.
- **Phân quyền chưa chặt:** mọi thao tác chỉ cần đăng nhập (không key riêng) — `cần product xác nhận`.

## Ảnh hưởng tới
- **Phiếu mua hàng** (`Tổng chi phí mua hàng` + dòng SP `Chi phí mua hàng`/`% chi phí`).
- **Kho / Phiếu nhập** (chi phí cộng vào giá trị nhập kho + **giá vốn**).
- **Thu chi** (phiếu chi khi thanh toán ngay), **Công nợ NCC** (331), **Bút toán & MISA**.
