---
controller: LichSuThayDoiDomainController
module: he-thong
service: Core
man_hinh_ui: "Quản trị hệ thống (QuanTriArea) > Duyệt thay đổi domain đơn vị (phía ADMIN HỆ THỐNG): list yêu cầu chờ duyệt; nút Duyệt, Từ chối"
muc_dich: "Bản SanboxArea (route api/QuanTriArea/...) — phía ADMIN HỆ THỐNG (HT-AD / HT-KT) xử lý yêu cầu đổi domain do đơn vị gửi: duyệt, từ chối, tra lịch sử toàn hệ thống. Tách biệt với bản Controllers (phía đơn vị tạo/hủy)."
alias: ["LichSuThayDoiDomain admin", "duyệt domain", "QuanTriArea domain", "duyệt thay đổi domain"]
auth: public
state_machine: [ChoDuyet, Duyet, TuChoi, HuyBo]
deprecated: false
entities: [LichSuThayDoiDomain]
stored_procs: []
depends: ["ILichSuThayDoiDomainService"]
cross_module_in: ["FE Quản trị hệ thống/Duyệt domain gọi duyệt/từ chối + tra lịch sử (role HT-AD, HT-KT)"]
cross_module_out: ["Duyệt → áp domain mới cho đơn vị (side-effect trong service); kết quả phản ánh về màn lịch sử phía đơn vị"]
---

# LichSuThayDoiDomainController (SanboxArea) — Duyệt thay đổi domain (phía admin hệ thống)

Bản **Areas/SanboxArea**, route `api/QuanTriArea/LichSuThayDoiDomain/...`, `[TienIch(..., TI_HeThong, true)]` (đánh dấu khu quản trị). Phía **Admin hệ thống** (`HT-AD`, `HT-KT`) xử lý yêu cầu đổi domain mà đơn vị đã gửi: **Duyệt**, **Từ chối**, **tra lịch sử toàn hệ thống**. Dùng chung `ILichSuThayDoiDomainService` với bản đơn vị nhưng gọi các method khác (Duyet/TuChoi/TimKiemAdmin).

> **Bản đôi:** node này là biến thể SanboxArea (suffix `_Core` để tránh trùng tên file với bản Controllers). Bản phía đơn vị (tạo/hủy yêu cầu): `LichSuThayDoiDomainController.md`. Hai bản KHÁC endpoint/role — không phải copy y hệt.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Duyệt yêu cầu | Duyệt | GET `api/QuanTriArea/LichSuThayDoiDomain/CapNhatTrangThaiDuyet/{id:Guid}` | `LichSuThayDoiDomain_CapNhatTrangThaiDuyet` (HT-AD, HT-KT) | public | yêu cầu ở trạng thái Chờ duyệt | `CapNhatTrangThaiDuyet(id, CurrentUser)` → Duyệt + áp domain mới cho đơn vị. | EF `LichSuThayDoiDomain` | GET đổi state (side-effect qua GET) — lệch REST |
| 2 | Từ chối yêu cầu | Từ chối | POST `api/QuanTriArea/LichSuThayDoiDomain/CapNhatTrangThaiTuChoi` | `LichSuThayDoiDomain_CapNhatTrangThaiTuChoi` (HT-AD, HT-KT) | public | ModelState valid; đang Chờ duyệt | `CapNhatTrangThaiTuChoi(payload, CurrentUser)` → Từ chối (kèm lý do trong DTO). | EF | — |
| 3 | Tra lịch sử (toàn HT) | Grid / Lọc | POST `api/QuanTriArea/LichSuThayDoiDomain/TimKiemTheoDieuKienAdmin` | `LichSuThayDoiDomain_TimKiemTheoDieuKienAdmin` (HT-AD, HT-KT) | public | — | `TimKiemTheoDieuKienAdmin(filter, CurrentUser)` — lọc lịch sử trên phạm vi toàn hệ thống (không bó theo đơn vị). | EF | — |

## Quy tắc nghiệp vụ (gating / state)
- Chỉ xử lý yêu cầu đang **Chờ duyệt** → chuyển **Duyệt** hoặc **Từ chối**.
- **Duyệt** kéo theo side-effect: áp domain mới cho đơn vị (thực thi trong service — cần xác nhận chi tiết áp ở đâu).
- Quyền giới hạn role hệ thống `HT-AD`, `HT-KT` (khác bản đơn vị `DV-AD-TONG`).

## Quan hệ
- **Gọi sang:** `ILichSuThayDoiDomainService` (CapNhatTrangThaiDuyet / CapNhatTrangThaiTuChoi / TimKiemTheoDieuKienAdmin).
- **Dữ liệu (entities):** `LichSuThayDoiDomain`. DTO: `LichSuThayDoiDomainDto`, `LichSuThayDoiDomainFilterDto`.
- **Ripple / đồng bộ:** Duyệt → cập nhật domain đơn vị; trạng thái phản ánh về màn lịch sử phía đơn vị (`TimKiemTheoDieuKienDV`).

## Luồng xuyên module
- (Đơn vị) `ThemMoi` → bản ghi Chờ duyệt → (Admin) bản này `CapNhatTrangThaiDuyet`/`CapNhatTrangThaiTuChoi`. Duyệt = áp domain mới; Từ chối/Hủy = giữ domain cũ.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas/SanboxArea/LichSuThayDoiDomainController.cs:22-23` — `CapNhatTrangThaiDuyet` dùng **HTTP GET** cho thao tác duyệt (đổi state + áp domain) → side-effect qua GET, lệch REST; cần xác nhận chống double-submit.
- Trùng TÊN class với bản `Controllers/LichSuThayDoiDomainController.cs` (cùng `LichSuThayDoiDomainController`) nhưng khác namespace/route/role — dễ nhầm khi search; node này tách `_Core` để phân biệt.
- `[TienIch(..., true)]` đánh dấu khu QuanTriArea (admin) — ẩn khỏi menu thường của đơn vị.
