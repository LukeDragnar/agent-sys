---
controller: AIDonViPromptController
module: he-thong
service: SandBoxAI
man_hinh_ui: "Cấu hình AI (SandBox) > Quản lý prompt theo đơn vị (danh sách + thêm/sửa/xóa, nút 'Khởi tạo mẫu')"
muc_dich: "Quản lý CRUD bộ prompt AI riêng của từng đơn vị (mã/tên/prompt/mô tả) dùng cho tính năng AI trong SandBox; có nút khởi tạo mẫu chuẩn cho đơn vị mới."
alias: ["AIDonViPrompt", "prompt AI đơn vị", "mẫu prompt", "cấu hình prompt SandBox"]
auth: public
state_machine: []
deprecated: false
entities: [AIDonViPrompt]
stored_procs: []
depends: ["SandBoxAIDataContext (EF Core, BulkInsertOrUpdate)", "AutoMapper"]
cross_module_in: ["FE màn cấu hình AI SandBox → CRUD + KhoiTaoMau"]
cross_module_out: []
---

# AIDonViPromptController — Quản lý prompt AI theo đơn vị

CRUD chuẩn cho bảng `AIDonViPrompt` (mã, tên, prompt, mô tả) — mỗi đơn vị giữ bộ prompt riêng (`IdDonVi`). Mọi thao tác **scope cứng theo `CurrentUser.DonViId`**: đọc/sửa/xóa bản ghi của đơn vị khác → trả NotFound. Có thêm `KhoiTaoMau` clone bộ prompt mẫu (`IdDonVi == Guid.Empty`) sang đơn vị hiện tại. Controller-level không có `[SBAuthorize]`, từng method gắn `[SBAuthorize]` (login, không check permission key cụ thể — chỉ comment gợi ý key).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (click 1 dòng) | GET `api/AIDonViPrompt/TimTheoId/{id?}` | — | public (login) | — | GetById; nếu `IdDonVi != CurrentUser.DonViId` → NotFound. | EF `AIDonViPrompt` | Gọi `GetByIdAsync` **2 lần** (1 lần check, 1 lần trả) — dư 1 query |
| 2 | Thêm mới | "Thêm mới" | POST `api/AIDonViPrompt/ThemMoi` | (gợi ý: AIDonViPrompt_ThemMoi) | public (login) | — | Map DTO→entity, ép `IdDonVi = CurrentUser.DonViId`, Insert. ModelState sai → lỗi validate. | EF insert | Permission key chỉ ở comment, thực tế chỉ cần login |
| 3 | Cập nhật | "Lưu" | POST `api/AIDonViPrompt/CapNhatThongTin` | (gợi ý: AIDonViPrompt_CapNhatThongTin) | public (login) | — | GetById theo Id; null hoặc khác đơn vị → NotFound; ngược lại Update (MarkDirty Ma/Ten/Prompt/MoTa/IsDelete). | EF update | — |
| 4 | Xóa | "Xóa" | POST `api/AIDonViPrompt/Xoa/{id}` | (gợi ý: AIDonViPrompt_Xoa) | public (login) | — | GetById; null → NotFound; nếu **không phải SuperUser** và khác đơn vị → NotFound; ngược lại Delete (soft). | EF delete | SuperUser xóa được bản ghi của đơn vị khác |
| 5 | Tìm theo điều kiện (phân trang) | Lưới danh sách | POST `api/AIDonViPrompt/TimTheoDieuKien` | — | public (login) | Mở màn hình | Lọc cứng `IdDonVi == DonViId`; KeyWord match `Ma`/`Ten`; sort theo `NgayTao` (luôn ASC dù có/không Sorts); skip/take phân trang; trả total. | EF query | Nhánh if/else của Sorts y hệt nhau → tham số sort FE gửi bị bỏ qua |
| 6 | Khởi tạo mẫu | "Khởi tạo mẫu" | POST `api/AIDonViPrompt/KhoiTaoMau` | — | public (login) | Đơn vị chưa có prompt | Nếu đơn vị đã có bản ghi → trả message "Đơn vị đã khởi tạo mẫu" (không tạo). Ngược lại clone toàn bộ bản mẫu `IdDonVi==Guid.Empty`, gán Id/DonVi/audit mới, BulkInsertOrUpdate trong transaction. | EF bulk + transaction | Idempotent theo "đã có bất kỳ bản ghi nào" (không theo từng mã) |

## Quan hệ
- **Gọi sang:** chỉ EF nội bộ (SandBoxAIDataContext).
- **Dữ liệu (entities):** `AIDonViPrompt` (IdDonVi, Ma, Ten, Prompt, MoTa) — kế thừa BaseEntity (Id, audit, IsDelete).
- **Ripple / đồng bộ:** không có cross-service; bộ prompt được các tính năng AI khác trong SandBox đọc dùng.

## Lỗi đã biết / lưu ý kỹ thuật
- `AIDonViPromptController.cs:34-42` `TimTheoIdAsync` — gọi `GetByIdAsync` 2 lần (check rồi trả) → query thừa.
- `AIDonViPromptService.cs:58-65` `TimTheoDieuKienAsync` — nhánh `if (Sorts.Any())` và `else` đều `OrderBy(NgayTao)` → cấu hình sort từ FE vô tác dụng.
- Permission key (`AIDonViPrompt_ThemMoi`…) chỉ nằm trong comment; runtime chỉ yêu cầu login → mọi user đăng nhập đều CRUD được trong phạm vi đơn vị mình.
