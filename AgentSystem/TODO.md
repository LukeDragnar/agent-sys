# TODO

## Docs Management — Project Knowledge Base

### Vấn đề
- 20 người dùng, docs thay đổi thường xuyên
- MD files trong git không realtime
- Cần giải pháp collaborative editing

### Kế hoạch 2 giai đoạn

**Giai đoạn 1 (tạm thời — chưa có server)**
- Giữ MD files trong git (`projects/`)
- Mọi người `git pull` khi cần docs mới
- Chấp nhận không realtime

**Giai đoạn 2 (sau khi server deploy)**
- Migrate docs sang DB
- Build MCP server endpoint: `get_docs(project, file)`, `search_docs(query)`
- Web UI để edit docs (không tốn API cost, chỉ dùng để edit)
- Mỗi người config `.mcp.json` trỏ vào server chung → realtime
- Architecture:
  ```
  [Web UI] → edit → [DB via AgentService API]
                          ↕ MCP
  [Claude Code terminal] → get_docs() → realtime
  ```

### Tasks cần làm (Giai đoạn 2)
- [ ] Design DB schema cho docs (projects, files, content, versions)
- [ ] Build API endpoints cho docs CRUD trong AgentService
- [ ] Build MCP server endpoint
- [ ] Build Web UI edit docs
- [ ] Viết hướng dẫn setup `.mcp.json` cho team
