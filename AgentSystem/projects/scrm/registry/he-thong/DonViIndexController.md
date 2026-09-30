---
controller: DonViIndexController
module: he-thong
service: ESExtended
man_hinh_ui: "nội bộ — không có UI (đăng ký/đồng bộ map đơn vị→IndexName, tra cứu IndexName cho service ngoài)"
muc_dich: "Quản lý mapping đơn vị+domain → IndexName của Elasticsearch (đồng bộ tất cả đơn vị, lọc danh sách, và cho service ngoài như WorkFlow lookup IndexName)."
alias: ["đăng ký index đơn vị", "map index đơn vị", "lookup index name", "DonViIndex"]
auth: public
state_machine: []
deprecated: false
entities: [DonViIndex]
stored_procs: []
depends: [IDonViIndexService]
cross_module_in: ["WorkFlow gọi GetIndexName để lookup IndexName domain CongViec", "Job/tool HT gọi SyncAllDonVi"]
cross_module_out: []
---

# DonViIndexController — Map đơn vị → IndexName (ESExtended)

Route `api/DonViIndex`, không `[TienIch]`/`[SBAuthorize]` → **public** (GetIndexName còn `[AllowAnonymous]` rõ ràng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ tất cả đơn vị | — (không UI) | POST `api/DonViIndex/SyncAllDonVi` | — | public | — | Tạo/đồng bộ row DonViIndex cho tất cả đơn vị | EF | — |
| 2 | Danh sách map index | — | POST `api/DonViIndex/TimTheoDieuKien` | — | public | — | Lọc theo `DonViIndexFilter` (Domain, SuDung, paging) | EF | — |
| 3 | Lookup IndexName | — (service-to-service) | GET `api/DonViIndex/IndexName/{idDonVi}?domain=CongViec` | — | anonymous | — | Trả IndexName của đơn vị+domain; null → trả lỗi "chưa đăng ký index domain X". Phục vụ WorkFlow khỏi truy DB ESExtended trực tiếp | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [anonymous] GET `api/DonViIndex/IndexName/{idDonVi}` — WorkFlow lookup IndexName domain CongViec để query ES trực tiếp.

## Quan hệ
- **Gọi sang:** `IDonViIndexService` (SyncAllDonVi/TimTheoDieuKien/Get).
- **Dữ liệu (entities):** `DonViIndex` (đơn vị + domain → IndexName, cờ SuDung).
- **Ripple / đồng bộ:** không publish; chỉ đọc/ghi DB ESExtended.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonViIndexController.cs:21-33` — SyncAllDonVi/TimTheoDieuKien public, không phân quyền.
