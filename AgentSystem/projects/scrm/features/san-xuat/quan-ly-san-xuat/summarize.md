---
alias: ["quản lý sản xuất", "đơn hàng sản xuất", "lệnh sản xuất", "work order", "lệnh làm việc", "yêu cầu sản xuất", "production request", "sản xuất theo định mức"]
module: san-xuat
trang_thai: "cần product xác nhận — module Sản Xuất ở mức sơ khai (level 1 theo INDEX). Nhiều nút/endpoint còn để quyền rỗng (chỉ cần đăng nhập), một số entity nghi legacy (ProductionOrder). Cần product xác nhận đã bật bán chính thức cho khách hay đang thử nghiệm."
goi: "cần product xác nhận"
quyen: "cần product xác nhận — các nút chính (Thêm đơn / Tạo Production Request / Tách WO / In phiếu SX) yêu cầu 1 trong các role AD-TONG, AD-CN, SANXUAT. Nhiều nút điều hành WO (bắt đầu/tạm dừng/hủy sản xuất, xóa) hiện để quyền rỗng → chỉ cần đăng nhập (cần product xác nhận có chủ ý hay thiếu sót)."
phu_thuoc: ["Thiết lập BOM (định mức NVL + quy trình công đoạn — đầu vào bắt buộc để tách WO)", "Kho / Warehouse (đọc tồn kho, tạo phiếu xuất NVL + phiếu nhập thành phẩm cho từng bước)", "Sản phẩm & Đơn vị tính (chọn sản phẩm cần sản xuất, quy đổi đơn vị)"]
link_flow: ./flow.md
---

# Quản lý sản xuất

## Là gì
Đây là nơi **điều hành toàn bộ quy trình sản xuất** trong shop/nhà máy: từ lúc nhận **đơn đặt hàng sản xuất**, lập **yêu cầu sản xuất** (Production Request — tính phần còn thiếu so với tồn kho), **tách thành các lệnh sản xuất** (Work Order), rồi **chạy từng công đoạn** của lệnh — xuất nguyên vật liệu, nhập thành phẩm, hoàn thành từng bước cho tới khi ra sản phẩm cuối.

Tính năng này gồm chuỗi màn hình nối tiếp nhau:
- **Đơn đặt hàng sản xuất**: nhập đơn cần sản xuất, giữ chỗ (lock) sản phẩm trong kho.
- **Lệnh sản xuất (Product Orders)**: từ yêu cầu sản xuất, gắn BOM (định mức) và bấm "Tách" để sinh ra các lệnh làm việc (WO).
- **Lệnh công việc (Work Order)**: chọn các WO, đưa sang Planner để dựng quy trình theo BOM.
- **Planner / điều hành bước**: bắt đầu, tạm dừng, hủy; với từng công đoạn thì tạo phiếu xuất NVL và phiếu nhập thành phẩm, rồi hoàn thành bước.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / sửa đơn đặt hàng sản xuất | ✅ | quyền AD-TONG/AD-CN/SANXUAT (cần product xác nhận) |
| Xóa dòng / đơn đặt hàng | ✅ | xóa theo từng dòng chi tiết; hết dòng thì xóa cả đơn |
| Giữ chỗ (lock) sản phẩm trong kho | ✅ | trên đơn đặt hàng |
| Tạo Yêu cầu sản xuất (Production Request) | ✅ | từ phần còn thiếu so với tồn kho |
| Tách yêu cầu thành Lệnh sản xuất (WO) | ✅ | yêu cầu chưa tách; phải có chọn BOM |
| Đưa WO sang Planner (dựng quy trình) | ✅ | WO chưa bắt đầu, có gắn BOM |
| Bắt đầu / tạm dừng / hủy sản xuất | ✅ | đúng trạng thái; nhiều nút hiện chỉ cần đăng nhập |
| Tạo phiếu xuất NVL theo bước | ✅ | bước chưa hoàn thành |
| Tạo phiếu nhập thành phẩm theo bước | ✅ | bước chưa hoàn thành |
| Hoàn thành bước / hoàn thành lệnh | ✅ | mọi sản phẩm của bước đã có phiếu |
| In phiếu nhập / xuất sản xuất | ✅ | quyền AD-TONG/AD-CN/SANXUAT |
| **Hoàn / hủy phiếu kho khi xóa WO** | ❌ | xóa lệnh KHÔNG tự hoàn phiếu nhập/xuất đã tạo → tồn kho có thể lệch (xem Cần chú ý) |

## Để làm gì / cho ai
- **Chủ shop / quản lý sản xuất:** biết đang cần sản xuất gì, thiếu bao nhiêu so với tồn, theo dõi lệnh chạy tới công đoạn nào.
- **Nhân viên sản xuất / thủ kho:** chạy từng công đoạn, xuất nguyên liệu, nhập thành phẩm, in phiếu.
- **Kế hoạch (planner):** xếp lịch các lệnh, quyết định lệnh nào làm trước.

## Cách dùng
1. Vào module **Sản xuất → Đơn đặt hàng** (`/sanxuat/lenh-dathang`): thêm đơn cần sản xuất, chọn sản phẩm và số lượng. Có thể **giữ chỗ (lock)** số lượng trong kho.
2. Trên đơn, bấm **Tạo Production Request (từ phần thiếu)** — hệ thống lấy số cần trừ tồn kho để ra số cần sản xuất, rồi **Lưu Production Request**.
3. Vào **Lệnh sản xuất** (`/sanxuat/lenh-sanxuat`): mỗi yêu cầu chọn **BOM** (định mức) phù hợp, bấm **Split → Preview** để xem trước WO, rồi **Thêm vào danh sách WO** để tách.
4. Vào **Lệnh công việc** (`/sanxuat/lenh-lamviec`): tick chọn các WO cần làm, bấm **Đưa sang Planner** để dựng quy trình theo BOM.
5. Trong **Planner**: **Bắt đầu** lệnh; với từng công đoạn, **tạo phiếu xuất** nguyên vật liệu, **tạo phiếu nhập** thành phẩm, rồi **hoàn thành bước**. Bước cuối hoàn thành → lệnh ở trạng thái **Hoàn thành**.

## Cần chú ý
- **BOM là bắt buộc.** Không có BOM (định mức + quy trình công đoạn) thì không tách được Work Order. Lệnh WO cũ không gắn BOM sẽ báo lỗi khi đưa sang Planner.
- **Xóa lệnh sản xuất KHÔNG hoàn phiếu kho.** Nếu đã tạo phiếu xuất NVL / phiếu nhập thành phẩm rồi mới xóa WO, các phiếu kho đó **không bị hủy** → tồn kho có thể lệch. Hãy cân nhắc kỹ trước khi xóa lệnh đã chạy.
- **Sửa lại BOM không tự cập nhật lệnh đã tách.** Quy trình của WO được "sao chép" từ BOM tại thời điểm tách; sửa BOM sau đó không hồi tố lên các WO đã tạo.
- **Phải có phiếu cho mọi sản phẩm của bước** mới hoàn thành được bước đó; nếu còn sản phẩm chưa tạo phiếu sẽ bị chặn.
- **Một số nút điều hành (tạm dừng, hủy, xóa) hiện không yêu cầu quyền riêng** — bất kỳ ai đăng nhập có thể bấm. Cần product xác nhận đây là chủ ý hay cần siết quyền.
- **Trùng entity:** màn "Production Order" (CRUD danh mục) là một entity riêng, nghi cũ/song song với luồng yêu cầu sản xuất thật. Cần product xác nhận màn này còn dùng.

## Ảnh hưởng tới
- **Kho / tồn kho:** mỗi phiếu xuất NVL và phiếu nhập thành phẩm tạo chuyển động tồn kho thật ở Warehouse.
- **Định mức (BOM):** là khuôn mẫu để dựng quy trình; mọi tính toán NVL cần đều dựa vào BOM.
- **Sản phẩm:** thành phẩm nhập kho có thể là sản phẩm bán hoặc bán thành phẩm cho bước/đơn sau.
