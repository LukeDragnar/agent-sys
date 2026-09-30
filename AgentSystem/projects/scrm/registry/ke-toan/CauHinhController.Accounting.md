---
controller: CauHinhController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Cấu hình chung: chọn trạng thái đơn bán hàng để auto sinh phiếu bán hàng kế toán + trạng thái sinh chứng từ bù trừ / chứng từ giảm doanh thu. Lưu dạng JSON 1 bản ghi / đơn vị."
muc_dich: "Lưu/đọc cấu hình kế toán cấp đơn vị (service Accounting) dưới dạng JSON: ngưỡng trạng thái đơn để tự sinh phiếu bán hàng kế toán & chứng từ liên quan. Khác hẳn CauHinhController của service Invoice (cấu hình HĐĐT)."
alias: ["cấu hình kế toán", "cấu hình accounting", "cấu hình sinh phiếu kế toán", "CauHinh Accounting", "cấu hình tự tạo phiếu bán hàng kế toán"]
auth: mixed   # SBAuthorize (UI) + PrivateSBAuthorize (DongBoPhieuCrm/Order gọi GetCauHinhInit_Private)
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [ICauHinhService (Accounting.Services.Generated), IRepository<CauHinh>, AutoMapper]
cross_module_in:
  - "Service nội bộ Accounting (DongBoPhieuCrmService/PhieuBanHangService) đọc GetCauHinhInit để quyết định sinh phiếu"
cross_module_out: []
---

# CauHinhController (Accounting) — Cấu hình sinh phiếu/chứng từ kế toán cấp đơn vị

> ⚠️ **Trùng tên với `CauHinhController` của service Invoice** (file `CauHinhController.md` cùng thư mục) — đây là controller của **service Accounting**, namespace `Accounting.Services.Generated.ICauHinhService`. Cùng entity `CauHinh` (cột `JsonSetting`, 1 bản ghi/đơn vị) nhưng schema JSON khác: ở đây chứa `TrangThaiTaoPhieuBanHang`, `TrangThaiTaoChungTuBuTru`, `TrangThaiTaoChungTuGiamDoanhThu`.
> Cấu hình nén trong JSON 1 cột → đổi setting không cần migration. Có cặp UI/internal: `GetCauHinhInit` (token) vs `GetCauHinhInit_Private` (truyền idDonVi).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình | (load form cấu hình) | `GET api/CauHinh/GetCauHinhInit` | — (`[SBAuthorize()]` trống) | public | tab cấu hình | Đọc `CauHinh` theo `IdDonVi == DonViId`, deserialize JsonSetting → `CauHinhSettingDto`; không có → DTO rỗng. | EF `CauHinh` | — |
| 2 | Lưu cấu hình | Nút "Lưu" | `POST api/CauHinh/SaveCauHinhInit` | — | public | — | Validate: nếu `TrangThaiTaoPhieuBanHang == null` mà có cấu hình tạo chứng từ bù trừ/giảm DT → lỗi; nếu `TrangThaiTaoPhieuBanHang > TrangThaiTao(ChungTuBuTru/GiamDoanhThu)` → lỗi (ngưỡng phải ≤). Upsert 1 bản ghi/đơn vị (insert hoặc map-merge JsonSetting), ghi LichSuTacDong (ADD/UPDATE). | EF Insert/Update | Comment-out toàn bộ nhánh "super user → IdDonVi = Guid.Empty" (L48-53,138-144) → super user lưu vào chính DonViId của họ. |

## Endpoint nội bộ (private / service-to-service)
- [internal] **`GET api/CauHinh/GetCauHinhInit_Private?idDonVi=`** (`PrivateSBAuthorize`): như #1 nhưng nhận `idDonVi` trực tiếp — các luồng nội bộ (DongBoPhieuCrm/sinh phiếu) đọc cấu hình theo đơn vị mà không có token user.

## Quy tắc nghiệp vụ (gating ngưỡng trạng thái)
1. `TrangThaiTaoPhieuBanHang == null` (không cấu hình sinh phiếu BH) **nhưng** có `TrangThaiTaoChungTuBuTru` hoặc `TrangThaiTaoChungTuGiamDoanhThu` → **lỗi** (không được cấu hình chứng từ khi chưa có mốc sinh phiếu).
2. Nếu có `TrangThaiTaoPhieuBanHang`: phải **≤** `TrangThaiTaoChungTuBuTru` và **≤** `TrangThaiTaoChungTuGiamDoanhThu` (mốc sinh phiếu không được muộn hơn mốc sinh chứng từ); vi phạm → lỗi.
3. Cấu hình này là **cờ điều kiện** cho luồng tự sinh phiếu/chứng từ kế toán khi đơn bán hàng đạt trạng thái tương ứng (đọc ở DongBoPhieuCrm/PhieuBanHang).

## Quan hệ
- **Gọi sang:** `ICauHinhService` (Accounting) → `IRepository<CauHinh>`; AutoMapper map DTO↔JsonSetting; ghi `LichSuTacDong` (EventLichSuTacDong).
- **Dữ liệu (entities):** `CauHinh` (IdDonVi, JsonSetting, IsDelete) — dùng chung kiểu entity với Invoice nhưng nội dung JSON khác.
- **Ripple / đồng bộ:** đổi cấu hình → luồng đồng bộ phiếu CRM / sinh phiếu bán hàng kế toán đọc lại để biết "đơn ở trạng thái nào thì auto sinh phiếu/chứng từ".

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.Service/Generated/CauHinhService.cs`
  - Nhánh "super user/HT_AD → IdDonVi = Guid.Empty" bị comment-out ở mọi method (L48-53, L76-81, L138-144) → super user thao tác trên DonViId của chính họ, không phải bộ default hệ thống (docstring class L19-21 nói ngược lại — lệch comment vs code).
  - `CauHinhService` ctor (L34-38) inject `IOptions<ServiceSettings>` & khai báo `_serviceCommonCache` nhưng không gán/dùng → field thừa.
- Controller `Accounting/Accounting.API/Controllers/CauHinhController.cs`: `[SBAuthorize()]` trống (UI) + `[PrivateSBAuthorize]` (private); attribute `[SBAuthorize("CauHinhController")]` ở class-level đã comment-out.
- Entity `Accounting/Accounting.DataContext/Entity/CauHinh.cs`.
