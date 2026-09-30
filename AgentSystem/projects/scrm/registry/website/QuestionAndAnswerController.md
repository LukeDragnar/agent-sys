---
controller: QuestionAndAnswerController
module: website
service: Website
man_hinh_ui: "Quản trị website — màn Quản lý Q&A (Hỏi & Đáp): list + Thêm/Sửa/Xóa câu hỏi-trả lời hiển thị trên storefront"
muc_dich: "CRUD Q&A (Hỏi & Đáp) của website theo đơn vị: tìm theo điều kiện, thêm/sửa/xóa, có guard cross-tenant theo DonViId."
alias: ["quản lý Q&A website", "quản lý hỏi đáp website", "FAQ admin website", "question and answer admin"]
auth: public               # [SBAuthorize("key", ...)] có permission key, scope DV-AD-TONG
state_machine: []
deprecated: false
entities: [QuestionAndAnswer]
stored_procs: []
depends: [Website (IQuestionAndAnswerService)]
cross_module_in: []
cross_module_out: []
---

# QuestionAndAnswerController — Quản lý Q&A website (admin)

> Controller `Website.API/Controllers/Api/QuestionAndAnswerController.cs`, route `api/QuestionAndAnswer`, base `BaseCoreController`, `[ApiController]`, `partial`. `[TienIch("BannerController", "Quản lý link website", TI_Website)]` (TienIch label copy nhầm "BannerController"/"Quản lý link website"). Permission scope `DV-AD-TONG`. Guard cross-tenant: TimTheoId/Xoa kiểm `IdDonVi == CurrentUser.DonViId`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút Tìm kiếm | POST `api/QuestionAndAnswer/TimTheoDieuKien` | `QuestionAndAnswer_TimTheoDieuKien` | public | màn list | Trả list Q&A theo filter + CurrentUser | EF | scope `DV-AD-TONG` |
| 2 | Thêm mới Q&A | nút Thêm | POST `api/QuestionAndAnswer/ThemMoi` | `QuestionAndAnswer_ThemMoi` | public | popup Thêm | `ThemMoiAsync(body, user)` | EF | - |
| 3 | Tìm theo id | mở popup Sửa | GET `api/QuestionAndAnswer/TimTheoId/{id:Guid}` | `QuestionAndAnswer_TimTheoId` | public | per-row | Trả Q&A **chỉ khi** `IdDonVi == CurrentUser.DonViId`, ngược lại "Không tìm thấy Q&A!" | EF | guard tenant — giữ nguyên |
| 4 | Cập nhật Q&A | nút Sửa | POST `api/QuestionAndAnswer/CapNhatThongTin` | `QuestionAndAnswer_CapNhatThongTin` | public | popup Sửa | `CapNhatThongTin(model, user)` | EF | - |
| 5 | Xóa Q&A | nút Xóa | POST `api/QuestionAndAnswer/Xoa/{id:Guid}` | `QuestionAndAnswer_Xoa` | public | per-row | Guard tenant (`IdDonVi == DonViId`) trước, sai → MESS_NotFound; rồi `DeleteAsync(id)` | EF | - |

## Quan hệ
- **Gọi sang:** `IQuestionAndAnswerService` (TimTheoDieuKienAsync, ThemMoiAsync, GetByIdAsync, CapNhatThongTin, DeleteAsync).
- **Dữ liệu (entities):** `QuestionAndAnswer` (IdDonVi-scoped).
- **Ripple / đồng bộ:** storefront `QAController` đọc Q&A này (`GetAllQAByAnonymosAsync`).

## Lỗi đã biết / lưu ý kỹ thuật
- `[TienIch("BannerController", "Quản lý link website", ...)]` label sai (copy từ Banner) — cosmetic, không ảnh hưởng routing nhưng gây nhầm khi tra TienIch.
- Guard cross-tenant ở TimTheoId (L49) & Xoa (L65) — invariant bảo mật, giữ nguyên.
- Thông báo lỗi "Không tìm thấy Q&A!" cũng dùng nhầm ở `TagController` (copy-paste).
