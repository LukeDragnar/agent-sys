CREATE TABLE OrchestratorLogs (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    AgentType   NVARCHAR(50)     NOT NULL,
    Input       NVARCHAR(MAX)    NOT NULL,
    Output      NVARCHAR(MAX)    NOT NULL,
    DurationMs  INT              NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_OrchestratorLogs_AgentType ON OrchestratorLogs (AgentType);
CREATE INDEX IX_OrchestratorLogs_CreatedAt ON OrchestratorLogs (CreatedAt DESC);
