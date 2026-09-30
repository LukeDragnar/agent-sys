---
alias: ["công nợ", "bù trừ công nợ", "phiếu bù trừ", "cấn trừ công nợ", "công nợ phải thu", "công nợ phải trả", "131", "331", "báo cáo công nợ tổng hợp"]
module: ke-toan
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Phiếu bù trừ: chỉ màn Danh sách có quyền riêng; lập phiếu / lấy chứng từ / xem chi tiết / xóa chỉ cần đăng nhập (kể cả lập phiếu ghi tài chính — rủi ro, xem dev.md). Báo cáo công nợ tổng hợp: chỉ cần đăng nhập, nhưng phải qua cổng kiểm quyền xem báo cáo đơn hàng (cấu hình bên Đơn hàng)."
phu_thuoc: ["Khách hàng (công nợ phải thu; ghi nhận giảm khi bù trừ)", "Nhà cung cấp / Kho (công nợ phải trả; ghi nhận giảm khi bù trừ)", "Phiếu mua / phiếu bán hàng (nguồn phát sinh công nợ; gắn phiếu bù trừ tự động)", "Bút toán kế toán (hạch toán Nợ/Có khi bù trừ)", "MISA / kết nối kế toán (đẩy phiếu bù trừ)", "Cấu hình quyền xem báo cáo đơn hàng (gate báo cáo công nợ)"]
link_flow: ./flow.md
---

# Công nợ (Bù trừ công nợ + Báo cáo công nợ)

## Là gì
Nhóm tính năng **quản lý công nợ** của đơn vị, gồm 2 phần:
- **Phiếu bù trừ công nợ:** khi cùng một đối tượng vừa **nợ mình** (phải thu) vừa **mình nợ họ** (phải trả), lập phiếu **cấn trừ** để giảm đồng thời cả hai khoản. Ví dụ một đối tác vừa là khách hàng (mình bán cho họ) vừa là nhà cung cấp (mình mua của họ).
- **Báo cáo công nợ tổng hợp:** xem tổng quan công nợ **phải thu (TK 131 — khách hàng)** và **phải trả (TK 331 — nhà cung cấp)** theo từng đối tượng, có thể bấm vào để xem chi tiết.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Lấy chứng từ phải thu / phải trả của một đối tượng | ✅ | chỉ cần đăng nhập; phải chọn đối tượng + tài khoản phải thu/trả + ngày |
| Lập phiếu bù trừ công nợ | ✅ | chỉ cần đăng nhập (lưu ý: thao tác ghi tài chính nhưng không gắn quyền riêng — xem dev.md) |
| Xem danh sách phiếu bù trừ | ✅ | cần quyền xem danh sách |
| Xem chi tiết phiếu bù trừ | ✅ | chỉ cần đăng nhập |
| Xóa phiếu bù trừ | ⚠️ | chỉ cần đăng nhập; **không xóa được phiếu tạo tự động**; xóa là xóa vĩnh viễn + hoàn lại công nợ 2 bên |
| Xem báo cáo công nợ tổng hợp (131 / 331) | ✅ | chỉ cần đăng nhập + qua cổng kiểm quyền xem báo cáo đơn hàng |
| Xem chi tiết công nợ theo đối tượng | ✅ | như trên |
| Sửa số tiền công nợ trực tiếp | ❌ | công nợ phát sinh từ chứng từ gốc (mua/bán/thu/chi); không sửa tay ở đây — chỉ bù trừ hoặc qua chứng từ gốc |

## Để làm gì / cho ai
- **Kế toán:** giảm thủ tục thu/chi qua lại khi đối tác vừa là khách vừa là NCC — cấn trừ một lần; theo dõi tổng phải thu/phải trả; đối chiếu công nợ từng đối tượng.
- **Chủ shop / quản lý:** nắm được mình đang bị nợ bao nhiêu (phải thu) và đang nợ ai (phải trả).

## Cách dùng

**Lập phiếu bù trừ công nợ:**
1. Vào **Kế toán → Phiếu bù trừ công nợ**, bấm lập phiếu mới.
2. Chọn đối tượng (vừa là KH vừa là NCC), tài khoản phải thu / phải trả, ngày bù trừ.
3. Bấm **Lấy chứng từ** → hệ thống gom các chứng từ phải thu (từ Khách hàng) và phải trả (từ Nhà cung cấp) của đối tượng.
4. Chọn các dòng và số tiền cần bù trừ, rồi **Lưu phiếu bù trừ**.
5. Hệ thống giảm đồng thời công nợ phải thu (KH) và phải trả (NCC), sinh bút toán hạch toán, đẩy MISA.

**Xem báo cáo công nợ:**
1. Vào **Kế toán → Báo cáo → Báo cáo công nợ tổng hợp**.
2. Chọn tab **131 (phải thu)** hoặc **331 (phải trả)**, lọc theo chi nhánh, khoảng ngày, nhóm đối tượng, từ khóa.
3. Bảng hiện công nợ từng đối tượng; bấm một đối tượng để xem **chi tiết** từng chứng từ.

## Cần chú ý
- **Bù trừ chỉ thực hiện được khi đối tượng vừa có phải thu vừa có phải trả** (hoặc bạn chọn đúng tài khoản tương ứng). Nếu chỉ có một chiều thì không có gì để cấn trừ.
- **Phiếu bù trừ là thao tác all-or-nothing:** chỉ lưu khi cả hai bên (giảm phải thu KH + giảm phải trả NCC) đều thành công; một bên lỗi thì hủy toàn bộ.
- **Không xóa được phiếu bù trừ tạo tự động** (hệ thống sinh khi mua/bán). Phiếu lập tay thì xóa được, nhưng **xóa là vĩnh viễn** và sẽ **hoàn lại công nợ** đã giảm cho cả hai bên.
- **Báo cáo công nợ đi qua cổng kiểm quyền xem báo cáo đơn hàng** — nếu bạn không có quyền đó thì sẽ bị chặn ("Bạn không có quyền xem báo cáo đơn hàng"), không phải lỗi báo cáo.
- **Tên đối tượng trên báo cáo** được ghép từ dữ liệu khách hàng / nhà cung cấp; nếu một mã trùng cả hai thì hiển thị "KH: ... - NCC: ...".

## Ảnh hưởng tới
- **Công nợ khách hàng (131)** và **công nợ nhà cung cấp (331)** — bù trừ làm giảm cả hai.
- **Bút toán & sổ kế toán**, **đồng bộ MISA**.
- **Phiếu mua / phiếu bán hàng** — phiếu bù trừ tạo tự động được gắn ngược về phiếu gốc.
