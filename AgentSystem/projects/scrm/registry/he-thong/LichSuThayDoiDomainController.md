---
controller: LichSuThayDoiDomainController
module: he-thong
service: Core
man_hinh_ui: "Quản trị đơn vị > Domain đơn vị / Lịch sử thay đổi domain (phía ĐƠN VỊ): list yêu cầu thay đổi domain; nút Tạo yêu cầu, Hủy bỏ"
muc_dich: "Phía ĐƠN VỊ (DV-AD-TONG) gửi và quản lý yêu cầu thay đổi domain của đơn vị: tạo mới yêu cầu, hủy bỏ yêu cầu, tra lịch sử thay đổi theo điều kiện. (Bước duyệt/từ chối nằm ở bản SanboxArea — xem LichSuThayDoiDomainController_Core.)"
alias: ["LichSuThayDoiDomain", "thay đổi domain", "domain đơn vị", "yêu cầu đổi domain"]
auth: public
state_machine: [ChoDuyet, Duyet, TuChoi, HuyBo]
deprecated: false
entities: [LichSuThayDoiDomain]
stored_procs: []
depends: ["ILichSuThayDoiDomainService"]
cross_module_in: ["FE Quản trị đơn vị/Domain gọi tạo/hủy yêu cầu + tra lịch sử (role DV-AD-TONG)"]
cross_module_out: ["Yêu cầu sau khi tạo (ChoDuyet) chuyển sang luồng duyệt của Admin hệ thống (SanboxArea LichSuThayDoiDomainController_Core)"]
---

# LichSuThayDoiDomainController — Yêu cầu thay đổi domain (phía đơn vị)

Controller phía **đơn vị** trong luồng đổi domain. Admin đơn vị (role `DV-AD-TONG`) **tạo** yêu cầu thay đổi domain, **hủy** yêu cầu, và **tra cứu** lịch sử thay đổi của đơn vị mình. Mọi action có permission key riêng + `[SBAuthorize]`. Logic ở `ILichSuThayDoiDomainService`; controller validate ModelState rồi gọi service kèm `CurrentUser`.

> **Cặp đôi với** `LichSuThayDoiDomainController_Core.md` (Areas/SanboxArea, route `api/QuanTriArea/...`): bản đó là phía **Admin hệ thống** (HT-AD/HT-KT) **duyệt/từ chối** yêu cầu. Hai bản KHÁC nhau về endpoint & role (không phải bản sao y hệt).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo yêu cầu đổi domain | Tạo yêu cầu | POST `api/LichSuThayDoiDomain/ThemMoi` | `LichSuThayDoiDomain_ThemMoi` (DV-AD-TONG) | public | ModelState valid | `Create(model, CurrentUser)` — tạo yêu cầu mới (vào trạng thái Chờ duyệt). | EF `LichSuThayDoiDomain` | — |
| 2 | Hủy bỏ yêu cầu | Hủy bỏ | GET `api/LichSuThayDoiDomain/CapNhatTrangThaiHuyBo/{id:Guid}` | `LichSuThayDoiDomain_CapNhatTrangThaiHuyBo` (DV-AD-TONG) | public | yêu cầu chưa được duyệt (hợp lý) | `CapNhatTrangThaiHuyBo(id, CurrentUser)` → trạng thái Hủy bỏ. | EF | GET làm thao tác đổi state (side-effect qua GET) — lệch REST |
| 3 | Tra lịch sử (đơn vị) | Grid / Lọc | POST `api/LichSuThayDoiDomain/TimKiemTheoDieuKienDV` | `LichSuThayDoiDomain_TimKiemTheoDieuKienDV` (DV-AD-TONG) | public | — | `TimKiemTheoDieuKienDV(filter, CurrentUser)` — lọc lịch sử theo phạm vi đơn vị của user. | EF | — |

## Quy tắc nghiệp vụ (gating / state)
- Yêu cầu đổi domain đi theo state-machine: **Chờ duyệt → (Admin hệ thống) Duyệt | Từ chối**, hoặc **(Đơn vị) Hủy bỏ**.
- `ThemMoi` tạo bản ghi ở trạng thái chờ duyệt; `CapNhatTrangThaiHuyBo` chỉ hợp lệ khi chưa duyệt (ràng buộc thực thi trong service — cần xác nhận).
- Quyền duyệt/từ chối KHÔNG nằm ở controller này (ở SanboxArea `_Core`).

## Quan hệ
- **Gọi sang:** `ILichSuThayDoiDomainService` (Create / CapNhatTrangThaiHuyBo / TimKiemTheoDieuKienDV).
- **Dữ liệu (entities):** `LichSuThayDoiDomain` (yêu cầu + trạng thái + domain cũ/mới). DTO: `LichSuThayDoiDomainDto`, `LichSuThayDoiDomainFilterDto`.
- **Ripple / đồng bộ:** yêu cầu tạo ở đây → xuất hiện trong màn duyệt của Admin hệ thống (SanboxArea); khi được Duyệt sẽ áp domain mới cho đơn vị (side-effect trong service).

## Luồng xuyên module
- Đơn vị `ThemMoi` (Chờ duyệt) → Admin hệ thống mở `api/QuanTriArea/LichSuThayDoiDomain` → `CapNhatTrangThaiDuyet` (áp domain mới) hoặc `CapNhatTrangThaiTuChoi`. Đơn vị có thể `CapNhatTrangThaiHuyBo` trước khi yêu cầu được xử lý.

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuThayDoiDomainController.cs:33-34` — `CapNhatTrangThaiHuyBo` dùng **HTTP GET** để đổi trạng thái (side-effect qua GET) → lệch REST, dễ bị trigger ngoài ý muốn (prefetch/crawler).
- Controller phía đơn vị **không có** duyệt/từ chối — đó là hành vi đúng (tách quyền), nhưng khi triage cần biết 2 file khác nhau (bản này vs `_Core`).
