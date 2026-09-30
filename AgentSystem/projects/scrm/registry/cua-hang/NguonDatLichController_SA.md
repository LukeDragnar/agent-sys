---
controller: NguonDatLichController
module: cua-hang
service: Booking
man_hinh_ui: "Khu quản trị hệ thống — area SandBox (route `api/SanboxArea/NguonDatLich`): màn quản trị danh mục 'nguồn đặt lịch' HỆ THỐNG (IdDonVi = Guid.Empty) dùng làm dữ liệu gốc cho tất cả đơn vị. Chỉ HT-AD truy cập."
muc_dich: "CRUD bộ danh mục 'nguồn đặt lịch' cấp hệ thống (IdDonVi rỗng) — đây là template để các đơn vị 'Khởi tạo mặc định' copy về. Bản SandBoxArea dành cho super-admin hệ thống."
alias: ["nguồn đặt lịch hệ thống", "nguồn đặt lịch sandbox", "quản trị nguồn đặt lịch", "NguonDatLich SA", "danh mục nguồn đặt lịch gốc"]
auth: internal
state_machine: []
deprecated: false
entities: [NguonDatLich]
stored_procs: []
depends: [BaseService<NguonDatLich> (EF SqlSugar IRepository), UserPrincipalService]
cross_module_in: [Đơn vị gọi KhoiTaoMacDinh (bản _DV) copy danh mục hệ thống này về đơn vị]
cross_module_out: [Là nguồn template cho NguonDatLich cấp đơn vị (IdDonVi=Guid.Empty)]
---

# NguonDatLichController (SandBoxArea) — Quản trị nguồn đặt lịch hệ thống

> **Area SandBox / quản trị hệ thống.** Route prefix `api/SanboxArea/...`; controller-level `[SBAuthorize]` (chỉ cần đăng nhập hệ thống) + `[TienIchAttribute(..., MaTienIchSCRM.TI_HeThong, true)]` (cờ `true` = tiện ích hệ thống). Mọi quyền action map role **HT-AD**. Thao tác trên dữ liệu `IdDonVi == Guid.Empty` (danh mục gốc dùng chung).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list) | "Tìm kiếm" / grid | POST `api/SanboxArea/NguonDatLich/TimTheoDieuKien_SA` | `NguonDatLich_TimTheoDieuKien_SA` | internal (HT-AD) | màn list | Lọc `IdDonVi == Guid.Empty`; Keyword `Ten.Contains` (lower); sort `Sorts` hoặc `NgayCapNhat` desc; phân trang | EF | - |
| 2 | Thêm mới | "Thêm mới" → modal | POST `api/SanboxArea/NguonDatLich/ThemMoi_SA` | `NguonDatLich_ThemMoi_SA` | internal (HT-AD) | nút Thêm | Validate Ten (trim); `Ma=null`; nếu `MacDinh=true` → bỏ mặc định bản hệ thống khác; chống trùng Ten trong phạm vi hệ thống; gán `IdDonVi=Guid.Empty`; insert | EF | Thứ tự khác bản _DV: set bỏ-mặc-định TRƯỚC khi check trùng tên (Service:84-97) — nếu trùng tên sẽ trả lỗi nhưng đã update bản mặc-định cũ |
| 3 | Lấy chi tiết theo Id | (mở modal sửa) | GET `api/SanboxArea/NguonDatLich/TimTheoId/{id}` | `NguonDatLich_TimTheoId` | internal (HT-AD) | per-row | `GetByIdAsync(id)` → map DTO trả về (KHÔNG check IdDonVi) | EF | Không guard `IdDonVi==Guid.Empty` — đọc được cả bản ghi của đơn vị (Controller:53-56). Dùng chung key `NguonDatLich_TimTheoId` với bản _DV |
| 4 | Cập nhật | "Sửa" → modal | POST `api/SanboxArea/NguonDatLich/CapNhat_SA` | `NguonDatLich_CapNhat_SA` | internal (HT-AD) | nút Sửa | Validate Ten; chống trùng Ten (khác Id) trong hệ thống; guard mặc-định-duy-nhất; MarkDirty Ten/Icon/SuDung/MacDinh; update | EF | Không verify `item.Id` có `IdDonVi==Guid.Empty` trước update (Service:48) |
| 5 | Xóa | "Xóa" (confirm) | GET `api/SanboxArea/NguonDatLich/Xoa_SA/{id}` | `NguonDatLich_Xoa_SA` | internal (HT-AD) | nút Xóa | Lấy theo Id; chặn nếu `IdDonVi != Guid.Empty` ("không có quyền xóa"); chặn nếu `Ma==TRUCTIEP`/`WEBSITE`; xóa cứng | EF | Dùng HTTP **GET** cho xóa (Controller:73) |
| 6 | Đặt làm mặc định | toggle "Mặc định" | GET `api/SanboxArea/NguonDatLich/SetMacDinh_SA/{id}` | `NguonDatLich_SetMacDinh_SA` | internal (HT-AD) | per-row | Transaction: bỏ mặc định bản hệ thống cũ (`IdDonVi==Guid.Empty`) → set bản này; lỗi → rollback | EF (transaction) | Message "Không tìm thấy tài khoản!" khi không thấy (sai ngữ cảnh) (Service:149) |

## Quy tắc nghiệp vụ (gating / ràng buộc)
- **Phạm vi hệ thống:** tất cả thao tác `_SA` lọc/gán `IdDonVi == Guid.Empty` (danh mục gốc dùng chung), KHÔNG dùng `userLogin.DonViId`.
- **Chống trùng tên** trong phạm vi hệ thống (Ten trim + lower).
- **Mặc định duy nhất** trong phạm vi hệ thống (ThemMoi/CapNhat/SetMacDinh_SA).
- **Không xóa nguồn lõi:** chặn xóa khi `IdDonVi != Guid.Empty` (không phải bản hệ thống) hoặc `Ma == TRUCTIEP`/`WEBSITE`.

## Quan hệ
- **Gọi sang:** không cross-service (EF qua `BaseService<NguonDatLich>`). Dùng chung 1 service class `NguonDatLichService` với bản `_DV` (vùng `#region Dành cho SUPER_ADMIN`).
- **Dữ liệu (entities):** `NguonDatLich` (IdDonVi=Guid.Empty cho bản hệ thống).
- **Ripple / đồng bộ:** danh mục này là template — đơn vị gọi `KhoiTaoMacDinh` (bản `_DV`) copy về đơn vị mình. Sửa/xóa ở đây KHÔNG tự đồng bộ ngược về các đơn vị đã copy.

## Lỗi đã biết / lưu ý kỹ thuật
- **`TimTheoId` (SA) không guard phạm vi** — `Controller:53-56` trả thẳng `GetByIdAsync(id)` không check `IdDonVi==Guid.Empty`; HT-AD có thể đọc nguồn của bất kỳ đơn vị. Cùng quyền key `NguonDatLich_TimTheoId` với bản _DV → 1 key dùng 2 area.
- **`ThemMoi_SA` side-effect trước khi validate trùng** — `Service:84-97`: nhánh `MacDinh==true` bỏ mặc định bản cũ TRƯỚC khi check trùng tên; nếu sau đó trùng tên → trả lỗi nhưng bản mặc-định cũ đã bị set `MacDinh=false` (data lệch, không rollback). Bản `_DV` (`ThemMoi_DV` :300) đã đảo thứ tự đúng (check trùng trước).
- **`CapNhat_SA` không verify chủ sở hữu** — `Service:48` update theo item client, chỉ chống trùng tên (`IdDonVi==Guid.Empty`) nhưng không khẳng định `item.Id` thật sự là bản hệ thống trước `UpdateAsync`.
- **Xóa dùng HTTP GET** — `Controller:73` `Xoa_SA` `[HttpGet]`; hành động phá hủy nên POST/DELETE.
- **Message "Không tìm thấy tài khoản!"** trong `SetMacDinh_SA` (`Service:149`) — copy nhầm ngữ cảnh.
- Code thật: controller `Booking.API/Areas/SandBoxArea/NguonDatLichController.cs`; service dùng chung `Booking.Service/Generated/NguonDatLichService.cs` (vùng `#region Dành cho SUPER_ADMIN` :47-184).
