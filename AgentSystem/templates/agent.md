# Template — Agent (vai trò AI mới)

> Copy khi thêm 1 vai vào `agents/`. Agent = "ai làm" (vai trò dùng nhiều việc), khác Skill = "làm việc gì" (recipe 1 tác vụ → `skills/`).

```markdown
# <ten>-agent — <mô tả 1 dòng vai trò>

## Trigger
<câu/lệnh user nói để gọi vai này>

## Nhiệm vụ
<vai này làm gì, ranh giới làm/không làm>

## Đầu vào cần đọc
- <file kho phải đọc trước: rules/, qa-code, registry node, pattern liên quan>

## Quy trình
1. ...
2. ...

## Checklist tự verify trước khi báo xong
- [ ] ...

## Đầu ra
<format kết quả trả về>
```

> Quy tắc: agent bulk-fix phải đọc memory/pattern liên quan trước + tự verify trước khi báo (xem CLAUDE.md "Agent Learning").
