# CodeMap - Source Code Analyzer

CodeMap dùng **Roslyn** (C# compiler API) để quét toàn bộ source code, trích xuất cấu trúc (class, method, property, API endpoint, dependency), lưu vào SQL Server, và expose qua REST API + MCP Server.

## Mục đích

- **Giảm token**: Claude Code query trực tiếp thay vì grep/glob nhiều lần (~90% tiết kiệm)
- **Tăng accuracy**: Roslyn parse AST chính xác, không bị sai do regex
- **Web UI**: Xem source map trên browser, search, dependency graph

## Kiến trúc

```
┌─────────────────────────────────────────────┐
│ AgentSystem                                  │
│                                              │
│  RoslynAnalyzer ──→ SQL Server               │
│    (quét .cs)       (lưu symbols)            │
│                         │                    │
│         ┌───────────────┼───────────────┐    │
│         ▼               ▼               ▼    │
│    REST API        MCP Server       Web UI   │
│  (CodeMapController)  (Phase 2)    (Phase 3) │
└─────────────────────────────────────────────┘
```

## Database Tables

| Table | Mô tả |
|-------|--------|
| `CodeSymbols` | Class, Interface, Enum, Method, Property — mọi symbol trong source |
| `CodeApiEndpoints` | API endpoints trích từ `[HttpGet]`, `[Route]` attributes |
| `CodeDependencies` | Dependency graph: injection, inheritance, implementation |

## API Endpoints

Tất cả endpoint yêu cầu Bearer JWT auth.

### Scan

```
POST /api/codemap/{projectId}/scan
Body: { "rootPath": "D:\\PROJECT\\Git\\scrm" }
Response: ScanStats (totalFiles, classes, methods, endpoints...)
```

### Query

```
GET /api/codemap/{projectId}/symbols?name=OrderService&type=Class
GET /api/codemap/{projectId}/endpoints?route=order&method=POST
GET /api/codemap/{projectId}/dependencies?symbol=OrderService&depth=2
GET /api/codemap/{projectId}/search?q=workflow&limit=20
```

### Stats & Tree (cho Web UI)

```
GET /api/codemap/{projectId}/stats
GET /api/codemap/{projectId}/tree
```

### Clear

```
DELETE /api/codemap/{projectId}
```

## Cách sử dụng

### 1. Tạo project (nếu chưa có)

```
POST /api/projects
{ "name": "SCRM", "description": "Main CRM system" }
→ Response: { "id": "xxx-xxx-xxx" }
```

### 2. Scan source code

```
POST /api/codemap/{projectId}/scan
{ "rootPath": "D:\\PROJECT\\Git\\scrm" }
```

Scan sẽ:
- Tìm tất cả file `.cs` (bỏ qua `bin/`, `obj/`, `.vs/`)
- Parse bằng Roslyn → extract symbols, endpoints, dependencies
- Xóa data cũ của project → insert data mới
- Trả về stats

### 3. Query

**Tìm class:**
```
GET /api/codemap/{projectId}/symbols?name=OrderService
```

Response:
```json
[{
  "name": "OrderService",
  "fullName": "SCRM.Services.Order.OrderService",
  "symbolType": "Class",
  "filePath": "Services/Order/OrderService.cs",
  "lineNumber": 15,
  "baseTypes": "IOrderService",
  "children": [
    { "name": "CreateOrder", "symbolType": "Method", "returnType": "Task<OrderDto>", "parameters": "CreateOrderDto dto" },
    { "name": "GetById", "symbolType": "Method", "returnType": "Task<OrderDto>", "parameters": "int id" }
  ],
  "dependencies": [
    { "toSymbol": "IOrderRepository", "dependencyType": "Injection" },
    { "toSymbol": "IMapper", "dependencyType": "Injection" }
  ]
}]
```

**Tìm endpoint:**
```
GET /api/codemap/{projectId}/endpoints?route=order
```

Response:
```json
[{
  "httpMethod": "POST",
  "routeTemplate": "api/Order",
  "controllerName": "OrderController",
  "actionName": "Create",
  "filePath": "Controllers/OrderController.cs",
  "lineNumber": 35,
  "serviceChain": ["OrderController", "OrderService", "OrderRepository"]
}]
```

**Dependency tree:**
```
GET /api/codemap/{projectId}/dependencies?symbol=OrderService
```

Response:
```json
{
  "symbol": "OrderService",
  "dependsOn": [
    { "toSymbol": "IOrderRepository", "dependencyType": "Injection" },
    { "toSymbol": "IMapper", "dependencyType": "Injection" }
  ],
  "usedBy": [
    { "fromSymbol": "OrderController", "dependencyType": "Injection" },
    { "fromSymbol": "OrderSyncJob", "dependencyType": "Injection" }
  ]
}
```

## RoslynAnalyzer — Chi tiết kỹ thuật

### Những gì được extract

| Loại | Cách detect |
|------|-------------|
| Class, Interface, Struct, Record | `TypeDeclarationSyntax` |
| Enum | `EnumDeclarationSyntax` |
| Method | `MethodDeclarationSyntax` (children of type) |
| Property | `PropertyDeclarationSyntax` (children of type) |
| API Endpoint | `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]` attributes |
| Route | `[Route("api/[controller]")]` attribute trên controller |
| Dependency (Injection) | Constructor parameters |
| Dependency (Inheritance) | Base class trong `BaseList` |
| Dependency (Implementation) | Interface trong `BaseList` (detect bằng prefix `I` + uppercase) |

### File bị bỏ qua

- `**/bin/**`, `**/obj/**`, `**/.vs/**`
- `**/node_modules/**`, `**/dist/**`
- `*.spec.ts`, `*.d.ts`

## TypeScriptAnalyzer — Chi tiết kỹ thuật

Dùng regex parse `.ts` files (không cần Roslyn cho TS).

| Loại | Cách detect |
|------|-------------|
| Component | `@Component` decorator → SymbolType = "Component" |
| Service | `@Injectable` decorator → SymbolType = "Service" |
| Class | `export class ...` |
| Interface | `export interface ...` |
| Enum | `export enum ...` |
| Method | Method declarations trong class body |
| Dependency (Injection) | `constructor(private xxx: YyyService)` |
| Inheritance | `class A extends B` |

## MCP Integration

MCP tools đã được thêm vào `mcp/server.js`. Claude Code tự gọi khi cần.

**MCP Tools:**

| Tool | Mô tả |
|------|--------|
| `find_symbol` | Tìm class/method/interface theo tên |
| `find_endpoint` | Tìm API endpoint theo route |
| `get_dependencies` | Lấy dependency tree |
| `search_code` | Full-text search symbols |
| `scan_codemap` | Trigger scan/re-scan project |

MCP config đã có sẵn trong `.mcp.json` — tools mới tự available khi restart MCP server.

## Web UI

Truy cập tại: `http://localhost:4201/codemap` (sau khi login)

Các tab:

- **Overview**: Stats dashboard — số class, method, endpoint, dependencies, last scan time
- **Search**: Gõ keyword → kết quả realtime, click vào xem chi tiết (members, dependencies)
- **Endpoints**: Bảng tất cả API routes, filter theo route/method, hiển thị service chain
- **Tree**: Duyệt source theo folder/namespace, expand/collapse
- **Dependencies**: Nhập tên class → xem depends on + used by, click navigate

## Troubleshooting

### Scan chậm

Scan lần đầu có thể mất 30-60 giây cho codebase lớn (~1000+ files). Lần sau chỉ cần re-scan khi code thay đổi.

### Lỗi parse

Nếu file `.cs` có syntax error, RoslynAnalyzer sẽ skip file đó và log warning. Các file khác vẫn được scan bình thường.

### Endpoint không đúng route

Roslyn chỉ parse static attributes. Nếu route được build dynamic (string concatenation), sẽ không detect được.
