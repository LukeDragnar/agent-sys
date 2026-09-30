---
controller: CauHinhController
module: marketing
service: PartnerConnect
man_hinh_ui: "Marketing > Kết nối đối tác > Cấu hình PartnerConnect (cấu hình init/chung của service đồng bộ sang đối tác — Bravo, TingTong...); một phần cấu hình API token."
muc_dich: "CRUD cấu hình service PartnerConnect theo đơn vị: cấu hình init (SaveCauHinhInit), cấu hình chung (CauHinhChungDto), cấu hình API (token gọi private). Lái việc đồng bộ data sang đối tác."
alias: ["CauHinh PartnerConnect", "cấu hình kết nối đối tác", "cấu hình partner connect", "token api partner", "SaveCauHinhInit partner"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhPartnerConnect]
stored_procs: []
depends:
  - ICauHinhService (PartnerConnect.Services.Generated)
cross_module_in: ["service nội bộ gọi GetCauHinhPartnerConnect/{idDonVi} + GetCauHinhAPIPrivate/{idDonVi} (PrivateSBAuthorize)"]
cross_module_out: ["cấu hình đồng bộ sang Bravo / TingTong (qua CauHinhKetNoiPartnerConnect)"]
---

# CauHinh — Cấu hình service PartnerConnect (per đơn vị)

> Lưu/đọc cấu hình đơn vị cho service đồng bộ dữ liệu sang đối tác. 3 nhóm: cấu hình init (`SaveCauHinhInit`/`GetCauHinhInit`), cấu hình chung (`CauHinhChungDto`), cấu hình API (token để đối tác/service private gọi vào). `SaveCauHinhChung` + `LuuCauHinhAPI` validate ModelState. Một số endpoint mở `PrivateSBAuthorize` cho service-to-service theo `idDonVi`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình init | Nút "Lưu" màn cấu hình | `POST api/CauHinh/SaveCauHinhInit` | `CauHinh_PartnerConnect_LuuCauHinhPartnerConnect` (DV-AD-TONG, HT-CSKH) | public | màn cấu hình | Lưu `CauHinhSettingInitDto` của đơn vị. | EF | — |
| 2 | Lấy cấu hình init | Mở màn cấu hình | `POST api/CauHinh/GetCauHinhInit` | `CauHinh_PartnerConnect_GetCauHinhInit` (DV-AD-TONG) | public | màn cấu hình | Trả cấu hình init đơn vị. | EF | — |
| 3 | Lưu / lấy cấu hình chung | Nút "Lưu cấu hình chung" | `POST api/CauHinh/SaveCauHinhChung` · `POST api/CauHinh/GetCauHinhChung` | `CauHinh_PartnerConnect_SaveCauHinhChung` · `_GetCauHinhChung` (DV-AD-TONG) | public | màn cấu hình | Save validate ModelState (invalid → MESS_Model_UnValidate); lưu/đọc `CauHinhChungDto`. | EF | — |
| 4 | Lưu / lấy cấu hình API + sinh token | Tab "Cấu hình API", nút "Sinh token" | `POST api/CauHinh/LuuCauHinhAPI` · `GET .../GetCauHinhAPI` · `GET .../SinhMaTokenAPI` | `[SBAuthorize]` (key gốc DV-AD-TONG đã comment) | public | tab cấu hình API | LuuCauHinhAPI validate ModelState. SinhMaTokenAPI sinh token mới — **token cũ hết hiệu lực ngay khi lưu lại bằng LuuCauHinhAPI**. | EF | Key `CauHinh_Core_LuuCauHinhAPI`/`_SinhMaTokenAPI` (DV-AD-TONG) bị comment (L77,110) → chỉ cần đăng nhập. |

## Endpoint nội bộ (private / service-to-service)
- `[private]` `GET api/CauHinh/Get` — lấy cấu hình runtime theo `CurrentUser` (key `LayCauHinhCore` đã comment, đổi sang `PrivateSBAuthorize`).
- `[private]` `GET api/CauHinh/GetCauHinhPartnerConnect/{idDonVi}` — service khác lấy cấu hình theo đơn vị (trả `CauHinhSettingDto`).
- `[private]` `GET api/CauHinh/GetCauHinhAPIPrivate/{idDonVi}` — lấy cấu hình API (token) theo đơn vị để xác thực inbound.

## Quan hệ
- **Gọi sang:** `ICauHinhService`.
- **Dữ liệu (entities):** cấu hình PartnerConnect (1 bản/đơn vị) + token API.
- **Ripple/đồng bộ:** cấu hình lái hành vi đồng bộ Bravo/TingTong; token API là cổng cho inbound private.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:77,110` — key quyền cấu hình API (`CauHinh_Core_LuuCauHinhAPI`, `_SinhMaTokenAPI`) bị comment, thay bằng `[SBAuthorize]` trống → mất giới hạn role DV-AD-TONG cho thao tác đụng token.
- `SinhMaTokenAPI` + `LuuCauHinhAPI` cùng đổi token → gọi lệch thứ tự có thể vô hiệu token đang dùng.
