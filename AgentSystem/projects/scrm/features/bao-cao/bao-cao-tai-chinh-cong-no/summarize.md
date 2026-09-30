---
alias: ["báo cáo tài chính", "báo cáo công nợ", "công nợ phải thu", "công nợ phải trả", "131", "331", "báo cáo công nợ tổng hợp", "báo cáo công nợ trong báo cáo"]
module: bao-cao
trang_thai: cần product xác nhận
ghi_chu: "Trùng feature ke-toan/cong-no — đây là góc xem báo cáo công nợ trong module Báo cáo tổng. Controller thật BaoCaoKeToanController (Accounting); module Báo cáo tổng 'chưa gate' (chưa có controller/menu riêng)."
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo công nợ không có khóa quyền tính năng riêng (chỉ cần đăng nhập), nhưng đi qua cổng kiểm quyền xem báo cáo đơn hàng (cấu hình bên Đơn hàng). Không có quyền đó → bị chặn 'Bạn không có quyền xem báo cáo đơn hàng'."
phu_thuoc: ["Khách hàng (công nợ phải thu 131)", "Nhà cung cấp / Kho (công nợ phải trả 331)", "Phiếu mua / phiếu bán hàng (nguồn phát sinh công nợ)", "Cấu hình quyền xem báo cáo đơn hàng (gate)", "Feature ke-toan/cong-no (tài liệu gốc của báo cáo công nợ)"]
link_flow: ./flow.md
---

# Báo cáo tài chính (công nợ)

> **Lưu ý trùng lặp:** "Báo cáo tài chính (công nợ)" trong module Báo cáo tổng **chính là Báo cáo công nợ tổng hợp** đã có tài liệu đầy đủ ở **`features/ke-toan/cong-no/`**. Tài liệu này chỉ tóm tắt góc nhìn "báo cáo" và **trỏ về tài liệu gốc** để tránh viết lại. Nội dung lập/xử lý phiếu bù trừ công nợ xem ở `ke-toan/cong-no`.

## Là gì
Báo cáo xem tổng quan **công nợ phải thu (TK 131 — khách hàng)** và **phải trả (TK 331 — nhà cung cấp)** theo từng đối tượng, có drill-down xem chi tiết từng chứng từ. Đây là phần "đọc/báo cáo" của nghiệp vụ Công nợ; thao tác cấn trừ (phiếu bù trừ) nằm ở module Kế toán.

> **Trạng thái ở module Báo cáo tổng:** module Báo cáo tổng **chưa có menu riêng** cho báo cáo công nợ. Báo cáo công nợ thực tế **đã có và đang chạy** dưới module **Kế toán → Báo cáo**. Nếu sau này gom về menu Báo cáo tổng thì vẫn là cùng một báo cáo.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo công nợ tổng hợp (tab 131 / 331) | ✅ | đăng nhập + qua cổng kiểm quyền xem báo cáo đơn hàng |
| Xem chi tiết công nợ theo đối tượng (drill-down) | ✅ | như trên |
| Lọc theo chi nhánh, khoảng ngày, nhóm đối tượng, từ khóa | ✅ | bắt buộc chọn loại tài khoản 131 hoặc 331 |
| Sửa số tiền công nợ trực tiếp | ❌ | công nợ phát sinh từ chứng từ gốc (mua/bán/thu/chi); không sửa tay ở báo cáo |
| Lập / cấn trừ công nợ | ➡️ | làm ở module Kế toán → Phiếu bù trừ công nợ (xem `ke-toan/cong-no`) |

## Để làm gì / cho ai
- **Kế toán / chủ shop:** theo dõi tổng phải thu (đang bị nợ) và phải trả (đang nợ ai), đối chiếu công nợ từng đối tượng.
- **Lãnh đạo:** nắm bức tranh tài chính công nợ của đơn vị.

## Cách dùng
1. Vào **Kế toán → Báo cáo → Báo cáo công nợ tổng hợp**.
2. Chọn tab **131 (phải thu)** hoặc **331 (phải trả)**, lọc chi nhánh / khoảng ngày / nhóm đối tượng / từ khóa.
3. Bảng hiện công nợ từng đối tượng; bấm một đối tượng để xem **chi tiết** từng chứng từ.

## Cần chú ý
- **Phải qua cổng kiểm quyền xem báo cáo đơn hàng:** không có quyền đó → bị chặn "Bạn không có quyền xem báo cáo đơn hàng". Đây không phải lỗi báo cáo mà là phân quyền (cấu hình bên Đơn hàng).
- **Bắt buộc chọn loại tài khoản 131 hoặc 331** — không có thì báo cáo không chạy đúng.
- **Tên đối tượng** ghép từ dữ liệu khách hàng / nhà cung cấp; nếu một mã trùng cả hai → hiển thị "KH: ... - NCC: ...".
- **Không sửa số công nợ trực tiếp** — số phát sinh từ chứng từ gốc (mua/bán/thu/chi). Muốn điều chỉnh thì xử lý qua chứng từ gốc hoặc phiếu bù trừ.

## Ảnh hưởng tới
- Read-only — chỉ đọc, không ghi.
- Cùng dữ liệu công nợ với **phiếu bù trừ công nợ** (module Kế toán); xem chi tiết nghiệp vụ ở `features/ke-toan/cong-no/`.
