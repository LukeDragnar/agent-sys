---
controller: ResetTonKhoImeiController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > màn hàng bán / tồn kho (quanly-kho-hangban) — nút nhỏ đỏ 'Reset tồn kho và imei' trên từng dòng tồn (mỗi dòng = 1 SanPham + 1 Kho). Nút CHỈ hiện khi (a) có quyền `ResetTonKhoImei_Reset` (sbAuthorize) VÀ (b) cấu hình kho bật `choPhepResetTonKho` (config-kho). Mặc định ẩn."
muc_dich: "Reset (đưa về 0) tồn kho của 1 sản phẩm tại 1 kho: tạo 1 PhieuXuat 'Xuất khác' rút hết số đang tồn, set KhoChiTiet.SoLuong=0, đánh IMEI DaNhapKho→DaXuatKho, set SanPhamLoDate.SoLuong=0; rồi đồng bộ kế toán + lịch sử kho."
alias: [reset ton kho, reset tồn kho, reset imei, reset ton kho va imei, xoa ton kho, dua ton ve 0, ResetTonKhoImei]
auth: mixed
state_machine: []
deprecated: false
entities: [PhieuXuat, PhieuXuatSanPham, KhoChiTiet, SanPhamImei, SanPhamLoDate, SanPham, DMSanPham, DMKho]
stored_procs: []
depends: [SanPham, DMSanPham (dung tích), DMKho (phân loại/tình trạng xuất), KhoChiTiet, SanPhamImei, SanPhamLoDate, CommonService (SinhMaPhieu + SyncKhoChiTietLichSu), DongBoPhieuSangKeToanService, LogActivityHelper, CauHinhKho (choPhepResetTonKho)]
cross_module_in: []
cross_module_out: [Accounting (DongBoPhieuSangKeToanService.DongBoPhieuXuat — đẩy chứng từ phiếu xuất nếu đơn vị dùng kế toán), KhoChiTietLichSu (SyncKhoChiTietLichSu type XuatKho → báo cáo kho), LichSuTacDong (audit UPDATE)]
---

# ResetTonKhoImeiController — Reset tồn kho & IMEI về 0

> Controller 1 hành động duy nhất: `POST api/ResetTonKhoImei/Reset` với `{IdSanPham, IdKho}`. Reset = **xuất sạch** tồn của 1 SP tại 1 kho bằng cách tạo 1 `PhieuXuat` loại "Xuất khác" (HoanThanh ngay), kéo `KhoChiTiet.SoLuong=0`, IMEI `DaNhapKho→DaXuatKho`, `SanPhamLoDate.SoLuong=0`. Scope **cứng theo `DonViId` + `IdChiNhanh` của user** (không reset chéo chi nhánh). Toàn bộ thay đổi DB nằm trong 1 transaction (ExecutionStrategy); 2 bước hậu-commit (sync kế toán + sync lịch sử kho) bọc try/catch riêng nên lỗi không làm fail reset. Đây là thao tác **phá tồn không hoàn tác** → UI gate 2 lớp (quyền + cấu hình bật) + confirm danger.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Reset tồn kho & IMEI của 1 SP tại 1 kho | Nút đỏ nhỏ, tooltip "Reset tồn kho và imei" (trên dòng tồn, màn quanly-kho-hangban) | `POST api/ResetTonKhoImei/Reset` body `{IdSanPham, IdKho}` | `ResetTonKhoImei_Reset` | public (`SBAuthorize`, roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QL-KHO, DV-QL-KHO-TONG) | Nút hiện khi có quyền **VÀ** `choPhepResetTonKho=true` (cấu hình kho). Payload null / `IdSanPham`=Empty / `IdKho`=Empty → lỗi sớm. | Validate `IdSanPham`+`IdKho` bắt buộc + tìm `SanPham`. Lấy `KhoChiTiet` theo DonVi+ChiNhanh+SP+Kho, **chỉ xét dòng `SoLuong>0`**; rỗng → trả lỗi "Tồn kho đã rỗng, không cần reset". Tạo 1 `PhieuXuat` (Ma sinh qua `SinhMaPhieu`, `XuatKhac`/`KieuLoaiXuat.Khac`, `HoanThanh`, `IdTuKho=idKho`, ghi chú "Reset tồn kho và imei"). Mỗi KCT tồn → 1 `PhieuXuatSanPham` (SoLuong=tồn, SoLuongTruoc=tồn, SoLuongSau=0, GiaVon=`GiaNhapTrungBinh`, ThanhTien cộng dồn `TongSoTien`; copy dung tích từ KCT, fallback DMSanPham/SanPham; quy đổi ĐVT nếu bật). Set KCT `SoLuong/SoLuongChoXuat/SoLuongDungTichChiTiet/GiaNhapTrungBinh=0`. IMEI trạng thái `DaNhapKho` → `DaXuatKho`, lưu `IdPhieuNhapCu=IdPhieu cũ`, `IdPhieu=phiếu xuất mới`. LoDate `SoLuong>0` → 0. Insert/Update tất cả trong 1 transaction. Trả `{MaPhieuXuat, IdPhieuXuat, SoKhoChiTietDuocReset, SoImeiDaXuat, SoLoDateDuocReset, TongSoLuongXuat, TongTien}`. | EF (transaction, không SP) | UI gate quyền **đã comment off** ở FE (`onResetTonKho` L173-177 + `loadCurrentUser` L164-171): hiện chỉ còn `*sbAuthorize` + `*ngIf=isAllowReset` ở HTML. Reset là thao tác **không undo** — không có phiếu nhập đối ứng để phục hồi. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)

Thứ tự rẽ nhánh trong `Reset(idSanPham, idKho, userLogin)`:
1. `idSanPham==Empty || idKho==Empty` → **lỗi** "IdSanPham và IdKho là bắt buộc".
2. Không tìm thấy `SanPham` (theo Id, nolock) → **lỗi** "Không tìm thấy sản phẩm".
3. Lọc `KhoChiTiet` theo `IdDonVi + IdChiNhanh + IdSanPham + IdKho` rồi giữ dòng `SoLuong>0`. Nếu rỗng → **lỗi** "Tồn kho đã rỗng, không cần reset" (no-op, không tạo phiếu).
4. Có tồn → tạo PhieuXuat + N dòng PhieuXuatSanPham (N = số KCT còn tồn). `IsCoSanPhamImei = SanPham.IsSanPhamImei ?? false`.
5. Dung tích dòng: ưu tiên `kct.TenDungTich/IdDungTich/SoLuongDungTichChiTiet`; thiếu → fallback `DMSanPham (KieuLoai=DungTich, SuDung=true)` / `SanPham.ValueDonViDem * SoLuong`.
6. Quy đổi ĐVT: chỉ set `SoLuongBanTheoDonViQuyDoi/JsonQuyDoi` khi `SanPham.IsSuDungQuyDoiDonViTinh==true`.
7. IMEI: chỉ đụng dòng `TrangThai==DaNhapKho` (đang chuyển kho / đã xuất bỏ qua).
8. LoDate: chỉ dòng `SoLuong>0`.
9. Hậu-commit: `DongBoPhieuSangKeToanService.DongBoPhieuXuat` (tự no-op nếu đơn vị không bật kế toán) + `SyncKhoChiTietLichSu(type=XuatKho)` — cả 2 bọc try/catch, lỗi không fail reset.

## Quan hệ
- **Gọi sang:** `CommonService.SinhMaPhieu(PhieuXuat,...)` (sinh mã phiếu xuất); `CommonService.SyncKhoChiTietLichSu` (đẩy event XuatKho cho báo cáo kho); `DongBoPhieuSangKeToanService.DongBoPhieuXuat` (đồng bộ chứng từ sang Accounting, set trước 4 field `[NotMapped]` IdPhanLoai/TenPhanLoai/MaTinhTrang/TenTinhTrang từ `DMKho`); `LogActivityHelper.LogActivity` (audit `EnumActivity.UPDATE`, TableId KhoChiTiet).
- **Dữ liệu (entities):** ghi mới `PhieuXuat` + `PhieuXuatSanPham`; update `KhoChiTiet` (SoLuong/SoLuongChoXuat/SoLuongDungTichChiTiet/GiaNhapTrungBinh→0), `SanPhamImei` (TrangThai→DaXuatKho, IdPhieu/IdPhieuNhapCu), `SanPhamLoDate` (SoLuong→0). Đọc `SanPham`, `DMSanPham` (dung tích), `DMKho` (phân loại/tình trạng xuất).
- **Ripple / đồng bộ:** PhieuXuat mới → Accounting (nếu bật kế toán) qua `DongBoPhieuXuat`; KhoChiTietLichSu nhận event XuatKho → báo cáo tồn/kho. Không bắn MassTransit IMEI-log như SanPhamImeiController (reset chỉ đổi state IMEI trực tiếp, không qua `EventSanPhamImeiLogMessage`).

## Luồng xuyên module
- **Reset tồn:** user bấm nút trên dòng tồn (đã bật cấu hình + có quyền) → confirm danger → `POST Reset` → trong 1 transaction: tạo PhieuXuat "Xuất khác" HoanThanh + zero KhoChiTiet + IMEI→DaXuatKho + LoDate→0 → sau commit đẩy PhieuXuat sang **Kế toán** (chứng từ xuất) + đẩy **KhoChiTietLichSu** (XuatKho) → grid FE reload. PhieuXuat sinh ra hiển thị/đối soát như phiếu xuất khác bình thường.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/ResetTonKhoImeiController.cs`
  - L8 attribute `TienIchAttribute("ResetTonKhoImeiController", "Quản lý tồn kho imei", TI_Kho)` — đăng ký tiện ích nhóm Kho.
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/ResetTonKhoImeiService.cs`
  - Thao tác **không hoàn tác**: không sinh phiếu nhập đối ứng; tồn về 0 vĩnh viễn (chỉ còn vết qua PhieuXuat "Reset tồn kho và imei").
  - L184-188 chỉ reset IMEI `DaNhapKho`; IMEI `DangChuyenKho` của SP/kho đó **không bị đụng** → có thể lệch giữa KhoChiTiet (=0) và IMEI đang chuyển.
  - L244-255 sync kế toán nuốt exception (`catch {}`) → nếu đẩy chứng từ lỗi, reset vẫn báo thành công, kế toán có thể thiếu phiếu (phải đối soát thủ công).
  - L258-268 `SyncKhoChiTietLichSu` cũng nuốt exception (comment "lichsu sẽ sync lại sau").
- FE `scrm_frontend/src/app/pages/WarehouseProduct/quanly-kho/quanly-kho-hangban/quanly-kho-hangban.component.ts`
  - L164-171 `loadCurrentUser` (gate isSuperUser/isCSKH) + L174-177 check `isAllowReset` trong `onResetTonKho` đều **comment off** → hiện chỉ gate bằng `*sbAuthorize` + `*ngIf=isAllowReset` ở template (L259-260). Route `quan-ly-khohangban` cũng đang comment trong routing module (L101-102).
