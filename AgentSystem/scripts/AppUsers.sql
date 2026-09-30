-- ============================================================
-- TABLE: Users
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE Users (
    Id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    Username         NVARCHAR(200)    NOT NULL,
    PasswordHash     NVARCHAR(MAX)    NOT NULL,
    Role             NVARCHAR(50)     NOT NULL DEFAULT 'user',
    WeeklyTokenLimit INT              NOT NULL DEFAULT 100000,
    IsActive         BIT              NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);

-- ============================================================
-- SELECT
-- ============================================================
SELECT Id, Username, Role, WeeklyTokenLimit, IsActive, CreatedAt FROM Users ORDER BY Username;
SELECT * FROM Users WHERE Id = '00000000-0000-0000-0000-000000000000';
SELECT * FROM Users WHERE Username = 'admin';

-- ============================================================
-- UPDATE
-- ============================================================
UPDATE Users SET Role = 'admin', WeeklyTokenLimit = 0
WHERE Id = '00000000-0000-0000-0000-000000000000';

UPDATE Users SET IsActive = 0 WHERE Id = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- DELETE
-- ============================================================
DELETE FROM Users WHERE Id = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- Thống kê usage theo tuần
-- ============================================================
SELECT IsoWeek, IsoYear, SUM(InputTokens + OutputTokens) AS TotalTokens
FROM UsageLogs
WHERE UserId = '00000000-0000-0000-0000-000000000000'
GROUP BY IsoYear, IsoWeek
ORDER BY IsoYear DESC, IsoWeek DESC;
