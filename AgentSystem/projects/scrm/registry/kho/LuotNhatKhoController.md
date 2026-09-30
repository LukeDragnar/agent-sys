---
controller: LuotNhatKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý nhặt kho > Danh sách lượt nhặt (WarehouseProduct/quan-ly-luot-nhat). Tạo lượt nhặt từ màn Thu kho tác nghiệp (landing-page/thu-kho-tac-nghiep)."
muc_dich: "Quản lý lượt nhặt hàng (soạn đơn) trong kho: gom nhiều đơn TM → chia vị trí/SKU thành các phiếu nhặt, đề xuất đường đi nhặt hàng tối ưu."
alias: ["lượt nhặt", "luot nhat", "nhặt hàng", "nhặt kho", "soạn đơn", "lượt nhặt kho", "LN", "phiếu nhặt", "picking", "picking wave"]
auth: mixed
state_machine: []
deprecated: false
entities: [LuotNhatKho, PhieuNhatKho, PhieuNhatKhoDonHang, PhieuNhatKhoSanPham, ViTriKho, ViTriSanPham, KhoChiTiet, Kho, SanPham]
stored_procs: []
depends:
  - LuotNhatKhoService (WarehouseProduct.Service.Generated)
  - OrderLogisticService (HTTP: DonHangLogistic/GetDsDonHangThuongMai, DonHangLogistic/UpdateTrangThaiTaoPhieuNhat)
  - CauHinhService (SoLuongSKUMaxPhieuNhat — số SKU tối đa/phiếu nhặt)
  - CommonService.DropdownDmSanPham (đơn vị tính)
  - CommonCacheService / RedisCacheService (số tự tăng mã LN/PN + Redis lock)
cross_module_in:
  - OrderLogistic/DonHangLogistic (đơn thương mại — nguồn sản phẩm chia phiếu nhặt)
cross_module_out:
  - OrderLogistic.DonHangLogistic.TrangThaiTaoPhieuNhat (set true khi tạo / false khi xóa lượt nhặt)
---

# LuotNhatKho — Lượt nhặt kho (soạn đơn / picking wave)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách lượt nhặt (grid) | nút "Tìm kiếm" | POST `TimTheoDieuKien` | `LuotNhatKho_TimTheoDieuKien` | public | màn `quan-ly-luot-nhat` | Lọc theo `IdDonVi + IdChiNhanh` của user, khoảng `NgayCapNhat` (TuNgay/DenNgay), order giảm theo NgayCapNhat. KeyWord match `Ma` HOẶC mã đơn trong `DonHangTMInfoRes` (parse từ JsonData). Trả `(datas, total)` đã phân trang. | EF `LuotNhatKho` (TableWithNolock) | Toàn bộ query `.ToList()` rồi mới filter KeyWord/paging trong memory — không phân trang ở DB (LuotNhatKhoService.cs:100,128) |
| 2 | Lấy DS sản phẩm theo đơn (popup chi tiết) | mở chi tiết đơn trong lượt nhặt | POST `GetDsSanPhamByDonHang` | `[SBAuthorize]` (mở) | internal-ish | trong popup chi tiết lượt nhặt | Gọi OrderLogistic lấy đơn TM, trả DS sản phẩm + đơn vị tính (quy đổi nếu `IsSuDungQuyDoiDonViTinh`), cờ `IsExistedPN` (SP đã có trong phiếu nhặt nào của lượt). | HTTP OrderLogistic + EF PhieuNhatKho/SanPham | — |
| 3 | Tạo lượt nhặt | nút "Tạo lượt nhặt" (màn Thu kho tác nghiệp) | POST `TaoLuotNhat` | `LuotNhatKho_TaoLuotNhat` | public | chọn đơn TM chưa tạo phiếu nhặt | Gom đơn → build cây vị trí kho → chia phiếu nhặt theo SKU max + thứ tự ưu tiên vị trí → sinh mã LN/PN (Redis lock + số tự tăng), update `TrangThaiTaoPhieuNhat=true` ở OrderLogistic, lưu toàn bộ trong 1 transaction. | EF (insert LuotNhatKho + PhieuNhatKho* ) + HTTP OrderLogistic | Chặn nếu thiếu cấu hình `SoLuongSKUMaxPhieuNhat`; chặn nếu đơn đã có phiếu nhặt; nếu fail xoá cache số tự tăng (:191,:194,:447) |
| 4 | Đồng bộ / bổ sung SP vào phiếu nhặt | (nội bộ, từ chi tiết) | POST `SyncPhieuNhat` | `LuotNhatKho_SyncPhieuNhat` | public | đơn có SP mới chưa nằm phiếu nhặt nào | Lấy SP trong đơn chưa được chia → gán vào phiếu nhặt hiện tại (lấy vị trí đầu theo ThuTu), insert PhieuNhatKhoSanPham, tính lại `ViTriDeXuat` (chuỗi vị trí ngăn cách `->`). | EF PhieuNhatKhoSanPham/ViTri | Insert SP **không** nằm trong transaction (:572) → lỗi giữa chừng có thể lệch dữ liệu |
| 5 | Xóa lượt nhặt | nút "Xóa" → confirm | POST `DeleteLuotNhat/{id:Guid}` | `LuotNhatKho_DeleteLuotNhat` | public | mỗi dòng lượt nhặt | Xóa LuotNhatKho + toàn bộ PhieuNhatKho/DonHang/SanPham (transaction), gọi OrderLogistic set `TrangThaiTaoPhieuNhat=false` cho các đơn liên quan, xoá cache số tự tăng. | EF delete cascade thủ công + HTTP OrderLogistic | Phải gọi OrderLogistic update trạng thái **trước** transaction — fail update → trả lỗi, chưa xóa (:480) |
| 6 | DS đơn hàng theo lượt nhặt | (internal) | POST `GetDanhSachDonHangTheoLuotNhat/{idLuotNhat}` | `[PrivateSBAuthorize]` | internal | service-to-service | Trả distinct `IdDonHang` của tất cả phiếu nhặt thuộc lượt (lọc theo DonVi/ChiNhanh user). | EF join PhieuNhatKho × PhieuNhatKhoDonHang | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetDanhSachDonHangTheoLuotNhat/{idLuotNhat}` — caller khác (vd OrderLogistic/đăng đơn) lấy danh sách đơn trong lượt nhặt để xử lý tiếp.

## Quy tắc nghiệp vụ (chia phiếu nhặt — blueprint trace)
1. **Bắt buộc có cấu hình `SoLuongSKUMaxPhieuNhat > 0`** (per DonVi). Thiếu → trả lỗi "Vui lòng cấu hình số lượng tối đa SKU trong một phiếu nhặt!" (:191).
2. **Đơn đã tạo phiếu nhặt → chặn**: nếu `LstIdDonHang` có đơn đã tồn tại trong `PhieuNhatKhoDonHang` → trả lỗi liệt kê mã đơn (:194).
3. **Chia phiếu cho SP xác định được vị trí** (`PhanBoPhieuNhatDoiVoiSanPhamXacDinhDuocViTri`, :597): duyệt vị trí theo thứ tự ưu tiên; mỗi phiếu chứa tối đa `SoLuongSKUMaxPhieuNhat` SKU; mỗi SKU chỉ phân bổ 1 lần; đầy phiếu → mở phiếu mới (cùng IdKho).
4. **Chia phiếu cho SP KHÔNG cấu hình vị trí** (`PhanBoPhieuNhatDoiVoiSanPhamKhongXacDinhDuocViTri`, :652): group theo IdKho, chia khối `Ceiling(total / SKUMax)` phiếu, vị trí = `Guid.Empty`/null.
5. **Mã phiếu**: `GenCode(stt, prefix)` = `{prefix}-{stt:D10}`, prefix `LN{ddMMyyyy}` / `PN{ddMMyyyy}`; số tự tăng lấy qua cache→max DB; giới hạn 1 tỷ (:742).
6. **Redis lock** `RedisLockRetry` (20 lần, delay 300ms, TTL 30s) quanh việc cấp số tự tăng + tạo phiếu để tránh trùng mã đa luồng (:793).

## Quan hệ
- **Gọi sang:** OrderLogistic `DonHangLogistic/GetDsDonHangThuongMai` (lấy SP đơn TM), `DonHangLogistic/UpdateTrangThaiTaoPhieuNhat` (cờ đã tạo phiếu nhặt) · `CauHinhService` (SKU max) · `CommonService.DropdownDmSanPham` (đvt) · `CommonCacheService`/`RedisCacheService` (số tự tăng + lock).
- **Dữ liệu (entities):** `LuotNhatKho` (Ma, JsonData=snapshot DS SP đơn, SoTuTang) → 1-n `PhieuNhatKho` (IdKho, ViTriDeXuat, SoTuTang) → n `PhieuNhatKhoDonHang` (IdDonHang, MaDonHang) + n `PhieuNhatKhoSanPham` (IdSanPham, IdViTriKho, SoLuong, IdDonViTinh). Tham chiếu `ViTriKho`/`ViTriSanPham` (cây vị trí), `KhoChiTiet`/`SanPham`/`Kho`.
- **Ripple / đồng bộ:** **không trừ tồn kho** (chỉ soạn đơn/đề xuất đường đi). Side-effect cross-module duy nhất = đặt `DonHangLogistic.TrangThaiTaoPhieuNhat` true/false ở OrderLogistic.

## Luồng xuyên module
- **OrderLogistic → Kho (tạo lượt nhặt):** chọn đơn TM ở màn Thu kho tác nghiệp → `TaoLuotNhat` lấy chi tiết đơn từ OrderLogistic → build cây vị trí + chia phiếu nhặt theo SKU max → lưu LN/PN → gọi ngược OrderLogistic set `TrangThaiTaoPhieuNhat=true`. Xóa lượt nhặt đảo chiều cờ về `false`.
- LuotNhatKho **không** sinh phiếu xuất / không hạch toán kế toán; là bước soạn hàng trước khi đăng đơn / xuất giao vận.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `LuotNhatKhoService.cs:100,128` — `TimTheoDieuKien` query `.ToList()` toàn bộ trước rồi filter KeyWord + paging in-memory; data lớn → tốn RAM, không tận dụng index/paging DB.
- BE `LuotNhatKhoService.cs:194` — chặn tạo nếu đơn đã có phiếu nhặt (`PhieuNhatKhoDonHang`); :191 chặn nếu chưa cấu hình `SoLuongSKUMaxPhieuNhat`.
- BE `LuotNhatKhoService.cs:413,480` — phụ thuộc OrderLogistic `UpdateTrangThaiTaoPhieuNhat`: tạo set true / xóa set false; fail HTTP → trả lỗi, rollback nghiệp vụ tương ứng.
- BE `LuotNhatKhoService.cs:572` — `SyncPhieuNhat` insert PhieuNhatKhoSanPham **ngoài transaction** (khác Tạo/Xóa có transaction) → rủi ro lệch nếu lỗi sau insert.
- BE `LuotNhatKhoService.cs:447,516` — fail nghiệp vụ xoá cache số tự tăng LN/PN để lần sau đọc lại từ DB (tránh lệch mã).
- BE `LuotNhatKhoController.cs:29` — `GetDsSanPhamByDonHang` chỉ `[SBAuthorize]` (không gắn permission key cụ thể) khác các action khác có key riêng.
- FE `luotnhat-danhsach.component.ts:80,148` — grid cột `donHangTMInfoRes` đọc từ JsonData snapshot; popup chi tiết truyền `idsDonHang = donHangTMInfoRes.map(r=>r.id)`.
- FE service `luotnhatkho.service.ts` — base URL `api/LuotNhatKho`; `timTheoDieuKien` kế thừa từ BaseService; còn `taoLuotNhat/syncPhieuNhat/deleteLuotNhat/getDsSanPhamByDonHang`.
