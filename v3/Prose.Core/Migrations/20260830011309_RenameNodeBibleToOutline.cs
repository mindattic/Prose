using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Bible → Outline rename (author ruling 2026-08-29, Phase 2+3 of the refactor plan): the
    /// per-book "Node Bible" is not holy scripture that auto-wins conflicts — it is the outline,
    /// one leg of the Outline ⇄ Book ⇄ Entities three-way symbiosis. Hand SQL (not the EF-scaffolded
    /// drop/recreate) so no data is lost: sp_rename for every column/table/index, full
    /// SYSTEM_VERSIONING OFF/ON dance for the two temporal tables involved (Nodes, NodeSpineVersions).
    /// Idempotent — every step is guarded so `--migrate-sql --schema` can run against a DB that
    /// already has some or all of the rename applied (partial-apply-then-retry safe).
    /// </summary>
    public partial class RenameNodeBibleToOutline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- ===== Nodes (temporal): NodeBible/NodeBibleGeneratedAt -> NodeOutline/NodeOutlineGeneratedAt =====
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Nodes') AND name = 'NodeBible')
                BEGIN
                    ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);

                    EXEC sp_rename 'dbo.Nodes.NodeBible', 'NodeOutline', 'COLUMN';
                    EXEC sp_rename 'dbo.Nodes_History.NodeBible', 'NodeOutline', 'COLUMN';
                    EXEC sp_rename 'dbo.Nodes.NodeBibleGeneratedAt', 'NodeOutlineGeneratedAt', 'COLUMN';
                    EXEC sp_rename 'dbo.Nodes_History.NodeBibleGeneratedAt', 'NodeOutlineGeneratedAt', 'COLUMN';

                    ALTER TABLE [dbo].[Nodes]
                        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History],
                                                     DATA_CONSISTENCY_CHECK = OFF));
                END;

                -- ===== NodeSpineVersions (temporal): BibleHash -> OutlineHash =====
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.NodeSpineVersions') AND name = 'BibleHash')
                BEGIN
                    ALTER TABLE [dbo].[NodeSpineVersions] SET (SYSTEM_VERSIONING = OFF);

                    EXEC sp_rename 'dbo.NodeSpineVersions.BibleHash', 'OutlineHash', 'COLUMN';
                    EXEC sp_rename 'dbo.NodeSpineVersions_History.BibleHash', 'OutlineHash', 'COLUMN';

                    ALTER TABLE [dbo].[NodeSpineVersions]
                        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[NodeSpineVersions_History],
                                                     DATA_CONSISTENCY_CHECK = OFF));
                END;

                -- ===== CharacterEmotionalLedgers (NOT temporal): SourceBibleHash -> SourceOutlineHash =====
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CharacterEmotionalLedgers') AND name = 'SourceBibleHash')
                    EXEC sp_rename 'dbo.CharacterEmotionalLedgers.SourceBibleHash', 'SourceOutlineHash', 'COLUMN';

                -- ===== ArchivedBooks (NOT temporal): NodeBible -> NodeOutline =====
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ArchivedBooks') AND name = 'NodeBible')
                    EXEC sp_rename 'dbo.ArchivedBooks.NodeBible', 'NodeOutline', 'COLUMN';

                -- ===== NodeBibleSections table -> NodeOutlineSections (rename in place, no data loss) =====
                IF OBJECT_ID('dbo.NodeBibleSections') IS NOT NULL AND OBJECT_ID('dbo.NodeOutlineSections') IS NULL
                BEGIN
                    EXEC sp_rename 'dbo.NodeBibleSections', 'NodeOutlineSections';
                    EXEC sp_rename 'dbo.NodeOutlineSections.PK_NodeBibleSections', 'PK_NodeOutlineSections';
                    EXEC sp_rename 'dbo.NodeOutlineSections.FK_NodeBibleSections_Nodes_NodeId', 'FK_NodeOutlineSections_Nodes_NodeId';
                    EXEC sp_rename 'dbo.NodeOutlineSections.IX_NodeBibleSections_NodeId', 'IX_NodeOutlineSections_NodeId', 'INDEX';
                    EXEC sp_rename 'dbo.NodeOutlineSections.UX_NodeBibleSections_Node_Type', 'UX_NodeOutlineSections_Node_Type', 'INDEX';
                END;

                -- ===== Data migrations =====
                IF OBJECT_ID('dbo.NodeOutlineSections') IS NOT NULL
                    UPDATE [dbo].[NodeOutlineSections] SET [SectionType] = 'AuthorNotes' WHERE [SectionType] = 'NarrativeLocks';

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContinuityClaims') AND name = 'SourceType')
                    UPDATE [dbo].[ContinuityClaims] SET [SourceType] = 'outline' WHERE [SourceType] = 'bible';

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ReconciliationDecisions') AND name = 'WinningSourceType')
                    UPDATE [dbo].[ReconciliationDecisions] SET [WinningSourceType] = 'outline' WHERE [WinningSourceType] = 'bible';

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ReconciliationDecisions') AND name = 'EditMechanism')
                    UPDATE [dbo].[ReconciliationDecisions] SET [EditMechanism] = 'outline_section' WHERE [EditMechanism] = 'bible_section';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Data migrations (reverse)
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ReconciliationDecisions') AND name = 'EditMechanism')
                    UPDATE [dbo].[ReconciliationDecisions] SET [EditMechanism] = 'bible_section' WHERE [EditMechanism] = 'outline_section';

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ReconciliationDecisions') AND name = 'WinningSourceType')
                    UPDATE [dbo].[ReconciliationDecisions] SET [WinningSourceType] = 'bible' WHERE [WinningSourceType] = 'outline';

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContinuityClaims') AND name = 'SourceType')
                    UPDATE [dbo].[ContinuityClaims] SET [SourceType] = 'bible' WHERE [SourceType] = 'outline';

                IF OBJECT_ID('dbo.NodeOutlineSections') IS NOT NULL
                    UPDATE [dbo].[NodeOutlineSections] SET [SectionType] = 'NarrativeLocks' WHERE [SectionType] = 'AuthorNotes';

                -- NodeOutlineSections table -> NodeBibleSections
                IF OBJECT_ID('dbo.NodeOutlineSections') IS NOT NULL AND OBJECT_ID('dbo.NodeBibleSections') IS NULL
                BEGIN
                    EXEC sp_rename 'dbo.NodeOutlineSections.UX_NodeOutlineSections_Node_Type', 'UX_NodeBibleSections_Node_Type', 'INDEX';
                    EXEC sp_rename 'dbo.NodeOutlineSections.IX_NodeOutlineSections_NodeId', 'IX_NodeBibleSections_NodeId', 'INDEX';
                    EXEC sp_rename 'dbo.NodeOutlineSections.FK_NodeOutlineSections_Nodes_NodeId', 'FK_NodeBibleSections_Nodes_NodeId';
                    EXEC sp_rename 'dbo.NodeOutlineSections.PK_NodeOutlineSections', 'PK_NodeBibleSections';
                    EXEC sp_rename 'dbo.NodeOutlineSections', 'NodeBibleSections';
                END;

                -- ArchivedBooks
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ArchivedBooks') AND name = 'NodeOutline')
                    EXEC sp_rename 'dbo.ArchivedBooks.NodeOutline', 'NodeBible', 'COLUMN';

                -- CharacterEmotionalLedgers
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CharacterEmotionalLedgers') AND name = 'SourceOutlineHash')
                    EXEC sp_rename 'dbo.CharacterEmotionalLedgers.SourceOutlineHash', 'SourceBibleHash', 'COLUMN';

                -- NodeSpineVersions (temporal)
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.NodeSpineVersions') AND name = 'OutlineHash')
                BEGIN
                    ALTER TABLE [dbo].[NodeSpineVersions] SET (SYSTEM_VERSIONING = OFF);
                    EXEC sp_rename 'dbo.NodeSpineVersions_History.OutlineHash', 'BibleHash', 'COLUMN';
                    EXEC sp_rename 'dbo.NodeSpineVersions.OutlineHash', 'BibleHash', 'COLUMN';
                    ALTER TABLE [dbo].[NodeSpineVersions]
                        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[NodeSpineVersions_History],
                                                     DATA_CONSISTENCY_CHECK = OFF));
                END;

                -- Nodes (temporal)
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Nodes') AND name = 'NodeOutline')
                BEGIN
                    ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);
                    EXEC sp_rename 'dbo.Nodes_History.NodeOutlineGeneratedAt', 'NodeBibleGeneratedAt', 'COLUMN';
                    EXEC sp_rename 'dbo.Nodes.NodeOutlineGeneratedAt', 'NodeBibleGeneratedAt', 'COLUMN';
                    EXEC sp_rename 'dbo.Nodes_History.NodeOutline', 'NodeBible', 'COLUMN';
                    EXEC sp_rename 'dbo.Nodes.NodeOutline', 'NodeBible', 'COLUMN';
                    ALTER TABLE [dbo].[Nodes]
                        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History],
                                                     DATA_CONSISTENCY_CHECK = OFF));
                END;
                """);
        }
    }
}
