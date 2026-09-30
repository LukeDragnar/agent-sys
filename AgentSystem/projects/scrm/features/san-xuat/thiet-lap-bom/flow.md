# Luồng — Thiết lập BOM (định mức)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (IdDonVi) + chi nhánh (IdChiNhanh).
- Sản phẩm đích, nguyên vật liệu/bán thành phẩm, đơn vị tính (UOM) và kho đã có để chọn.
- (Khuyến nghị) Danh mục **Công đoạn (DmCongDoan)** đã khai báo để đặt tên/thứ tự bước.
- Thêm/Sửa/Xóa BOM cần role AD-TONG/AD-CN/SANXUAT; Clone hiện chỉ cần đăng nhập.

## Luồng dùng (user — màn Thiết lập BOM)
1. Mở **Sản xuất → Thiết lập BOM** (`/sanxuat/lenh-bomsetup`). Bên trái: danh sách BOM; bên phải: quy trình + nguyên liệu + kết quả của BOM đang chọn.
2. **Tạo BOM** (`ThemMoi`): nhập sản phẩm, SoLuongTheoCongThuc (mẻ), KichThuoc; nếu tick `IsMacDinh` → hệ thống clear cờ mặc định ở BOM khác cùng IdSanPham; InsertAsync.
3. Chọn BOM → **Tạo Bước** (`onAddStep`): thêm công đoạn (Ten, ThuTu), kéo–thả sắp xếp (`onDropStep`), nhập thời gian chờ/hoàn thành (ngày/giờ).
4. Mỗi bước thêm **Nguyên vật liệu** (đầu vào, Loai=0): chọn sản phẩm, kho, đơn vị, định mức, % sai lệch, tick `IsSanPhamChinh`/`IsBanThanhPham`. Thêm **Kết quả** (đầu ra, Loai=1) tương tự.
5. **Lưu quy trình** (`CapNhatQuyTrinh`): **xóa toàn bộ BomStep + BomProduct cũ rồi insert lại** danh sách hiện tại trong 1 transaction.
6. Tùy chọn: **Sao chép** (`CloneBomSetup`) → deep-copy BomSetup + BomStep + BomProduct với Id mới (remap stepId), sinh mã mới, tên "- Copy". **Xóa** (`Xoa`) → DeleteAsync.

## Luồng cấu hình (admin — Danh mục Công đoạn)
- `DmCongDoanController`: CRUD `DmCongDoan {Ma, Ten}` (`ThemMoi`/`CapNhatThongTin`/`Xoa`/`TimTheoId`/`TimTheoDieuKien`). Toàn bộ quyền riêng bị comment → chỉ cần đăng nhập. Bước BOM tham chiếu công đoạn theo Ten/Ma (liên kết lỏng, không FK cứng).

## Case biên
- **Đặt IsMacDinh** → clear cờ mặc định ở mọi BOM khác cùng IdSanPham (BomSetupService.cs:50).
- **Sửa BOM khác đơn vị/chi nhánh** → trả NotFound (tenant guard controller L78-85, chỉ ở CapNhatThongTin).
- **Xóa / Sao chép BOM khác tenant** → **KHÔNG có guard** → có thể thao tác chéo đơn vị/chi nhánh (lỗ hổng, xem dev.md).
- **Lưu quy trình lần 2** → mọi Id step/product đổi hoàn toàn (replace-all).
- **Đổi tên công đoạn (DmCongDoan)** → bước BOM đã đặt theo tên cũ có thể không đồng bộ (liên kết lỏng).
- **Clone** → chỉ cần đăng nhập (key thật comment, controller L135).

## Ví dụ cụ thể có số
BOM "Nước rửa tay 70/mẻ" cho sản phẩm Nước rửa tay, SoLuongTheoCongThuc = 70 (chai).
- Bước 1 "Pha chế": NVL đầu vào = Cồn 14 lít, Glycerin 2 lít; Kết quả = Dung dịch 70 lít (bán thành phẩm, IsBanThanhPham=true).
- Bước 2 "Đóng chai": NVL đầu vào = Dung dịch 70 lít + Chai rỗng 70 cái; Kết quả = Nước rửa tay 70 chai (IsSanPhamChinh=true).
- Đặt BOM này `IsMacDinh=true` → nếu trước đó có BOM "Nước rửa tay 100/mẻ" đang mặc định, cờ đó bị clear.
- Khi tách Work Order cho 70 chai: định mức scale theo 70/70 = 1.0 → cồn 14 lít, glycerin 2 lít. Nếu sản xuất 140 chai (LotSize gấp đôi) thì scale 140/70 = 2.0 → cồn 28 lít, glycerin 4 lít.

## Hậu điều kiện
- Thêm/Sửa/Xóa BOM ghi DB (entity BomSetup/BomStep/BomProduct), không RabbitMQ.
- BOM trở thành nguồn cho `WorkOrderController.ChuyenWOPlanner` (vật chất hóa quy trình) và `SaleOrderProductionController` (LotSize/KichThuoc). Thay đổi BOM **không** hồi tố lên WO/PR đã tạo.
