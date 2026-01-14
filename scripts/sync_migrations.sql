-- Script to sync migration history with existing database
-- Run this if your database tables exist but __EFMigrationsHistory is out of sync

-- First, ensure the migrations history table exists
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Insert all migrations up to (but NOT including) AddChatConversations as already applied
-- Using ON CONFLICT to avoid duplicates
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20250819121312_identity3', '9.0.0'),
    ('20251010180810_AddPreferencesJsonToUser', '9.0.0'),
    ('20251013022123_first', '9.0.0'),
    ('20251013055802_AddInventoryModule', '9.0.0'),
    ('20251013115625_AddSalesModule', '9.0.0'),
    ('20251013120656_AddSaleOrderForeignKeyToStockMovement', '9.0.0'),
    ('20251013121723_StandardizeTableNaming', '9.0.0'),
    ('20251031173129_SyncModelChanges', '9.0.0'),
    ('20251031180038_AddChatbotPersistence', '9.0.0'),
    ('20251031184135_rollback', '9.0.0'),
    ('20251103134934_AddHRManagementModule', '9.0.0'),
    ('20251107224217_AddAuditSystem', '9.0.0'),
    ('20251108001102_FixAuditSystemIdCapture', '9.0.0'),
    ('20251108025347_OptimizeAuditIndexes', '9.0.0'),
    ('20251108025813_OptimizeAuditIndexesComposite', '9.0.0'),
    ('20251108031849_AddAuditReadTracking', '9.0.0'),
    ('20251109234656_AddTimeTrackingModule', '9.0.0'),
    ('20251110025704_AddPayrollTimeTracking', '9.0.0'),
    ('20251110052846_AddFinancialModule', '9.0.0'),
    ('20251110133012_Payroll', '9.0.0'),
    ('20251112125954_AddAssetManagement', '9.0.0'),
    ('20251112164546_AddAssetDocumentsAndTransfers', '9.0.0'),
    ('20251113232410_assets', '9.0.0'),
    ('20251117172535_20251117120000_AddPayrollModule', '9.0.0'),
    ('20251118130514_20251118103000_AddTenancyFoundation', '9.0.0'),
    ('20251118173420_20251118121500_AddTenantDatabaseNameUniqueIndex', '9.0.0'),
    ('20251118181714_AddTenantIdToCustomerAndSupplier', '9.0.0'),
    ('20251119173014_AddOnboardingPersistence', '9.0.0'),
    ('20251119175842_AddUserOnboardingProgress', '9.0.0'),
    ('20251124204325_slq', '9.0.0'),
    ('20251124235058_AddTenancyToIdentity', '9.0.0'),
    ('20251126112516_AddDashboardEntities', '9.0.0'),
    ('20251130041707_update', '9.0.0'),
    ('20251130042122_AddUserDashboardLayoutsTable', '9.0.0'),
    ('20251130042403_AddWidgetRoleConfigurationsTable', '9.0.0'),
    ('20251201063720_update2', '9.0.0'),
    ('20251201213543_AddKanbanEnhancements', '9.0.0'),
    ('20251202124656_kanbanupdate', '9.0.0'),
    ('20251202125444_FixKanbanBoardCreatedAt', '9.0.0'),
    ('20251202125653_AddMissingKanbanTables', '9.0.0'),
    ('20251202175043_RefineAuditSystem_AddTenantAndReferences', '9.0.0'),
    ('20251203120039_AddModulePermissions', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- The new AddChatConversations migration will be applied by 'dotnet ef database update'
