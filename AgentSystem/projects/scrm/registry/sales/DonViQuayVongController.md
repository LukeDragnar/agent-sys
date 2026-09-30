---
controller: DonViQuayVongController
module: sales
service: ChiaContactQuayVong
man_hinh_ui: "nội bộ — không có UI; service-to-service, gọi từ Contact khi bật/tắt cấu hình chia contact xoay vòng (CH1)"
muc_dich: "Đăng ký / gỡ đơn vị vào danh sách chia contact xoay vòng. Là cổng đồng bộ trạng thái bật-tắt cấu hình CH1 từ service Contact sang service ChiaContactQuayVong."
alias: ["DonViQuayVong", "đơn vị chia xoay vòng", "chia contact xoay vòng", "CH1 xoay vòng"]
auth: internal
state_machine: []
deprecated: false
entities: [DonViQuayVong]
stored_procs: []
depends: ["ChiaContactQuayVong DataContext (EF Core)"]
cross_module_in: ["Contact service (CauHinhService) → khi bật CH1 gọi UpsertByDonVi; khi tắt CH1 gọi DeleteByDonVi"]
cross_module_out: ["bảng DonViQuayVong là nguồn để job/consumer chia lại contact xoay vòng đọc danh sách đơn vị tham gia"]
---

# DonViQuayVongController — Đăng ký đơn vị chia contact xoay vòng

Controller **thuần service-to-service** (cả 2 endpoint `[PrivateSBAuthorize]`), không có UI. Giữ danh sách đơn vị (`DonViQuayVong.IdDonVi`) đã bật cấu hình chia contact xoay vòng (CH1). Service `Contact` là caller: bật CH1 → upsert đơn vị; tắt CH1 → xóa. Entity tối giản (chỉ `IdDonVi` + BaseEntity).

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` **POST `api/DonViQuayVong/UpsertByDonVi`** — body `{ IdDonVi }`. Validate `IdDonVi != Guid.Empty` (sai → lỗi "IdDonVi không hợp lệ"). Gọi `UpsertByDonViAsync(IdDonVi, CurrentUser)` → thêm mới nếu chưa có / cập nhật nếu đã có. Trả Id bản ghi. **Caller:** Contact service khi user bật CH1 (cấu hình chia xoay vòng).
- `[PrivateSBAuthorize]` **POST `api/DonViQuayVong/DeleteByDonVi`** — body `{ IdDonVi }`. Validate `IdDonVi != Guid.Empty`. Gọi `DeleteByDonViAsync(IdDonVi)` → gỡ đơn vị khỏi danh sách xoay vòng. **Caller:** Contact service khi user tắt CH1.

## Quan hệ
- **Gọi sang:** chỉ EF nội bộ (ChiaContactQuayVong DataContext).
- **Dữ liệu (entities):** `DonViQuayVong` (chỉ `IdDonVi?` + BaseEntity) — bảng đăng ký đơn vị tham gia chia xoay vòng.
- **Ripple / đồng bộ:** Contact service (`CauHinhService`/`ChiaContactXoayVongHook`) là nguồn bật/tắt; consumer `ChiaLaiContactXoayVongConsumer` (bên Contact) thực hiện chia lại contact dựa trên danh sách đơn vị này.

## Luồng xuyên module (bật/tắt chia xoay vòng)
1. User bật cấu hình CH1 (chia contact xoay vòng) trong Contact → `CauHinhService` gọi `UpsertByDonVi` (private) sang ChiaContactQuayVong → đơn vị được ghi vào `DonViQuayVong`.
2. Cơ chế chia (consumer/job xoay vòng) đọc danh sách `DonViQuayVong` để xác định đơn vị nào áp dụng chia xoay vòng.
3. User tắt CH1 → Contact gọi `DeleteByDonVi` → gỡ đơn vị khỏi danh sách → ngừng chia xoay vòng cho đơn vị đó.

## Lỗi đã biết / lưu ý kỹ thuật
- Cả 2 endpoint chỉ `[PrivateSBAuthorize]` (shared secret service-to-service) — không gọi trực tiếp từ FE.
- DTO request `DonViQuayVongUpsertReq` khai báo ngay trong file controller (`DonViQuayVongController.cs:52-55`).
- Validate chỉ chặn `Guid.Empty`; không kiểm tra đơn vị có tồn tại thật ở Core/User → trách nhiệm đảm bảo tính hợp lệ nằm ở caller (Contact).
