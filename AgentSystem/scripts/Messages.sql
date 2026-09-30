-- ============================================================
-- TABLE: Messages
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
CREATE TABLE Messages (
    Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Role      NVARCHAR(50)     NOT NULL,
    Content   NVARCHAR(MAX)    NOT NULL,
    CreatedAt DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_SessionId')
    CREATE INDEX IX_Messages_SessionId ON Messages (SessionId);

-- ============================================================
-- INSERT
-- ============================================================
INSERT INTO Messages (Id, SessionId, Role, Content, CreatedAt)
VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', N'user', N'Nội dung tin nhắn', SYSUTCDATETIME());

-- ============================================================
-- UPDATE
-- ============================================================
UPDATE Messages SET Content = N'Nội dung đã chỉnh sửa'
WHERE Id = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- DELETE
-- ============================================================
DELETE FROM Messages WHERE Id = '00000000-0000-0000-0000-000000000000';
DELETE FROM Messages WHERE SessionId = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- SELECT
-- ============================================================
SELECT * FROM Messages
WHERE SessionId = '00000000-0000-0000-0000-000000000000'
ORDER BY CreatedAt ASC;
