-- ============================================================
-- TABLE: Sessions
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sessions')
CREATE TABLE Sessions (
    Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    Title     NVARCHAR(500)    NOT NULL,
    AgentType NVARCHAR(100)    NOT NULL,
    UserId    UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
);

-- ============================================================
-- INSERT
-- ============================================================
INSERT INTO Sessions (Id, Title, AgentType, CreatedAt)
VALUES (NEWID(), N'Session tiêu đề ở đây', N'BA', SYSUTCDATETIME());

-- ============================================================
-- UPDATE
-- ============================================================
UPDATE Sessions SET Title = N'Tiêu đề mới'
WHERE Id = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- DELETE
-- ============================================================
DELETE FROM Sessions WHERE Id = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- SELECT
-- ============================================================
SELECT * FROM Sessions ORDER BY CreatedAt DESC;
SELECT * FROM Sessions WHERE Id = '00000000-0000-0000-0000-000000000000';
SELECT * FROM Sessions WHERE UserId = '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAt DESC;
