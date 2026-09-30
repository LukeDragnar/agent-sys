---
controller: TroLyAIController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Trợ lý AI (widget trợ lý ảo): sinh đoạn báo cáo kết quả kinh doanh dạng hội thoại (so sánh tháng này/tháng trước về lead, chốt đơn, doanh số, marketing). Không có bộ lọc — chạy theo user đăng nhập."
muc_dich: "Trợ lý ảo AI (POC/demo): sinh text báo cáo kinh doanh theo template từ số liệu 2 SP doanh số sale + lead marketing. Có 2 endpoint debug GetUser/GetUserV2 trả thông tin user. KHÔNG gọi LLM thật."
alias: ["trợ lý ai", "tro ly ai", "trợ lý ảo", "ai report", "báo cáo trợ lý ảo", "chatbot báo cáo"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [SCRM_Report_ReportDoanhSoTongQuan, SCRM_Report_ReportLeadByNhanSuMkt_Search]
depends: [Report/CauHinhService (GetAsync), SB.Core HybridCache (GetUserV2 lấy user từ cache), SqlSugar (GetConnection + GetDataSetAll stored proc; mở connection riêng bằng SCRM_ReportStr), DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# TroLyAIController — Trợ lý AI (POC sinh báo cáo kinh doanh dạng hội thoại)

> Controller view/report THUẦN ĐỌC (3 endpoint public, trong đó 2 là debug). `AIReport` sinh **text báo cáo theo template** (không phải LLM thật): gọi song song 4 lần `GetBaoCaoSale`/`GetBaoCaoMarketing` (tháng này + tháng trước), so sánh số liệu rồi ghép câu chữ tiếng Việt ("Tăng/Giảm so với tháng trước X%"). Nguồn số liệu là 2 SP DB SCRM_Report (mở `SqlSugarClient` riêng bằng connection string `SCRM_ReportStr`). `[TienIchAttribute(..., TI_Report_Chung)]`; tất cả method `[SBAuthorize()]` không key (public, chỉ check đăng nhập). ⚠️ Là bản POC/demo: **hardcode năm 2024, tháng 12** (`thang = 12`, "12/2024"/"11/2024") → không chạy đúng theo thời gian thực.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Sinh báo cáo trợ lý AI | Widget trợ lý ảo | `POST api/TroLyAI/AIReport` | `[SBAuthorize]` (không key) | public | màn trợ lý AI | Chạy 4 query (sale + mkt × tháng này/trước), tổng hợp số liệu, sinh `ModelReportAI.ListData` gồm câu chữ + giá trị + cờ hiển thị biểu đồ (tổng lead, chốt đơn, doanh số...). | **SP** `SCRM_Report_ReportDoanhSoTongQuan` (sale), `SCRM_Report_ReportLeadByNhanSuMkt_Search` (marketing) | **POC**: hardcode tháng 12/2024 (L55, L291-292) → số liệu cố định, không theo thời gian thực. |
| 2 | Lấy thông tin user (debug) | (không UI) | `POST api/TroLyAI/GetUser` | `[SBAuthorize]` (không key) | public | debug | Trả thẳng `CurrentUser` (Ok). | EF/SP: không (trả token user) | endpoint debug, không phải nghiệp vụ. |
| 3 | Lấy user theo Id cache (debug) | (không UI) | `POST api/TroLyAI/GetUserV2` | `[SBAuthorize]` (không key) | public | debug | Đọc UserLogin từ HybridCache theo **GUID hardcode** `8ec8e4f9-...549545`. | Cache (HybridCache) | **debug hardcode**: GUID user cố định (L55 controller) → chỉ phục vụ test, nên gỡ khỏi production. |

## Quan hệ
- **Gọi sang:** Report `CauHinhService` (GetAsync); SB.Core HybridCache (GetUserV2). `AIReport` tự mở `SqlSugarClient` riêng (`SCRM_ReportStr`) chạy SP, không qua `_dbDapper` chung.
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua 2 stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/AIReportService.cs`
  - L55 — `var thang = 12;` hardcode; L291-292/L357 — `tuNgay/denNgay` cố định năm 2024 → **báo cáo không động theo thời gian thực** (bản POC chưa hoàn thiện).
  - L344 — SP `SCRM_Report_ReportDoanhSoTongQuan` (GetBaoCaoSale); L414 — SP `SCRM_Report_ReportLeadByNhanSuMkt_Search` (GetBaoCaoMarketing).
  - L115/L150-151 — text tháng "12/2024"/"11/2024" hardcode trong câu báo cáo.
  - "Trợ lý AI" trên thực tế **không gọi LLM/AI** — chỉ ghép template từ số liệu SP.
- BE `Report/Report.API/Controllers/TroLyAIController.cs`
  - L51-57 — `GetUserV2` đọc cache theo GUID user hardcode `8ec8e4f9-dcaf-4bcd-840c-184243549545` → endpoint debug, rủi ro lộ thông tin, nên gỡ.
  - Tất cả method `[SBAuthorize()]` không key → chỉ kiểm tra đăng nhập, không gắn quyền chi tiết.
