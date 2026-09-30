-- Migration: AddCodeSymbolExtraInfo
-- Date: 2026-04-08
-- Description: Add ExtraInfo column for enum values, attributes, const values

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CodeSymbols') AND name = 'ExtraInfo')
    ALTER TABLE [CodeSymbols] ADD [ExtraInfo] nvarchar(max) NULL;

PRINT 'Migration AddCodeSymbolExtraInfo completed successfully.';
