---
name: TODO Token Budget Dashboard (ROADMAP 4.1)
description: Trạng thái dở dang của Token Budget Dashboard — phần backend/FE đã code, hook CLI đã build, chưa deploy + chưa test thật
type: project
---

# Token Budget Dashboard — TODO tiếp theo

**Trạng thái 2026-04-09**: Code đã viết xong, **chưa deploy**, **chưa test end-to-end**.

## Những gì đã làm

- BE entities + migration: thêm `GitUser` column vào `UsageLogs` + `Sessions` + index
  - Migration files: `20260409000010_AddGitUserTracking.sql`, `20260409000011_AddCliSentinelUser.sql`
  - **Đã chạy migration trên DB `thiennq_agen_system` (server 10.0.161.12)** từ máy local
- BE: `TokenBudgetController` với 2 endpoints
  - `GET /api/token-budget/dashboard?from=&to=` (auth required) — totals + leaderboard by GitUser + 14d trend + top 50 sessions
  - `POST /api/token-budget/cli-report` (no auth) — receive token report từ Claude Code hook
- BE: dashboard service group theo `GitUser` (date range, không phải week/year)
- FE: `/token-budget` route + nav rail icon `$` + component show all users + date filter (Tuần này / Tháng này / custom)
- FE: `auth.service.ts` thêm getGitUser/setGitUser, `auth.interceptor.ts` gửi header `X-Git-User`
- Hook CLI: `scripts/claude-code/usage-reporter.js` + `usage-reporter-worker.js` (fire-and-forget detached worker, không block Claude)
- Deploy script: `scripts/deploy-all.bat` (gọi lại docker_build_and_push-Devonline.bat + npm run deploy-staging + Portainer webhook)

## Việc PHẢI làm tiếp

**Why:** Code đã sẵn nhưng dashboard sẽ rỗng cho đến khi:
1. BE mới được deploy lên `10.0.161.21:50080`
2. Mỗi member config hook trong `~/.claude/settings.json`
3. Hỏi Claude vài câu để generate data

**How to apply:**

### Deploy
- [ ] Chạy `scripts\deploy-all.bat` để build + push BE + FE image lên `hub.pushsale.vn`
- [ ] Verify endpoint sống: `curl http://10.0.161.21:50080/swagger/index.html`
- [ ] Test CLI report endpoint: `curl -X POST http://10.0.161.21:50080/api/token-budget/cli-report -H "Content-Type: application/json" -d '{"gitUser":"test","claudeSessionId":"test-001","inputTokens":100,"outputTokens":50}'` → kỳ vọng `{"logged":true,...}`
- [ ] Mở `http://10.0.161.21:50888/token-budget` xem có thấy "test" trong leaderboard không

### Config hook trên máy
- [ ] Mở `~/.claude/settings.json` (Windows: `%USERPROFILE%\.claude\settings.json`), merge khối `hooks.Stop` (xem `scripts/claude-code/README.md`)
- [ ] Test: hỏi Claude 1 câu → refresh dashboard → phải thấy git user của mình

### Phổ biến cho team
- [ ] Share link `scripts/claude-code/README.md` cho cả team setup hook
- [ ] Verify `git config user.name` của mỗi member khác nhau

## Pitfalls đã gặp

- **Auto-merge `auth.service.ts`**: Khi pull về có thay đổi URL login (`/api/auth/login` → `http://10.0.161.21:50888/api/auth/login`), git tự merge với phần thêm getGitUser của tôi — đã verify OK.
- **FE call `/api` qua port 50888**: Nginx FE chưa có proxy `/api` → BE 50080 trong `frontend/nginx.conf`. Có thể đã setup chỗ khác hoặc cần thêm. **Cần verify** trước khi test login từ web triển khai.
- **Backend lock khi build local**: AgentService.exe đang chạy → `dotnet build` fail. Phải kill process trước hoặc build subproject khác để check compile.
- **Hook không nhận token usage trực tiếp**: Claude Code hook không pass token info, phải parse `transcript_path` JSONL và lấy `usage` của assistant message cuối.
- **Sentinel user FK**: Tất cả CLI logs gắn với userId `00000000-0000-0000-0000-000000000001` (`_cli_sentinel`) để bypass FK constraint. Migration đã insert.

## Files chính cần review nếu có bug

- [TokenBudgetController.cs](backend/AgentService/Controllers/TokenBudgetController.cs) — endpoint logic
- [UsageRepository.cs](backend/AgentInfrastructure/Repositories/UsageRepository.cs) — `GetGitUsersByDateRange`, `GetTotalsByRange`
- [token-budget.component.ts](frontend/src/app/token-budget/token-budget.component.ts) — date filter logic
- [usage-reporter.js](scripts/claude-code/usage-reporter.js) — parse transcript + spawn worker
- [scripts/claude-code/README.md](scripts/claude-code/README.md) — hướng dẫn setup hook
