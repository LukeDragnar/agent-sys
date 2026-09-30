---
name: SP pattern team ecommerce_connect
description: Pattern stored procedure team dùng cho "process get data" jobs. Bao gồm header USE/SET, ALTER, throttle qua UpdatedDate + backoff ResendNo, UPDLOCK READPAST ROWLOCK, OUTPUT inserted.*
type: reference
---

Template SP "process get data" cho job-based picking trong ecommerce_connect (và các .NET project tương tự team này).

**Why:** User shared 2 SP mẫu (`PRC_EcommerceConnect_OrderReturnTempShopee_ProcessSendOrderReturnToCrm_GetData`, `PRC_EcommerceConnect_OrderTempShopee_ProcessSendOrderToCrm_GetData`) sau khi thấy tôi viết SP ngắn bị thiếu pattern team. Pattern này có throttle + retry backoff + race-safe lock.

**Khi dùng:** Mọi SP picking row cho timer-based job xử lý.

## Template

```sql
USE [{DbName}]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
EXEC dbo.[PRC_{Name}]
                    @pResendNoMax = 3,
                    @pNumRecord = 200
*/

CREATE OR ALTER PROCEDURE [dbo].[PRC_{Name}]
    @pResendNoMax INT = 5
    , @pNumRecord INT = 10
AS
BEGIN
    SET NOCOUNT ON
    SET ARITHABORT ON
    DECLARE @Start DATETIME = GETDATE()
    DECLARE @startdate AS DATETIME = GETDATE()
END

BEGIN
    -- Anti-double-pick: row đang được tiến trình khác xử lý trong N phút → skip
    ;WITH tmpDataInProcess AS (
        SELECT DISTINCT {BusinessKey}
        FROM {Table} WITH (NOLOCK)
        WHERE IsDeleted = 0
            AND InProcess = 1
            AND UpdatedDate >= DATEADD(MINUTE, -5, @startdate)
            AND ResendNo < @pResendNoMax
    )
    , tmpData AS (
        SELECT TOP(@pNumRecord) Id
        FROM {Table} WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE IsDeleted = 0
            -- Backoff theo ResendNo: lần retry n càng xa thì delay càng dài
            AND UpdatedDate < DATEADD(MINUTE, -5 * ResendNo, @startdate)
            AND ResendNo < @pResendNoMax
            AND {BusinessKey} NOT IN (SELECT * FROM tmpDataInProcess)
        ORDER BY PartnerTimeStamp
    )

    UPDATE t
    SET
        t.InProcess = 1
        , t.UpdatedByName = '{JobName}'
        , t.UpdatedDate = GETDATE()
    OUTPUT inserted.*
    FROM {Table} t
    JOIN tmpData tmpData ON t.Id = tmpData.Id
END

PRINT DATEDIFF(MILLISECOND,@start,GETDATE())
```

## Key elements

1. **Header**: `USE [DbName]`, `SET ANSI_NULLS ON`, `SET QUOTED_IDENTIFIER ON` — bắt buộc
2. **`CREATE OR ALTER PROCEDURE`** — chạy được cả lần đầu và lần sau (idempotent)
3. **Default param values** — `INT = 5`, `INT = 10` để chạy ad-hoc EXEC không cần truyền
4. **`SET NOCOUNT ON; SET ARITHABORT ON`** — performance + tránh client confusion
5. **2 CTE pattern**:
   - `tmpDataInProcess`: row đang InProcess=1 trong N phút → exclude (tránh 2 tiến trình cùng pick)
   - `tmpData`: row thỏa điều kiện retry — `UpdatedDate < @start - N*ResendNo phút` (backoff)
6. **Lock hint `WITH (UPDLOCK, READPAST, ROWLOCK)`** — atomic pick row, skip rows đã locked
7. **`OUTPUT inserted.*`** — trả về full row sau UPDATE (atomic select + lock + return)
8. **`ORDER BY PartnerTimeStamp`** (hoặc field tương đương) — FIFO theo thứ tự webhook/data đến
9. **`PRINT DATEDIFF`** — log timing cuối SP

## Throttle / backoff

- Throttle window 5 phút cho return webhooks, 10 phút cho order webhooks
- ResendNo backoff: lần 0 → ngay, lần 1 → 5 phút, lần 2 → 10 phút, ...
- Cap retry: `ResendNo < @pResendNoMax` (default 5)

## Khi dedup business key

- Nếu table có business key (PartnerOrderCode, PartnerReturnCode, etc.) → dùng trong tmpDataInProcess CTE
- Nếu table không có (DataReceive — mỗi row độc lập per webhook) → bỏ tmpDataInProcess CTE, chỉ throttle qua UpdatedDate

## Anti-patterns

- ❌ `UPDATE TOP + OUTPUT INTO @tbl` rồi SELECT — KHÔNG dùng pattern này (mặc dù atomic, nhưng không match style team)
- ❌ Không có throttle window → 2 tiến trình cùng pick lại row đang xử lý
- ❌ `CREATE INDEX` trong SP file — xem `feedback_no_db_index.md`
- ❌ Quên `SET ANSI_NULLS ON / SET QUOTED_IDENTIFIER ON`
- ❌ Quên `OUTPUT inserted.*` → caller không lấy được data
