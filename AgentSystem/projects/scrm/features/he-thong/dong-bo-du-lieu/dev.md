# Dev — Đồng bộ dữ liệu (theo đơn vị)

> Service: **Core** (BE .NET). Màn nội bộ super-admin (Hệ thống). Đồng bộ chạy nền qua **RabbitMQ** sang dịch vụ **MarketingCampaign**; khóa tiến trình bằng **Redis**. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Vai trò | Ghi chú |
|---|---|---|
| Đồng bộ theo đơn vị (chỉ SuperUser) | DS đơn vị dùng Marketing Campaign + nút đồng bộ/hủy | route FE: cần xác nhận khi cần (registry chưa ghi route FE) |

## Endpoint / Controller (trace registry)
**DongBoTheoDonViController** — route `api/DongBoTheoDonVi/...` (không Area). **Không có `[SBAuthorize]` ở class-level**; từng action có `[SBAuthorize("...")]` riêng (gợi ý HT-AD, HT-CSKH) + chặn cứng `CurrentUser.IsSuperUser != true` → `BadRequest`. (registry: `projects/scrm/registry/he-thong/DongBoTheoDonViController.md`)

| MaTinhNang (key) | Route | Mục đích |
|---|---|---|
| `DongBoTheoDonViController_AddOrUpdateDonVi` | POST `api/DongBoTheoDonVi/AddOrUpdateDonVi` | Đăng ký đơn vị (CurrentUser) dùng Marketing Campaign |
| `DongBoTheoDonViController_HuyDongBo` | POST `api/DongBoTheoDonVi/HuyDongBo` (body `DongBoTheoDonViPayload`) | Hủy tiến trình đồng bộ của đơn vị |
| `DongBoTheoDonViController_TimTheoDieuKien` | POST `api/DongBoTheoDonVi/TimTheoDieuKien` (body `DongBoTheoDonViFilter`) | DS đơn vị dùng Marketing Campaign theo filter |
| `DongBoTheoDonViController_DongBoThongTinDonVi` | POST `api/DongBoTheoDonVi/DongBoThongTinDonVi` (body `DongBoTheoDonViPayload`) | Validate → check khóa Redis → publish RabbitMQ theo `KieuLoai` |

## Entities & messages
- Entity `DongBoTheoDonVi` — đăng ký đơn vị dùng Marketing Campaign.
- Enum `EnumEventDongBoTheoDonVi` — `KieuLoai`: SyncCustomerCampaign / UpdateCustomerAddressFullText / SyncCustomerSpending.
- Enum `EnumKieuLoaiUpdateCustomerChiTieu` — validate `ListKieuLoaiChiTieu` cho nhánh chi tiêu.
- Messages RabbitMQ: `EventSyncCustomerDonViMessage`, `EventUpdateCustomerAddressFullTextMessage`, `EventUpdateCustomerChiTieuMessage`.

## Luồng code
1. `DongBoThongTinDonVi` chặn `IsSuperUser != true` → `BadRequest`.
2. Validate `ModelState`, `IdsDonVi` không rỗng.
3. Rẽ nhánh theo `payload.KieuLoai`:
   - Check batch cap (2.000 / 10.000 / 2.000 + kiểu loại chi tiêu).
   - Vòng lặp đọc khóa Redis `KeyCache_Processing_DongBo_Theo_DonVi(KieuLoai, idDonVi)` → có khóa thì chặn.
   - Pass → `IPublishEndpointDonHang.Publish(...)` message tương ứng cho từng `idDonVi`.
4. Trả `MethodResult<bool>.ResultWithData(true)`.
5. Consumer **MarketingCampaign** nhận message và chạy đồng bộ thực tế (đọc DB Report nếu `IsSyncFromDBReport`).

`AddOrUpdateDonVi` / `HuyDongBo` / `TimTheoDieuKien` → `IDongBoTheoDonViService` (EF).

## Bug-prone / lưu ý (path:line)
- **`DongBoTheoDonViController.cs:34,43,57,71` — chặn `IsSuperUser` trả `BadRequest(ModelState)`** (ModelState thường rỗng) thay vì 403/Forbid → client khó phân biệt lỗi quyền với lỗi dữ liệu. Nên trả `Forbid()` hoặc message rõ.
- **`DongBoTheoDonViController.cs:88-92,113-117,143-147` — race condition:** controller chỉ **đọc** khóa Redis để chặn chạy chồng, **không tự set khóa** (khóa do consumer set/clear) → 2 request gần nhau đều qua check nếu consumer chưa kịp set lock → đồng bộ chồng. Nên set khóa ngay tại controller trước khi publish.
- **`DongBoTheoDonViController.cs:30` — không có `[SBAuthorize]` class-level**; bảo vệ chỉ dựa vào attribute từng method + check SuperUser runtime → nếu thêm action mới mà quên attribute thì hở. Lưu ý khi mở rộng controller.
- Check khóa Redis nằm trong **vòng lặp riêng TRƯỚC** vòng publish → giữa 2 vòng có khoảng trống, càng tăng cửa sổ race.

> KHÔNG sửa code ở session này — chỉ ghi nhận; để session security xử lý.

## File source (path:line)
- BE controller: `DongBoTheoDonViController.cs:30` (class), `:34/:43/:57/:71` (chặn SuperUser), `:88-147` (check khóa Redis + publish).
- Service: `IDongBoTheoDonViService` (AddOrUpdate / Huy / TimTheoDieuKien); `IPublishEndpointDonHang` (publish RabbitMQ); `IRedisCacheService` (khóa tiến trình).
- Registry: `projects/scrm/registry/he-thong/DongBoTheoDonViController.md`.
- Consumer (cross-service): MarketingCampaign — `EventSyncCustomerDonViMessage` / `EventUpdateCustomerAddressFullTextMessage` / `EventUpdateCustomerChiTieuMessage`.
