-- Migration: AddCodeMapIndexes
-- Date: 2026-04-08
-- Description: Additional indexes for faster CodeMap queries

-- Covering index: search by name returns full info without table lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ProjectId_Name_Cover')
    CREATE INDEX [IX_CodeSymbols_ProjectId_Name_Cover]
    ON [CodeSymbols]([ProjectId], [Name])
    INCLUDE ([FullName], [SymbolType], [FilePath], [LineNumber], [Namespace], [Accessibility], [ReturnType], [Parameters], [BaseTypes], [ExtraInfo], [ParentId]);

-- ParentId lookup (get children of a class)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ParentId')
    CREATE INDEX [IX_CodeSymbols_ParentId] ON [CodeSymbols]([ParentId])
    INCLUDE ([Name], [SymbolType], [ReturnType], [Parameters], [LineNumber], [ExtraInfo], [Accessibility]);

-- Namespace filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ProjectId_Namespace')
    CREATE INDEX [IX_CodeSymbols_ProjectId_Namespace] ON [CodeSymbols]([ProjectId], [Namespace]);

-- FilePath lookup (find all symbols in a file)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ProjectId_FilePath')
    CREATE INDEX [IX_CodeSymbols_ProjectId_FilePath] ON [CodeSymbols]([ProjectId], [FilePath]);

-- Dependency: reverse lookup (who depends on this symbol?)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeDependencies_ToSymbolId')
    CREATE INDEX [IX_CodeDependencies_ToSymbolId] ON [CodeDependencies]([ToSymbolId])
    INCLUDE ([FromSymbolId], [DependencyType]);

-- Endpoint: covering index for listing
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeApiEndpoints_ProjectId_Cover')
    CREATE INDEX [IX_CodeApiEndpoints_ProjectId_Cover]
    ON [CodeApiEndpoints]([ProjectId])
    INCLUDE ([HttpMethod], [RouteTemplate], [ControllerName], [ActionName], [FilePath], [LineNumber], [SymbolId]);

-- Drop old non-covering indexes that are now redundant
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeSymbols_ProjectId_Name')
    DROP INDEX [IX_CodeSymbols_ProjectId_Name] ON [CodeSymbols];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeApiEndpoints_ProjectId_Route')
    DROP INDEX [IX_CodeApiEndpoints_ProjectId_Route] ON [CodeApiEndpoints];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CodeApiEndpoints_ProjectId_Method')
    DROP INDEX [IX_CodeApiEndpoints_ProjectId_Method] ON [CodeApiEndpoints];

PRINT 'Migration AddCodeMapIndexes completed successfully.';
