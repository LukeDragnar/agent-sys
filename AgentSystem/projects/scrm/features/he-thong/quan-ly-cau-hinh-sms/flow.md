# Luồng — Quản lý cấu hình SMS (duyệt đơn vị dùng tài khoản SMS)

## Tiền điều kiện
- Người dùng đăng nhập với role DV-AD-TONG (admin tổng đơn vị) hoặc HT-KT (kế toán hệ thống).
- Đã có tài khoản SMS (AccountSms) để đơn vị đăng ký dùng.
- Bản ghi đăng ký `AccountSmsUnitUsed` được tạo (qua Thêm mới hoặc luồng đăng ký dùng tài khoản).

## Luồng dùng (admin/kế toán — duyệt)
1. Mở **Quản trị → Quản lý phê duyệt tài khoản cấu hình hệ thống** (cấp phép tài khoản SMS).
2. Lọc theo **Đơn vị**/**Chi nhánh** → **Tìm kiếm** → gọi `TimTheoDieuKien`, trả danh sách đơn vị đăng ký dùng tài khoản (chỉ bản ghi thuộc đơn vị đăng nhập).
3. Mỗi dòng hiển thị trạng thái phê duyệt `isUseDefaultAccepted`:
   - `null/undefined` → "Đang đợi duyệt" (badge vàng)
   - `true` → "Đã được duyệt" (badge xanh)
   - `false` → "Bị từ chối" (badge đỏ)
4. Bấm **Phê duyệt** (✓, hiện khi chưa = true) → confirm → `ChangeAccept/{id}` với accept=true → trạng thái → Đã duyệt.
5. Bấm **Hủy phê duyệt** (✗, hiện khi chưa = false) → confirm → accept=false → trạng thái → Bị từ chối.

## Luồng cấu hình (admin)
- **Thêm mới** bản ghi đơn vị-dùng-tài-khoản: `ThemMoi` tạo quan hệ đơn vị ↔ tài khoản SMS (trạng thái ban đầu Đang đợi duyệt).
- **Xóa** bản ghi: `Xoa/{id}` (kiểm tra tồn tại trước khi xóa).
- **Xem chi tiết**: `TimTheoId/{id}` — chỉ trả nếu bản ghi thuộc đơn vị đăng nhập, ngược lại "Không tìm thấy".

## Các trường hợp / case biên
- **Đổi trạng thái ngược:** đang Đã duyệt → bấm Hủy phê duyệt → Bị từ chối; đang Bị từ chối → Phê duyệt lại → Đã duyệt. Mỗi lần đổi đều có confirm.
- **Xem bản ghi của đơn vị khác:** `LayTheoIdAsync` lọc theo `CurrentUser.DonViId` → không thuộc đơn vị → trả "Không tìm thấy" (text backend ghi nhầm "Không tìm thấy Q&A!").
- **Kiểm tra đã duyệt (`IsAccepted`):** nhận param `id` nhưng service chỉ dùng `CurrentUser` (đơn vị đăng nhập) — truyền id nào cũng cho kết quả theo đơn vị đăng nhập (xem dev.md).
- **Gửi tin khi chưa duyệt:** module SMS kiểm tra trạng thái duyệt; chưa duyệt → đơn vị bị chặn gửi qua tài khoản đó.
- **Đầu vào rỗng (Tìm kiếm không chọn đơn vị):** trả danh sách trong phạm vi đơn vị đăng nhập.

## Ví dụ cụ thể có số
Đơn vị "Spa An Nhiên" (chi nhánh CN-Q1) đăng ký dùng tài khoản SMS đại lý "BRANDNAME_X" (loại SMS):
- Sau đăng ký: 1 dòng, trạng thái `null` → "Đang đợi duyệt".
- Kế toán hệ thống (HT-KT) mở màn, lọc đơn vị "Spa An Nhiên" → thấy 1 dòng vàng.
- Bấm **Phê duyệt** (✓) → confirm → `ChangeAccept/{id}` accept=true → dòng chuyển xanh "Đã được duyệt".
- Module SMS sau đó cho phép "Spa An Nhiên" gửi tin qua BRANDNAME_X.
- Một tháng sau quá hạn → kế toán bấm **Hủy phê duyệt** (✗) → dòng chuyển đỏ "Bị từ chối" → đơn vị không gửi qua tài khoản này được nữa.

## Hậu điều kiện
- Trạng thái duyệt của bản ghi `AccountSmsUnitUsed` được cập nhật trong DB.
- Khả năng gửi tin của đơn vị qua tài khoản tương ứng thay đổi theo trạng thái mới.

---

## Cấu hình gateway/nhà cung cấp SMS (ConfigSMS) — *(bổ sung Pha 2 — ConfigSMS gateway)*

> Phần này độc lập với luồng duyệt đơn vị-dùng-tài-khoản ở trên. ConfigSMS là CRUD **cấu hình kết nối tới nhà cung cấp SMS (gateway)** mà hệ thống đọc khi gửi tin. Trace: `ConfigSMSController` (service Core, route `api/QuanTriArea/ConfigSMS`).

### Tiền điều kiện
- Người dùng đã đăng nhập (controller `[SBAuthorize]`). Hiện **không có quyền riêng cho từng action** — mọi user authenticated đều CRUD được (xem dev.md, Bug-prone).
- Có thông tin kết nối từ nhà cung cấp SMS (mã, server/nhà cung cấp, brandname, khóa API/secret).

### Luồng cấu hình (admin — CRUD gateway)
1. **Xem danh sách:** `TimTheoDieuKien` (lọc/paging theo `ConfigSMSFilterDto`) hoặc `LayTatCa` → lưới cấu hình gateway.
2. **Xem chi tiết:** `TimTheoId/{id}` → `GetByIdAsync(id)` trả 1 bản ghi `ConfigSMS`.
3. **Thêm mới:** `ThemMoi` → validate model → map `ConfigSMSDto` → `ConfigSMS` → `InsertAsync`.
4. **Cập nhật:** `CapNhatThongTin` → validate → check tồn tại (`GetByIdAsync`, nhánh `data.Data == null` → `MESS_NotFound`) → `UpdateAsync`.
5. **Xóa:** `Xoa/{id}` → (định) check tồn tại → `DeleteAsync(id)` → trả `true`.

### Luồng đọc (hệ thống gửi tin)
- Khi gửi SMS, service gửi tin gọi `GetByMa/{ma}` → `GetCauHinhSMSByMaAsync(ma)` lấy cấu hình theo mã để biết kết nối tới nhà cung cấp nào. Endpoint này **`[AllowAnonymous]`** (không cần đăng nhập).

### Case biên (ConfigSMS gateway)
- **Cập nhật bản ghi không tồn tại:** `CapNhatThongTin` → `data.Data == null` → trả `MESS_NotFound` (chặn đúng).
- **Xóa bản ghi không tồn tại:** nhánh check `if (data == null)` dùng `data` = `MethodResult` (luôn != null) → **không bao giờ vào nhánh NotFound**, vẫn gọi `DeleteAsync` → có thể xóa "rỗng" mà không báo lỗi tồn tại (bug, xem dev.md `:122`).
- **Đoán mã để lấy cấu hình:** vì `GetByMa` mở public, gọi `GET .../GetByMa/{ma}` với mã đoán được → **lộ cấu hình gateway gồm khóa API/secret** (`ApiKey`/`SecretKey`/`BrandName`) mà không cần đăng nhập (lỗ hổng, xem dev.md `:135-136`).
- **Model không hợp lệ (Thêm/Cập nhật):** `ModelState.IsValid == false` → trả `MESS_Model_UnValidate`.

### Ví dụ cụ thể có số
Khai báo gateway nhà cung cấp "ESMS" cho gửi tin brandname "SPA-AN-NHIEN":
- Admin **Thêm mới** 1 cấu hình: mã `SMS_ESMS_01`, server/nhà cung cấp ESMS, BrandName `SPA-AN-NHIEN`, ApiKey/SecretKey do ESMS cấp, `SuDung = true`, `IsSanbox = false` → `InsertAsync` lưu 1 bản ghi `ConfigSMS`.
- Khi hệ thống gửi 1 tin, gọi `GetByMa/SMS_ESMS_01` → trả về bản ghi → dùng ApiKey/SecretKey kết nối ESMS gửi tin với brandname `SPA-AN-NHIEN`.
- **Lỗ hổng minh họa:** một người ngoài, không đăng nhập, gọi `GET /api/QuanTriArea/ConfigSMS/GetByMa/SMS_ESMS_01` → nhận đúng bản ghi gồm ApiKey/SecretKey → có thể giả mạo gửi tin.

### Hậu điều kiện (ConfigSMS gateway)
- Bản ghi `ConfigSMS` được thêm/sửa/xóa trong DB.
- Việc gửi tin qua nhà cung cấp tương ứng thay đổi theo cấu hình mới (mã/khóa/brandname/bật-tắt).
