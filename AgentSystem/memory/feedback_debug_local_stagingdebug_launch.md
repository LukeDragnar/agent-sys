---
name: feedback_debug_local_stagingdebug_launch
description: Khi user nói "debug local" (SCRM) — BE StagingDebug launchSettings + service URL; FE environment.stagingdebug.ts domainDebug phải khớp domain feature (admin→staging, không để tenant) + chỉ service đã-sửa-code trỏ localhost
metadata:
  type: feedback
---

🔴 **TRIGGER → ĐỌC SKILL NGAY (đừng đào lại):** user nói *"debug local" / "chạy staging debug" / "config stagingdebug"* → **mở `projects/scrm/skills/stagingdebug-config.md` LÀM THEO NGAY** (FE service→localhost + domainDebug tenant; BE launchSettings `Staging`→`StagingDebug`; port map — OrderLogistic=50024). KHÔNG tự đào appsettings/launchSettings/DI-binding từ đầu. *(2026-07: quên skill → mò config từ đầu → user "lâu thế".)*

Khi user bảo **"debug local"** (SCRM): nhiều BE service **CHƯA có profile `StagingDebug` trong `launchSettings.json`**, và `appsettings.StagingDebug.json` có thể trỏ service URL không nhất quán (staging vs localhost, phần lớn dòng localhost bị comment).

**Why:** Lưu 1 cấu hình / thao tác thường **gọi HTTP sang nhiều service khác** (vd `CauHinhService.Save` đồng bộ sang **WarehouseProduct** (`CapNhatSuDungKhoTongWarehouse`) + **Core** (`CapNhatChiNhanhTong`) + **Contact** (`DongBoCauHinh`)). Chỉ chạy OrderLogistic + Invoice local là **chưa đủ** — call sang service không tới được → `ClientHelper.cs` ném `" Hệ thống đang update. Vui lòng thử lại"` (lỗi CÂM, che exception thật). Dễ tưởng nhầm là bug code mình.

**How to apply:**
- Khi user "debug local": **hỏi/xác nhận service nào chạy local**, và nhắc các service mà luồng đang test sẽ **gọi chéo** (không chỉ service chính).
- Gặp lỗi `" Hệ thống đang update"` → **KHÔNG phải lỗi field/code mình** trước tiên → truy `ClientHelper` throw + stack trace → tìm `PostAsync/GetAsync` nào fail → service đó không tới được.
- Sửa: BE `appsettings.StagingDebug.json` bỏ comment `<Service>ServiceUrl: http://localhost:<port>` (nếu chạy local) hoặc để staging (nếu tới được). Là **debug-only, KHÔNG commit** (như `environment.stagingdebug.ts`).
- Service CHƯA có profile StagingDebug trong launchSettings → phải thêm `ASPNETCORE_ENVIRONMENT=StagingDebug` mới F5 đúng env.

**FE (`environment.stagingdebug.ts`, chạy `ng serve` config stagingdebug) — 2 thứ hay quên (bài học 2026-07):**
- **`domainDebug` = domain/tenant FE auth vào.** Feature **admin/hệ thống (quản trị, đơn vị, SB team, gán sale) → phải `https://staging.sandbox.com.vn`** (login tài khoản hệ thống). Để nhầm domain **tenant** (vd `kimcucspa.sandbox.com.vn`) → user không phải SB team → guard chặn, dropdown/list rỗng, màn quản trị không vào được. Ngược lại feature tenant-specific mới để domain tenant.
- **Service URL: CHỈ service ĐÃ SỬA CODE mới bỏ comment về `localhost:PORT`**, còn lại giữ staging (CORE_API→50001, `SERVICE_API_WORK`(WorkFlow)→50071). Để nguyên staging = FE gọi Core/WorkFlow **của staging** → KHÔNG thấy code mình sửa (bug "chạy mãi không thấy thay đổi").
- **Nguyên tắc run BE local: chỉ run service đã sửa code**; service khác dùng của staging (Authen/File/... URL trỏ staging, JWT vẫn hợp lệ vì StagingDebug chung key staging).
- **Chủ động**: user nói "debug feature admin" → TỰ suy ra + set `domainDebug=staging` + trỏ URL service-đã-sửa về localhost, đừng đợi user nhắc ("bạn không suy nghĩ à").
- Chi tiết cấu hình: [[stagingdebug-config]] (skill `projects/scrm/skills/stagingdebug-config.md`).
