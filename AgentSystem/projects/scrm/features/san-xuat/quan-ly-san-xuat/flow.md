# Luồng — Quản lý sản xuất

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (IdDonVi) và chi nhánh (IdChiNhanh).
- Sản phẩm cần sản xuất đã có **BOM (định mức)** với quy trình công đoạn + nguyên vật liệu đầu vào (Loai=0) và kết quả đầu ra (Loai=1). Không có BOM → không tách được Work Order.
- Kho/Warehouse hoạt động (luồng gọi HTTP đồng bộ sang Warehouse để đọc tồn + tạo phiếu nhập/xuất).
- Các nút chính (Thêm đơn / Tạo PR / Tách WO / In phiếu) cần role AD-TONG/AD-CN/SANXUAT; nhiều nút điều hành WO hiện chỉ cần đăng nhập.

## Luồng dùng (chuỗi 5 màn)
1. **Đơn đặt hàng** (`/sanxuat/lenh-dathang`): tạo đơn đặt hàng sản xuất (header `SaleOrderDonHang` + chi tiết). Tính `TongSoLuong`. Tùy chọn **Lock sản phẩm** → set `IsLock`/`SoLuongLock` trên chi tiết, tổng hợp vào `SanPhamLock`.
2. **Tạo Production Request**: từ đơn, bấm "Tạo Production Request (từ phần thiếu)" → hệ thống đọc tồn kho (Warehouse `GetTonKhoByListSanPham`), lấy `số cần − tồn` = số cần sản xuất; "Lưu Production Request" → tạo `SaleOrderProduction`, lấy LotSize/KichThuoc từ **BomSetup**, set dòng đơn `SaleOrderDonHangChiTiet.TrangThai=2`, header `TrangThaiPR=1`.
3. **Lệnh sản xuất** (`/sanxuat/lenh-sanxuat`): mỗi PR chọn BOM, "Split → Preview" xem trước WO; "Thêm vào danh sách WO" → `WorkOrder/ThemMoi` (InsertAsyncV2) tách PR thành các WO (set PR `IsTachDon=true`); WO mới `TrangThai=ChuaBatDau`, `IsPlan=false`.
4. **Lệnh công việc** (`/sanxuat/lenh-lamviec`): tick chọn WO (`IsPlan=true`), "Đưa sang Planner" → `ChuyenWOPlanner` vật chất hóa BOM thành `WorkOrderStep` + `WorkOrderStepProduct`, scale `SoLuongDinhMuc` theo `SoLuong/SoLuongTheoCongThuc`; WO `TrangThai=ChuaXepLich`.
5. **Planner / điều hành bước**:
   - **Bắt đầu** (`BatDauWorkOrder`): WO `DaXepLich`, `BuocIndex=1`, step thứ tự 1 → `DangXuLy`; `BatDauSanXuat` → `TrangThaiSanXuat=DangSanXuat`.
   - Mỗi bước: **Tạo phiếu xuất** NVL (`LuuVaTaoPhieuXuatByStep` → Warehouse `PhieuXuat/ThemMoi`, KieuLoaiXuat=2) → stamp IdPhieu; **Tạo phiếu nhập** thành phẩm (`LuuVaTaoPhieuNhapByStep` → Warehouse `PhieuNhap/ThemMoi`, MaDmPhanLoaiNhap=10), auto-gen lô `LSX` cho SP lô-date, GiaNhap=ΣTienXuat/ΣSoLuongNhap.
   - **Hoàn thành bước** (`HoanThanhWorkOrderStep`): yêu cầu mọi SP đã có phiếu; có bước kế → bước kế `DangXuLy` + đẩy số đầu ra sang đầu vào bước kế; bước cuối → WO `TrangThaiSanXuat=HoanThanh`.

## Luồng cấu hình (admin)
- Phụ thuộc **Thiết lập BOM** (xem feature `thiet-lap-bom`): khai báo định mức + quy trình công đoạn trước. BOM mặc định (`IsMacDinh`) được ưu tiên gợi ý khi tạo PR/tách WO.
- Master data **Công đoạn (DmCongDoan)** dùng để đặt tên/thứ tự các bước trong BOM.

## Case biên
- **Tách lệnh khi PR đã tách** (`IsTachDon==true`) → lỗi "Đơn đã được tách".
- **Đưa sang Planner khi WO không có IdBomSetup** → lỗi "đơn WO cũ" (không dựng được quy trình).
- **Bắt đầu WO đang DangSanXuat / HoanThanh** → bị chặn (sai state).
- **Hoàn thành bước còn SP `IdPhieu==null`** → chặn "Vui lòng tạo các phiếu".
- **Xóa WO sau khi đã tạo phiếu** → cascade delete step/product nhưng **KHÔNG hoàn/void phiếu kho** → tồn kho lệch (lỗi đã biết, xem dev.md).
- **CapNhatTrangThaiSanXuat (raw)** không guard → có thể nhảy state tùy ý, phá state machine.
- **Sửa đơn đặt hàng** = xóa hết chi tiết rồi insert lại → IdChiTiet đổi; tham chiếu cũ mất.
- **Xóa PR** → reset dòng đơn `TrangThai` về 0 (cho phép tạo PR lại).
- **Sửa BOM sau khi tách WO** → không hồi tố lên WO đã vật chất hóa.

## Ví dụ cụ thể có số
Shop B cần sản xuất **100 chai nước rửa tay**, tồn kho hiện có **30 chai**.
- Tạo đơn đặt hàng: SP = nước rửa tay, số lượng = 100. Hệ thống đọc tồn = 30 → phần thiếu = **70**.
- Tạo Production Request: số cần sản xuất = 70. Chọn BOM "Nước rửa tay 70/mẻ" (SoLuongTheoCongThuc=70), LotSize lấy từ BOM.
- Tách WO: sinh 1 WO cho 70 chai, gắn BOM. Đưa sang Planner → BOM có 2 công đoạn (Pha chế → Đóng chai). NVL pha chế scale theo 70/70 = 1.0 (đúng định mức gốc): ví dụ cồn 14 lít, glycerin 2 lít.
- Chạy bước "Pha chế": tạo phiếu xuất 14 lít cồn + 2 lít glycerin (Warehouse xuất kho), nhập 70 lít dung dịch (bán thành phẩm). Hoàn thành bước → đầu ra 70 lít đẩy sang đầu vào bước "Đóng chai".
- Chạy bước "Đóng chai": xuất 70 lít dung dịch + 70 chai rỗng, nhập **70 chai thành phẩm** (lô `LSX...`). Hoàn thành bước cuối → WO **Hoàn thành**.

## Hậu điều kiện
- Mỗi phiếu xuất/nhập tạo **chuyển động tồn kho thật** ở Warehouse: NVL giảm, thành phẩm tăng.
- Tạo PR set dòng đơn `TrangThai=2`; xóa PR reset về `0`.
- Toàn module **KHÔNG dùng RabbitMQ/MassTransit** (ripple kho qua HTTP đồng bộ); nếu Warehouse lỗi giữa chừng có thể để lại trạng thái dở (cần product/dev xác nhận xử lý rollback).
