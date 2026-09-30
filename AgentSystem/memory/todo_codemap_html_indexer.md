---
name: TODO — CodeMap HTML/Template Indexer
description: Mở rộng CodeMap để index HTML template (Angular FE) — hiện chỉ index C# qua Roslyn
type: project
---

**Mục tiêu:** Mở rộng CodeMap scanner để index HTML template (Angular `.html`), giúp truy vấn nhanh component usage, directive, binding, class CSS... mà không phải grep toàn bộ FE.

**Why:** Hiện CodeMap chỉ dùng Roslyn → chỉ hiểu C# (class/method/endpoint). Với scrm_frontend (Angular 15 + ng-zorro) phải fallback grep, tốn token và chậm. User xác nhận muốn mở task này (2026-04-09).

**How to apply:** Khi có thời gian build, follow scope dưới. Khi user hỏi tìm element/component/directive trong FE, vẫn fallback grep cho đến khi task này done.

---

## Scope công việc

### 1. HTML Parser
- Chọn lib: **AngleSharp** (.NET, dễ tích hợp với codemap hiện tại) hoặc parse5 (node-side).
- Ưu tiên AngleSharp vì codemap backend đang là .NET.

### 2. Scanner mới (HtmlScanner)
Duyệt `*.html` trong FE, build AST, extract:
- **Tag** + selector component (`<app-xxx>`, `<nz-table>`, `<nz-form-item>`...)
- **Structural directive** (`*ngIf`, `*ngFor`, `*ngSwitch`...)
- **Attribute directive** (`[formGroup]`, `[ngClass]`, `nzType`...)
- **Event binding** (`(click)`, `(nzOnOk)`...)
- **Two-way** (`[(ngModel)]`)
- **Class CSS / id**
- **Text node** (cần quyết định — xem "Câu hỏi cần chốt")

### 3. Schema mở rộng
Thêm bảng/collection `HtmlElement` song song với `Symbol`:
```
HtmlElement {
  Id, ProjectId, FilePath, Line, Column,
  TagName, ComponentSelector?,
  Attributes: [{name, value, kind: prop|event|directive|class|id}],
  TextContent?,
  ParentTag?
}
```

### 4. API + CLI
Endpoint mới:
- `GET /search-html?keyword=...&kind=tag|attr|class|text`
- `GET /find-component-usage?selector=app-xxx`
- `GET /find-directive-usage?name=ngFor`

CLI lệnh mới:
- `bash codemap.sh html <keyword>`
- `bash codemap.sh component <selector>`
- `bash codemap.sh directive <name>`

### 5. Re-scan
Lần đầu cho scrm_frontend (~vài nghìn template). Đo size index để quyết định có cần lọc bớt không.

### 6. Test + tune
- Lọc noise: whitespace text, HTML comment.
- Lọc tag chuẩn HTML quá phổ biến (`div`, `span`) khỏi text search? Hoặc giữ nhưng có flag.

---

## Câu hỏi cần chốt trước khi code

1. **Index text node hay không?**
   - Có: tìm được chữ hiển thị UI ("Đơn hàng", "Lưu", "Hủy"...) — rất hữu ích cho task fix UI/đa ngôn ngữ.
   - Không: index nhỏ hơn nhiều lần, chỉ search được structural.
   - Đề xuất: **có**, nhưng chỉ index text node có độ dài > 1 ký tự (loại whitespace).

2. **Resolve component selector → file `.ts`?**
   - Có: query "tìm component HieuOrderListComponent" → trả về tất cả nơi `<app-hieu-order-list>` xuất hiện.
   - Cần thêm pass scan TypeScript decorator `@Component({ selector: '...' })`.
   - Đề xuất: **có**, làm phase 2 sau khi HTML index xong.

3. **Có scan `.scss/.css` luôn không?**
   - Hữu ích để find class definition.
   - Đề xuất: **không phase 1**, làm sau nếu cần.

4. **Index `.cshtml` (Razor) cho BE không?**
   - SCRM BE có dùng Razor không? → cần check. Nếu không thì bỏ qua.

---

## Phụ thuộc / Blocker
- Cần access source codemap backend (chưa rõ repo nằm đâu — hỏi user khi bắt đầu).
- Cần quyết storage: SQLite hiện tại có chịu nổi thêm bảng HtmlElement không, hay cần tách DB.

## Trạng thái
- 2026-04-09: Mở task, chưa start code. Đang chờ user prioritize.
