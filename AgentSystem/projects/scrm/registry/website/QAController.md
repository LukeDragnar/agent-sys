---
controller: QAController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang Hỏi & Đáp (Q&A), tìm câu hỏi theo keyword; render `~/Views/Themes/{ThemeCode}/Home/QADetail.cshtml`"
muc_dich: "Render trang Q&A storefront: tìm/hiển thị danh sách câu hỏi-trả lời công khai của website theo đơn vị (lọc keyword)."
alias: ["trang hỏi đáp website", "Q&A storefront", "FAQ website", "câu hỏi thường gặp website"]
auth: mixed                # [TokenAuthentication] storefront; service gọi GetAllQAByAnonymosAsync (anonymous theo đơn vị)
state_machine: []
deprecated: false
entities: [QuestionAndAnswer, LinkWebsite]
stored_procs: []
depends: [Website (IQuestionAndAnswerService, ILinkWebsiteService)]
cross_module_in: []
cross_module_out: []
---

# QAController — Trang Hỏi & Đáp storefront

> Controller MVC `Website.API/Controllers/QAController.cs`, base `Controller`, `[TokenAuthentication]`. Storefront resolve theo domain. Lấy Q&A bằng `GetAllQAByAnonymosAsync` (truy vấn theo `IdDonVi` lấy từ cấu hình domain, không cần user nội bộ).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/hiển thị Q&A | trang Q&A + ô tìm | GET/POST `QA/Search` (action `Search`, không có `[Route]` → route mặc định `QA/Search`) | — | mixed | storefront | Resolve domain → `GetAllQAByAnonymosAsync(gridInfo, IdDonVi)`; gán `LstQuestionAndAnswer` (rỗng nếu null), set `ViewBag.KeyWord`, render `Home/QADetail.cshtml` | EF (QuestionAndAnswer) qua service | Không có `[Route]`/`[HttpGet]` rõ → dựa route mặc định MVC `{controller}/{action}` |

## Quan hệ
- **Gọi sang:** `IQuestionAndAnswerService.GetAllQAByAnonymosAsync`, `ILinkWebsiteService.GetDataByDomain`.
- **Dữ liệu (entities):** `QuestionAndAnswer` (lọc theo IdDonVi của website), `LinkWebsite`.
- **Ripple / đồng bộ:** không. Quản trị Q&A (CRUD) ở `QuestionAndAnswerController` (admin API) — node riêng.

## Lỗi đã biết / lưu ý kỹ thuật
- Action `Search` không gắn `[HttpGet]`/`[HttpPost]`/`[Route]` → dùng convention routing. Nếu đổi sang attribute-routing nơi khác phải đồng bộ.
- Storefront read-only; phần thêm/sửa/xóa Q&A nằm ở admin controller riêng (`QuestionAndAnswerController`).
