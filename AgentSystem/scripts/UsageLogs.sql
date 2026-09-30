-- ============================================================
-- TABLE: UsageLogs
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UsageLogs')
CREATE TABLE UsageLogs (
    Id           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId       UNIQUEIDENTIFIER NOT NULL,
    SessionId    UNIQUEIDENTIFIER NOT NULL,
    InputTokens  INT              NOT NULL,
    OutputTokens INT              NOT NULL,
    IsoWeek      INT              NOT NULL,
    IsoYear      INT              NOT NULL,
    CreatedAt    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UsageLogs_UserId_Week')
    CREATE INDEX IX_UsageLogs_UserId_Week ON UsageLogs (UserId, IsoWeek, IsoYear);

-- ============================================================
-- SELECT
-- ============================================================
SELECT * FROM UsageLogs WHERE UserId = '00000000-0000-0000-0000-000000000000'
ORDER BY CreatedAt DESC;

-- Tổng token tuần hiện tại của 1 user
SELECT SUM(InputTokens + OutputTokens) AS TotalTokens
FROM UsageLogs
WHERE UserId = '00000000-0000-0000-0000-000000000000'
  AND IsoWeek = DATEPART(ISO_WEEK, GETUTCDATE())
  AND IsoYear = YEAR(GETUTCDATE());

-- Top 10 users tuần này
DECLARE @w INT = DATEPART(ISO_WEEK, GETUTCDATE());
DECLARE @y INT = YEAR(GETUTCDATE());
SELECT TOP 10 u.Username, SUM(l.InputTokens + l.OutputTokens) AS TotalTokens
FROM UsageLogs l
JOIN Users u ON u.Id = l.UserId
WHERE l.IsoWeek = @w AND l.IsoYear = @y
GROUP BY u.Id, u.Username
ORDER BY TotalTokens DESC;

-- ============================================================
-- DELETE
-- ============================================================
DELETE FROM UsageLogs WHERE CreatedAt < DATEADD(WEEK, -12, GETUTCDATE());
