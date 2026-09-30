---
controller: NguonDatLichController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Đặt lịch — màn 'Quản lý nguồn đặt lịch' (cấp Đơn vị): list + Thêm/Sửa/Xóa + đặt mặc định + bật/tắt sử dụng; nút 'Khởi tạo mặc định' để copy danh mục nguồn hệ thống về đơn vị"
muc_dich: "CRUD danh mục 'nguồn đặt lịch' (kênh khách đặt: trực tiếp, website, fanpage, hotline...) ở phạm vi từng Đơn vị; chọn nguồn mặc định và copy bộ nguồn hệ thống về đơn vị."
alias: ["nguồn đặt lịch", "kênh đặt lịch", "nguồn đặt", "danh mục nguồn đặt lịch", "NguonDatLich đơn vị"]
auth: mixed
state_machine: []
deprecated: false
entities: [NguonDatLich]
stored_procs: []
depends: [BaseService<NguonDatLich> (EF SqlSugar IRepository), UserPrincipalService (UserLogin.DonViId)]
cross_module_in: [GetAllByDonVi_DV_Private — service khác gọi private để lấy danh mục nguồn theo idDonVi]
cross_module_out: [Booking đặt lịch (DatLich) tham chiếu IdNguonDatLich; nguồn mặc định dùng khi tạo lịch]
---

# NguonDatLichController — Quản lý nguồn đặt lịch (cấp Đơn vị)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy chi tiết theo Id | (mở modal sửa) | GET `api/NguonDatLich/TimTheoId/{id}` | `NguonDatLich_TimTheoId` | public | per-row | `GetByIdAsync(id)`; nếu `Data.IdDonVi != CurrentUser.DonViId` → tạo error (xem known-issue) rồi vẫn map & trả về DTO | EF | **Guard cross-tenant không chặn**: chỉ gọi `ResponseResult(...error)` rời rạc (không return), cuối hàm vẫn trả data → user có thể đọc nguồn của đơn vị khác (Controller:30-34) |
| 2 | Tìm theo điều kiện (list) | "Tìm kiếm" / load grid | POST `api/NguonDatLich/TimTheoDieuKien_DV` | `NguonDatLich_TimTheoDieuKien_DV` | public | màn list | Lọc theo `IdDonVi == user.DonViId`; Keyword `Ten.Contains` (lower); sort theo `Sorts` hoặc mặc định `NgayCapNhat` desc; phân trang | EF | - |
| 3 | Thêm mới | "Thêm mới" → modal | POST `api/NguonDatLich/ThemMoi_DV` | `NguonDatLich_ThemMoi_DV` | public | nút Thêm | Validate Ten bắt buộc (trim); `Ma=null`; chống trùng Ten trong đơn vị; nếu `MacDinh=true` → bỏ mặc định nguồn khác cùng đơn vị; gán `IdDonVi=user.DonViId`; insert | EF | - |
| 4 | Cập nhật | "Sửa" → modal | POST `api/NguonDatLich/CapNhat_DV` | `NguonDatLich_CapNhat_DV` | public | nút Sửa | Validate Ten; chống trùng Ten (khác Id) trong đơn vị; guard mặc-định-duy-nhất; MarkDirty Ten/Icon/SuDung/MacDinh; update | EF | Không kiểm tra `item.Id` thuộc đúng `user.DonViId` trước khi update — chỉ chống trùng tên (Service:229) |
| 5 | Xóa | "Xóa" (confirm) | GET `api/NguonDatLich/Xoa_DV/{id}` | `NguonDatLich_Xoa_DV` | public | nút Xóa | Lấy bản ghi theo `Id + IdDonVi==user.DonViId`; chặn xóa nếu `Ma == TRUCTIEP`/`WEBSITE` (nguồn mặc định hệ thống); xóa cứng (`DeleteAsync`) | EF | Dùng HTTP **GET** cho hành động xóa (Controller:76) |
| 6 | Đặt làm mặc định | toggle "Mặc định" | GET `api/NguonDatLich/SetMacDinh_DV/{id}` | `NguonDatLich_SetMacDinh_DV` | public | per-row | Trong transaction (execution strategy): bỏ `MacDinh` nguồn mặc định cũ cùng đơn vị → set bản ghi này `MacDinh=true` (mặc định duy nhất); lỗi → rollback + trả message | EF (transaction) | Message lỗi khi không thấy là "Không tìm thấy tài khoản!" (sai ngữ cảnh — copy nhầm, nên là 'nguồn đặt lịch') (Service:193) |
| 7 | Khởi tạo mặc định | "Khởi tạo mặc định" | POST `api/NguonDatLich/KhoiTaoMacDinh` | `NguonDatLich_KhoiTaoMacDinh` | public | màn list | Copy các nguồn hệ thống (`IdDonVi==Guid.Empty`) chưa có tên trùng trong đơn vị về `IdDonVi=user.DonViId` (chỉ chép Ten/Icon/SuDung); insert batch | EF | - |
| 8 | Lấy tất cả nguồn theo đơn vị | (dropdown/danh sách) | GET `api/NguonDatLich/GetAllByDonVi_DV` | `NguonDatLich_GetAllByDonVi_DV` | public | dùng cho form đặt lịch | List toàn bộ nguồn `IdDonVi==user.DonViId` (map DTO, không phân trang) | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`GetAllByDonVi_DV_Private/{id}`** (GET) — service khác truyền `idDonVi`, trả toàn bộ nguồn đặt lịch của đơn vị đó (không lệ thuộc token user). Dùng khi cần enrich danh mục nguồn cho đơn vị khác (vd hiển thị lịch sử/đơn cross-service).

## Quy tắc nghiệp vụ (gating / ràng buộc)
- **Phạm vi tenant:** mọi thao tác `_DV` lọc theo `userLogin.DonViId` (lấy từ `UserPrincipalService`). Nguồn "hệ thống" có `IdDonVi == Guid.Empty` (chỉ bản `_SA` thao tác — xem `NguonDatLichController_SA`).
- **Chống trùng tên:** Ten (trim, so sánh lower) không được trùng trong cùng đơn vị (ThemMoi/CapNhat).
- **Mặc định duy nhất:** mỗi đơn vị tối đa 1 nguồn `MacDinh=true` — enforce ở ThemMoi/CapNhat/SetMacDinh_DV (bỏ mặc định bản cũ trước khi set bản mới).
- **Không xóa nguồn lõi:** `Ma == TRUCTIEP` hoặc `WEBSITE` → chặn xóa (nguồn mặc định hệ thống).
- **Khởi tạo mặc định:** chỉ copy nguồn hệ thống có tên chưa tồn tại trong đơn vị (`datas.RemoveAll(... datasOwner.Contains ...)`).

## Quan hệ
- **Gọi sang:** không gọi cross-service (thuần EF qua `BaseService<NguonDatLich>` / `IRepository`).
- **Dữ liệu (entities):** `NguonDatLich` (Ten, Ma, Icon, SuDung, MacDinh, IdDonVi, NgayCapNhat).
- **Ripple / đồng bộ:** đổi nguồn/mặc định ảnh hưởng form đặt lịch (DatLich tham chiếu nguồn); không clear cache trong code đọc được.

## Lỗi đã biết / lưu ý kỹ thuật
- **`TimTheoId` guard cross-tenant bị bỏ qua** — `Controller:30-34`: khi `Data.IdDonVi != CurrentUser.DonViId` chỉ tạo `ResponseResult(...error)` nhưng **không `return`**, code chạy tiếp tới dòng cuối map & trả về data của đơn vị khác → rò rỉ dữ liệu cross-tenant. Cần `return` ngay tại nhánh lỗi.
- **`CapNhat_DV` không verify chủ sở hữu** — `Service:229` update theo `item` client gửi lên, chỉ chống trùng tên trong đơn vị nhưng không khẳng định bản ghi `item.Id` thuộc `user.DonViId` trước khi `UpdateAsync` → rủi ro sửa nhầm bản ghi đơn vị khác nếu Id bị giả mạo (Xóa thì có check, Cập nhật thì không).
- **Xóa dùng HTTP GET** — `Controller:76` `Xoa_DV` là `[HttpGet]`; hành động phá hủy nên dùng POST/DELETE (rủi ro CSRF/prefetch).
- **Message "Không tìm thấy tài khoản!"** trong `SetMacDinh_DV` (`Service:193`) và `SetMacDinh_SA` — copy nhầm ngữ cảnh, nên là "nguồn đặt lịch".
- **`KhoiTaoMacDinh` không bọc transaction** — insert batch sau khi lọc trùng (`Service:326-345`); nếu chạy đồng thời 2 lần có thể chèn trùng (race) vì check-then-insert không atomic.
- Code thật: controller `Booking.API/Controllers/NguonDatLichController.cs`; service `Booking.Service/Generated/NguonDatLichService.cs` (vùng `#region Dành cho DON_VI` :186-360).
