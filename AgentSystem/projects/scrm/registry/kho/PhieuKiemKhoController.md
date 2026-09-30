---
controller: PhieuKiemKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "WarehouseProduct route `quan-ly-kiem-kho` (QuanlyKiemkhoComponent) — list phiếu kiểm kho, nút 'Tạo phiếu' + per-row Sửa/Xóa/Kiểm kho; màn kiểm thực tế = phieu-kiemkho-checkinfo (quét SP, nút 'Kiểm tra kết quả')"
muc_dich: "Quản lý phiếu kiểm kho: tạo/sửa/xóa phiếu, quét SP (imei/barcode) ghi số lượng đã kiểm, đối chiếu SL kiểm vs SL tồn hệ thống → ghi trạng thái Kiểm đủ/Kiểm sai. KHÔNG tự nắn tồn, chỉ ghi nhận lệch."
alias: ["kiểm kho", "phiếu kiểm kho", "kiểm đếm tồn", "đối chiếu tồn", "quét kiểm kho", "kiểm tra số lượng tồn"]
auth: mixed
state_machine: []
deprecated: false
entities: [PhieuKiemKho, PhieuKiemKhoChiTietSanPham, PhieuKiemKhoChiTietImei]
stored_procs: []
depends: [WarehouseProduct, Core/Common (chi nhánh + elastic SP), Redis (STT tự tăng sinh mã)]
cross_module_in: []
cross_module_out: []
---

# PhieuKiemKhoController — Kiểm kho (đối chiếu / ghi lệch tồn)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (icon search) | POST `api/PhieuKiemKho/TimTheoDieuKien` | `PhieuKiemKho_TimTheoDieuKien` | public | luôn | List phiếu theo chi nhánh/kho/keyword/người kiểm/SP. Phân quyền dữ liệu: non-admin-tổng & non-CSKH chỉ thấy phiếu mình tạo / là người kiểm / là NV phụ trách | EF | - |
| 2 | Tạo phiếu kiểm | "Tạo phiếu" (icon plus) → `onTaoPhieuKiemKho` | POST `api/PhieuKiemKho/TaoPhieuKiem` | `PhieuKiemKho_TaoPhieuKiem` | public | luôn | Insert PhieuKiemKho; chặn trùng Mã/Tên trong đơn vị; reload cache STT tự tăng | EF | - |
| 3 | Cập nhật phiếu | "Sửa" (icon edit) → `onCapNhatPhieuKiemKho` | POST `api/PhieuKiemKho/CapNhatPhieuKiem` | `PhieuKiemKho_CapNhatPhieuKiem` | public | luôn | Update các field cấu hình phiếu (kho, người kiểm, thời gian, SP, số lần kiểm tối đa...); chặn trùng Mã/Tên | EF (MarkDirty từng field) | - |
| 4 | Xóa phiếu | "Xóa" (menu, icon delete) → `onDelete` | POST `api/PhieuKiemKho/Xoa/{id}` | `PhieuKiemKho_Xoa` | public | luôn | Soft-delete phiếu (IsDelete=true) + xóa cứng chi tiết SP & IMEI liên quan, trong transaction | EF | **FE/BE lệch route**: service FE gọi `XoaPhieuKiem/{id}` còn BE expose `Xoa/{id}` → nút Xóa gọi qua method khác hoặc lỗi 404; cần xác nhận (kiemkho.service.ts:38-41) |
| 5 | Chi tiết phiếu | mở form sửa | GET `api/PhieuKiemKho/ChiTietPhieuKiem/{id}` | `PhieuKiemKho_ChiTietPhieuKiem` | public | luôn | Trả detail phiếu + bind tên kho/chi nhánh/người kiểm/SP (qua elastic) | EF + Core elastic | - |
| 6 | Generate mã phiếu | (auto khi tạo phiếu) | POST `api/PhieuKiemKho/GenerateMaPhieuKiem` | `PhieuKiemKho_GenerateMaPhieuKiem` | public | luôn | Sinh mã `{TENKHO}-{ddMMyyyy}-{STT+1}` từ STT tự tăng (Redis lock); chặn nếu mã trùng | EF + Redis | - |
| 7 | Quét SP (imei/barcode) | màn kiểm: nhập KeyGen → `CheckProduct()` | POST `api/PhieuKiemKho/CheckProduct` | `[SBAuthorize]` (mở — chỉ cần đăng nhập) | public | trong khoảng `NgayBatDauKiemKho..NgayKetThucKiemKho` | Tìm SP theo imei (DaNhapKho) hoặc barcode trong kho; validate thuộc danh sách SP phiếu (nếu SPECIFIC_PRODUCT); IMEI: chặn quét trùng imei, +1 SoLuongDaKiem + insert ChiTietImei; barcode: tạo/cập nhật dòng ChiTietSanPham. **Chỉ ghi SoLuongDaKiem, KHÔNG đụng tồn** | EF (transaction) | Permission key bị comment, chỉ còn `[SBAuthorize]` rỗng (controller:78-83) |
| 8 | Sửa SL đã kiểm thủ công | màn kiểm (sửa số trên dòng SP) → `UpdateSLDaKiem` | POST `api/PhieuKiemKho/UpdateSLDaKiem` | `[SBAuthorize]` (mở) | trong thời gian cho phép kiểm | Set `SoLuongDaKiem = reqDto.SoLuongDaKiem` cho 1 SP (insert nếu chưa có). Không đụng tồn | EF | Quyền `[SBAuthorize]` rỗng (controller:86-91) |
| 9 | Tải DS sản phẩm kiểm | màn kiểm (grid) → `LoadGridDanhSachSanPhamKiem` | POST `api/PhieuKiemKho/LoadGridDanhSachSanPhamKiem` | `PhieuKiemKho_LoadGridDanhSachSanPhamKiem` | public | luôn | SPECIFIC_PRODUCT → show DS SP cấu hình; ALL/OPTIONAL → show theo tồn kho; kèm SoLuongDaKiem, trạng thái kiểm (chỉ hiện khi đã kiểm hoặc phiếu hết hạn), số lần đã kiểm / còn lại | EF (join KhoChiTiet+SanPham+đã kiểm) + elastic | - |
| 10 | Kiểm tra kết quả / chốt lượt | "Kiểm tra kết quả" → `KiemTraKetQua()` | POST `api/PhieuKiemKho/KiemTraKetQua` | `PhieuKiemKho_KiemTraKetQua` | public | trong thời gian cho phép kiểm | Đối chiếu `SoLuongDaKiem` vs `SoLuongDaNhap` (imei: count SP imei DaNhapKho; barcode: KhoChiTiet.SoLuong) → set TrangThaiKiem (KiemDu nếu bằng, KiemSai nếu lệch); +1 SoLanDaKiem; insert/update ChiTietSanPham. **CHỈ ghi lệch, KHÔNG nắn tồn KhoChiTiet/SanPhamImei** | EF (transaction) | - |

## Quy tắc nghiệp vụ (gating)
1. **Thời gian kiểm**: mọi thao tác kiểm (CheckProduct / UpdateSLDaKiem / KiemTraKetQua) yêu cầu `NgayBatDauKiemKho <= Now <= NgayKetThucKiemKho`, ngược lại trả "Không trong thời gian cho phép kiểm kho!".
2. **Phạm vi SP theo `KieuLoaiSanPham`**:
   - `SPECIFIC_PRODUCT` → SP quét phải nằm trong `LstIdSanPham`; nếu rỗng → "Phiếu kiểm chưa chọn sản phẩm!".
   - `ALL_PRODUCT` / `OPTIONAL` → kiểm theo toàn bộ SP tồn trong kho.
3. **Trạng thái kiểm** (`GetTrangThaiKiem`, service:415-419): `SoLuongDaKiem == SoLuongDaNhap` → KiemDu; khác → KiemSai. Đây là blueprint trace: mô phỏng SL kiểm vs SL nhập để verify nhãn.
4. **Số lần kiểm**: `SoLanKiemLaiToiDa` vs `SoLanDaKiem` → `IsHetLuotKiem` khi đã kiểm >= tối đa.

## Quan hệ
- **Gọi sang:** Core/Common (`GetAllChiNhanhByCurrentUser`, `GetSanPhamElasticByIds` lấy tên SP); Redis (lock + cache STT tự tăng để gen mã).
- **Dữ liệu (entities):** `PhieuKiemKho` (cấu hình phiếu), `PhieuKiemKhoChiTietSanPham` (SL đã kiểm + trạng thái kiểm per SP), `PhieuKiemKhoChiTietImei` (log từng imei đã quét); đọc `KhoChiTiet`, `SanPhamImei`, `SanPham`, `Kho`, `User` để đối chiếu.
- **Ripple / đồng bộ:** KHÔNG có. Kiểm kho chỉ ghi nhận chênh lệch (đã kiểm vs hệ thống), không cộng/trừ tồn, không sinh phiếu nhập/xuất điều chỉnh, không bắn event cross-service. Việc nắn tồn (nếu cần) do user tự tạo phiếu nhập/xuất riêng.

## Lỗi đã biết / lưu ý kỹ thuật
- **FE/BE lệch route Xóa** — BE: `Xoa/{id}` (controller:48-53); FE service: `XoaPhieuKiem/{id}` (kiemkho.service.ts:38-41). Cần xác nhận nút Xóa thực tế hoạt động.
- **`throw ex` + Console.WriteLine sau throw** trong `GetMaxSTTTuTangTheoDonVi` (service:397-398): `throw ex` reset stacktrace + dòng `Console.WriteLine` là dead code. Không ảnh hưởng chức năng.
- **Quyền không đồng nhất**: `CheckProduct`, `UpdateSLDaKiem` dùng `[SBAuthorize]` rỗng (permission key bị comment) → chỉ cần đăng nhập là quét/sửa SL kiểm được; các endpoint còn lại có key riêng. Cần xác nhận có chủ ý.
- **CheckImeiAsync đã bị thay bằng CheckProductAsync** — toàn bộ `CheckImeiAsync` (controller:71-76, service:612-697) bị comment, thay bằng `CheckProduct` (gộp xử lý imei + barcode). Bỏ qua phần comment.
- **`KiemTraKetQua` không trừ SP đã kiểm đúng khi kiểm lại** — block lọc `TrangThaiKiemValue == KiemSai` cho `IsKiemKhoLai` bị comment (service:893-897); hiện chốt lại toàn bộ SP mỗi lượt.
- **Controller `partial`** (`public partial class PhieuKiemKhoController`) — node này cover file Controllers/PhieuKiemKhoController.cs; cần xác nhận có file partial bổ sung không.
