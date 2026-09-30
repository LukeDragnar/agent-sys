---
name: CodeMap — bắt buộc dùng khi hỏi SCRM
description: Khi cần tìm hiểu code SCRM, BẮT BUỘC dùng CodeMap. MCP tools hoặc curl API. Không được skip.
type: feedback
---

Khi cần tìm hiểu code SCRM (entity, service, controller, enum, dependencies, call graph...), **BẮT BUỘC dùng CodeMap**.

**Why:** CodeMap đã build xong (Roslyn scanner + MCP Server + REST API), trả về structured metadata chính xác hơn và tốn ít token hơn ~10x so với grep/glob raw files. User đã nhắc nhở nhiều lần vì Claude không dùng dù tool có sẵn.

**How to apply:**

1. **Xác định project của task** → check `codemap-projects.json` + `memory/reference_codemap_projectid.md`:
   - **Đã scan** (có trong `codemap-projects.json`) → sang bước 2
   - **Chưa scan** → STOP, hiển thị menu (bước 4)

2. **Query qua `bash codemap.sh`** (CLI only, không curl trực tiếp — xem `feedback_codemap_cli_only.md`):
   - `bash codemap.sh search <keyword>`
   - `bash codemap.sh symbol <name> Class`
   - `bash codemap.sh endpoint <route> <method>`
   - `bash codemap.sh deps <ClassName>`

3. **CodeMap fail** (trả về empty / timeout / server error) → STOP, hiển thị menu (bước 4) kèm lý do cụ thể.

4. **Menu bắt buộc — KHÔNG tự grep, phải user chọn**:
   ```
   CodeMap không dùng được cho task này.
   Lý do: <cụ thể — "project X chưa scan" / "search empty cho keyword Y" / "server remote 10.0.161.21:50888 timeout" / "projectId Z trả 500">

   Chọn hướng xử lý:
   1. Scan/Rescan project   — `bash codemap.sh scan <path>` (nếu chưa scan) hoặc rescan (nếu data stale)
   2. Grep raw source       — ripgrep quét ~N files, ước tính ~M tokens (đắt hơn CodeMap ~10x)
   3. Skip                  — bỏ query này, xử lý task bằng cách khác (hỏi user, đọc file cụ thể user chỉ...)
   ```

5. **KHÔNG BAO GIỜ** tự fallback grep mà không qua menu ở bước 4. User chưa chọn → không grep.

6. **Nếu user chọn grep** → trước khi chạy, báo lại scope cụ thể (file pattern, folder, ước tính số file) rồi mới chạy.
