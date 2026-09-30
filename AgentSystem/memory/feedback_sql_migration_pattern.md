---
name: SQL migration pattern team — single CREATE TABLE
description: Migration tạo table mới dùng CREATE TABLE full schema trong 1 statement, không tách skeleton + ALTER ADD COLUMN.
type: feedback
---

Migration tạo table dùng **CREATE TABLE đầy đủ** trong 1 statement với IF NOT EXISTS guard. KHÔNG tách thành skeleton + ALTER ADD COLUMN từng cột.

**Why:** User feedback 2026-05-12: "thôi cho tôi câu create đi" sau khi tôi over-engineer migration thành skeleton (Id+PK) + 28 ALTER ADD COLUMN. Pattern team thực tế đơn giản: 1 CREATE TABLE full.

**How to apply:**

### Template

```sql
USE [DbName]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
Migration: Add {Table} table
Purpose: ...
*/

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{Table}')
BEGIN
    CREATE TABLE [dbo].[{Table}] (
        [Id]                          UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
        -- ... business columns
        [Field1]                      VARCHAR(50)         NULL,
        [Field2]                      INT                 NOT NULL DEFAULT 0,
        -- ...

        -- EntityBase columns (10 cột)
        [IsDeleted]                   BIT                 NOT NULL DEFAULT 0,
        [CreatedById]                 VARCHAR(200)        NULL,
        [CreatedByName]               VARCHAR(200)        NULL,
        [CreatedDate]                 DATETIME            NOT NULL DEFAULT GETDATE(),
        [UpdatedById]                 VARCHAR(200)        NULL,
        [UpdatedByName]               VARCHAR(200)        NULL,
        [UpdatedDate]                 DATETIME            NULL,
        [DeletedById]                 VARCHAR(200)        NULL,
        [DeletedByName]               VARCHAR(200)        NULL,
        [DeletedDate]                 DATETIME            NULL
    );
    PRINT 'Created table {Table}';
END
ELSE
BEGIN
    PRINT 'Table {Table} already exists — skipped';
END
GO
```

### Key rules

1. **USE [DbName] header + SET ANSI_NULLS / QUOTED_IDENTIFIER** — bắt buộc
2. **1 CREATE TABLE statement đầy đủ** — không tách skeleton + ALTER
3. **`IF NOT EXISTS` guard** — chạy lại an toàn (skip nếu đã có)
4. **Wrap names trong `[]`** — `[dbo].[TableName]`, `[ColName]`
5. **EntityBase 10 cột BẮT BUỘC** (mỗi entity inherit EntityBase đều cần):
   - `Id` (UNIQUEIDENTIFIER, PK, default NEWID())
   - `IsDeleted` (BIT, NOT NULL, default 0)
   - `CreatedById`, `CreatedByName` (VARCHAR(200), NULL)
   - `CreatedDate` (DATETIME, NOT NULL, default GETDATE())
   - `UpdatedById`, `UpdatedByName` (VARCHAR(200), NULL)
   - `UpdatedDate` (DATETIME, NULL)
   - `DeletedById`, `DeletedByName` (VARCHAR(200), NULL)
   - `DeletedDate` (DATETIME, NULL)
6. **KHÔNG tạo INDEX** — xem `feedback_no_db_index.md`
7. **Align columns** — căn cột để dễ đọc

### Khi nào ALTER (không CREATE)

Chỉ dùng ALTER khi:
- Table đã tồn tại trong prod và cần thêm cột mới (column-level migration)
- Đổi schema field hiện có (ADD/DROP/MODIFY 1 cột)
- File migration riêng: `migration_alter_{Table}_add_{Field}.sql`

KHÔNG dùng skeleton+ALTER pattern cho table mới — over-engineered.

### Anti-patterns

- ❌ Tách skeleton (Id+PK) ra CREATE rồi mỗi cột 1 ALTER ADD COLUMN — quá rườm rà, làm migration file dài 100+ dòng cho 20 cột
- ❌ Bỏ `IF NOT EXISTS` guard → script crash khi re-run
- ❌ CREATE INDEX trong migration — xem `feedback_no_db_index.md`
- ❌ Quên 1 trong 10 EntityBase columns — EF Core tracking sẽ lỗi

### Khi nào áp dụng

- Mọi migration tạo table mới
- Convention `ecommerce_connect`, SCRM, các project team này
