---
controller: TicketCustomerController
module: cong-viec
service: WorkFlow
man_hinh_ui: ticket-ho-tro (Guideline SandBox ERP)
auth: SBAuthorize (login) + scope trong service; PrivateSBAuthorize cho endpoint AI
entities: [TicketCustomer, SandboxIssue, DataReceiveTicket, TicketCustomerChat]
state_machine: MOI -> DANG_XU_LY -> DA_XU_LY (không có cổng duyệt/approve)
depends: [TicketCustomerService, TicketTenantConstants, TicketCustomerTrangThai, UserLogin, ConfigSystem(SANDBOX_GUIDELINE_DON_VI_ID)]
cross_module_out: [SandboxIssueController, TicketCustomerChatController, DataReceiveTicketConsumer, FilesApi, FE:ticket-ho-tro]
deprecated: false
---

# registry/cong-viec/TicketCustomerController

> **Snapshot** (nguồn: workflow đọc read-only Group4 + verify tay 2026-07-15). `path:line` có thể lệch — verify với code hiện tại.

## Vai trò
Controller **CHÍNH** của ticket hỗ trợ khách ("Guideline SandBox ERP") — toàn bộ list/CRUD/thống kê/chat-parent. List/detail issue đã tách sang `SandboxIssueController`; `TicketController` chỉ còn `GetQuyenTicket`.
- controller: `WorkFlow/WorkFlow.API/Controllers/TicketCustomerController.cs`
- service: `WorkFlow/WorkFlow.Service/Generated/Ticket/TicketCustomerService.cs`
- entity: `WorkFlow/WorkFlow.DataContext/Entity/TicketCustomer.cs`

## Endpoints
| Method | Route | Auth | Hành vi đúng |
|---|---|---|---|
| POST | `api/TicketCustomer/DanhSach` | `[SBAuthorize]` + scope | List theo scope: SB team cross-tenant, user thường ép `IdDonVi` mình. Cũng là drill-in (lọc `SandboxIssueMa/Id`). Total qua meta. |
| POST | `api/TicketCustomer/LayDanhSach` | `[PrivateSBAuthorize]` | [AI] list thô cross-tenant (thường `ChuaGom=true`). |
| GET | `api/TicketCustomer/GetByMa/{ma}` | `[SBAuthorize]` | Drill 1 ticket theo `MaTicket`; user thường siết `IdDonVi`. |
| POST | `api/TicketCustomer/ThongKe` | `[SBAuthorize]` | Card `Tong/Moi/DangXuLy/DaXuLy` + `DanhSachDonVi`. **KHÔNG ăn filter** — chỉ scope tenant. 4× CountAsync + GroupBy. |
| POST | `api/TicketCustomer/GuiDeXuat` | `[SBAuthorize]` | CREATE — sinh `MaTicket` `T{N}`, tenant = đơn vị người gửi. CSKH gửi hộ override `NguoiTaoUserId`. |
| POST | `api/TicketCustomer/CapNhat` | `[SBAuthorize]` | UPDATE `TieuDe/NoiDung/Loai/TrangThai/PhanTich/PhanHoiSandbox`. `PhanHoiSandbox` chỉ SB team. `TrangThai` đổi **tự do** (không gate duyệt). |
| POST | `api/TicketCustomer/Xoa/{id}` | `[SBAuthorize]` | DELETE — chỉ khi `TrangThai=MOI` + đúng đơn vị (SB team xóa mọi đơn vị). |
| POST | `api/TicketCustomer/CapNhatMaEnd` | `[PrivateSBAuthorize]` | [AI] gắn `SandboxIssueMa + SandboxIssueId`. |
| POST | `api/TicketCustomer/LinkIssue` | `[SBAuthorize]` | [WEB] liên kết/gỡ ticket↔issue (rỗng=gỡ). **Chỉ SB team.** |
| GET | `api/TicketCustomer/GetInitDataTicketForm` | `[SBAuthorize]` | Init form Gửi đề xuất. |

Ngoài controller: `GET api/Ticket/GetQuyenTicket` (cờ `CoTheChonNguoiGui`), `POST api/TicketCustomerChat/GuiTin` + `GET api/TicketCustomerChat/DanhSach/{id}`.

## Quyền / scope
- `[SBAuthorize]` trống = chỉ login+session, KHÔNG check role ở attribute. `[PrivateSBAuthorize]` = service-to-service (AI), cross-tenant.
- Phân quyền tập trung `LaSandboxTeamAsync(user)` (`:69`): SB team = `CoTheChonNguoiGui` HOẶC `DonViId==config SANDBOX_GUIDELINE_DON_VI_ID` → cross-tenant; user thường → `IdDonVi==DonViId` (null→`Guid.Empty`).
- Scope **chỉ mức đơn vị** — chưa có phân quyền theo sale (`NguoiTaoUserId`).

## Ripple (cross_module_out)
- **SandboxIssue** (`SandboxIssueController`/`TicketService`): nối `SandboxIssueMa + SandboxIssueId`. `LinkIssue`/`CapNhatMaEnd` đổi liên kết THÔ↔GOM → ảnh hưởng drill-down + đếm "số phiếu gom".
- **TicketCustomerChat** (Mongo): mỗi tin denormalize ngược lên TicketCustomer (`ChoSBPhanHoi`, `TinCuoiNoiDung/ThoiDiem`, `ThoiGianPhanHoiPhut`). Đổi field SLA/tin-cuối → gãy cột "Trao đổi" ở list + drawer.
- **DataReceiveTicketConsumer**: webhook đối tác tạo TicketCustomer/SandboxIssue (Type='ticket'|'ticketCustomer'). Đổi shape TicketCustomer → ảnh hưởng consumer.
- **Files API** (`{FILES_API}/Files/*`): `HinhAnhIds` = file id nối ';', render `GetFile?size=o`.
- **FE** `scrm_frontend/.../ticket-ho-tro` (list 3 tab + drill-down) + `ticket-detail-drawer` (chi tiết + chat). DTO output là `any` ở FE → đổi DTO gãy FE thầm lặng.

## Known-issue / gotcha
- **Nhầm controller:** list ticket ở đây, KHÔNG ở `TicketController` (gần rỗng).
- **Comment entity lệch behavior:** `TicketCustomer.cs:17` ghi `IdDonVi`="đơn vị hệ thống set cứng" nhưng `GuiDeXuat (:118)` set = đơn vị người gửi. Lọc/thống kê theo **khách** phải dùng `IdDonViKhachHang`.
- **Scope copy-paste 6 chỗ** (list/thongke/getByMa/capnhat/xoa/chat) — sửa 1 quên 1 = rò cross-tenant. Refactor về 1 helper trước khi thêm phân quyền sale (591-5).
- **ThongKe không ăn filter** (`:534`) — chiều mới (591-6) phải thêm param + WHERE.
- **`TrangThai` = chuỗi MÃ NVARCHAR** (không enum số); `Loai` chuỗi tự do không class hằng → so chuỗi cẩn thận collation.
- **Keyword không sargable** (LIKE `%..%`, trộn 5 cột) — không phải bộ lọc chính xác cho người tạo (926 cần field riêng).
- **`SalesPhuTrach`** chỉ webhook set + hiển thị, KHÔNG dùng lọc quyền → dùng `NguoiTaoUserId`.
- **Không có luồng duyệt/approve** — `TrangThai` đổi tự do (591-3 = tính năng mới).
- **Không có push notification** khi đổi trạng thái / chat mới — chỉ pull qua `ChoSBPhanHoi` (591-9 = tính năng mới).
- **Trùng tên bẫy:** `Core/TicketInfoService.cs` + `Report/TicketInfoService.cs` = khóa hàng đợi Redis, KHÔNG phải thống kê ticket.
