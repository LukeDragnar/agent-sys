---
controller: CauHinhController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài > Cấu hình tổng đài (lưu/lấy cấu hình init + cấu hình chung tổng đài của đơn vị)"
muc_dich: "Cấu hình tổng đài (Switchboard) cấp đơn vị: lưu cấu hình init, lấy cấu hình tổng đài và cấu hình init. (Trùng tên CauHinhController của module Contact → node này hậu tố _Switchboard). Kế thừa SBBaseController<CauHinh>."
alias: ["cấu hình tổng đài", "cài đặt tổng đài", CauHinhSwitchboard]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [ICauHinhService, Redis (redisCacheService)]
cross_module_in: [SCRM core/đơn vị (publish CauHinhChungMessage để đồng bộ cấu hình chung)]
cross_module_out: []
---

# CauHinh (Switchboard) — Cấu hình tổng đài cấp đơn vị

> ⚠️ Trùng tên `CauHinhController` với module Contact (service: Contact). Node này = bản Switchboard (`Controllers\CauHinhController.cs`, kế thừa `SBBaseController<CauHinh,CauHinh>`) → hậu tố `_Switchboard`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình init | Lưu | POST `SaveCauHinhInit` | `CauHinh_Switchboard_SaveCauHinhIniti` (DV-AD-TONG) | public | màn cấu hình tổng đài | Lưu cấu hình init tổng đài cho `CurrentUser`. | EF | — |
| 2 | Lấy cấu hình tổng đài | (load) | GET `Get` | `[SBAuthorize]` (login) | public | mở màn cấu hình / UI tổng đài cần đọc setting | Trả `CauHinhSettingDto` của `IdDonVi` user. Đọc cache trước; miss → query DB → nạp cache (TTL 1 ngày). **Đơn vị chưa khởi tạo → fallback đọc bản ghi `IdDonVi = Guid.Empty` (cấu hình mặc định hệ thống).** | EF + Redis cache | Key `CauHinh_Switchboard_LayCauHinhTongDai` đã comment → còn `[SBAuthorize]` rỗng |
| 3 | Lấy cấu hình init | (load) | GET `GetCauHinhInit` | `[SBAuthorize]` (login) | public | mở màn cấu hình | Như #2 nhưng map sang `CauHinhSettingInitDto` để bind form. | EF + Redis cache | — |
| 4 | CRUD chung (kế thừa base) | — | (theo `SBBaseController<CauHinh>`) | base | mixed | — | CRUD cơ bản entity CauHinh từ base controller. | EF | Hành vi phụ thuộc base — không liệt kê chi tiết |

## Endpoint nội bộ (private / service-to-service)
- [internal] `SaveCauHinhChung(CauHinhChungMessage)` — **không phải HTTP**, gọi từ consumer `EventSyncCauHinhChungConsumer`. Upsert cấu hình chung theo `IdDonVi` trong message + clear cache. Nuốt exception (try/catch trống) → lỗi đồng bộ không báo ngược.
- [internal] `CauHinhCheSoTheoUserLogin(UserLogin)` — service-level, gom field che số điện thoại (`IsChoPhepGoiKhiCheSo`, `ListRoleCheSoDienThoai`, `UuTienCheSoDienThoaiChuaRoleBiChan`, `ListUserMacDinhXemSoDienThoai`) từ `CauHinhSettingDto` → dùng khi quyết định ẩn/hiện số điện thoại lúc gọi.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
1. **Đọc cấu hình theo đơn vị**: `idDonVi = userLogin.DonViId`.
2. **Thứ tự lấy dữ liệu (`GetAsync(idDonVi)`)**:
   - Cache hit (`CauHinhSwitchboardSetting:{idDonVi}`) → trả ngay.
   - Cache miss → query `CauHinh` (`IsDelete == false && IdDonVi == idDonVi`).
     - Có bản ghi & `JsonSetting` không rỗng → deserialize → nạp cache (TTL 1 ngày).
     - Không có / rỗng → tạo DTO rỗng → nạp cache.
   - Vẫn `null`: `idDonVi == Guid.Empty` → DTO rỗng; `idDonVi != Guid.Empty` → **đệ quy `GetAsync(Guid.Empty)`** lấy cấu hình mặc định hệ thống.
3. **Lưu (`SaveCauHinhInit` / `Save<T>`)**: upsert 1 bản ghi duy nhất theo `(IdDonVi, IsDelete=false)` — insert nếu chưa có, ngược lại AutoMapper map đè field lên `JsonSetting` cũ + `MarkDirty(JsonSetting)`. Luôn **xóa cache** đơn vị sau khi lưu.

## Quan hệ
- **Gọi sang:** `ICauHinhService` → `reposCauHinh` (EF), `redisCacheService` (Redis cache); + base `SBBaseController<CauHinh>`.
- **Dữ liệu (entities):** `CauHinh` (cột chính `IdDonVi`, `JsonSetting` chứa toàn bộ setting serialize; lọc `IsDelete == false`).
- **Ripple / đồng bộ:** Consumer `EventSyncCauHinhChungConsumer` (`Switchboard.API/Consumer/`) nghe `CauHinhChungMessage` (MassTransit) → `SaveCauHinhChung` → upsert cấu hình chung của đơn vị + clear cache. Cạnh VÀO từ service phát message cấu hình chung của SCRM.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs (Switchboard):40-48` — `Get` key role `CauHinh_Switchboard_LayCauHinhTongDai` comment, còn `[SBAuthorize]` rỗng (login) → mọi user đăng nhập đọc được cấu hình đơn vị.
- `EventSyncCauHinhChungConsumer.cs:21` & `CauHinhService.cs:184` — `catch` rỗng/trả `false` không log: lỗi đồng bộ cấu hình chung bị nuốt, không retry, khó debug.
- `CauHinhController.cs:39,57-60`; `CauHinhService.cs:119-124,215-218,235-240` — nhánh superuser/HT_AD (ghi/đọc theo `Guid.Empty`) đã comment → admin hệ thống lưu vào đơn vị của mình, không ghi cấu hình mặc định.
- Cache TTL cố định 1 ngày; đồng bộ dựa hoàn toàn vào clear cache trong `Save`/`SaveCauHinhChung`, không có cơ chế invalidate khác.
- Kế thừa `SBBaseController<CauHinh, CauHinh>` → có thêm endpoint CRUD base không khai báo tường minh trong file controller.
