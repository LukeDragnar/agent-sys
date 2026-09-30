---
controller: ThongBaoController
module: he-thong
service: Notification
man_hinh_ui: "Hệ thống > Thông báo (soạn thông báo gửi cho chi nhánh/role): list + form thêm/sửa; nút Thêm mới / Sửa / Xóa"
muc_dich: "CRUD 'thông báo' do admin đơn vị soạn để gửi đến chi nhánh/role của đơn vị mình (lưu SQL ThongBao + ThongBaoNguoiNhan), nguồn cho job đẩy push tới topic tương ứng."
alias: ["ThongBao", "thông báo đơn vị", "soạn thông báo", "gửi thông báo nội bộ"]
auth: mixed
state_machine: [ChuaGui, DaGui]   # cờ DaGui — thông báo đã gửi thì không sửa/xóa được
deprecated: false
entities: [ThongBao, ThongBaoNguoiNhan]   # SQL (NotificationDataContext)
stored_procs: [GetNextThongBao]   # SP do JobSendRequestNotiToQueue gọi để lấy thông báo đến hạn gửi
depends: ["NotificationDataContext (SQL)", "IUserPrincipalService (đơn vị/chi nhánh hiện tại)", "FirebaseMessaging (chỉ endpoint test send-test)"]
cross_module_in: ["FE Hệ thống/Thông báo gọi CRUD", "JobSendRequestNotiToQueue → EXEC GetNextThongBao đọc ThongBao đến hạn"]
cross_module_out: ["notification_queue (Mongo) — job sinh queue từ ThongBao theo topic đơn vị/chi nhánh/role", "FCM (endpoint send-test demo)"]
---

# ThongBaoController — Soạn & quản lý thông báo gửi nội bộ đơn vị

Admin đơn vị soạn thông báo (tiêu đề/nội dung/ảnh/link, chọn ngày gửi, chọn nơi nhận = tất cả / chi nhánh / role). Lưu **SQL** (`ThongBao` + bảng map `ThongBaoNguoiNhan`). Định kỳ `JobSendRequestNotiToQueue` chạy SP `GetNextThongBao` lấy bản đến hạn → quy ra topic → đẩy `notification_queue` (Mongo) → push FCM.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách thông báo | Grid / Lọc | POST `api/ThongBao/TimTheoDieuKien` | `ThongBao_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | — | Lọc theo **đơn vị + chi nhánh của user**, keyword (TieuDe/NoiDung), TuNgay/DenNgay (theo NgayGui), SuDung; sort NgayCapNhat desc; phân trang. | EF `ThongBao` (nolock) | Sort luôn theo NgayCapNhat (nhánh if/else trùng nhau) |
| 2 | Xem chi tiết | Mở thông báo | GET `api/ThongBao/TimTheoId/{id:Guid?}` | `ThongBao_TimTheoId` (DV-AD-TONG/CN/MCN) | public | — | Get ThongBao + ghép `ThongBaoNguoiNhan` (tách ChiNhanhds/Roles từ chuỗi ';'). Không tồn tại → lỗi. | EF | — |
| 3 | Thêm mới | Lưu | POST `api/ThongBao/ThemMoi` | `ThongBao_ThemMoi` (DV-AD-TONG/CN/MCN) | public | — | Validate Tiêu đề/Nội dung không rỗng; nếu không "gửi tất cả" thì phải chọn chi nhánh **hoặc** role. Set đơn vị/chi nhánh từ user. Insert ThongBao + ThongBaoNguoiNhan trong **1 transaction (ExecutionStrategy)**. | EF transaction | — |
| 4 | Cập nhật | Lưu | POST `api/ThongBao/CapNhatThongTin` | `ThongBao_CapNhatThongTin` (DV-AD-TONG/CN/MCN) | public | Chỉ khi **chưa gửi** | Validate như thêm; `DaGui==true` → chặn ("không thể xóa thông báo đã gửi"); chỉ chủ đơn vị+chi nhánh được sửa. Xóa ThongBaoNguoiNhan cũ → tạo lại, transaction. | EF transaction | Message dùng chữ "xóa" cho thao tác sửa |
| 5 | Xóa | Xóa | POST `api/ThongBao/Xoa/{id:Guid}` | `ThongBao_Xoa` (DV-AD-TONG/CN/MCN) | public | — | Tồn tại? `NguoiTaoUserId != CurrentUser.Id` → "không có quyền xóa". Rồi `DeleteAsync(id)`. | EF | Check quyền theo **người tạo** (khác Update check theo đơn vị) |
| 6 | Test push FCM (demo) | (không có UI) | POST `api/ThongBao/send-test` | — | anonymous | Dev test | Gửi 1 message FCM trực tiếp tới DeviceToken nhập tay; bắt `FirebaseMessagingException` và trả gợi ý sửa (SenderIdMismatch/Unregistered/InvalidArgument). | FCM `SendAsync` | `[AllowAnonymous]` — endpoint debug còn để ngỏ trên môi trường thật |

## Endpoint nội bộ (private / service-to-service)
- [internal] **`JobSendRequestNotiToQueue` không gọi HTTP** mà `EXEC GetNextThongBao` trực tiếp lên SQL để lấy các ThongBao đến hạn (`NgayGui<=now`, chưa gửi) → map sang topic (HE_THONG / DONVI_ / DONVI_CN_ / DONVI_ROLE_ / DONVI_CN_ROLE_) → `CreateQueue` (Mongo). Đây là cầu nối SQL→Mongo→FCM.

## Quy tắc nghiệp vụ (chọn nơi nhận → topic)
Khi job xử lý 1 ThongBao (logic ở `JobSendRequestNotiToQueue.SendNoti`):
1. `IsHeThong==true && ThongBaoTatCa==true` → topic `HE_THONG` (toàn hệ thống).
2. `ThongBaoTatCa==true` (đơn vị) → topic `DONVI_<IdDonVi>`.
3. Có `IdDonVis` (superadmin chọn đơn vị) → mỗi đơn vị 1 topic `DONVI_<dv>`.
4. Chỉ `ChiNhanhds` (không Roles) → mỗi chi nhánh `DONVI_<dv>_CN_<cn>`.
5. Chỉ `Roles` (không ChiNhanhds) → mỗi role `DONVI_<dv>_ROLE_<role>`.
6. Có **cả** ChiNhanhds + Roles → tích Descartes `DONVI_<dv>_CN_<cn>_ROLE_<role>`.
Mỗi nhánh đẩy 1 bản vào `notification_queue` với `SendedDate=NgayGui`, `TrangThai=START`.

## Quan hệ
- **Gọi sang:** NotificationDataContext (SQL CRUD), IUserPrincipalService (đơn vị/chi nhánh), FCM (chỉ send-test).
- **Dữ liệu (entities):** `ThongBao` (master: TieuDe, NoiDung, Images, Url, NgayGui, SuDung, ThongBaoTatCa, IsHeThong, DaGui, IdDonVi, IdChiNhanh), `ThongBaoNguoiNhan` (ChiNhanhds/IdDonVis/Roles dạng chuỗi ';').
- **Ripple / đồng bộ:** ThongBao (SQL) → job → notification_queue (Mongo) → JobSendNotification → SendNotificationConsumer → FCM.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThongBaoController.cs:108-125` `Xoa` check quyền theo `NguoiTaoUserId` còn `CapNhat`/Update check theo `IdDonVi+IdChiNhanh` → **2 chuẩn phân quyền lệch nhau** trên cùng entity.
- `ThongBaoController.cs:129-187` `send-test` `[AllowAnonymous]` — endpoint demo push FCM mở public; nên gỡ/khóa ở Production.
- `ThongBaoService.cs:188,383` — thông báo lỗi dùng từ "xóa" cho hành động sửa ("Bạn không thể xóa thông báo đã gửi") → message gây hiểu nhầm.
- `ThongBaoService.cs:271-278` `TimTheoDieuKien` — 2 nhánh sort if/else **giống hệt nhau** (luôn OrderByDescending NgayCapNhat) → cờ Sorts vô tác dụng.
- `JobSendRequestNotiToQueue.cs:56-57` — môi trường `*Debug` (StagingDebug/ProductionDebug/SprintDebug) **return sớm** = job không chạy → debug sẽ không thấy thông báo gửi đi.
