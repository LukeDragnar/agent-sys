-- Add GitUser column to UsageLogs
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('UsageLogs') AND name = 'GitUser')
BEGIN
    ALTER TABLE UsageLogs ADD GitUser NVARCHAR(200) NOT NULL DEFAULT '';
END
GO

-- Add GitUser column to Sessions
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sessions') AND name = 'GitUser')
BEGIN
    ALTER TABLE Sessions ADD GitUser NVARCHAR(200) NOT NULL DEFAULT '';
END
GO

-- Index for dashboard queries grouped by GitUser + week
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsageLogs_GitUser_IsoWeek_IsoYear')
BEGIN
    CREATE NONCLUSTERED INDEX IX_UsageLogs_GitUser_IsoWeek_IsoYear
    ON UsageLogs (GitUser, IsoWeek, IsoYear);
END
GO
