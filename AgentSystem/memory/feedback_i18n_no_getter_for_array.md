---
name: i18n - KHÔNG dùng getter return array literal
description: Property initializer chứa tsb.t() phải khởi tạo trong constructor/ngOnInit, KHÔNG dùng getter (gây Angular CD loop, treo máy)
type: feedback
---

Khi wrap i18n cho TS class, **KHÔNG convert property initializer chứa array thành getter** kể cả khi cần `this.tsb.t()` (chưa available lúc init).

**Why:** 2026-05-06 — wrap i18n trang `marketing-dashboard-20250609` đổi 6 array property thành getter để gọi `this.tsb.t()`. Mỗi getter return `[...]` (array literal mới). Template `*ngFor="let item of lstX"` access getter mỗi change detection cycle → reference khác → IterableDiffer thấy "data changed" → re-render → trigger thêm CD → **vòng lặp infinite/extreme → CPU 100% → treo máy**.

User report: "loading rất lâu, treo máy" sau khi cập nhật i18n.

**How to apply:**

❌ KHÔNG làm:
```ts
get lstTrungSo() {
  return [
    { ten: this.tsb.t('label-trung-so', 'Trùng số'), id: true },
  ];
}
```

✅ Làm 1 trong 3 cách (theo thứ tự ưu tiên):

**Cách 1 — Khởi tạo trong constructor (sau `super()`, sau `inject(TranslateSBService)`):**
```ts
lstTrungSo: any[] = [];

constructor(...) {
  super(...);
  this.lstTrungSo = [
    { ten: this.tsb.t('label-trung-so', 'Trùng số'), id: true },
  ];
}
```

**Cách 2 — Khởi tạo trong `ngOnInit`** (tương tự, đặt trong `ngOnInit()` thay vì constructor).

**Cách 3 — Dùng pure pipe `tsb` trực tiếp trong template** nếu fallback static:
```html
<nz-option [nzLabel]="'label-trung-so' | tsb: 'Trùng số'" [nzValue]="true"></nz-option>
```
(Pure pipe có cache theo input, không re-evaluate mỗi tick.)

**Pattern detect khi review:**
- Grep `^\s*get\s+\w+.*\(\)` trong TS file vừa wrap
- Nếu getter body chứa `return [` hoặc `return {` + `this.tsb.t(` → SAI, fix ngay
- Acceptable: getter return string đơn giản (vd `get title() { return this.tsb.t(...); }`) — string immutable, không gây CD loop

Anti-pattern này áp dụng chung Angular, không chỉ i18n. Nhưng task wrap i18n hay vô tình gây ra do cần lazy access `this.tsb`.
