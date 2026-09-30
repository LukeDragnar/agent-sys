# SCRM Multi-lang Automation

Skill tự động xử lý đa ngôn ngữ (wrapping Vietnamese text) cho SCRM Frontend.

## Quick Start

**Trigger:** User nói một trong các câu sau:
- "bổ sung multi-lang"
- "bổ sung đa ngôn ngữ"
- "làm multi-lang all"
- "wrap file X"
- "update multi-lang file Y"

**Flow:**
1. Claude đọc `projects/scrm/skills/multi-lang/baseline.md` (lần cuối thực hiện)
2. Hỏi user lựa chọn mode (baseline/all/file/update)
3. Spawn `multi-lang-agent` theo mode được chọn
4. Update baseline sau khi done

## 4 Modes

| Mode | Trigger | Scope |
|------|---------|-------|
| **Baseline** | "bổ sung multi-lang" | All commits release_product kể từ lần cuối |
| **All** | "làm multi-lang all" | Toàn bộ FE + BE |
| **New File** | "làm multi-lang file X" | 1 file cụ thể (chưa có multi-lang) |
| **Update File** | "bổ sung multi-lang file X" | 1 file cụ thể (thêm missing translations) |

Xem chi tiết: [modes.md](modes.md)

## Output

1. **Fix summary**: X files, Y keys created
2. **SQL**: INSERT statements cho DB (user tự chạy)
3. **Trace**: Log chi tiết vào `projects/scrm/skills/multi-lang/trace/{date}.md`
4. **Auto-review**: Spawn review-agent để QA

## Rules

- ✅ HTML: `{{ 'key' | tsb : 'Vietnamese' }}`
- ✅ TS: `this.tsb.t('key', 'Vietnamese')`
- ✅ Attributes: `[placeholder]="'key' | tsb : 'text'"`
- ✅ Inject pattern: `private tsb = inject(TranslateSBService);`

Xem chi tiết: [agent.md](agent.md) · **lỗi hay gặp khi review: [review-pitfalls.md](review-pitfalls.md)** (7 pitfalls — key collision, tách dấu câu, prefix, field init order, reference→copy shift, concat whitespace, ký hiệu thuần)

## Memory Structure

```
projects/scrm/skills/multi-lang/
├── baseline.md          ← Lần cuối thực hiện
├── keys-log.md          ← Tất cả keys đã tạo
├── review-pitfalls.md   ← 7 lỗi hay gặp khi review multi-lang
└── trace/
    └── YYYY-MM-DD.md    ← Chi tiết fixes
```

## Integration

- Trigger: CLAUDE.md → reference skill này
- Auto-detect mode dựa vào user input
- Always ask user confirm trước khi execute
