---
controller: CauHinhKhoChiNhanhController
module: kho
service: WarehouseProduct
man_hinh_ui: "Cài đặt hệ thống > Cấu hình chi nhánh > Cấu hình kho chi nhánh (route caidat-hethong/.../cau-hinh-kho-chi-nhanh, cau-hinh-kho-chi-nhanh.component) — bật/tắt tự động gán kho + chọn kiểu loại gán kho theo từng chi nhánh"
muc_dich: "Cấu hình quy tắc gán kho mặc định theo chi nhánh: bật/tắt tự động gán kho (IsAutoGanKho) và chọn kiểu loại gán kho (KieuLoaiGanKho). Mỗi chi nhánh × đơn vị = 1 bản ghi cấu hình; non-admin chỉ thao tác chi nhánh mình."
alias: ["cấu hình kho chi nhánh", "tự động gán kho", "auto gán kho", "kiểu gán kho", "CauHinhKhoChiNhanh", "cau hinh kho chi nhanh"]
auth: mixed                # UpdateOrCreate/GetById: SBAuthorize(key, AD-TONG/CN/MCN); GetAll: [SBAuthorize()] rỗng (chỉ đăng nhập)
state_machine: []
deprecated: false
entities: [CauHinhKhoChiNhanh]
stored_procs: []
depends: [WarehouseProduct, CommonCacheService, LogActivityHelper]
cross_module_in: []
cross_module_out: []
---

# CauHinhKhoChiNhanhController — Cấu hình kho theo chi nhánh (auto gán kho)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình (upsert) | nút Lưu (per chi nhánh) | POST `api/CauHinhKhoChiNhanh/UpdateOrCreate` | `CauHinhKhoChiNhanhController_UpdateOrCreate` (AD-TONG/AD-CN/AD-MCN) | public | màn cấu hình | Upsert theo (IdDonVi, IdChiNhanh): chưa có → insert, có → update `IsAutoGanKho`+`KieuLoaiGanKho`; ghi LichSuTacDong; **clear cache** cấu hình kho chi nhánh. Non-admin bị ép `IdChiNhanh = userLogin.IdChiNhanh` | EF | Validate `KieuLoaiGanKho` phải thuộc enum `Enum_Kho_KieuLoaiGanKho`, null→0 |
| 2 | Lấy theo id chi nhánh | mở cấu hình 1 chi nhánh | POST `api/CauHinhKhoChiNhanh/GetById` | `CauHinhKhoChiNhanhController_GetById` (AD-TONG/AD-CN/AD-MCN) | public | per chi nhánh | Trả cấu hình theo (IdDonVi, IdChiNhanh); thiếu IdChiNhanh → lỗi; non-admin ép về chi nhánh mình; không có → "Không tìm thấy cấu hình" | EF | - |
| 3 | Lấy tất cả cấu hình | load màn list | POST `api/CauHinhKhoChiNhanh/GetAll` | `SBAuthorize()` (rỗng — chỉ cần đăng nhập) | public | màn list | Trả list cấu hình trong đơn vị; non-admin **chỉ chi nhánh mình** | EF | Permission key gốc bị comment (`..._GetAll`), còn `[SBAuthorize()]` rỗng — lệch so với #1/#2 (cần xác nhận có chủ ý) |

## Quy tắc nghiệp vụ (validate — blueprint để trace)
1. **Phân quyền theo chi nhánh:** non-admin (`!CheckUserLoginIsRoleAdmin`) bị ép `IdChiNhanh = userLogin.IdChiNhanh` ở cả 3 method → chỉ xem/sửa cấu hình chi nhánh đang đăng nhập. (CauHinhKhoChiNhanhService.cs:36-39, 56-59, 74-77)
2. **UpdateOrCreate**: thiếu `IdChiNhanh` → "Cần truyền id chi nhánh"; `KieuLoaiGanKho` null→0; không thuộc enum `Enum_Kho_KieuLoaiGanKho` → "Kiểu loại gán kho không đúng"; `IsAutoGanKho` null→false. (CauHinhKhoChiNhanhService.cs:70-95)
3. **Upsert** theo (IdDonVi, IdChiNhanh): insert → log `EnumActivity.ADD`; update → DeepClone bản cũ, MarkDirty 2 field, log `EnumActivity.UPDATE`. Cuối cùng `ClearCache_CauHinhKho_ChiNhanh(IdChiNhanh)`. (CauHinhKhoChiNhanhService.cs:97-135)

## Quan hệ
- **Gọi sang:** `ICommonCacheService.ClearCache_CauHinhKho_ChiNhanh` (xóa cache sau khi lưu); `ILogActivityHelper.LogActivity` (lịch sử tác động, TableId `SCRM_WAREHOUSEPRODUCT_CauHinhKhoChiNhanh`).
- **Dữ liệu (entities):** `CauHinhKhoChiNhanh` (IdDonVi, IdChiNhanh, IsAutoGanKho, KieuLoaiGanKho).
- **Ripple / đồng bộ:** cấu hình này được CACHE và đọc bởi luồng gán kho khi nhập/xuất (qua CommonCacheService). Đổi cấu hình → clear cache để luồng gán kho áp ngay. Không có cross-service trực tiếp.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/CauHinhKhoChiNhanhController.cs`.
- 3 method controller đều `virtual` (cho phép override) — cẩn trọng nếu có lớp kế thừa thay đổi hành vi.
- `GetAll` để `[SBAuthorize()]` rỗng (controller L45) trong khi key thật bị comment (L44) → chỉ cần đăng nhập gọi được; lệch với UpdateOrCreate/GetById (giới hạn 3 role admin). Cần xác nhận chủ ý.
- Phụ thuộc cache: quên `ClearCache` sau khi sửa entity bằng đường khác → luồng auto-gán-kho đọc cấu hình cũ.
</content>
