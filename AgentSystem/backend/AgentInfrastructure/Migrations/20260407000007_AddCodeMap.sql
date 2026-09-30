-- Migration: AddCodeMap
-- Date: 2026-04-07
-- Description: Add CodeMap tables for Roslyn source code analysis

IF OBJECT_ID('CodeSymbols','U') IS NULL
CREATE TABLE [CodeSymbols] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [Name] nvarchar(500) NOT NULL,
    [FullName] nvarchar(1000) NOT NULL,
    [SymbolType] nvarchar(50) NOT NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [LineNumber] int NOT NULL,
    [Namespace] nvarchar(500) NOT NULL DEFAULT '',
    [ParentId] uniqueidentifier NULL,
    [Accessibility] nvarchar(50) NOT NULL DEFAULT '',
    [ReturnType] nvarchar(500) NULL,
    [Parameters] nvarchar(2000) NULL,
    [BaseTypes] nvarchar(1000) NULL,
    [ScannedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_CodeSymbols_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CodeSymbols_Parent] FOREIGN KEY ([ParentId]) REFERENCES [CodeSymbols]([Id]) ON DELETE NO ACTION
);

IF OBJECT_ID('CodeApiEndpoints','U') IS NULL
CREATE TABLE [CodeApiEndpoints] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [HttpMethod] nvarchar(10) NOT NULL,
    [RouteTemplate] nvarchar(500) NOT NULL,
    [ControllerName] nvarchar(200) NOT NULL,
    [ActionName] nvarchar(200) NOT NULL,
    [SymbolId] uniqueidentifier NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [LineNumber] int NOT NULL,
    [ScannedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_CodeApiEndpoints_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CodeApiEndpoints_Symbol] FOREIGN KEY ([SymbolId]) REFERENCES [CodeSymbols]([Id]) ON DELETE NO ACTION
);

IF OBJECT_ID('CodeDependencies','U') IS NULL
CREATE TABLE [CodeDependencies] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [FromSymbolId] uniqueidentifier NOT NULL,
    [ToSymbolId] uniqueidentifier NULL,
    [ToSymbolName] nvarchar(500) NOT NULL,
    [DependencyType] nvarchar(50) NOT NULL,
    [ScannedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_CodeDependencies_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CodeDependencies_From] FOREIGN KEY ([FromSymbolId]) REFERENCES [CodeSymbols]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CodeDependencies_To] FOREIGN KEY ([ToSymbolId]) REFERENCES [CodeSymbols]([Id]) ON DELETE NO ACTION
);

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ProjectId_Name')
    CREATE INDEX [IX_CodeSymbols_ProjectId_Name] ON [CodeSymbols]([ProjectId], [Name]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ProjectId_SymbolType')
    CREATE INDEX [IX_CodeSymbols_ProjectId_SymbolType] ON [CodeSymbols]([ProjectId], [SymbolType]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_FullName')
    CREATE INDEX [IX_CodeSymbols_FullName] ON [CodeSymbols]([FullName]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeApiEndpoints_ProjectId_Route')
    CREATE INDEX [IX_CodeApiEndpoints_ProjectId_Route] ON [CodeApiEndpoints]([ProjectId], [RouteTemplate]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeApiEndpoints_ProjectId_Method')
    CREATE INDEX [IX_CodeApiEndpoints_ProjectId_Method] ON [CodeApiEndpoints]([ProjectId], [HttpMethod]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeDependencies_ProjectId_From')
    CREATE INDEX [IX_CodeDependencies_ProjectId_From] ON [CodeDependencies]([ProjectId], [FromSymbolId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeDependencies_ProjectId_ToName')
    CREATE INDEX [IX_CodeDependencies_ProjectId_ToName] ON [CodeDependencies]([ProjectId], [ToSymbolName]);

PRINT 'Migration AddCodeMap completed successfully.';
