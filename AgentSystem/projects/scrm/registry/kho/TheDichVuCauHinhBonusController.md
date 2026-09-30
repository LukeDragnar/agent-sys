---
controller: TheDichVuCauHinhBonusController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Thẻ dịch vụ > Cấu hình nạp thẻ (bonus nạp thẻ) — 2 màn song song: thedichvu/thedichvu-cauhinh-bonus (cấp đơn vị) và thedichvu-chinhanhtao/thedichvu-chinhanhtao-cauhinh-bonus (cấp chi nhánh). Cùng gọi 1 controller này. Nút: Thêm mới / Sửa / Xóa cấu hình bonus theo mốc số tiền nạp tối thiểu."
muc_dich: "Cấu hình BONUS khi khách NẠP thẻ dịch vụ (thẻ số tiền): với mỗi mốc 'số tiền nạp tối thiểu' định nghĩa số tiền/tỉ lệ bonus được cộng thêm. CRUD bảng cấu hình bonus theo đơn vị."
alias: ["cấu hình bonus nạp thẻ", "bonus nạp thẻ", "cấu hình nạp thẻ", "khuyến mãi nạp thẻ", "cộng bonus thẻ", "config bonus thẻ dịch vụ"]
auth: public
state_machine: []
deprecated: false
entities: [TheDichVuCauHinhBonus]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Order/Thu ngân (khi nạp thẻ dịch vụ áp bonus theo mốc nạp)]
cross_module_out: [TheDichVuKhachHang (số tiền bonus cộng vào thẻ khách khi nạp)]
---

# TheDichVuCauHinhBonusController — Cấu hình bonus khi nạp thẻ dịch vụ

> Bảng cấu hình tham số: mỗi dòng = 1 mốc `SoTienNapToiThieu` → `SoTienBonus` (theo `DonViBonus`: 1 = phần trăm %, 2 = số tiền tuyệt đối). Khi khách nạp thẻ số tiền, hệ thống tra mốc phù hợp để cộng bonus. Controller chỉ lo CRUD cấu hình, không thực hiện nạp.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm cấu hình theo điều kiện | "Tìm kiếm" lưới cấu hình bonus | POST `api/TheDichVuCauHinhBonus/TimTheoDieuKien` | `TheDichVuCauHinhBonus_TimTheoDieuKien` | public | màn list | Lọc cấu hình theo `IdDonVi` user; nếu có `Keyword` (số) → lọc `SoTienBonus >= Keyword OR SoTienNapToiThieu >= Keyword`; phân trang, sort theo NgayCapNhat desc | EF | Keyword so sánh kiểu `>=` trên 2 cột số (không phải tìm chuỗi) — dễ gây kết quả bất ngờ nếu user hiểu là "tìm gần đúng" (Service:34-38) |
| 2 | Lấy tất cả cấu hình | (load dropdown/list đầy đủ) | GET `api/TheDichVuCauHinhBonus/LayTatca` | `TheDichVuCauHinhBonus_LayTatca` | public | - | Trả toàn bộ cấu hình bonus theo `IdDonVi` (không phân trang) | EF | - |
| 3 | Thêm mới cấu hình | "Thêm mới" → `create` → POST ThemMoi | POST `api/TheDichVuCauHinhBonus/ThemMoi` | `TheDichVuCauHinhBonus_ThemMoi` | public (chỉ DV-AD-TONG) | popup thêm | Validate: trùng `SoTienNapToiThieu` trong đơn vị → "Đã tồn tại cấu hình!"; `DonViBonus ∉ {1,2}` → "Đon vị không phù hợp!"; nếu `DonViBonus==1` (%) và `SoTienBonus >= 100` → "Số bonus không được vượt 100%!". Gán IdDonVi rồi insert | EF | Chỉ DV-AD-TONG được thêm/sửa/xóa (Tìm/LayTatca rộng hơn) |
| 4 | Tìm theo Id | (mở popup sửa lấy data) | GET `api/TheDichVuCauHinhBonus/TimTheoId/{id}` | `TheDichVuCauHinhBonus_TimTheoId` | public | per-row | Lấy 1 cấu hình theo id; không thuộc đơn vị → "Không tìm thấy cấu hình!". Trả DTO (qua AutoMapper) | EF | Controller bọc `ResultWithData(Map(data.Data))` — nếu GetByIdAsync trả lỗi (data.Data null) vẫn map null, mất message lỗi (Controller:58-62) |
| 5 | Cập nhật cấu hình | "Lưu" → `update` → POST CapNhatThongTin | POST `api/TheDichVuCauHinhBonus/CapNhatThongTin` | `TheDichVuCauHinhBonus_CapNhatThongTin` | public (chỉ DV-AD-TONG) | popup sửa | Validate trùng `SoTienNapToiThieu` (loại trừ chính nó), `DonViBonus ∈ {1,2}`, `%`→`SoTienBonus < 100`. MarkDirty DonViBonus/SoTienBonus/SoTienNapToiThieu rồi update | EF | - |
| 6 | Xóa cấu hình | "Xóa" → `Xoa` | GET `api/TheDichVuCauHinhBonus/Xoa/{id}` | `TheDichVuCauHinhBonus_Xoa` | public (chỉ DV-AD-TONG) | per-row, confirm | Không thuộc đơn vị → "Không tìm thấy cấu hình!"; ngược lại soft-delete (base DeleteAsync) | EF | - |

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint `[PrivateSBAuthorize]`. Việc áp bonus khi nạp thẻ thực hiện ở luồng Order/nạp thẻ (đọc bảng cấu hình này), không qua endpoint riêng.

## Quy tắc nghiệp vụ (gating / ngưỡng — blueprint để trace)
1. **Đơn vị bonus** (`DonViBonus`): chỉ chấp nhận `1` (phần trăm %) hoặc `2` (số tiền). Khác → "Đon vị không phù hợp!". (Service:60-61,74-75)
2. **Trần % bonus**: khi `DonViBonus == 1`, `SoTienBonus >= 100` → chặn "Số bonus không được vượt 100%!". (Lưu ý: dùng `>= 100` nên đúng 100% cũng bị chặn). (Service:62-64,76-78)
3. **Không trùng mốc nạp**: trong cùng đơn vị, không cho 2 cấu hình cùng `SoTienNapToiThieu`. Thêm: bất kỳ trùng nào → chặn. Sửa: loại trừ chính bản ghi (`Id != model.Id`). (Service:57-59,71-73)
4. **Scope đơn vị**: mọi query/CRUD đều ràng `IdDonVi == userLogin.DonViId` → dữ liệu cô lập theo đơn vị.

## Quan hệ
- **Gọi sang:** không gọi service khác (CRUD thuần nội bộ).
- **Dữ liệu (entities):** `TheDichVuCauHinhBonus` (IdDonVi, SoTienNapToiThieu, SoTienBonus, DonViBonus).
- **Ripple / đồng bộ:** cấu hình này được tra cứu khi khách NẠP thẻ dịch vụ (thẻ số tiền) để cộng bonus vào `TheDichVuKhachHang` → ảnh hưởng số tiền/khuyến mãi còn lại của thẻ khách. Đổi cấu hình chỉ tác động lần nạp sau (không hồi tố thẻ đã nạp).

## Lỗi đã biết / lưu ý kỹ thuật
- `TheDichVuCauHinhBonusController.cs:58-62` — `LayTheoIdAsync` luôn bọc `MethodResult.ResultWithData(Map(data.Data))` kể cả khi `GetByIdAsync` trả lỗi "Không tìm thấy cấu hình!" → FE nhận `data=null` thay vì message lỗi.
- `TheDichVuCauHinhBonusService.cs:34-38` — `TimTheoDieuKien` dùng `Keyword` so sánh `>=` trên `SoTienBonus`/`SoTienNapToiThieu` (không phải search chuỗi/tên) → ý nghĩa filter dễ hiểu nhầm.
- `TheDichVuCauHinhBonusService.cs:62-64` — chặn `SoTienBonus >= 100` cho loại %, nên KHÔNG cho cấu hình bonus đúng 100% (cận biên, cần xác nhận có chủ ý).
- **Sai chính tả thông báo:** "Đon vị không phù hợp!" (thiếu dấu) lặp ở cả Insert/Update (Service:61,75).
- **2 màn FE dùng chung controller**: `thedichvu-cauhinh-bonus` (đơn vị) và `thedichvu-chinhanhtao-cauhinh-bonus` (chi nhánh) — không có endpoint riêng cho chi nhánh; sửa logic ảnh hưởng cả 2 màn.
- Controller-level gate `[TienIch(..., MaTienIchSCRM.TI_Kho)]` — đơn vị phải có tiện ích Kho mới truy cập.
