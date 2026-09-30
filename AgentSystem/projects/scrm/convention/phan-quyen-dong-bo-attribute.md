# Convention — Phân quyền SCRM: đồng bộ quyền bằng SCAN attribute đầu API

> Load-on-demand. Đọc khi: thêm **sàn TMĐT / chức năng mới** cần phân quyền, hoặc debug "vai trò X không thấy/không vào được màn Y".
> Nguồn: điều tra bug B161 (MKT không kết nối được Lazada) — 2026-07-10.

## Cơ chế (QUAN TRỌNG — permission SCRM là AUTO-GENERATED từ attribute)

SCRM **không khai permission bằng tay**. Có **scanner reflection** quét attribute đầu controller/action để **tự sinh danh mục quyền** vào bảng `Permision` (`Loai = "AUTO"`).

**2 tầng, chạy KHI GỌI TAY** (không phải startup):
1. **Aggregator** — `Core\Core.API\Controllers\Base\DongBoQuyenController.cs` · `POST api/DongBoQuyen/DongBoDanhMucQuyen` (quyền `DongBoQuyen_DongBoDanhMucQuyen`, chỉ role `HT-AD`/`HT-CSKH`): fan-out gọi `LayTatCaQuyenAsync` tới ~35 service con → gom → dedupe theo `KeyCode` → `InsertOrUpdateAsync`.
2. **Scan mỗi service** — `Libs\SB.Core\Base\DongBoQuyenController.cs` (`DongBoQuyenBaseController.DongBoDuLieu`): reflection mọi `ControllerActionDescriptor`:
   - Đọc `[TienIch]` của controller → **`null`/`MaTienIch` rỗng ⇒ `continue`, BỎ QUA cả controller** (line ~68-69). Có → sinh 1 permission **page** `KeyCode = MaChucNang`, `NhomTienIch = MaTienIch`.
   - Đọc `[SBAuthorize]` từng action → `null`/`Quyen` rỗng ⇒ bỏ qua. Có → sinh permission **chức năng** `KeyCode = Quyen`, seed `MoTaRole` (chuỗi role) để map role↔permission.
3. **Persist** — `PermisionService.InsertOrUpdateAsync`: ghi `Permision` (Loai=AUTO), **diff theo KeyCode**.

## 🔴 Hệ quả bắt buộc nhớ

- **Chức năng KHÔNG có `[TienIch]` trên controller → KHÔNG BAO GIỜ có permission** → không thể cấp cho role/đơn vị nào. (Endpoint vẫn chạy nếu `[SBAuthorize()]` rỗng — nhưng **UI/menu gate theo permission stub**, nên user không thấy nút.)
- Pattern SCRM: mỗi sàn có **1 ConnectController STUB** (action `return "true"`, KHÔNG phải nơi connect thật) chỉ để **scanner đăng ký quyền**. Connect thật chạy qua `AuthorizationController.CRM_GetAuthorizationUrl` (`[SBAuthorize()]` rỗng, phân biệt sàn bằng `EcommerceCode` payload).
- ⚠️ **Bẫy sync xóa oan:** `DongBoQuyen` **XÓA các KeyCode AUTO không còn xuất hiện** khi quét. → **CHỈ chạy khi TẤT CẢ service con healthy**; 1 service down lúc sync → quyền của nó bị coi là "biến mất" → xóa + xóa `RolePermision` map → mất quyền oan.

## ✅ Checklist THÊM 1 SÀN / CHỨC NĂNG MỚI có phân quyền

1. Thêm hằng `MaTienIchSCRM.TI_<Sàn>` trong `Libs\SB.Core\Contants.cs`.
2. Tạo `<Sàn>ConnectController` (copy mẫu Shopee/Tiktok trong `WarehouseProduct\...\Controllers\Ecommerce\`):
   - `[TienIch("<Sàn>ConnectController","<Sàn>Connect", MaTienIchSCRM.TI_<Sàn>)]`
   - action `[SBAuthorize("<Sàn>ConnectController_Connect<Sàn>","Kết nối <Sàn>","<role list — nhớ đủ DV-QL-MKT, DV-MKT-LEAD, DV-MKT nếu MKT được dùng>")]`
3. Build + deploy service → **chạy `POST api/DongBoQuyen/DongBoDanhMucQuyen`** (HT-AD, mọi service healthy).
4. **Config data (scanner KHÔNG tự làm):** khai row `TienIch` (MaTienIch) trong danh mục tiện ích + cấp `DonViTienIch` cho đơn vị + bật role tại màn phân quyền (`DonViRolePermision`). *(`AuthorizeAttribute` check quyền qua join `DonViTienIch → Permision ON NhomTienIch` → thiếu row `TienIch` thì đơn vị vẫn không thấy quyền — dễ sót nhất.)*
5. FE: đảm bảo có menu/nút cho chức năng, ẩn/hiện theo permission KeyCode.

→ **Bỏ sót bất kỳ bước 1-2 nào ⇒ scanner bỏ qua ⇒ không có quyền để cấp** (đúng ca Lazada thiếu → vai MKT không kết nối được).
