-- ============================================================
-- TABLE: Specs
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Specs')
CREATE TABLE Specs (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    SessionId   UNIQUEIDENTIFIER NOT NULL,
    Name        NVARCHAR(500)    NOT NULL,
    JsonSpec    NVARCHAR(MAX)    NOT NULL,
    Description NVARCHAR(1000)   NOT NULL DEFAULT '',
    Version     INT              NOT NULL,
    CreatedAt   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT UQ_Specs_SessionId_Version UNIQUE (SessionId, Version)
);

-- ============================================================
-- INSERT
-- ============================================================
INSERT INTO Specs (Id, SessionId, Name, JsonSpec, Description, Version, CreatedAt)
VALUES (
    NEWID(),
    '00000000-0000-0000-0000-000000000000',
    N'Tên feature',
    N'{"feature_name":"","description":"","fields":[],"actions":[],"business_flow":[],"validations":[]}',
    N'Mô tả ngắn',
    1,
    SYSUTCDATETIME()
);

-- ============================================================
-- UPDATE
-- ============================================================
UPDATE Specs SET Name = N'Tên mới', JsonSpec = N'{"feature_name":"updated"}'
WHERE Id = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- DELETE
-- ============================================================
DELETE FROM Specs WHERE Id = '00000000-0000-0000-0000-000000000000';
DELETE FROM Specs WHERE SessionId = '00000000-0000-0000-0000-000000000000';

-- ============================================================
-- SELECT
-- ============================================================
SELECT TOP 1 * FROM Specs
WHERE SessionId = '00000000-0000-0000-0000-000000000000'
ORDER BY Version DESC;

SELECT * FROM Specs
WHERE SessionId = '00000000-0000-0000-0000-000000000000'
ORDER BY Version DESC;
