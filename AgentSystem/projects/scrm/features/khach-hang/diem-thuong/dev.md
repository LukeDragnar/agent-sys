# Dev — Điểm thưởng & đổi thưởng (Cấu hình đổi điểm)

> Tài liệu kỹ thuật. Service: **Customer**. Trace gốc: `ConfigDoiDiem_GetCauHinhDoiDiem`.

## Màn hình & route

- **FE component**: `ConfigDoidiemComponent`
- **Route**: `config-doi-diem` (khai báo trong `customer-routing.module.ts`)
- **Tiêu đề**: "Cấu hình đổi điểm"
- **Cấu trúc UI**: 1 màn hình gồm
  - Form cấu hình chung (checkbox cho phép tích/đổi + 2 ô tỷ lệ `tienThanhDiem`/`diemThanhTien` + checkbox loại hàng).
  - Grid "Quy đổi chi tiết" theo hạng/nhóm khách hàng (thêm/sửa/xóa, có phân trang qua `BaseListComponent`).

## Controller

`Customer/Customer.API/Controllers/ConfigDoiDiemController.cs` — `[Route("api/[controller]")]`, `[SBAuthorize]`.

| Endpoint | Method | Permission (claim) | Vai trò | Mô tả |
|---|---|---|---|---|
| `GetCauHinhDoiDiem` | GET | `ConfigDoiDiem_GetCauHinhDoiDiem` | DV-AD-TONG | Lấy cấu hình chung; auto khởi tạo nếu chưa có |
| `CapNhatThongTin` | POST | `ConfigDoiDiem_CapNhatThongTin` | DV-AD-TONG | Cập nhật cấu hình chung + ghi `EventLichSuTacDong` |
| `KhoiTao` | GET | `ConfigDoiDiem_KhoiTao` | DV-AD-TONG, DV-QLDV | Tạo bản cấu hình mặc định cho đơn vị |
| `ThemMoiDoiDiemChiTiet` | POST | `ConfigDoiDiem_ThemMoiDoiDiemChiTiet` | DV-AD-TONG, DV-QLDV | Thêm quy đổi chi tiết theo hạng/nhóm |
| `CapNhatDoiDiemChiTiet` | POST | `ConfigDoiDiem_CapNhatDoiDiemChiTiet` | DV-AD-TONG, DV-QLDV | Sửa quy đổi chi tiết |
| `XoaDoiDiemChiTiet/{id}` | POST | `ConfigDoiDiem_XoaDoiDiemChiTiet` | DV-AD-TONG, DV-QLDV | Xóa quy đổi chi tiết |
| `TimTheoDieuKienDoiDiemChiTiet` | POST | `ConfigDoiDiem_TimTheoDieuKienDoiDiemChiTiet` | DV-AD-TONG, DV-QLDV | List + filter (keyword/hạng/nhóm) có phân trang |

`CapNhatThongTin` deep-clone bản cũ → update → đọc lại bản mới → `_logActivityHelper.LogActivity` với `TableId = SCRM_CUSTOMER_ConfigDoiDiem`, activity `UPDATE`.

## Entities

`Customer/Customer.DataContext/Entity/`:

- **`ConfigDoiDiem`** (cấu hình chung 1 bản/đơn vị): `DiemThanhTien` (tỷ lệ điểm→tiền, dùng khi đổi điểm), `TienThanhDiem` (tỷ lệ tiền→điểm, dùng khi tích điểm), `IdDonVi`, `IsAllowTichDiem`, `IsAllowDoiDiem`, `IsThuNganLuaChonDoiDiem`, và các cờ loại hàng được tích: `IsTichDiemDichVu`, `IsTichDiemSanPham`, `IsTichDiemCombo`, `IsTichDiemThe`, `IsTichDiemThuKhac`. Có extension `Clone()`.
- **`ConfigDoiDiemChiTiet`** (quy đổi riêng theo hạng/nhóm): `IdDonVi`, `Ten`, `IdHangKhachHang?`, `IdNhomKhachHang?`, `TiLeQuyDoiMotDonVi`, `LoaiQuyDoi` (1 = TienThanhDiem, 2 = DiemThanhTien — FE hiện luôn gửi `loaiQuyDoi = 1`).
- **`KhachHang`** (nơi điểm cộng/trừ): `SoDiemHienTai`, `SoDiemTichLuy`, `DiemTamTru` (giữ chỗ điểm chưa thanh toán xong).

DTO: `Libs/Models.Core/DTO/Customer.Client/ConfigDoiDiem/ConfigDoiDiemDto.cs` — gồm `ConfigDoiDiemDto`, `ConfigDoiDiemRedis` (+ `ListConfigDoiDiemChiTiet` cho cache), `ConfigDoiDiemChiTietDto/Filter/Redis`, `LayDiemQuyDoiCuaDonHangPayload/Dto`.

## Luồng code

### Đọc cấu hình (`GetCauHinhDoiDiem`)
`ConfigDoiDiemService.GetCauHinhDoiDiem()` → nếu đơn vị chưa có row (`TableWithNolock`) thì gọi `KhoiTao(DonViId)` (mặc định: bật tích+đổi, `DiemThanhTien=10000`, `TienThanhDiem=10000`, bật mọi cờ loại hàng) → map sang `ConfigDoiDiemDto`.

### Cập nhật cấu hình chung
`ConfigDoiDiemService.UpdateAsync(ConfigDoiDiem)` override: validate (`IsAllowDoiDiem`/`IsAllowTichDiem` không null; nếu bật đổi điểm thì `DiemThanhTien` không null và ≥ 1; nếu bật tích điểm thì `TienThanhDiem` không null và ≥ 1) → `MarkDirty` từng field → **`ClearCache_CauHinhDoiDiem_DonVi`** → `base.UpdateAsync`.

### Quy đổi chi tiết
`ThemMoi/CapNhat/Xoa/TimTheoDieuKien...ChiTiet`. `ValidateModel` chặn: tên rỗng, phải chọn hạng **hoặc** nhóm, `LoaiQuyDoi` ∈ {1,2}, `TiLeQuyDoiMotDonVi ≥ 1`, không trùng cặp (hạng, nhóm), hạng/nhóm phải tồn tại trong đơn vị. Mọi thao tác CUD đều `ClearCache_CauHinhDoiDiem_DonVi`.

### Cache
- Đọc: `ICommonCacheService.GetCache_CauHinhDoiDiem_DonVi(idDonVi)`; miss → `CommonRetrieveDataForCacheService.RetrieveDataForCache_CauHinhDoiDiem_DonVi` rồi `SetCache...` TTL 1 tháng. Cache dùng `ConfigDoiDiemRedis` (kèm list chi tiết).
- Sửa cấu hình → clear cache để lần dùng kế đọc bản mới.

### Tích / đổi điểm thực tế (lúc thanh toán) — `CustomerUpdateService`
`Customer/Customer.Service/Generated/CustomerUpdateService.cs`:
- `InitChiTieuKhachHangInsert_New(configDoiDiem, khachHang, donHang)`:
  - `SoTienDoiDiem` = tổng phần thanh toán bằng phương thức **Điểm** (`EnumLoaiQuy.Diem` / `PhuongThucThanhToanEnum.Diem`); chặn nếu vượt `TongThanhTien`.
  - `SoTienTichDiem` = tổng tiền các dòng đơn theo loại được bật (`IsTichDiemDichVu/SanPham/The/NapThe→The/Combo/ThuKhac`), bỏ phần trả bằng Điểm và TheTienMat; clamp ≤ `TongThanhTien`.
- Vòng tính điểm (quanh dòng 686–746):
  - Đổi điểm: `item.DoiDiem = SoTienDoiDiem / configDoiDiem.DiemThanhTien` (cần `IsAllowDoiDiem` và `DiemThanhTien ≥ 1`).
  - Tích điểm: `tienThanhDiem = configDoiDiem.TienThanhDiem` mặc định, nếu có `ConfigDoiDiemChiTiet` khớp `(IdNhomKhachHang, IdHangKhachHang, LoaiQuyDoi=TienThanhDiem)` thì dùng `TiLeQuyDoiMotDonVi`; `item.TichDiem = floor(SoTienTichDiem / tienThanhDiem)`.
  - Trừ `TongSoDiemTru` (combo gift `IsTruTichDiemKhachHang=true`) vào `item.TichDiem`; phần thiếu cộng vào `tongDiemThieuDeTru` rồi trừ trực tiếp `khachHang.SoDiemHienTai` (clamp 0).
  - Đổi điểm: kiểm tra `soDiemDaDoi ≤ SoDiemHienTai` rồi trừ.
- `LayDiemQuyDoiCuaDonHang` (trong `Common/CommonService.cs`): đọc Mongo `KhachHangDiemThuongLichSu` theo `IdDonHang`, tổng `SoDiemSuDung` (KieuLoai=DoiDiem) và `SoDiemTichLuy` (KieuLoai=TichDiem). Order service gọi qua `/api/Common/LayDiemQuyDoiCuaDonHang` để in lên phiếu.

## Bug-prone

- **`DiemThanhTien` vs `TienThanhDiem` dễ nhầm**: `TienThanhDiem` = bao nhiêu **tiền → 1 điểm** (tích); `DiemThanhTien` = **1 điểm → bao nhiêu tiền** (đổi). Tên ngược trực giác.
- **Tích điểm dùng `Math.Floor`, đổi điểm dùng `Convert.ToInt32`** (round). Hai cách làm tròn khác nhau → cẩn thận khi đối soát.
- **Cache TTL 1 tháng**: nếu quên `ClearCache` ở một path sửa mới, cấu hình cũ còn áp dụng rất lâu. Mọi CUD cấu hình **phải** clear cache `DonVi`.
- **`GetCauHinhDoiDiem` tự khởi tạo** khi đọc → side-effect insert ngay trong GET; nếu đơn vị có concurrent first-call có thể chạy `KhoiTao` 2 lần (đã có guard `Đã tồn tại cấu hình`).
- **FE luôn gửi `loaiQuyDoi = 1`** (`addOrUpdateDoiDiemChiTiet`), nên quy đổi chi tiết hiện chỉ áp cho chiều tích điểm (TienThanhDiem); chiều đổi điểm theo hạng/nhóm chưa dùng tới dù entity hỗ trợ `LoaiQuyDoi=2`.
- **Validate phân tán FE + BE**: ràng buộc tỷ lệ ≥ 1 lặp ở cả `addOrEdit` (FE) và `UpdateAsync`/`ValidateModel` (BE) — sửa rule nhớ đồng bộ hai nơi.
- **Thanh toán không có cấu hình** trả lỗi "Chưa cấu hình đổi điểm!" — đảm bảo `KhoiTao`/Get đã chạy cho đơn vị trước khi bán.

## File source

BE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm`):
- `Customer/Customer.API/Controllers/ConfigDoiDiemController.cs`
- `Customer/Customer.Service/Generated/ConfigDoiDiemService.cs`
- `Customer/Customer.Service/Generated/CustomerUpdateService.cs` (tích/đổi khi thanh toán)
- `Customer/Customer.Service/Generated/Common/CommonService.cs` (`LayDiemQuyDoiCuaDonHang`)
- `Customer/Customer.Service/Generated/Cache/CommonRetrieveDataForCacheService.cs`, `CommonCacheService.cs`, `CommonKeyCache.cs`
- `Customer/Customer.DataContext/Entity/ConfigDoiDiem.cs`, `ConfigDoiDiemChiTiet.cs`, `KhachHang.cs`
- `Libs/Models.Core/DTO/Customer.Client/ConfigDoiDiem/ConfigDoiDiemDto.cs`, `ConfigDoiDiemFilterDto.cs`
- (consumer phía Order) `Order/Order.Service/Generated/DonHangService.cs`, `DonHangServicePartial.cs`

FE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`):
- `src/app/pages/Customer/config-doidiem/config-doidiem.component.ts` / `.html` / `.scss`
- `src/app/services/customer/config-doidiem.service.ts`
- `src/app/pages/Customer/customer-routing.module.ts` (route `config-doi-diem`)
