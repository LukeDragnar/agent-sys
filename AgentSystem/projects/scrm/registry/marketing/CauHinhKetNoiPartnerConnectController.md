---
controller: CauHinhKetNoiPartnerConnectController
module: marketing
service: PartnerConnect
man_hinh_ui: "Marketing > Kết nối đối tác > Cấu hình kết nối (đơn vị nhập thông tin xác thực để kết nối TingTong / Bravo, lưu cấu hình theo loại đối tác partnerConnectType)."
muc_dich: "Quản lý cấu hình kết nối đối tác theo đơn vị: lưu/đọc cấu hình theo loại đối tác (TingTong, Bravo), thực hiện kết nối (auth) và test gọi API Bravo (item/kho/nhân viên/tồn)."
alias: ["CauHinhKetNoiPartnerConnect", "cấu hình kết nối partner", "kết nối TingTong", "kết nối Bravo", "partnerConnectType"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhKetNoiPartnerConnect]
stored_procs: []
depends:
  - ICauHinhKetNoiPartnerConnectService
  - IBravoService
  - TingTongHelper (PartnerConnect.Service.Generated.Helper)
cross_module_in: ["service nội bộ lấy GetCauHinhKetNoiByDonViPartnerConnectType (PrivateSBAuthorize) / GetCfgBravoPrivate/{id}"]
cross_module_out: ["kết nối + đồng bộ data sang TingTong (lead) và Bravo (item/warehouse/employee/inventory)"]
---

# CauHinhKetNoiPartnerConnect — Cấu hình kết nối đối tác (TingTong / Bravo)

> Lưu cấu hình xác thực kết nối theo đơn vị + loại đối tác (`partnerConnectType`). 2 nhánh đối tác chính: **TingTong** (auth + tạo lead) và **Bravo** (auth + lấy cấu hình + test các API item/kho/nhân viên/tồn). `InsertOrUpdateAsync` upsert 1 bản theo loại. `ConnectBravo` để `[AllowAnonymous]` (callback auth). File: file vật lý `CauHinhPartnerConnectController.cs` nhưng **class thật = `CauHinhKetNoiPartnerConnectController`** (route `api/CauHinhKetNoiPartnerConnect`).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu / upsert cấu hình kết nối | Nút "Lưu kết nối" | `POST api/CauHinhKetNoiPartnerConnect/InsertOrUpdateAsync` | `[SBAuthorize]` (key `InsertOneAsync` đã comment) | public | màn cấu hình kết nối | Upsert `CauHinhKetNoiPartnerConnect` theo đơn vị + loại. | EF | Key role (DV-AD-TONG/CN/MCN) đã comment (L30). |
| 2 | Tìm cấu hình theo điều kiện | Mở màn cấu hình | `GET api/CauHinhKetNoiPartnerConnect/TimTheoDieuKienAsync` | `CauHinhPartnerConnect_TimTheoDieuKienAsync` (DV-AD-TONG/CN/MCN) | public | màn cấu hình | Trả cấu hình kết nối của đơn vị. | EF | — |
| 3 | TingTong: lấy cấu hình / kết nối | Tab TingTong, nút "Kết nối" | `GET .../GetCauHinhTingTong` · `POST .../ConnectTingTongAsync` | `[SBAuthorize]` (key đã comment) | public | tab TingTong | Connect gọi auth TingTong → lưu token cấu hình. | EF + API ngoài | — |
| 4 | Bravo: lấy / lưu cấu hình + kết nối | Tab Bravo, nút "Kết nối"/"Lưu" | `GET .../GetCfgBravo` · `POST .../SaveCfgBravo` · `POST .../ConnectBravo` | `[SBAuthorize]`; **ConnectBravo `[AllowAnonymous]`** | mixed | tab Bravo | ConnectBravo auth Bravo (anonymous callback); SaveCfgBravo lưu cấu hình. | EF + API ngoài | ConnectBravo mở anonymous (L95) — endpoint auth không gắn quyền. |

## Endpoint nội bộ (private / service-to-service)
- `[private]` `GET .../GetCauHinhKetNoiByDonViPartnerConnectType?idDonVi&partnerConnectType` — service nội bộ lấy cấu hình theo đơn vị + loại để biết route đồng bộ.
- `[private]` `GET .../GetCfgBravoPrivate/{id}` — lấy cấu hình Bravo theo id (đồng bộ nền).
- `[internal/test]` `POST .../TingTongLead` (no auth attr) + `POST .../GetItem|GetWarehouse|GetEmployee|GetInventory` (`[SBAuthorize]`) — test gọi API Bravo/TingTong qua `IBravoService`/`TingTongHelper`.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- `partnerConnectType` quyết định bản cấu hình + đối tác đích (TingTong vs Bravo) → 1 đơn vị có nhiều bản theo loại.

## Quan hệ
- **Gọi sang:** `ICauHinhKetNoiPartnerConnectService`, `IBravoService`, `TingTongHelper` (HTTP ra TingTong/Bravo).
- **Dữ liệu (entities):** `CauHinhKetNoiPartnerConnect`.
- **Ripple/đồng bộ:** cấu hình này là điều kiện để consumer đồng bộ khách/đơn sang Bravo & lead sang TingTong.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhPartnerConnectController.cs:95` — `ConnectBravo` để `[AllowAnonymous]`, không gắn quyền.
- `CauHinhPartnerConnectController.cs:72-78` — `TingTongLead` không có attribute auth + **hardcode URL dev + JWT token** trong code (L75) → rủi ro test endpoint lọt production.
- Tên file ≠ tên class (file `CauHinhPartnerConnect...`, class `CauHinhKetNoiPartnerConnect...`).
