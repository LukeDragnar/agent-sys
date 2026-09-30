---
controller: KhuVucController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Booking-POS — màn 'Quản lý khu vực' (danh sách khu vực + chỗ ngồi theo cây; nút Thêm/Sửa/Xóa khu vực; dùng cho sơ đồ bàn POS/bi-a). Có thêm view bi-a quảng cáo (AllowAnonymous)."
muc_dich: "Quản lý khu vực (nhóm bàn/chỗ ngồi) theo Đơn vị + Chi nhánh; cung cấp dữ liệu khu vực-chỗ ngồi cho sơ đồ bàn POS, dropdown bàn, cấu hình SmartDevice và màn view bi-a quảng cáo."
alias: ["khu vực", "quản lý khu vực", "sơ đồ bàn", "khu vực bi-a", "khu vực POS", "config smart device theo khu vực"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhuVuc, ChoNgoi, DatLich, DatLichChiTiet]
stored_procs: []
depends: [Booking (KhuVuc/ChoNgoi/DatLich), Redis/Cache (KHU_VUC, CHO_NGOI), User (tên nhân viên phục vụ)]
cross_module_in: [POS/thu ngân — gọi GetAllKhuVucThuocChiNhanh / TimChoNgoiDangDung dựng sơ đồ bàn; SmartDevice — GetDSChiNhanhConfigSmartDevicePrivate/SaveChoNgoiSmartDevice (PrivateSBAuthorize); màn quảng cáo bi-a — GetAllKhuVucThuocChiNhanhAllowAll (AllowAnonymous)]
cross_module_out: [Cache KHU_VUC + CHO_NGOI theo ĐơnVị+ChiNhánh (set lại mỗi lần ghi); Xóa khu vực → xóa kèm toàn bộ ChoNgoi thuộc khu vực (cascade trong transaction)]
---

# KhuVucController — Quản lý khu vực (Booking / POS)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list khu vực + chỗ ngồi) | "Tìm kiếm" / load grid | POST `api/KhuVuc/TimTheoDieuKien` (và alias `TimChoNgoiTheoDonVi` — cùng service method) | `KhuVuc_TimTheoDieuKien` | public | màn list | Gán `IdDonVi`+`IdChiNhanh` từ user → list khu vực (group theo Id/Ten/Stt, sắp theo Stt), phân trang; với mỗi khu vực gắn `lstChoNgoi`, mỗi chỗ ngồi tính `IsDangSuDung` dựa trên DatLich `DangPhucVu` (mặc định TuNgay = -1 tháng, DenNgay = nay) | EF (TableWithNolock, ghép trong memory bằng `AsEnumerable`) | `TimChoNgoiTheoDonVi` (Service via controller:41) KHÔNG có `[SBAuthorize(key)]` riêng → chỉ login (khác bản có key của `TimTheoDieuKien`) dù gọi cùng method |
| 2 | Thêm mới khu vực | "Thêm mới" (modal) | POST `api/KhuVuc/ThemMoi` (override base) | `KhuVuc_ThemMoi` | public | nút Thêm | `InsertAsync`: chặn trùng tên (cùng DonVi+ChiNhánh, `SuDung=true`, so sánh Trim+Upper); set IdDonVi/IdChiNhanh từ user; insert; `LoadCache()` set lại cache KHU_VUC+CHO_NGOI | EF | - |
| 3 | Cập nhật thông tin khu vực | "Sửa" (modal) | POST `api/KhuVuc/CapNhatThongTin` (override base) | `KhuVuc_CapNhatThongTin` | public | nút Sửa per-row | `UpdateAsync`: chặn trùng tên (khác Id); MarkDirty Ten/GhiChu/SuDung/Stt; update; `LoadCache()` | EF | - |
| 4 | Xóa khu vực | "Xóa" (confirm) | POST `api/KhuVuc/Xoa/{id}` (override base) | `KhuVuc_Xoa` | public | nút Xóa per-row | Lấy chi tiết (`TimChoNgoiTheoId`); nếu khu vực `IsDangSuDung=true` → chặn "Khu vực đang sử dụng không thể xóa!"; ngược lại `DeleteAsync`: trong transaction xóa toàn bộ ChoNgoi thuộc khu vực (`IsDelete != true`) rồi xóa khu vực; `LoadCache()` | EF (transaction + execution strategy) | - |
| 5 | DS bàn bi-a theo đơn vị | grid bi-a | POST `api/KhuVuc/DanhSachBanBiaDonVi` | `[SBAuthorize()]` rỗng (chỉ login) | public | màn bi-a | List khu vực + chỗ ngồi (chỉ Id/Ten/ThuTu/IdDevice) theo DonVi+ChiNhánh, sắp theo Stt/ThuTu/Ten; `IsDangSuDung` = khu vực có chỗ ngồi | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [public — `[SBAuthorize()]` rỗng] **`TimChoNgoiDangDung`** (POST) — list chỗ ngồi đang có nhân viên phục vụ (`IdNhanVienPhucVu != null/Empty`) theo DonVi+ChiNhánh; enrich tên + avatar nhân viên từ bảng User. Dùng cho sơ đồ bàn POS.
- [public — `[SBAuthorize()]` rỗng] **`TimChoNgoiTheoIds`** (POST, body `List<Guid>`) — trả chỗ ngồi theo danh sách Id (Id/Ten/IdDevice/IdKhuVuc + TenKhuVuc); rỗng/null → lỗi "Ids là bắt buộc".
- [public — `[SBAuthorize()]` rỗng] **`TimAllChoNgoiTheoDonVi`** / **`TimAllChoNgoiTheoDonViTree`** / **`TimAllChoNgoiTheoChiNhanhTree/{id}`** (POST) — lấy cây khu vực→chỗ ngồi theo DonVi+ChiNhánh (Tree: trả flat {Id chỗ ngồi, Ten, Groupby = tên khu vực}). `TimAllChoNgoiTheoDonVi` join thêm DatLichChiTiet/DatLich để tính `IsDangSuDung`.
- [public — `[SBAuthorize()]` rỗng] **`GetAllKhuVucThuocChiNhanh`** (GET) — khu vực `SuDung=true` của DonVi+ChiNhánh user (qua cache KHU_VUC), sort Stt, map DTO. POS gọi dựng dropdown/sơ đồ.
- [internal — `[PrivateSBAuthorize]`] **`GetDSChiNhanhConfigSmartDevicePrivate`** (POST) — gom khu vực→chỗ ngồi theo từng chi nhánh trong đơn vị + IdDevice hiện tại; phục vụ cấu hình SmartDevice. **`SaveChoNgoiSmartDevice`** (POST, body `List<ChiNhanhConfigSmartDeviceDto>`) — cập nhật `IdDevice` cho từng chỗ ngồi + ghi LichSuTacDong.
- [anonymous — `[AllowAnonymous]`] **`GetAllKhuVucThuocChiNhanhAllowAll`** (POST, body có `IdDonVi`/`IdChiNhanh`) — khu vực `SuDung=true` theo đơn vị/chi nhánh truyền vào; dùng cho màn view bi-a quảng cáo (không token).

## Quan hệ
- **Gọi sang:** không gọi service khác (toàn bộ EF + Redis nội bộ Booking). Cache: `CacheKey.KHU_VUC`, `CacheKey.CHO_NGOI` (format theo DonViId+IdChiNhanh).
- **Dữ liệu (entities):** `KhuVuc` (Ten, GhiChu, SuDung, Stt, IdDonVi, IdChiNhanh), `ChoNgoi` (Ten, ThuTu, IdDevice, IdNhanVienPhucVu, IdKhuVuc), `DatLich`/`DatLichChiTiet` (tính trạng thái DangPhucVu cho chỗ ngồi), `User` (tên/avatar nhân viên phục vụ).
- **Ripple / đồng bộ:** mỗi lần Thêm/Sửa/Xóa → `LoadCache()` ghi đè cache KHU_VUC + CHO_NGOI theo DonVi+ChiNhánh → POS/dropdown lần sau lấy dữ liệu mới. Xóa khu vực → cascade xóa ChoNgoi.

## Lỗi đã biết / lưu ý kỹ thuật
- **`TimTheoDieuKien` thực thi join trong memory** — `KhuVucService.cs:220-221` gọi `tblChoNgoi.AsEnumerable()` + `tblDatLich.AsEnumerable()` rồi LINQ-to-Objects; kéo toàn bộ chỗ ngồi + đặt lịch (trong khoảng ngày) của DonVi+ChiNhánh về app rồi lọc → rủi ro hiệu năng khi nhiều bàn/lịch.
- **`IsDangSuDung` của chỗ ngồi suy ra từ `DatLich.KhachHang.Contains(idChoNgoi)`** — `KhuVucService.cs:233` so khớp chuỗi `KhachHang` chứa Id chỗ ngồi (không phải khóa ngoại); phụ thuộc format dữ liệu, dễ sai nếu KhachHang chứa chuỗi trùng.
- **`SaveChoNgoiSmartDeviceAsync` thiếu null-check thứ tự** — `KhuVucService.cs:604-607`: `DeepClone(entity)` và `lstChoNgoiOld.Add` chạy TRƯỚC `if (choNgoi == null) continue;`, và `entity` có thể null nếu Id không có trong `lstChoNgoi` → nguy cơ NRE khi clone/đọc `entity` null.
- **`TimChoNgoiTheoId` không null-check `kv`** — `KhuVucService.cs:437-439`: nếu query `FirstOrDefaultAsync()` trả null thì `kv.lstChoNgoi` ném NRE (luồng Xóa gọi method này trước khi xóa).
- **Hai endpoint `TimTheoDieuKien` và `TimChoNgoiTheoDonVi` trùng logic** — controller:34/41 cùng gọi `TimTheoDieuKienAsync` nhưng chỉ endpoint đầu có permission key, endpoint sau chỉ cần login → khác mức quyền cho cùng dữ liệu.
- Code thật: BE controller `Booking/Booking.API/Controllers/KhuVucController.cs`, service `Booking/Booking.Service/Generated/KhuVucService.cs`.
