# Module Ticket hỗ trợ (WorkFlow) — Tổng quan

> Nguồn: workflow đọc read-only 7 mảng con (BE + FE, Group4) + **Claude verify tay các fact then chốt với code thật 2026-07-15** (chat HinhAnhIds, filter DTO, hằng trạng thái, không có luồng duyệt, FE render/gửi ảnh). **Kho là snapshot** — `path:line` có thể lệch theo thời gian, mở source verify trước khi sửa.

## Mục đích
Module **Ticket** (dịch vụ **WorkFlow**, tên nghiệp vụ "Guideline SandBox ERP") quản lý **đề xuất/yêu cầu/bug của khách** gửi lên team Sandbox: khách gửi phiếu thô → AI gom thành phiếu chức năng → team Sandbox xử lý, chat 2 chiều (kèm ảnh), theo dõi trạng thái + SLA phản hồi.

## Các thực thể chính

| Thực thể | Lưu ở | Vai trò |
|---|---|---|
| **TicketCustomer** | SQL (BaseEntity) | Phiếu/đề xuất **THÔ** của khách — mỗi khách 1 dòng, **KHÔNG dedup**. Form mới ghi thẳng vào đây. `MaTicket` = `T{N}` unique toàn hệ thống. |
| **SandboxIssue** | SQL (BaseEntity) | Phiếu **CHỨC NĂNG sau GOM** — dedup theo `(IdDonVi, SandboxIssueMa)`, 1 mã = 1 dòng. Nhiều TicketCustomer trỏ về 1 SandboxIssue. |
| **DataReceiveTicket** | SQL (BaseEntity) | Bảng nhận **THÔ webhook** từ đối tác: lưu raw JSON, `InProcess=true` rồi publish message cho Consumer. `Type='ticket'\|'ticketCustomer'`. |
| **TicketCustomerChat** | **MongoDB** (POCO) | 1 tin chat trong ticket (text và/hoặc ảnh). `NguoiGuiLoai='SB'\|'KHACH'`, ảnh lưu dạng file-id nối `;`. |

3 entity SQL kế thừa `BaseEntity` → có sẵn `Id (Guid)`, `IsDelete`, `NgayTao/NgayCapNhat/NgayXoa`, `NguoiTao/CapNhat/Xoa`. Chat Mongo **KHÔNG** có BaseEntity → audit set thủ công.

## Quan hệ THÔ ↔ GOM
- `TicketCustomer.SandboxIssueMa` (VARCHAR(100), **NULLABLE**) + `TicketCustomer.SandboxIssueId` (Guid? FK → `SandboxIssue.Id`): set **cùng lúc** khi AI gom. `NULL` = chưa gom.
- `SandboxIssue.SandboxIssueMa` là **NOT NULL** (default `string.Empty`).
- `TicketCustomer.TrangThai` và `SandboxIssue.TrangThai` là **2 cột độc lập**; hằng `TicketCustomerTrangThai` chỉ áp cho TicketCustomer.

## Luồng chính
**1. Ticket vào (2 đường):**
- **Form**: `POST api/TicketCustomer/GuiDeXuat` → tạo 1 TicketCustomer, sinh `MaTicket`, `TrangThai=MOI`, chưa gom.
- **Webhook đối tác**: POST → lưu `DataReceiveTicket` (raw JSON) → publish → `DataReceiveTicketConsumer` → tạo TicketCustomer/SandboxIssue → set `IsDelete=1` cho DataReceiveTicket.

**2. Gom (AI):** list thô cross-tenant (`POST api/TicketCustomer/LayDanhSach`) → set `SandboxIssueMa + SandboxIssueId` (`CapNhatMaEnd`) + tạo/gắn SandboxIssue.

**3. Xử lý:** SB team mở phiếu, đổi `TrangThai`, ghi `PhanHoiSandbox` (chỉ SB team) qua `CapNhat`. Vòng đời **MOI → DANG_XU_LY → DA_XU_LY**.

**4. Chat (Trao đổi):** `POST api/TicketCustomerChat/GuiTin` + `GET api/TicketCustomerChat/DanhSach/{idTicket}` — text và/hoặc ảnh, lưu Mongo. Mỗi tin **denormalize** lên ticket cha (SQL): `ChoSBPhanHoi`, `TinCuoiNoiDung/ThoiDiem`, `ThoiGianPhanHoiPhut`.

**5. Đóng:** `TrangThai=DA_XU_LY`. Xóa chỉ khi `TrangThai=MOI` (+ đúng đơn vị; SB team xóa mọi đơn vị).

## Bảng trạng thái *(verify code 2026-07-15)*

| Mã (DB) | Ý nghĩa | Ghi chú |
|---|---|---|
| `MOI` | Mới tạo | Chỉ trạng thái này mới cho **Xóa**. |
| `DANG_XU_LY` | Đang xử lý | SB team đang làm. |
| `DA_XU_LY` | Đã xử lý | Đóng. |

- Hằng `TicketCustomerTrangThai`: `Moi="MOI"`, `DangXuLy="DANG_XU_LY"`, `DaXuLy="DA_XU_LY"`. Cột `TrangThai` = **NVARCHAR lưu MÃ chữ** (không phải int/enum).
- **KHÔNG có** trạng thái "duyệt/approve" — DANG_XU_LY/DA_XU_LY là **tiến độ**, không phải cổng duyệt.

## Phân quyền (mức hiện tại)
- Tenant scope **chỉ ở mức ĐƠN VỊ** (`IdDonVi`). SB team (SuperUser / HT_AD / HT_CSKH / đơn vị hệ thống / config `SANDBOX_GUIDELINE_DON_VI_ID`) → cross-tenant; user thường → chỉ `IdDonVi` mình.
- **CHƯA có** phân quyền theo sale (không lọc `NguoiTaoUserId`, chưa dùng `IdPhongBan/IdNhomNhanVien`).

## FE
Màn `ticket-ho-tro` có **3 tab**: `moi` (TicketCustomer thô, paging **server**), `issue` (SandboxIssue, filter **client**), `thongke` (**placeholder "đang phát triển"**). Tab issue + thongke chỉ SB team thấy. Chi tiết mở qua drawer `ticket-detail-drawer` (Tab Thông tin + Tab Trao đổi/chat).

→ Cẩm nang dev chi tiết + map task đang làm: xem `dev.md` cùng folder. Registry node: `registry/cong-viec/TicketCustomerController.md`.
