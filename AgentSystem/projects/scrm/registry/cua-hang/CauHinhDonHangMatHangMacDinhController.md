---
controller: CauHinhDonHangMatHangMacDinhController
module: cua-hang
service: WarehouseProduct
man_hinh_ui: "Cửa hàng / Kho — màn 'Cấu hình mặt hàng mặc định' (route `cauhinh-mathang-macdinh`, CauhinhMathangMacdinhComponent — list + nút Thêm mới + thao tác per-row; form thêm/sửa là modal CauhinhMathangMacdinhEditComponent)"
muc_dich: "Quản lý các bộ cấu hình mặt hàng/dịch vụ mặc định + dịch vụ tính giờ/chuyển bàn + khu vực-chỗ ngồi, để khi tạo đơn tại quầy (POS thu ngân) tự gợi ý SP/DV mặc định và tính giờ theo bàn."
alias: ["cấu hình mặt hàng mặc định", "mặt hàng mặc định khi tạo đơn", "cấu hình đơn tại quầy", "dịch vụ tính giờ", "dịch vụ chuyển bàn", "cấu hình bàn / chỗ ngồi POS", "combo dùng trước"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhDonHangMatHangMacDinh, DichVu, DichVuChiNhanh, DichVuGiaTheoKhungGio, SanPham]
stored_procs: []
depends: [WarehouseProduct, Booking (KhuVuc/ChoNgoi), ElasticSearch (SanPham), Redis/Cache (DichVu/Combo/CauHinh), Order/thungan (consumer)]
cross_module_in: [Order/thungan — ds-ban-nv.component & dang-phuc-vu-bi-a.component gọi LayCauHinhSuDung/LayCauHinhHienTai; Booking — LayTatCaDichVuTinhGioTheoKhuVuc (AllowAnonymous, lấy giá theo khung giờ)]
cross_module_out: [Cache CauHinhDonHangMatHangMacDinh theo ĐơnVị + theo ChiNhánh (clear mỗi lần ghi); màn POS thu ngân (gợi ý SP/DV mặc định + tính giờ theo bàn)]
---

# CauHinhDonHangMatHangMacDinhController — Cấu hình mặt hàng mặc định khi tạo đơn tại quầy

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list) | "Tìm kiếm" / load grid | POST `api/CauHinhDonHangMatHangMacDinh/TimTheoDieuKien` | `CauHinhDonHangMatHangMacDinhController_TimTheoDieuKien` | public | màn list | List cấu hình theo DonVi **+ ChiNhánh** của user, lọc theo Keyword (Ten.Contains) + SuDung; enrich tên khu vực/chỗ ngồi (gọi Booking), tên SP (Elastic), tên DV/combo (cache); phân trang | EF + cache DV/Combo + ES SanPham + HTTP Booking | - |
| 2 | Thêm mới cấu hình | "Thêm mới cấu hình" → `onAddNew` (modal) | POST `api/CauHinhDonHangMatHangMacDinh/ThemMoi` | `CauHinhDonHangMatHangMacDinhController_ThemMoi` | public | nút Thêm | Validate (tên bắt buộc/không trùng, SP/DV tồn tại, DV tính giờ/chuyển bàn hợp lệ, bàn chưa cấu hình trùng); insert; nếu `IsMacDinh=true` → bỏ mặc định các cấu hình khác cùng DonVi+ChiNhánh; clear cache; transaction | EF (transaction + execution strategy) | - |
| 3 | Cập nhật cấu hình | "Sửa" → `onEdit` (modal) | POST `api/CauHinhDonHangMatHangMacDinh/CapNhat` | `CauHinhDonHangMatHangMacDinhController_CapNhat` | public | nút Sửa per-row | Như ThemMoi nhưng update theo `Id`; serialize lại `CauHinhJson` (SP/DV/Combo); guard mặc định-duy-nhất; clear cache | EF (transaction) | Message trả về "Thêm mới thành công!" (copy nhầm — vẫn là update) |
| 4 | Xem chi tiết / mở sửa | (mở modal edit) | POST `api/CauHinhDonHangMatHangMacDinh/TimTheoId/{id}` | `CauHinhDonHangMatHangMacDinhController_TimTheoId` | public | - | Trả 1 cấu hình theo DonVi+ChiNhánh+Id; deserialize `CauHinhJson` → list SP (enrich từ Elastic), list DV (enrich từ cache), list combo | EF + ES + cache | - |
| 5 | Xóa cấu hình | "Xóa" → `onDelete` (confirm) | POST `api/CauHinhDonHangMatHangMacDinh/Xoa/{id}` | `CauHinhDonHangMatHangMacDinhController_Xoa` | public | nút Xóa per-row | Xóa cứng (`DeleteAsync`) theo DonVi+ChiNhánh+Id; clear cache | EF | **Clear cache sai key**: dùng `userLogin.Id` (id user) thay vì `userLogin.DonViId` → cache theo đơn vị KHÔNG bị xóa sau khi xóa cấu hình (Service:515) |
| 6 | Bật/tắt trạng thái sử dụng | toggle "Trạng thái sử dụng" → `onSuDung` | POST `api/CauHinhDonHangMatHangMacDinh/ThayDoiTrangThaiSuDung/{id}` | `CauHinhDonHangMatHangMacDinhController_ThayDoiTrangThaiSuDung` | public | per-row | Đảo bit `SuDung` (toggle, không exclusive); clear cache. (Khối logic exclusive cũ đã comment) | EF | - |
| 7 | Bật/tắt trạng thái mặc định | toggle "Trạng thái mặc định" → `onMacDinh` | POST `api/CauHinhDonHangMatHangMacDinh/ThayDoiTrangThaiMacDinh/{id}` | `CauHinhDonHangMatHangMacDinhController_ThayDoiTrangThaiMacDinh` | public | per-row | Đảo bit `IsMacDinh`; nếu bật → bỏ mặc định mọi cấu hình khác cùng DonVi+ChiNhánh (mặc định là duy nhất); clear cache | EF | Lookup `data` chỉ filter theo `IdDonVi` (thiếu IdChiNhanh) — khác với các method còn lại có thêm IdChiNhanh (Service:212) |

## Endpoint nội bộ (private / service-to-service)
- [public — chỉ cần đăng nhập, `[SBAuthorize()]` rỗng] **`LayCauHinhHienTai`** (POST) — POS/thu ngân lấy cấu hình hiện tại theo **ĐơnVị + ChiNhánh** từ cache (Redis 7 ngày); cache miss → retrieve + set; lỗi → trả list rỗng (không ném). Trả dạng `CauHinhDonHangMatHangMacDinhRedis`.
- [public — `[SBAuthorize()]` rỗng] **`LayCauHinhSuDung`** (POST) — trả list cấu hình `SuDung=true` (chỉ Id/Ten/IdKhuVuc/IdChoNgoi) cho dropdown bàn POS (Order/thungan `ds-ban-nv` gọi `getCauHinhSanPhamMacDinh`).
- [anonymous — `[AllowAnonymous]`] **`LayTatCaDichVuTinhGioTheoKhuVuc`** (GET, query `idDonVi`,`idChiNhanh`) — trả giá theo khung giờ/ngày-trong-tuần cho khu vực: join CauHinh × DichVu (SuDung+IsTinhGio, có DichVuChiNhanh) × DichVuGiaTheoKhungGio; chỉ lấy cấu hình có `IdDichVuTinhGio` và có bảng giá. Dùng cho hiển thị giá tính giờ.
- [public — KHÔNG có attribute SBAuthorize, chỉ controller-level] **`CheckBanTonTaiTrongCauHinh`** (POST) — kiểm tra danh sách `IdChoNgoi` (bàn) đã bị cấu hình ở bản ghi khác chưa; trả tên các cấu hình trùng. FE gọi để chặn cấu hình trùng bàn trước khi lưu.

## Quy tắc nghiệp vụ (gating / ràng buộc khi ThemMoi/CapNhat — `ValidateModel`)
Thứ tự kiểm tra (dừng ở lỗi đầu tiên):
1. `Ten` rỗng → "Tên cấu hình không được để trống!". (`SuDung == null` → lỗi, nhưng `SuDung` là `bool` non-nullable trên DTO → nhánh này thực tế không bao giờ true).
2. Trùng tên: tồn tại bản ghi cùng DonVi+ChiNhánh+Ten (khác Id) → "Tên cấu hình đã tồn tại!".
3. UPDATE mà `Id == null` → "Không tìm thấy Id bản ghi cấu hình!".
4. `DanhSachDichVuMacDinh` (distinct) — đếm DV thuộc DonVi phải khớp số lượng, lệch → "Không tìm thấy dịch vụ!".
5. `DanhSachSanPhamMacDinh` (distinct) — đếm SP thuộc DonVi phải khớp, lệch → "Không tìm thấy sản phẩm!".
6. `IdDichVuTinhGio` (nếu có): DV phải tồn tại (theo DonVi) → "...không tồn tại."; `IsTinhGio != true` → "...không được cài đặt là dịch vụ tính giờ"; trùng (cùng IdKhuVuc + IdDichVuTinhGio, khác Id) → "...đã được thiết lập trùng khu vực ở cấu [Ten]." (KhuVuc null = mặc định cho mọi khu vực).
7. `IdDichVuChuyenBan` (nếu có): DV tồn tại; phải `IsTinhGio==true && IsDichVuBan==true` → ngược lại "...không được cài đặt là dịch vụ chuyển bàn".
8. `IsExisted(Id, IdChoNgoi)`: bàn (`IdChoNgoi` join bằng `;`) không được trùng với bản ghi khác → trả tên các cấu hình trùng.
- **Invariant mặc định-duy-nhất**: tại bất kỳ thời điểm, mỗi DonVi+ChiNhánh chỉ có tối đa 1 cấu hình `IsMacDinh=true` — enforce ở ThemMoi/CapNhat/ThayDoiTrangThaiMacDinh (set các bản khác về false khi bật 1 bản).

## Quan hệ
- **Gọi sang:**
  - `Booking` — `GET /api/Common/LayAllKhuVucChoNgoi` (qua `_clientHelper`, token user) để enrich tên khu vực + chỗ ngồi.
  - `_commonService.GetSanPhamElasticByIds` — lấy info SP mặc định từ ElasticSearch.
  - Cache (Redis) — `GetCache_DichVu_DonVi`, `GetOrSetCache_Combo_DonVi`, `Get/Set/ClearCache_CauHinhDonHangMatHangMacDinh_DonVi`, `Get/SetCache_CauHinhDonHangMatHangMacDinh_ChiNhanh` (TTL 7 ngày).
- **Dữ liệu (entities):** `CauHinhDonHangMatHangMacDinh` (Ten, SuDung, IsMacDinh, CauHinhJson, IdKhuVuc, IdChoNgoi, IdDichVuTinhGio, IdDichVuChuyenBan; cấu hình SP/DV/Combo nằm trong `CauHinhJson` = `CauHinhJsonMatHangMacDinh`), `DichVu`, `DichVuChiNhanh`, `DichVuGiaTheoKhungGio`, `SanPham`.
- **Ripple / đồng bộ:** mỗi lần ghi (Thêm/Sửa/Xóa/đổi trạng thái) → clear cache cấu hình theo đơn vị → màn POS thu ngân lần sau lấy lại cấu hình mới (gợi ý SP/DV mặc định + dịch vụ tính giờ theo bàn).

## Luồng xuyên module (gợi ý mặt hàng mặc định khi tạo đơn tại quầy)
```
Admin tạo/sửa cấu hình (gán SP/DV mặc định + DV tính giờ + khu vực/bàn) → CauHinhJson + clear cache
  → POS thu ngân (Order/thungan): chọn bàn/khu vực
      → LayCauHinhSuDung / LayCauHinhHienTai (đọc cache theo ĐơnVị+ChiNhánh)
      → auto thêm SP/DV/combo mặc định vào đơn; DV tính giờ → tính tiền theo khung giờ (LayTatCaDichVuTinhGioTheoKhuVuc)
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`Xoa` clear cache sai key** — `Service:515` dùng `ClearCache_..._DonVi(userLogin.Id)` (id của user) thay vì `userLogin.DonViId`; cache cấu hình theo đơn vị KHÔNG được xóa sau khi xóa → POS có thể còn cache cấu hình đã xóa tới khi TTL hết (7 ngày) hoặc có thao tác ghi khác clear đúng.
- **`ThayDoiTrangThaiMacDinh` lookup thiếu IdChiNhanh** — `Service:212` chỉ filter `Id + IdDonVi` (các method khác filter thêm `IdChiNhanh`); rủi ro nếu cùng đơn vị nhiều chi nhánh có id trùng phạm vi.
- **`CheckBanTonTaiTrongCauHinh` không gắn permission** — chỉ có `[HttpPost]`, không `[SBAuthorize]`/`[AllowAnonymous]` → áp auth mặc định controller; cần xác nhận có chủ ý (endpoint check public theo controller).
- **`LayTatCaDichVuTinhGioTheoKhuVuc` là `[AllowAnonymous]`** — không token, nhận `idDonVi`/`idChiNhanh` từ query → trả bảng giá tính giờ cho bất kỳ ai biết id; cân nhắc lộ dữ liệu giá.
- **CapNhat trả message "Thêm mới thành công!"** (Service:176) — copy nhầm message, không ảnh hưởng logic.
- **`IsExisted` load toàn bảng `AsEnumerable()`** (Service:799) — kéo full bản ghi CauHinh về memory rồi lọc bàn trùng; lưu ý hiệu năng nếu số cấu hình lớn (và không filter theo DonVi/ChiNhánh — quét cross-tenant).
- **Cấu hình SP/DV/Combo lưu trong cột JSON `CauHinhJson`** (không cột riêng) → query theo SP/DV phải deserialize; entity có thêm `IdKhuVuc/IdChoNgoi/IdDichVuTinhGio/IdDichVuChuyenBan` ở cột riêng.
- Code thật: BE `WarehouseProduct/WarehouseProduct.API/Controllers/CauHinhDonHangMatHangMacDinhController.cs`, service `WarehouseProduct.Service/Generated/CauHinhDonHangMatHangMacDinhService.cs`, entity `WarehouseProduct.DataContext/Entity/CauHinhDonHangMatHangMacDinh.cs`; FE `pages/WarehouseProduct/cauhinh-mathang-macdinh/` + service `services/warehouseproduct/cauhinh-donhang-mathang-macdinh.service.ts`.
