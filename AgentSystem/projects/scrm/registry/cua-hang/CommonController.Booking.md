---
controller: CommonController
module: cua-hang
service: Booking
man_hinh_ui: "nội bộ / dùng chung — không có 1 màn riêng; cung cấp dropdown & dữ liệu dùng chung cho các màn Booking (đặt lịch, sơ đồ khu vực-chỗ ngồi, ca làm việc/khung giờ theo ngày) + khởi tạo dữ liệu mẫu khi đăng ký đơn vị. Một số endpoint phục vụ màn đặt lịch online (AllowAnonymous). (LƯU Ý: trùng tên với CommonController của PreOrder — file PreOrder ở CommonController.md; file này là bản Booking.)"
muc_dich: "Tập hợp API dùng chung của service Booking: lấy ngành nghề, trạng thái đặt lịch, dropdown khu vực-chỗ ngồi, ca làm việc/khung giờ theo ngày (chi nhánh/nhân viên), khởi tạo & xóa dữ liệu mẫu khi đăng ký/hủy đơn vị, và lấy nhân viên theo khung giờ."
alias: ["common booking", "dropdown khu vực chỗ ngồi", "lấy ca làm việc theo ngày", "khung giờ đặt lịch", "khởi tạo dữ liệu mẫu booking", "trạng thái đặt lịch", "nhân viên theo khung giờ"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhuVuc, ChoNgoi, NgayLamViec, KhungGio, KhungGioNgayLamViec, NguonDatLich, DatLich, User]
stored_procs: []
depends: [Booking (KhuVuc/ChoNgoi/NgayLamViec/KhungGio/NguonDatLich), Core (NganhNghe, DonVi), CauHinhService (DistanceTwoTimeFrame), RestAPIRequest (ca làm việc nhân viên, staff by working time), KhungGio/NgayLamViec init private API]
cross_module_in: [Màn đặt lịch / sơ đồ bàn POS — LayKhuVuc/LayKhuVucChoNgoi/GetListSeatWithArea; màn đặt online — LayCaLamViecChiNhanh/NhanVienTheoNgay, KhoiTaoDuLieuMacDinh (AllowAnonymous); flow đăng ký đơn vị — KhoiTaoDuLieuMacDinh/XoaDuLieuTheoDonVi]
cross_module_out: [Khởi tạo dữ liệu mẫu Booking khi đăng ký đơn vị (khung giờ Sáng/Chiều/Tối, 7 ngày làm việc, nguồn đặt lịch, 1 khu vực + 1 bàn); xóa nguồn đặt lịch khi hủy đơn vị; gọi sang Branch/RestAPI lấy nhân viên]
---

# CommonController (Booking) — API dùng chung của service Booking

> Trùng tên controller với `CommonController` của PreOrder (xem `CommonController.md`, hiện là scaffold rỗng). Node này là bản service **Booking** (`Booking.API/Controllers/Common/CommonController.cs`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy ngành nghề | dropdown ngành nghề | GET `api/Common/RequestGetNganhNghe` | controller `[SBAuthorize]` (login) | public | form cấu hình | Gọi Core `GET /api/NganhNghe/LayTatCa` | HTTP Core | - |
| 2 | Lấy trạng thái đặt lịch | dropdown trạng thái | GET `api/Common/GetTrangThaiDatLich` | `[SBAuthorize]` | public | filter lịch | Convert enum `EnumDatLich_TrangThai` → list | (enum) | - |
| 3 | Dropdown khu vực | dropdown khu vực | GET `api/Common/LayKhuVuc` | `[SBAuthorize]` | public | form đặt lịch | Khu vực theo DonVi+ChiNhánh user (Id/Ten) | EF | - |
| 4 | Dropdown chỗ ngồi theo khu vực | dropdown bàn | GET `api/Common/GetLstChoNgoiByIdKV/{idKV}` | `[SBAuthorize]` | public | sau khi chọn khu vực | Chỗ ngồi theo `IdKhuVuc` (Id/Ten) | EF | **Không filter DonVi/ChiNhánh** — `CommonService.cs:190` chỉ lọc IdKhuVuc → cross-tenant nếu biết idKV |
| 5 | Khu vực + chỗ ngồi (bàn trống) | sơ đồ bàn | GET `api/Common/LayKhuVucChoNgoi` | `[SBAuthorize]` | public | sơ đồ POS | Group khu vực→chỗ ngồi theo DonVi+ChiNhánh user, **chỉ bàn chưa có nhân viên phục vụ** (`!IdNhanVienPhucVu.HasValue`) | EF | - |
| 6 | Tất cả khu vực + chỗ ngồi | sơ đồ bàn (full) | GET `api/Common/LayAllKhuVucChoNgoi` | `[SBAuthorize]` | public | sơ đồ POS | Group khu vực→chỗ ngồi theo DonVi+ChiNhánh user (mọi bàn) | EF | - |
| 7 | Danh sách bàn kèm khu vực | flat seat list | GET `api/Common/GetListSeatWithArea` | `[SBAuthorize]` (lặp lại) | public | - | Flat {IdKhuVuc,TenKhuVuc,IdChoNgoi,TenChoNgoi} theo DonVi+ChiNhánh | EF | - |
| 8 | Lấy nhân viên theo khung giờ | chọn NV phục vụ | POST `api/Common/GetStaffByWorkingTime` | `[SBAuthorize]` | public | form đặt lịch | Lấy `IdKhungGio` của ngày trong tuần + thời điểm `Time` (DonVi+ChiNhánh), gọi RestAPI staff-by-working-time | EF + RestAPI (Branch) | - |

## Endpoint nội bộ / anonymous
- [anonymous — `[AllowAnonymous]`] **`LayCaLamViecChiNhanhTheoNgay`** (POST) — ca làm việc + khung giờ chi tiết của chi nhánh theo ngày. Nếu chi nhánh chưa có KhungGio/NgayLamViec → gọi private API `KhungGio/KhoiTaoByIdChiNhanhPrivate` + `NgayLamViec/KhoiTaoByIdChiNhanhPrivate` khởi tạo; sinh các mốc giờ cách nhau `DistanceTwoTimeFrame` (lấy từ `CauHinhService.GetAsync(idDonVi)`); mốc < hiện tại → IsDisabled "Hết chỗ".
- [anonymous — `[AllowAnonymous]`] **`LayCaLamViecNhanVienTheoNgay`** (POST) — như trên nhưng theo ca làm việc của nhân viên (gọi `RestApi_GetCaLamViecNhanVienByDay`).
- [public — `[SBAuthorize]`] **`LayCaLamViecTheoNgayCuThe`** (POST) — ca làm việc theo ngày cụ thể của chi nhánh **user hiện tại** (`_currentUser.IdChiNhanh`). **`LayCaLamViecTheoNgayCuThe_TheoChiNhanh`** (POST) — cùng logic nhưng theo `req.IdChiNhanh` truyền vào (cho service-to-service query CN bất kỳ).
- [anonymous — `[AllowAnonymous]`] **`KhoiTaoDuLieuMacDinh`** (POST, `InitOrgJson`) — khởi tạo dữ liệu mẫu Booking khi đăng ký đơn vị: nguồn đặt lịch (clone từ `IdDonVi=Empty`), 3 khung giờ Sáng/Chiều/Tối, 7 ngày làm việc + map khung giờ, 1 khu vực "Khu vực A" + 1 bàn "Bàn A1"; toàn bộ trong 1 transaction.
- [public — `[SBAuthorize]` controller-level] **`XoaDuLieuTheoDonVi`** (POST, `DonViDeleteReq`) — xóa nguồn đặt lịch theo đơn vị (transaction). (Chỉ xóa NguonDatLich, không xóa khu vực/bàn/khung giờ.)

## Quan hệ
- **Gọi sang:** Core `NganhNghe/LayTatCa`; Booking private `KhungGio/KhoiTaoByIdChiNhanhPrivate`, `NgayLamViec/KhoiTaoByIdChiNhanhPrivate`; `CauHinhService.GetAsync(idDonVi)` (DistanceTwoTimeFrame); `RestAPIRequestService.RestApi_GetCaLamViecNhanVienByDay` / `RestApi_GetStaffByWorkingTime` (Branch).
- **Dữ liệu (entities):** `KhuVuc`, `ChoNgoi`, `NgayLamViec`, `KhungGio`, `KhungGioNgayLamViec`, `NguonDatLich`, `DatLich`/`DatLichChiTiet`, `User`.
- **Ripple / đồng bộ:** đăng ký đơn vị → tạo bộ dữ liệu mẫu Booking; hủy đơn vị → xóa nguồn đặt lịch. Khung giờ sinh ra dùng cho màn đặt lịch online.

## Luồng xuyên module (khởi tạo đơn vị mới)
```
Đăng ký đơn vị (Core) → gọi Booking Common/KhoiTaoDuLieuMacDinh (AllowAnonymous)
  → 1 transaction: NguonDatLich (clone template Empty) + 3 KhungGio (Sáng/Chiều/Tối)
     + 7 NgayLamViec × map KhungGio + 1 KhuVuc "Khu vực A" + 1 ChoNgoi "Bàn A1"
  → đơn vị sẵn sàng đặt lịch / sơ đồ bàn
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`GetLstChoNgoiByIdKV` không lọc tenant** — `CommonService.cs:188-194` chỉ filter `IdKhuVuc` (bỏ tham số `user`) → trả chỗ ngồi của khu vực bất kỳ nếu biết idKV.
- **Vòng sinh khung giờ phụ thuộc cấu hình** — `CommonService.cs:307,324` (và bản nhân viên :377,395): bước nhảy = `DistanceTwoTimeFrame > 0 ? : DistanceTwoTimeFrameDefault`; có guard `> 0` nên tránh được loop vô hạn, nhưng phụ thuộc default > 0.
- **2 overload `KhoiTaoDuLieuMacDinhAsync` trùng body lớn** — `CommonService.cs:530` (message broker) và `:674` (InitOrgJson, gọi từ controller) gần như sao chép nguyên khối khởi tạo → maintain dễ lệch.
- **`XoaDuLieuTheoDonVi` chỉ xóa NguonDatLich** — `CommonService.cs:830-861`: không xóa KhuVuc/ChoNgoi/KhungGio/NgayLamViec đã tạo lúc khởi tạo → dữ liệu mẫu còn sót khi hủy đơn vị.
- **`GetListSeatWithArea` lặp `[SBAuthorize]`** — controller:62-63 đặt lại `[SBAuthorize]` dù controller-level đã có (thừa, không sai).
- **Nhiều method `async` không có `await` thực sự** — vd `LayKhuVuc`/`LayKhuVucChoNgoi` (CommonService.cs:177,199) chạy EF đồng bộ `.ToList()` trong method async (cảnh báo hiệu năng, không sai logic).
- Code thật: BE controller `Booking/Booking.API/Controllers/Common/CommonController.cs`, service `Booking/Booking.Service/Generated/Commons/CommonService.cs`.
