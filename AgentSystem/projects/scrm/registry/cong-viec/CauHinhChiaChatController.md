---
controller: CauHinhChiaChatController
module: cong-viec
service: ChiaSoChat
man_hinh_ui: "Chăm sóc/Chat > Cấu hình chia chat (đơn vị thiết lập quy tắc chia hội thoại tự động cho phòng ban/nhóm/nhân viên trực page)."
muc_dich: "CRUD cấu hình chia chat tự động (CauHinhChiaSoChat): tìm theo điều kiện, lưu, xóa, lấy chi tiết/theo nhiều id, dropdown cấu hình. Quy tắc quyết định hội thoại được chia cho ai."
alias: ["CauHinhChiaChat", "cấu hình chia chat", "chia chat tự động", "chia hội thoại", "config chia chat"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhChiaSoChat]
stored_procs: []
depends:
  - ICauHinhChiaSoChatService (ChiaSoChat.Service.Generated)
cross_module_in: ["consumer chia chat lấy CauHinhChiaChat_GetById_Private (PrivateSBAuthorize) khi hội thoại mới đến để áp quy tắc chia"]
cross_module_out: ["cấu hình lái việc gán hội thoại cho phòng ban/nhóm/nhân viên (FacebookChat conversation)"]
---

# CauHinhChiaChat — Cấu hình chia chat tự động

> CRUD quy tắc chia hội thoại tự động theo đơn vị. `TienIch(..., TI_ChatHub)`. Gồm: grid theo điều kiện, lưu (`CauHinhChiaChat_Save`), xóa (`CHCSC_Xoa`), lấy theo nhiều id / chi tiết, dropdown cấu hình. Có endpoint private `CauHinhChiaChat_GetById_Private` cho consumer chia chat (validate đủ IdDonVi+IdChiNhanh+IdCSC, thiếu → lỗi "Thiếu tham số bắt buộc"). Đa số endpoint gắn key role (HT-CSKH, DV-AD-*, DV-TRUC-PAGE).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | DS cấu hình chia chat | Grid cấu hình | `POST api/CauHinhChiaChat/TimTheoDieuKien` | `CauHinhChiaChat_TimTheoDieuKien` (HT-CSKH, DV-AD-TONG/CN/MCN, DV-TRUC-PAGE) | public | màn cấu hình | Trả grid cấu hình theo `CauHinhChiaChatFilterDto` + đơn vị. | EF (grid filter) | Key gốc ghi `"HT -CSKH"` (có space lỗi, L22). |
| 2 | Lưu cấu hình chia chat | Nút "Lưu" | `POST api/CauHinhChiaChat/CauHinhChiaChat_Save` | `CauHinhChiaChat_CauHinhChiaChat_Save` (HT-CSKH, DV-AD-*, DV-TRUC-PAGE) | public | màn cấu hình | Lưu/upsert `CauHinhChiaSoChatDto` của đơn vị. | EF | — |
| 3 | Xóa cấu hình | Nút "Xóa" | `GET api/CauHinhChiaChat/CHCSC_Xoa/{idCHCSC}` | `CauHinhChiaChat_CHCSC_Xoa` (HT-CSKH, DV-AD-*, DV-TRUC-PAGE) | public | dòng cấu hình | Xóa cấu hình theo id. | EF | Thao tác xóa dùng `GET` (nên là POST/DELETE) — L38. |
| 4 | Lấy chi tiết / theo nhiều id | Mở chi tiết | `GET .../CauHinhChiaChat_GetDetail/{idCHCSC}` · `POST .../CauHinhChiaChat_GetByIds` | `_GetDetail` có key; `_GetByIds` `[SBAuthorize]` trống | public | dòng cấu hình | Trả chi tiết 1 cấu hình / list theo danh sách id. | EF | — |
| 5 | Dropdown cấu hình chia (thủ công) | Dropdown chọn cấu hình | `GET .../CHCSChat_CSCTN_DropdownCHCSC` | `[SBAuthorize]` | public | màn chia thủ công | Trả dropdown cấu hình chia cho màn chia tay (CSCTN). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[private]` `POST api/CauHinhChiaChat/CauHinhChiaChat_GetById_Private` — consumer chia chat lấy cấu hình theo (IdDonVi, IdChiNhanh, IdCSC); thiếu tham số → trả lỗi "Thiếu tham số bắt buộc".

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- Cấu hình chia chat (`CauHinhChiaSoChat`) định nghĩa cách auto-gán hội thoại → consumer đọc qua endpoint private khi hội thoại đến; cấu hình sai/thiếu → hội thoại không được chia tự động.

## Quan hệ
- **Gọi sang:** `ICauHinhChiaSoChatService`.
- **Dữ liệu (entities):** `CauHinhChiaSoChat` (quy tắc chia, scope đơn vị).
- **Ripple/đồng bộ:** lái việc gán hội thoại FacebookChat cho phòng ban/nhóm/nhân viên.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhChiaChatController.cs:38` — `CHCSC_Xoa` là thao tác xóa nhưng dùng `[HttpGet]` (không idempotent-safe theo REST).
- `CauHinhChiaChatController.cs:22` — chuỗi role có khoảng trắng thừa `"HT -CSKH"` → cần kiểm tra parse role không lệch.
- `CauHinhChiaChat_GetByIds` dùng `[SBAuthorize]` trống (không key) khác với các endpoint cùng controller có key role.
