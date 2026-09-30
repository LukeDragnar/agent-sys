# Claude Code → Token Budget Hook

Hook tự động báo cáo token usage từ Claude Code CLI lên dashboard chung
(`http://10.0.161.21:50080` → web `http://10.0.161.21:50888/token-budget`).

## Cách hoạt động

1. Sau mỗi lần Claude trả lời xong (`Stop` event), Claude Code chạy hook
2. Hook đọc transcript JSONL của session, lấy `usage` của assistant message cuối
3. POST lên `/api/token-budget/cli-report` cùng với `git config user.name`
4. Backend lưu vào DB → dashboard hiện ngay

## Setup (mỗi member làm 1 lần)

### 1. Đảm bảo `git config user.name` đã set
```bash
git config user.name        # phải có tên (vd: Nguyen Quang Thien)
# nếu chưa có: git config --global user.name "Tên Của Bạn"
```

### 2. Thêm hook vào `~/.claude/settings.json`

Mở file `~/.claude/settings.json` (Windows: `C:\Users\<user>\.claude\settings.json`),
thêm/merge khối `hooks` sau:

```json
{
  "hooks": {
    "Stop": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "node \"D:/PROJECT/Git/scrm_claude/AgentSystem/scripts/claude-code/usage-reporter.js\"",
            "timeout": 10
          }
        ]
      }
    ]
  }
}
```

> Sửa đường dẫn `D:/PROJECT/Git/scrm_claude/AgentSystem` cho khớp với máy bạn.

### 3. Test

Bắt đầu 1 conversation Claude Code mới, hỏi gì đó. Sau khi Claude trả lời:
- Mở `http://10.0.161.21:50888/token-budget`
- Bạn sẽ thấy git user của mình trong leaderboard, session mới trong "Top Conversations"

## Troubleshooting

**Không thấy data lên dashboard:**
1. Kiểm tra backend còn sống: `curl http://10.0.161.21:50080/swagger/index.html`
2. Test hook thủ công:
   ```bash
   echo '{"session_id":"test-123","transcript_path":"<path-to-transcript>","cwd":"."}' \
     | node D:/PROJECT/Git/scrm_claude/AgentSystem/scripts/claude-code/usage-reporter.js
   ```
3. Verify git user: `git config user.name`

**Override gateway (nếu BE chạy local):**
```bash
# trong settings.json hook command
"command": "AGENT_API_URL=http://localhost:5137 node \"...\""
```

## Notes

- Hook chạy với `timeout: 10s` — nếu API down, hook fail silent, không block Claude
- Mỗi `Stop` event report **chỉ usage của assistant turn vừa rồi** (không double-count)
- `claudeSessionId` được hash MD5 thành Guid → cùng session = cùng row trong DB
- Tất cả CLI usage gắn với sentinel user `_cli_sentinel` (không hiện trong admin panel)
