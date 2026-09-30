---
controller: DongBoTheoDonViController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Đồng bộ theo đơn vị (chỉ SuperUser): danh sách đơn vị dùng Marketing Campaign + nút đồng bộ/hủy đồng bộ dữ liệu"
muc_dich: "Quản lý việc đăng ký đơn vị sử dụng Marketing Campaign và chạy đồng bộ dữ liệu khách hàng theo đơn vị (sync customer campaign / cập nhật full-text địa chỉ / sync chi tiêu) qua RabbitMQ, có khóa tiến trình bằng Redis."
alias: ["đồng bộ theo đơn vị", "sync đơn vị", "đồng bộ marketing campaign", "đồng bộ khách hàng theo đơn vị"]
auth: internal
state_machine: []
deprecated: false
entities: [DongBoTheoDonVi]
stored_procs: []
depends: ["IDongBoTheoDonViService", "IPublishEndpointDonHang (RabbitMQ)", "IRedisCacheService"]
cross_module_in: ["Frontend Hệ thống (chỉ SuperUser)"]
cross_module_out: ["MarketingCampaign service (consumer EventSyncCustomerDonViMessage / EventUpdateCustomerAddressFullTextMessage / EventUpdateCustomerChiTieuMessage qua RabbitMQ)"]
---

# DongBoTheoDonViController — Đồng bộ dữ liệu khách hàng theo đơn vị (Marketing Campaign)

Controller quản lý đăng ký đơn vị dùng Marketing Campaign + kích hoạt đồng bộ dữ liệu khách hàng theo từng đơn vị. Route `api/DongBoTheoDonVi/...` (không Area). **Không có `[SBAuthorize]` ở class-level** nhưng từng action có `[SBAuthorize("...")]` riêng (role gợi ý `HT-AD, HT-CSKH`); ngoài ra **mọi action chặn cứng `CurrentUser.IsSuperUser != true` → BadRequest** ngay đầu hàm. Action `DongBoThongTinDonVi` là logic gating phức tạp: validate batch size, kiểm tra khóa Redis (tránh chạy chồng), rồi publish message RabbitMQ theo `KieuLoai`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm/cập nhật đơn vị | Thêm đơn vị | POST `api/DongBoTheoDonVi/AddOrUpdateDonVi` | `DongBoTheoDonViController_AddOrUpdateDonVi` | internal (SuperUser) | Chỉ SuperUser | Đăng ký đơn vị (CurrentUser) dùng Marketing Campaign | EF (service) | Không SuperUser → `BadRequest(ModelState)` (ModelState rỗng) |
| 2 | Hủy đồng bộ | Hủy | POST `api/DongBoTheoDonVi/HuyDongBo` (body `DongBoTheoDonViPayload`) | `DongBoTheoDonViController_HuyDongBo` | internal (SuperUser) | Chỉ SuperUser | Hủy tiến trình đồng bộ của đơn vị theo payload | EF (service) | — |
| 3 | Danh sách đơn vị | (danh sách/lọc) | POST `api/DongBoTheoDonVi/TimTheoDieuKien` (body `DongBoTheoDonViFilter`) | `DongBoTheoDonViController_TimTheoDieuKien` | internal (SuperUser) | Chỉ SuperUser | Trả DS đơn vị dùng Marketing Campaign theo filter | EF (service) | — |
| 4 | Đồng bộ dữ liệu đơn vị | Đồng bộ | POST `api/DongBoTheoDonVi/DongBoThongTinDonVi` (body `DongBoTheoDonViPayload`) | `DongBoTheoDonViController_DongBoThongTinDonVi` | internal (SuperUser) | Chỉ SuperUser; cần chọn ≥1 đơn vị | Validate → check khóa Redis → publish RabbitMQ message theo `KieuLoai` (xem Quy tắc nghiệp vụ) | RabbitMQ publish + Redis lock | — |

## Quy tắc nghiệp vụ (gating / ngưỡng) — action `DongBoThongTinDonVi`
Thứ tự kiểm tra (dừng & trả lỗi tại điều kiện đầu tiên fail):
1. `IsSuperUser != true` → `BadRequest`.
2. `ModelState` không valid → lỗi model.
3. `IdsDonVi` null/rỗng → "Cần chọn đơn vị để đồng bộ!".
4. Rẽ nhánh theo `payload.KieuLoai` (`EnumEventDongBoTheoDonVi`):
   - **SyncCustomerCampaign**: `BatchSize > 2000` → lỗi "Số bản ghi tối đa là 2.000!". Với mỗi `idDonVi`: nếu Redis có khóa `KeyCache_Processing_DongBo_Theo_DonVi(SyncCustomerCampaign, idDonVi)` → "Đang có tiến trình đồng bộ dữ liệu khác đang chạy!". Pass → publish `EventSyncCustomerDonViMessage{IdDonVi, BatchSize, IsSyncFromDBReport}`.
   - **UpdateCustomerAddressFullText**: `BatchSize > 10000` → lỗi "Số bản ghi tối đa là 10.000!". Check khóa Redis tương tự → publish `EventUpdateCustomerAddressFullTextMessage{...}`.
   - **SyncCustomerSpending**: `BatchSize > 2000` → lỗi "...tối đa là 2.000!". `ListKieuLoaiChiTieu` phải không rỗng và mọi phần tử thuộc `EnumKieuLoaiUpdateCustomerChiTieu` → nếu sai "Kiểu loại dữ liệu chi tiêu không đúng". Check khóa Redis → publish `EventUpdateCustomerChiTieuMessage{..., ListKieuLoai}`.
5. Cuối cùng trả `MethodResult<bool>.ResultWithData(true)`.

> Lưu ý: việc check khóa Redis nằm trong vòng lặp riêng TRƯỚC vòng publish, nhưng **không tự set khóa** trong controller (khóa do consumer set/clear) → vẫn có race nếu 2 request gần nhau.

## Luồng xuyên module
- SuperUser bấm "Đồng bộ" → controller validate + check Redis lock → **publish message RabbitMQ** → consumer phía **MarketingCampaign** nhận và chạy đồng bộ khách hàng/địa chỉ/chi tiêu theo đơn vị (side-effect cross-service, có thể đọc từ DB Report nếu `IsSyncFromDBReport`).

## Quan hệ
- **Gọi sang:** `IDongBoTheoDonViService` (AddOrUpdate/Huy/TimTheoDieuKien), `IPublishEndpointDonHang` (publish RabbitMQ), `IRedisCacheService` (kiểm tra khóa tiến trình).
- **Dữ liệu (entities):** đăng ký đơn vị dùng Marketing Campaign; messages `EventSyncCustomerDonViMessage`, `EventUpdateCustomerAddressFullTextMessage`, `EventUpdateCustomerChiTieuMessage`.
- **Ripple / đồng bộ:** publish event → consumer MarketingCampaign chạy đồng bộ bất đồng bộ.

## Lỗi đã biết / lưu ý kỹ thuật
- `DongBoTheoDonViController.cs:34,43,57,71` — chặn `IsSuperUser` trả `BadRequest(ModelState)` (ModelState thường rỗng) thay vì 403/Forbid → client khó phân biệt lỗi quyền với lỗi dữ liệu.
- `DongBoTheoDonViController.cs:88-92,113-117,143-147` — chỉ **đọc** khóa Redis để chặn chạy chồng, controller **không set khóa** → 2 request đồng thời đều qua check nếu consumer chưa kịp set lock (race condition).
- `DongBoTheoDonViController.cs:30` — không có `[SBAuthorize]` ở class-level; bảo vệ chỉ dựa vào attribute từng method + check SuperUser runtime.
