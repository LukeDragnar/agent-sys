# Quản lý bảo hành — Ghi chú kỹ thuật (dev)

> ⚠️ **KẾT LUẬN QUAN TRỌNG: CHƯA tìm thấy implementation cụ thể của module "Quản lý bảo hành" (màn hình Danh sách bảo hành / Xử lý bảo hành) trong source BE lẫn FE.**
> Đã ripgrep toàn bộ `BaoHanh` / `bao-hanh` / `baohanh` / `warranty` (kèm biến thể không dấu) trên cả hai repo. Bảo hành hiện chỉ tồn tại ở dạng **field trên đơn hàng/sản phẩm** + vài **enum lý do xuất kho** + **1 trang web marketing tĩnh** (không phải app SCRM). **Cần product/dev xác nhận** trước khi viết tài liệu kỹ thuật chính thức.

> Tóm tắt nghiệp vụ: [summarize.md](./summarize.md) · Luồng: [flow.md](./flow.md) · FAQ: [faq.md](./faq.md)

## Màn hình & route
- **CHƯA tìm thấy** component/route cho "Danh sách bảo hành" hay "Xử lý bảo hành".
- FE: không có folder `*bao*hanh*` / `*warranty*`; không có routing/module nào tham chiếu bảo hành như một trang.
- Menu chưa gắn `MaTinhNang` cho tính năng này (đúng như đề bài) → không lần ra được route từ phân quyền.
- Bảo hành chỉ xuất hiện như **label/field** trên các màn có sẵn:
  - i18n key `lbl-thoi-han-bao-hanh` = "Thời hạn bảo hành" / "Warranty Period"
    - `scrm_frontend\lang.txt` (≈ L2160-2161)
  - Màn thu ngân / sửa đơn dùng `hanBaoHanh`:
    - `scrm_frontend\src\app\pages\Order\thungan\thungan-edit\thungan-edit.component.ts` (≈ L2042, `item?.hanBaoHanh`)
    - `scrm_frontend\src\app\pages\Order\thungan\thungan-edit-popup-dungchung\thungan-edit-popup-dungchung.component.ts` (+ `.html`)
  - Màn sửa sản phẩm hiển thị label bảo hành:
    - `scrm_frontend\src\app\pages\WarehouseProduct\quanly-sanpham\edit-sanpham\edit-sanpham.component.html`
    - `scrm_frontend\src\app\pages\WarehouseProduct\quanly-sanphm-chinhanhtao\edit-sanpham-chinhanhtao\edit-sanpham-chinhanhtao.component.html`
    - `scrm_frontend\src\app\pages\ecommerce\edit-sanpham\edit-sanpham.component.html`

## Controller (nếu tìm thấy)
- **KHÔNG có** controller/service quản lý bảo hành trong app SCRM.
- Match `BaoHanh` duy nhất ở tầng controller là trang **website marketing tĩnh** (không liên quan CRM):
  - `scrm\Website\Website.API\Controllers\LienHeController.cs` — action `[Route("BaoHanh")] BaoHanh()` trả về view chính sách bảo hành (L191-204).
  - View: `scrm\Website\Website.API\Views\Themes\T0002\Home\BaoHanh.cshtml`
  - Asset tĩnh: `scrm\Website\Website.API\wwwroot\html\bao-hanh\`
- Các `*Service.cs` trong `Order\Order.Service\Generated\` (DonHangService, DonHangServicePartial, DonHangChiTietService, DonHangLichHenService) chỉ match vì **map field** `HanBaoHanh`/`NgayKichHoatBaoHanh` — **không có business logic bảo hành**.

## Entities (đã tìm thấy — chỉ field, KHÔNG có entity phiếu bảo hành riêng)
- `scrm\Order\Order.DataContext\Entity\DonHangSanPham.cs` (dòng sản phẩm trong đơn):
  - `DateTime? HanBaoHanh` (L93) — hạn bảo hành
  - `DateTime? NgayKichHoatBaoHanh` (L100) — ngày kích hoạt bảo hành
- DTO mirror:
  - `scrm\Libs\Models.Core\DTO\Order.Client\DonHangSanPham\DonHangSanPhamDto.cs` (`HanBaoHanh` L63, `NgayKichHoatBaoHanh` L70)
  - `scrm\Libs\Models.Core\DTO\Order.Client\DonHang\DonHangDto.cs` (lặp ở nested DTO L460/462, L727/731; thêm `double? SoGiay_LichSuSuaHanBaoHanh` L1925 — tracker thời gian sửa lịch sử hạn bảo hành)
- **KHÔNG tìm thấy** entity kiểu `PhieuBaoHanh` / `BaoHanhChiTiet` / `WarrantyRequest`.

## Enum / hằng số liên quan (đã tìm thấy)
- `scrm\Libs\SB.Core\Contants.cs` — enum lý do xuất/nhập kho có nhóm bảo hành:
  - `XuatDoiMoiTrongCSBaoHanh = 5` — Xuất đổi mới trong CS bảo hành
  - `BaoHanhTinhPhiDichVuSuaChuaBanLinhKien = 6` — Bảo hành tính phí: dịch vụ sửa chữa, bán linh kiện
  - `TraHangNhanBaoHanh = 7` — Trả hàng nhận bảo hành
  - `BaoHanhKhongTinhPhiFOC = 9` — Bảo hành không tính phí (FOC)
  - `BaoHanhTinhPhiVaFOCKeToanKiemSoatLai = 10` — Bảo hành tính phí và FOC (kế toán kiểm soát lại)
  → Đây là bằng chứng nghiệp vụ bảo hành **được dự kiến** qua các lý do xuất kho, nhưng **chưa có màn hình/luồng** điều phối chúng.

## Luồng code
- **CHƯA xác định được** — không có controller → service → repository nào cho bảo hành.
- Luồng duy nhất chạm tới bảo hành: khi tạo/sửa **đơn hàng**, field `HanBaoHanh`/`NgayKichHoatBaoHanh` được map qua DonHangService và hiển thị read-only ở màn thu ngân / sửa sản phẩm.

## Bug-prone / điểm cần lưu ý (giả định — cần xác nhận khi implement)
- Phân biệt **FOC vs có phí** (enum 6/9/10) dễ ghi nhận sai loại → sai báo cáo kế toán; cần ràng buộc rõ khi chọn lý do xuất kho.
- **Đổi mới** phải đi đôi: "Xuất đổi mới" (5) + "Trả hàng nhận bảo hành" (7) — thiếu một vế làm lệch tồn IMEI.
- Hàng **IMEI**: thao tác phải đúng mã IMEI; nhầm IMEI khi đổi/sửa làm sai lịch sử máy.
- `HanBaoHanh` phụ thuộc `NgayKichHoatBaoHanh` — nếu chưa kích hoạt mà tính hạn theo ngày bán sẽ sai. `SoGiay_LichSuSuaHanBaoHanh` cho thấy hạn có thể bị **sửa tay** → cần audit log khi chỉnh hạn.

## File source (tham chiếu)
**Backend** (`D:\PROJECT\Git\_SourceForCode\Group2\scrm`):
- `Order\Order.DataContext\Entity\DonHangSanPham.cs`
- `Libs\Models.Core\DTO\Order.Client\DonHangSanPham\DonHangSanPhamDto.cs`
- `Libs\Models.Core\DTO\Order.Client\DonHang\DonHangDto.cs`
- `Libs\SB.Core\Contants.cs`
- `Website\Website.API\Controllers\LienHeController.cs` (website marketing — KHÔNG phải CRM)
- `Website\Website.API\Views\Themes\T0002\Home\BaoHanh.cshtml`

**Frontend** (`D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`):
- `lang.txt` (key `lbl-thoi-han-bao-hanh`)
- `src\app\pages\Order\thungan\thungan-edit\thungan-edit.component.ts`
- `src\app\pages\Order\thungan\thungan-edit-popup-dungchung\thungan-edit-popup-dungchung.component.ts` (+ `.html`)
- `src\app\pages\WarehouseProduct\quanly-sanpham\edit-sanpham\edit-sanpham.component.html`
- `src\app\pages\WarehouseProduct\quanly-sanphm-chinhanhtao\edit-sanpham-chinhanhtao\edit-sanpham-chinhanhtao.component.html`
- `src\app\pages\ecommerce\edit-sanpham\edit-sanpham.component.html`

## Việc cần xác nhận với product/dev
1. Đã có module "Quản lý bảo hành" chưa, hay đang lên kế hoạch? (hiện chỉ có field + enum)
2. Nếu chưa: bảo hành xử lý ở đâu — qua đơn hàng + phiếu xuất/nhập kho theo lý do, hay sẽ có màn riêng?
3. Quy tắc tính `HanBaoHanh` từ `NgayKichHoatBaoHanh` (cố định bao lâu? kích hoạt thủ công hay tự động?).
4. Phân quyền màn bảo hành (MaTinhNang chưa gắn).
