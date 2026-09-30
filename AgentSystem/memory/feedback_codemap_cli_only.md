---
name: CodeMap — dùng CLI không gọi API trực tiếp
description: Khi query CodeMap phải dùng codemap.sh (CLI), KHÔNG được gọi curl API trực tiếp
type: feedback
---

Khi query CodeMap, phải dùng `bash codemap.sh <command>`, KHÔNG gọi `curl /api/codemap/...` trực tiếp.

**Why:** User quy định CLI là cách duy nhất để query CodeMap. CLI wrapper xử lý auth, cache, project selection — gọi API trực tiếp bỏ qua các logic đó.

**How to apply:**
1. Khi user hỏi về code SCRM (hoặc project đã scan) → **tự kiểm tra backend đang chạy chưa** (`curl -s http://localhost:5137/swagger/index.html -o /dev/null -w "%{http_code}"`)
2. Nếu chưa chạy → **tự start backend**: từ repo root chạy `nohup dotnet run --project backend/AgentService/AgentService.csproj --no-build > /dev/null 2>&1 &` rồi đợi ready
3. Sau đó dùng `CODEMAP_PROJECT_ID="..." bash codemap.sh search/symbol/deps/endpoint/stats`
4. KHÔNG bao giờ viết `curl /api/codemap/...` trực tiếp
5. KHÔNG hỏi user "backend đã chạy chưa" — tự check và tự xử lý
