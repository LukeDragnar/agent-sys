# Luồng — Đồng bộ dữ liệu (theo đơn vị)

## Tiền điều kiện
- Người dùng đăng nhập là **SuperUser** (`CurrentUser.IsSuperUser == true`). Mọi action chặn cứng ngay đầu hàm nếu không phải SuperUser.
- Đơn vị muốn đồng bộ đã được **đăng ký dùng Marketing Campaign** (qua `AddOrUpdateDonVi`).
- Dịch vụ **Marketing Campaign** (consumer RabbitMQ) đang chạy để nhận và xử lý message đồng bộ.
- Redis hoạt động để kiểm tra khóa tiến trình tránh chạy chồng.

## Luồng dùng (đồng bộ — SuperUser)
1. SuperUser mở **Hệ thống → Đồng bộ theo đơn vị**.
2. Xem danh sách đơn vị (`TimTheoDieuKien`), chọn ≥1 đơn vị cần đồng bộ.
3. Chọn **kiểu đồng bộ** (`KieuLoai` thuộc `EnumEventDongBoTheoDonVi`): SyncCustomerCampaign / UpdateCustomerAddressFullText / SyncCustomerSpending, và **BatchSize**.
4. Bấm **Đồng bộ** → `DongBoThongTinDonVi` chạy chuỗi kiểm tra (xem Quy tắc nghiệp vụ).
5. Pass hết → hệ thống **publish message RabbitMQ** tương ứng cho từng đơn vị → trả `true`.
6. Consumer Marketing Campaign nhận message, chạy đồng bộ thực tế (có thể đọc từ DB Report nếu `IsSyncFromDBReport`).

## Luồng cấu hình (đăng ký đơn vị — SuperUser)
1. `AddOrUpdateDonVi`: đăng ký đơn vị hiện tại (CurrentUser) dùng Marketing Campaign.
2. `HuyDongBo`: hủy tiến trình đồng bộ của đơn vị theo payload.

## Quy tắc nghiệp vụ (gating action `DongBoThongTinDonVi` — dừng tại điều kiện đầu tiên fail)
1. `IsSuperUser != true` → `BadRequest`.
2. `ModelState` không valid → lỗi model.
3. `IdsDonVi` null/rỗng → "Cần chọn đơn vị để đồng bộ!".
4. Rẽ nhánh theo `KieuLoai`:
   - **SyncCustomerCampaign:** `BatchSize > 2000` → "Số bản ghi tối đa là 2.000!". Mỗi `idDonVi`: nếu Redis có khóa `KeyCache_Processing_DongBo_Theo_DonVi(SyncCustomerCampaign, idDonVi)` → "Đang có tiến trình đồng bộ dữ liệu khác đang chạy!". Pass → publish `EventSyncCustomerDonViMessage{IdDonVi, BatchSize, IsSyncFromDBReport}`.
   - **UpdateCustomerAddressFullText:** `BatchSize > 10000` → "Số bản ghi tối đa là 10.000!". Check khóa Redis tương tự → publish `EventUpdateCustomerAddressFullTextMessage{...}`.
   - **SyncCustomerSpending:** `BatchSize > 2000` → "...tối đa là 2.000!". `ListKieuLoaiChiTieu` phải không rỗng và mọi phần tử thuộc `EnumKieuLoaiUpdateCustomerChiTieu` → sai thì "Kiểu loại dữ liệu chi tiêu không đúng". Check khóa Redis → publish `EventUpdateCustomerChiTieuMessage{..., ListKieuLoai}`.
5. Trả `MethodResult<bool>.ResultWithData(true)`.

## Các trường hợp / case biên
- **Không phải SuperUser** → `BadRequest(ModelState)` (ModelState rỗng) → client khó phân biệt với lỗi dữ liệu (known-issue).
- **Không chọn đơn vị** → "Cần chọn đơn vị để đồng bộ!".
- **Vượt batch cap** (2.000 hoặc 10.000) → lỗi tương ứng, không publish.
- **Chi tiêu thiếu/sai kiểu loại** → "Kiểu loại dữ liệu chi tiêu không đúng".
- **Đơn vị đang có khóa Redis cùng kiểu** → "Đang có tiến trình đồng bộ dữ liệu khác đang chạy!".
- **Race condition:** controller chỉ **đọc** khóa Redis, không tự **set** khóa (khóa do consumer set/clear) → 2 request gần nhau đều qua check nếu consumer chưa kịp set lock → chạy chồng (known-issue).

## Ví dụ cụ thể có số
Đơn vị X có 5.000 khách cần đồng bộ lại sang Marketing Campaign.
- SuperUser chọn X, kiểu **SyncCustomerCampaign**, BatchSize = 2.000 → OK (≤ 2.000) → publish message đồng bộ 2.000 bản đầu. Chạy tiếp 2 đợt nữa cho đủ 5.000.
- Nếu đặt BatchSize = 3.000 → báo "Số bản ghi tối đa là 2.000!", không chạy.
- Cần chuẩn hóa địa chỉ 8.000 khách → kiểu **UpdateCustomerAddressFullText**, BatchSize = 8.000 → OK (≤ 10.000) → publish 1 message.
- Trong khi đợt SyncCustomerCampaign của X còn chạy, SuperUser bấm đồng bộ X lần nữa cùng kiểu → bị chặn "Đang có tiến trình đồng bộ dữ liệu khác đang chạy!" (nếu consumer đã kịp set khóa Redis).

## Hậu điều kiện
- **Đồng bộ thành công:** message đã được publish; dịch vụ Marketing Campaign cập nhật dữ liệu khách/địa chỉ/chi tiêu của đơn vị (bất đồng bộ).
- **Validate fail:** không publish, không tác động dữ liệu.
- Controller **không tự set khóa Redis** → trạng thái khóa do consumer quản lý; nếu consumer chưa set, lần bấm kế tiếp có thể lọt.
