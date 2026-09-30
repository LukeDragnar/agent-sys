-- Sentinel "CLI" user used as FK target for CLI-reported usage logs
IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = '00000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO Users (Id, Username, PasswordHash, Role, WeeklyTokenLimit, IsActive, CreatedAt)
    VALUES (
        '00000000-0000-0000-0000-000000000001',
        '_cli_sentinel',
        '',
        'system',
        0,
        0,
        SYSUTCDATETIME()
    );
END
GO
