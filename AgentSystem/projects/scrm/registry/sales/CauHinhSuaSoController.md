---
controller: CauHinhSuaSoController
module: sales
service: Contact
man_hinh_ui: "nội bộ — không có UI riêng (cấu hình ma trận role được phép sửa số/field contact theo đơn vị; gọi từ màn cấu hình contact)"
muc_dich: "Cấu hình ma trận phân quyền sửa số/field contact theo đơn vị: mỗi UnitCode (field/cột) được gán danh sách Roles được phép sửa."
alias: [CauHinhSuaSo, "cấu hình sửa số", "ma trận sửa số", "phân quyền sửa số"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhSuaSo]
stored_procs: []
depends: ["CauHinhSuaSoService (ICauHinhSuaSoService)", "CommonCacheService (cache cấu hình sửa số theo đơn vị)"]
cross_module_in: []
cross_module_out: ["Cache CauHinhSuaSoSetting theo đơn vị — clear sau mỗi Save"]
---

# CauHinhSuaSo — Cấu hình ma trận quyền sửa số/field contact

> Controller nhỏ (43 dòng, 2 endpoint). Quản lý ma trận `UnitCodeId → Roles`: mỗi field/cột (UnitCode, map từ enum `EnumCauHinhSuaSoContact`) được gán danh sách role được phép chỉnh sửa. Có cache theo đơn vị.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình ma trận theo đơn vị | (load form cấu hình) | GET `GetCauHinhInit` | `CauHinhSuaSoController_GetCauHinhInit` | public | Mở màn cấu hình sửa số | Đọc cache trước (`GetCache_CauHinhSuaSoSetting_DonVi`); miss → query `CauHinhSuaSo` theo `IdDonVi` (UnitCodeId + Roles), order theo UnitCodeId, set cache rồi trả. Rỗng → trả lỗi "Lỗi lấy dữ liệu". | EF + cache | Cache-aside; nếu DB rỗng trả error thay vì list rỗng (đơn vị mới chưa cấu hình → FE thấy lỗi) |
| 2 | Lưu cấu hình ma trận | Lưu | POST `SaveCauHinhInit` | `CauHinhSuaSoController_SaveCauHinhInit` | public | Bấm Lưu trên form cấu hình | Upsert: nếu đơn vị đã có bản ghi → loop update `Roles` theo từng UnitCodeId khớp; chưa có → insert mới full ma trận (gán `EnumUnitCode = ((EnumCauHinhSuaSoContact)UnitCodeId).ToString()`). Cuối: clear cache đơn vị. Trả `Guid.Empty` + "Lưu cấu hình thành công". | EF + cache | Xem lỗi kỹ thuật |

## Quy tắc nghiệp vụ (gating)
1. **Đơn vị ĐÃ có cấu hình** (`settingsSuaSo.Any()` true) → chỉ UPDATE `Roles` cho từng `item.UnitCodeId` khớp request; UnitCode mới trong request mà DB chưa có → **bỏ qua, không insert** (chỉ update existing).
2. **Đơn vị CHƯA có cấu hình** → INSERT toàn bộ `CauHinhSuaSoMatrixDtos` thành bản ghi mới.
3. Mọi nhánh kết thúc bằng **clear cache** `CauHinhSuaSoSetting` theo đơn vị (nếu không cache lệch ở GetCauHinhInit).

## Quan hệ
- **Gọi sang:** `ICauHinhSuaSoService` (+ `CommonCacheService` cho cache).
- **Dữ liệu (entities):** `CauHinhSuaSo` (IdDonVi, UnitCodeId, EnumUnitCode, Roles).
- **Ripple / đồng bộ:** cache đơn vị phải clear sau mỗi Save; GetCauHinhInit phụ thuộc cache này.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhSuaSoService.cs:64` — GetCauHinhInit trả `ResultWithError("Lỗi lấy dữ liệu")` khi list rỗng → đơn vị chưa cấu hình lần nào sẽ nhận lỗi thay vì danh sách rỗng (FE cần phân biệt).
- `CauHinhSuaSoService.cs:74-86` — nhánh update CHỈ update các UnitCode đã tồn tại trong DB; UnitCode mới (enum mới thêm) gửi từ request sẽ **không được insert** vào đơn vị đã có cấu hình → ma trận thiếu cột nếu enum mở rộng sau khi đơn vị đã lưu.
- `CauHinhSuaSoService.cs:108-110` — catch nuốt exception, trả message tĩnh "Lỗi lưu cấu hình", không log chi tiết `ex` → khó debug khi lưu fail.
- `CauHinhSuaSoService.cs:74` — query lấy `settingsSuaSo` dùng `TableWithNolock()` rồi update ngay trong cùng request (đọc nolock + ghi) — chấp nhận được vì lọc theo đơn vị, nhưng cần lưu ý nếu 2 admin cùng đơn vị lưu đồng thời.
