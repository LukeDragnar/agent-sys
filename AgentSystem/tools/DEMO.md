# Tools Demo — So sánh Ripgrep vs CodeMap vs MCP

Demo thực tế sự khác biệt giữa 3 tools và khi nào dùng cái nào.

---

## 1️⃣ Ripgrep — Fast Local Pattern Search

**Đặc điểm:**
- ✅ Fast (0.2-0.6s)
- ✅ Regex support (PCRE2)
- ✅ Local, không network
- ✅ Hoạt động offline
- ❌ Chỉ search text/pattern, không hiểu code structure

**Demo:**

```bash
# Tìm tất cả class có tên "Service"
rg "class \w+Service" --type cs

# Output:
# Service/OrderService.cs:12:    public class OrderService : IBaseService
# Service/ChiTieuService.cs:5:    public class ChiTieuService : IChiTieuService
# Service/CustomerService.cs:18:   public class CustomerService

# Tìm method gọi TableWithNolock()
rg "\.TableWithNolock\(\)" --type cs -A 2

# Output:
# DataContext/Repository.cs:45:        _repos.TableWithNolock()
# DataContext/Repository.cs:46:            .Where(x => x.IdDonVi == id)
# DataContext/Repository.cs:47:            .ToListAsync();
```

**Use case:**
- ✅ Tìm pattern/regex trong code
- ✅ Tìm file theo name pattern
- ✅ Tìm comment TODO/FIXME
- ✅ Search across FE + BE cùng lúc

---

## 2️⃣ CodeMap — Code Intelligence & Dependencies

**Đặc điểm:**
- ✅ Hiểu code structure (class, method, dependency)
- ✅ Find dependencies + reverse dependencies
- ✅ Find API endpoints
- ✅ Centralized index (team shared)
- ❌ Chậm hơn (depend on backend)
- ❌ Cần backend running
- ❌ Chỉ BE (C#), không FE

**Demo:**

```bash
# Find class definition
bash tools/codemap/codemap.sh symbol OrderService Class

# Output:
# OrderService.cs:12 (file path + line)
# Dependencies: IOrderRepository, IChiTieuService, ILogService
# Used by: OrderController, OrderManagementService, BatchJob

# Find reverse dependencies (ai gọi OrderService?)
bash tools/codemap/codemap.sh deps OrderService

# Output:
# OrderController.cs:25 (inject)
# OrderBatchJob.cs:10 (inject)
# OrderManagementService.cs:5 (inject)

# Find API endpoint
bash tools/codemap/codemap.sh endpoint order POST

# Output:
# POST /api/order
# Handler: OrderController.Save()
# Input: OrderSaveDto
# Return: IMethodResult<Guid>
```

**Use case:**
- ✅ Tìm class/method definition (BE C# only)
- ✅ Tìm dependencies + who uses it
- ✅ Understand API endpoints + flow
- ✅ Refactor safely (see all callers)
- ❌ Pattern search (use Ripgrep)
- ❌ FE code (use Ripgrep)

---

## 3️⃣ MCP Server — AI Integration (TODO)

**Đặc điểm:**
- ✅ Programmatic access to Ripgrep + CodeMap
- ✅ Claude AI can call tools automatically
- ✅ Integration with AI workflows
- ❌ Not yet deployed/tested
- ❌ Need npm setup

**Demo (when ready):**

```javascript
// MCP client example
const mcp = require('./tools/mcp');

// Search via MCP
const results = await mcp.search('OrderService');
// → Array of symbols from CodeMap

// Ripgrep via MCP
const patterns = await mcp.ripgrep('TableWithNolock');
// → Array of matches from Ripgrep
```

**Use case (future):**
- ✅ Claude AI doing code analysis
- ✅ Automated refactoring
- ✅ Code intelligence in custom tools

---

## 📊 Comparison Table

| Feature | Ripgrep | CodeMap | MCP |
|---------|---------|---------|-----|
| **Speed** | ⚡ Fast (0.2-0.6s) | 🐢 Slower (1-3s) | - |
| **Pattern search** | ✅ | ❌ | ✅ |
| **Code structure** | ❌ | ✅ BE only | ✅ |
| **Dependencies** | ❌ | ✅ | ✅ |
| **Offline** | ✅ | ❌ | ❌ |
| **FE (TypeScript)** | ✅ | ❌ | ✅ |
| **BE (C#)** | ✅ | ✅ | ✅ |
| **Status** | ✅ Ready | ⚠️ Backend down | ❌ Not deployed |

---

## 🎯 Decision Tree — Which Tool to Use?

```
Need to search?
├─ Pattern/regex? → RIPGREP ✅
│  (find "class.*Service", "TODO", regex patterns)
│
├─ Class definition + dependencies? → CODEMAP (BE only)
│  (find OrderService, who uses it, API endpoints)
│
├─ Need AI integration? → MCP (future)
│  (Claude AI analyzing code)
│
└─ FE (TypeScript)? → RIPGREP ✅
   (CodeMap doesn't support TS yet)
```

---

## 💡 Real-World Examples

### Example 1: Find all "TODO" comments

```bash
# Ripgrep
rg "TODO|FIXME" --type cs --type ts

# Result: All TODO/FIXME in BE + FE
```

**Tool:** Ripgrep ✅ (pattern search)

---

### Example 2: Understand OrderService architecture

```bash
# CodeMap
bash tools/codemap/codemap.sh symbol OrderService Class
bash tools/codemap/codemap.sh deps OrderService depth:3

# Result: Class definition, methods, all callers, dependencies tree
```

**Tool:** CodeMap ✅ (code structure)

---

### Example 3: Find where MarkDirty is called

```bash
# Ripgrep
rg "\.MarkDirty\(" --type cs -B 3 -A 1

# Result: All MarkDirty calls with context
```

**Tool:** Ripgrep ✅ (pattern + context)

---

### Example 4: Refactor OrderService — check all impact

```bash
# CodeMap
bash tools/codemap/codemap.sh deps OrderService

# Result: See all methods that call OrderService
#         → Safe to refactor knowing all callers
```

**Tool:** CodeMap ✅ (dependency analysis)

---

## 🚀 Next Steps

1. **Ripgrep** — Ready now, use for pattern search
2. **CodeMap** — Waiting for backend, use when online
3. **MCP** — Deploy when needed (AI integration)

---

**Last updated:** 2026-05-05
