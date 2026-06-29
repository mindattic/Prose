using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddStrandSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    BeatTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    IsChapterStart = table.Column<bool>(type: "bit", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StructureRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Act = table.Column<int>(type: "int", nullable: false),
                    SceneType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmotionalTone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaceHint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoiceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NarratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSec = table.Column<double>(type: "float", nullable: true),
                    LastRequestId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Stale = table.Column<bool>(type: "bit", nullable: false),
                    EntityStale = table.Column<bool>(type: "bit", nullable: false),
                    WasCorrected = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmotionalScore = table.Column<double>(type: "float", nullable: true),
                    GapAfterMs = table.Column<int>(type: "int", nullable: true),
                    GapAfterAudioPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Premise = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArcTarget = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Number = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Html = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InWorldDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KnowledgeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutlineJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefinementReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckpointJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterReadModels",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    RefreshedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterReadModels", x => x.CharacterId);
                });

            migrationBuilder.CreateTable(
                name: "ClaimConfirmations",
                columns: table => new
                {
                    ClaimUid = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceChapterId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourcePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ConfirmedAt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimConfirmations", x => new { x.ClaimUid, x.SourceChapterId, x.SourcePath });
                });

            migrationBuilder.CreateTable(
                name: "ClaimContradictions",
                columns: table => new
                {
                    AUid = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BUid = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DetectedAt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimContradictions", x => new { x.AUid, x.BUid });
                });

            migrationBuilder.CreateTable(
                name: "ContinuityClaims",
                columns: table => new
                {
                    ClaimUid = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    EntityKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Predicate = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Object = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourcePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceChapterId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceChapterNumber = table.Column<int>(type: "int", nullable: true),
                    SourceChapterTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Snippet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Voice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstAssertedAt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastConfirmedAt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResolvedAt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedAt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedToField = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupersededBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StoryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContinuityClaims", x => x.ClaimUid);
                });

            migrationBuilder.CreateTable(
                name: "DistributedWorkQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClaimedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributedWorkQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InWorldCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrammarNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityReviewQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClaimedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityReviewQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PersonaId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PersonaName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PersonaBlurb = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Improvements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contradictions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityReviewSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    AvgScore = table.Column<double>(type: "float", nullable: false),
                    ScoreDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummaryMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityReviewSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Seed = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GenerationCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AudioCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CharsNarrated = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptMarkdownPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptPdfPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CombinedAudioPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastPlayedBeatIndex = table.Column<int>(type: "int", nullable: true),
                    LastPlayedSec = table.Column<double>(type: "float", nullable: true),
                    ParentEpisodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChapterId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExtractionRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAt = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedAt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScopeId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NewClaims = table.Column<int>(type: "int", nullable: false),
                    ConfirmedClaims = table.Column<int>(type: "int", nullable: false),
                    ContradictedClaims = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(900)", maxLength: 900, nullable: false),
                    ChapterId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Snippet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedFix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DedupKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FocusGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarkdownFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileRoot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "topic"),
                    Scope = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    Triggers = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    AutoTier = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkdownFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProseEmbeddings",
                columns: table => new
                {
                    ScopeKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    Dimensions = table.Column<int>(type: "int", nullable: false),
                    EmbeddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProseEmbeddings", x => new { x.ScopeKind, x.ScopeId });
                });

            migrationBuilder.CreateTable(
                name: "RepositoryDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoutePath = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeriesItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => new { x.Key, x.UniverseId });
                });

            migrationBuilder.CreateTable(
                name: "Species",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sentient = table.Column<bool>(type: "bit", nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrandAmendments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandAmendments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrandAudioEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandAudioEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StrandCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCanon = table.Column<bool>(type: "bit", nullable: false),
                    CanonAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    ParentStrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousStrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortKey = table.Column<double>(type: "float", nullable: false),
                    CombinedAudioPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ScriptMarkdownPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptPdfPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoiceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    VoiceModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoiceStability = table.Column<double>(type: "float", nullable: true),
                    VoiceSimilarity = table.Column<double>(type: "float", nullable: true),
                    VoiceStyle = table.Column<double>(type: "float", nullable: true),
                    VoiceSeed = table.Column<int>(type: "int", nullable: true),
                    TtsEngine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrandBible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrandBibleGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StrandUserStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrandUserStoriesUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GenerationCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AudioCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CharsNarrated = table.Column<int>(type: "int", nullable: false),
                    NarratedBeatCount = table.Column<int>(type: "int", nullable: false),
                    TotalBeatsToNarrate = table.Column<int>(type: "int", nullable: false),
                    LastPlayedBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastPlayedSec = table.Column<double>(type: "float", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Strands_Strands_ParentStrandId",
                        column: x => x.ParentStrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Strands_Strands_PreviousStrandId",
                        column: x => x.PreviousStrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrandSpineVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandVersion = table.Column<int>(type: "int", nullable: false),
                    BibleHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserStoriesHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AmendmentCount = table.Column<int>(type: "int", nullable: false),
                    PinnedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PinnedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandSpineVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Taxonomies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Domain = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxonomies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Taxonomies_Taxonomies_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Taxonomies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Universe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UniversePrimer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortKey = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceChangeLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Before = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    After = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuleTarget = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceChangeLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeatModeLog",
                columns: table => new
                {
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    DetectionMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatModeLog", x => x.BeatId);
                    table.ForeignKey(
                        name: "FK_BeatModeLog_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookChapterOrder",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookChapterOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookChapterOrder_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookChapterOrder_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChapterBeats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeatGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    SortKey = table.Column<double>(type: "float", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Act = table.Column<int>(type: "int", nullable: false),
                    StructureRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SceneType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmotionalTone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaceHint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InWorldDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AudioPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationSec = table.Column<double>(type: "float", nullable: true),
                    NarratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasCorrected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterBeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterBeats_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ammunitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Caliber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ammunitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ammunitions_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Apparels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Functionality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatItSays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriceRange = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AugCompatible = table.Column<bool>(type: "bit", nullable: false),
                    GeneCompatible = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apparels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apparels_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Archetypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BehavioralSignature = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnderStress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AtRest = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archetypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archetypes_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Automata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    KindOfBeing = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutonomyLevel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Dimensions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PowerSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locomotion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Countermeasures = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Automata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Automata_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BeatEntityMentions",
                columns: table => new
                {
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatEntityMentions", x => new { x.BeatId, x.EntityId });
                    table.ForeignKey(
                        name: "FK_BeatEntityMentions_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeatEntityMentions_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookProtagonists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookProtagonists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookProtagonists_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookProtagonists_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChapterCharacters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterCharacters_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterCharacters_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitlePrefix = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KindOfBeing = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Pronouns = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Birthdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    LifeStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NarrativeFunction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NarrationVoice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Augmentations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyLife = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TerritoryRange = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heritage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeightCm = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<int>(type: "int", nullable: false),
                    Build = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HairColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HairStyle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HairLength = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EyeColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkinTone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Complexion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisibleAugmentations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostureMovement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhysicalClothingStyle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PsychologySecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpeechVocabulary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpeechCadence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpeechSubtext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpeechUnderPressure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpeechIntimacyRegister = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BioBatteryMaxCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BioBatteryRecovery = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerGoods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subcategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlavorProfile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PopularityRank = table.Column<int>(type: "int", nullable: false),
                    Slogan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerGoods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerGoods_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Codename = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContractStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientTier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationPlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Opposition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payout = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CrewSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeLimit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapabilityCombat = table.Column<int>(type: "int", nullable: false),
                    CapabilityStealth = table.Column<int>(type: "int", nullable: false),
                    CapabilityHacking = table.Column<int>(type: "int", nullable: false),
                    CapabilitySocial = table.Column<int>(type: "int", nullable: false),
                    CapabilityMedical = table.Column<int>(type: "int", nullable: false),
                    CapabilityTech = table.Column<int>(type: "int", nullable: false),
                    CapabilityTransport = table.Column<int>(type: "int", nullable: false),
                    CapabilityDemolitions = table.Column<int>(type: "int", nullable: false),
                    CapabilitySurveillance = table.Column<int>(type: "int", nullable: false),
                    CapabilityLinguistics = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Entities_ClientEntityId",
                        column: x => x.ClientEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contracts_Entities_LocationPlaceId",
                        column: x => x.LocationPlaceId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Corponations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Headquarters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    FullLegalName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StockDesignation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valuation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Revenue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Employees = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SovereignTerritory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoundingStory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityForce = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyDetail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelationshipToBig20 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corponations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Corponations_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CyberwareItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstallationRequirements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectionRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Maintenance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StreetPrice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicensedPrice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyberwareItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyberwareItems_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Edges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Sentiment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StoryValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StoryValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvalidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Edges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Edges_Entities_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Edges_Entities_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntertainmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subcategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Distributor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Genre = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Medium = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalImpact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntertainmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntertainmentItems_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityEmbeddings",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    Dimensions = table.Column<int>(type: "int", nullable: false),
                    EmbeddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityEmbeddings", x => x.EntityId);
                    table.ForeignKey(
                        name: "FK_EntityEmbeddings_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValueKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StoryValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StoryValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityProperties_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityStateEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AspectKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Verb = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Delta = table.Column<double>(type: "float", nullable: true),
                    AtStoryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InWorldValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InWorldValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeatGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    Snippet = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityStateEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityStateEvents_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TacticalUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentItems_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Allegiance = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Motto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ideology = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Territory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Leadership = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NarrativeFunction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factions_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlyoverEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Substrate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Territory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhysicalDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BehavioralProfile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThreatLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HumanRemnants = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GlmzMigrationRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlyoverEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlyoverEntities_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Genemods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetSystem = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SourceOrganism = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Procedure = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExpressionTime = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reversibility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SocialPerception = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TierAvailability = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genemods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Genemods_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabSpecimens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginLab = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Substrate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhysicalDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BehavioralProfile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThreatLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContainmentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContaminationRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PacificationProtocol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PitiableQualities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSpecimens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSpecimens_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TierAvailability = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Cost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materials_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Motifs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Motifs_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Outlet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reporter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aftermath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Casualties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RunnerRelevance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                    table.ForeignKey(
                        name: "FK_News_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pharmaceuticals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Subcategory = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MethodOfUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddictionRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StreetPrice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmaceuticals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pharmaceuticals_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Territory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Climate = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Demographics = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Economy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PowerStructure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AtmosphereFeel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeoLat = table.Column<double>(type: "float", nullable: false),
                    GeoLng = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Places_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Psionics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Discipline = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnhancementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mechanism = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abilities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SideEffects = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcquisitionMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectionRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorporateInterest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Psionics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Psionics_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Attribution = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    QuoteText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    InWorld = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.EntityId);
                    table.ForeignKey(
                        name: "FK_Records_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subsidiaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentCorponationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentCorponationAlias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LineOfBusiness = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicFacing = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subsidiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subsidiaries_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subsidiaries_Entities_ParentCorponationId",
                        column: x => x.ParentCorponationId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyntheticLives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    KindOfBeing = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Disposition = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Habitat = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LifeStatus = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservedBehavior = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncounterFrequency = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ConfirmedSightings = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DtiRating = table.Column<double>(type: "float", nullable: false),
                    Paratechnological = table.Column<bool>(type: "bit", nullable: false),
                    KnownAge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrackPattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KnownLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiplomaticSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BehavioralNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DamageHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaceDecoration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyntheticLives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyntheticLives_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subcategory = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SocialImpact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Technologies_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transportations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Propulsion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Speed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Capacity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Range = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TierAvailability = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Cost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Autonomy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Armament = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CommonUsage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transportations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transportations_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyEntries_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Weapons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TacticalUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CulturalContext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidjourneyPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dalle3Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weapons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Weapons_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeBeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    SortKey = table.Column<double>(type: "float", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AudioPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NarratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSec = table.Column<double>(type: "float", nullable: true),
                    WasCorrected = table.Column<bool>(type: "bit", nullable: false),
                    TextHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceBeatGuid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stale = table.Column<bool>(type: "bit", nullable: false),
                    LastRequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeatTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StructureRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Act = table.Column<int>(type: "int", nullable: false),
                    SceneType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmotionalTone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaceHint = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeBeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeBeats_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeCorrections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatIndex = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Applied = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeCorrections_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeSurveys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Pacing = table.Column<int>(type: "int", nullable: true),
                    Voice = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasInbox = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeSurveys_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FocusGroupMembers",
                columns: table => new
                {
                    FocusGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonaId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PersonaName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PersonaBlurb = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusGroupMembers", x => new { x.FocusGroupId, x.PersonaId });
                    table.ForeignKey(
                        name: "FK_FocusGroupMembers_FocusGroups_FocusGroupId",
                        column: x => x.FocusGroupId,
                        principalTable: "FocusGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    StorageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BeatServiceLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Service = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WasApplicable = table.Column<bool>(type: "bit", nullable: false),
                    WasActive = table.Column<bool>(type: "bit", nullable: false),
                    BlockSizeChars = table.Column<int>(type: "int", nullable: false),
                    WrittenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatServiceLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatServiceLog_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BeatServiceLog_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterEmotionalLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Character = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Want = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Need = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Wound = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoiceRegister = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Inferred = table.Column<bool>(type: "bit", nullable: false),
                    SourceBibleHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEmotionalLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterEmotionalLedgers_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmotionalExaminations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffortTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmotionalDepthScore = table.Column<double>(type: "float", nullable: false),
                    Register = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BeatCount = table.Column<int>(type: "int", nullable: false),
                    BlockingCount = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ExaminedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmotionalExaminations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmotionalExaminations_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantPayoffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayoffBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlantDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PayoffDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsTransparent = table.Column<bool>(type: "bit", nullable: false),
                    TransparencyNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortKey = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantPayoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantPayoffs_Beats_PayoffBeatId",
                        column: x => x.PayoffBeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlantPayoffs_Beats_PlantBeatId",
                        column: x => x.PlantBeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlantPayoffs_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrandBeats",
                columns: table => new
                {
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortKey = table.Column<double>(type: "float", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandBeats", x => new { x.StrandId, x.BeatId });
                    table.ForeignKey(
                        name: "FK_StrandBeats_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StrandBeats_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrandPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    BeatCount = table.Column<int>(type: "int", nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandPublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrandPublications_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrandReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonaId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PersonaName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PersonaBlurb = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    FlowScore = table.Column<int>(type: "int", nullable: true),
                    ReviewText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Improvements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contradictions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BeatCount = table.Column<int>(type: "int", nullable: false),
                    FocusGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FocusGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClusterId = table.Column<int>(type: "int", nullable: true),
                    ClusterLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrandReviews_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrandReviewSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    AvgScore = table.Column<double>(type: "float", nullable: false),
                    ScoreDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummaryMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandReviewSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrandReviewSummaries_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrandScoreHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MeanScore = table.Column<double>(type: "float", nullable: false),
                    Sd = table.Column<double>(type: "float", nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    BeatCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandScoreHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrandScoreHistories_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityTags",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityTags", x => new { x.EntityId, x.TagId });
                    table.ForeignKey(
                        name: "FK_EntityTags_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityTaxonomies",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxonomyId = table.Column<int>(type: "int", nullable: false),
                    StoryValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoryValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityTaxonomies", x => new { x.EntityId, x.TaxonomyId, x.StoryValidFrom });
                    table.ForeignKey(
                        name: "FK_EntityTaxonomies_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityTaxonomies_Taxonomies_TaxonomyId",
                        column: x => x.TaxonomyId,
                        principalTable: "Taxonomies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AmmunitionAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmmunitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmmunitionAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmmunitionAliases_Ammunitions_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmmunitionCompatibleWeapons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmmunitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmmunitionCompatibleWeapons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmmunitionCompatibleWeapons_Ammunitions_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmmunitionCompatibleWeapons_Entities_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AmmunitionStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmmunitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmmunitionStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmmunitionStoryHooks_Ammunitions_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmmunitionVariants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmmunitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmmunitionVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmmunitionVariants_Ammunitions_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApparelAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApparelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApparelAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApparelAliases_Apparels_ApparelId",
                        column: x => x.ApparelId,
                        principalTable: "Apparels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApparelMaterials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApparelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApparelMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApparelMaterials_Apparels_ApparelId",
                        column: x => x.ApparelId,
                        principalTable: "Apparels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApparelStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApparelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApparelStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApparelStoryHooks_Apparels_ApparelId",
                        column: x => x.ApparelId,
                        principalTable: "Apparels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApparelWornBy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApparelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CharacterEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApparelWornBy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApparelWornBy_Apparels_ApparelId",
                        column: x => x.ApparelId,
                        principalTable: "Apparels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApparelWornBy_Entities_CharacterEntityId",
                        column: x => x.CharacterEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArchetypeOpposites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    OppositeArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeOpposites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeOpposites_Archetypes_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "Archetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArchetypeOpposites_Entities_OppositeArchetypeId",
                        column: x => x.OppositeArchetypeId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchetypeSimilars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    SimilarArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Threshold = table.Column<double>(type: "float", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeSimilars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeSimilars_Archetypes_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "Archetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArchetypeSimilars_Entities_SimilarArchetypeId",
                        column: x => x.SimilarArchetypeId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchetypeUnless",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeUnless", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeUnless_Archetypes_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "Archetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchetypeWillAlways",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeWillAlways", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeWillAlways_Archetypes_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "Archetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchetypeWillNever",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchetypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeWillNever", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeWillNever_Archetypes_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "Archetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomatonAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomatonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatonAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomatonAliases_Automata_AutomatonId",
                        column: x => x.AutomatonId,
                        principalTable: "Automata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomatonArmament",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomatonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatonArmament", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomatonArmament_Automata_AutomatonId",
                        column: x => x.AutomatonId,
                        principalTable: "Automata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutomatonArmament_Entities_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomatonDeployments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomatonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    DeploymentEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatonDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomatonDeployments_Automata_AutomatonId",
                        column: x => x.AutomatonId,
                        principalTable: "Automata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutomatonDeployments_Entities_DeploymentEntityId",
                        column: x => x.DeploymentEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomatonSensors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomatonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    SensorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatonSensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomatonSensors_Automata_AutomatonId",
                        column: x => x.AutomatonId,
                        principalTable: "Automata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomatonStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomatonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatonStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomatonStoryHooks_Automata_AutomatonId",
                        column: x => x.AutomatonId,
                        principalTable: "Automata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAffiliations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAffiliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAffiliations_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterAffiliations_Entities_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAliases_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAncestryDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SubRegion = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Percent = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAncestryDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAncestryDetails_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterArchetypeScores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchetypeName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterArchetypeScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterArchetypeScores_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBehavioralMaps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBehavioralMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBehavioralMaps_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBehavioralRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBehavioralRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBehavioralRules_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBelongingsExtras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBelongingsExtras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBelongingsExtras_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBelongingsGear",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    GearName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GearEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBelongingsGear", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBelongingsGear_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterBelongingsGear_Entities_GearEntityId",
                        column: x => x.GearEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CharacterBioBatteryThresholds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Threshold = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Consequence = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBioBatteryThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBioBatteryThresholds_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterChangelog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Beat = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InWorldDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterChangelog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterChangelog_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterConditions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SinceChapter = table.Column<int>(type: "int", nullable: true),
                    UntilChapter = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterConditions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterCyberware",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BodyLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstalledDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Replaces = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCyberware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterCyberware_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterGeneticAncestries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Percent = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterGeneticAncestries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterGeneticAncestries_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterHomeTurfs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterHomeTurfs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterHomeTurfs_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterHomeTurfs_Entities_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterKnowledge",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LearnedChapter = table.Column<int>(type: "int", nullable: true),
                    LearnedChapterId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceBeat = table.Column<int>(type: "int", nullable: true),
                    SourceSnippet = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterKnowledge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterKnowledge_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterNeuralAbilities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CostPercent = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverdrawnRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Passive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterNeuralAbilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterNeuralAbilities_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterPhysicalMarks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Mark = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterPhysicalMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterPhysicalMarks_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterPsychologyTraits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Trait = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterPsychologyTraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterPsychologyTraits_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRelationships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmotionalCore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoryTension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SinceChapter = table.Column<int>(type: "int", nullable: true),
                    UntilChapter = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterRelationships_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSpeechPhrases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Phrase = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSpeechPhrases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSpeechPhrases_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStatPhrases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Phrase = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStatPhrases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterStatPhrases_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStatScalars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValueKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValueText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValueNumber = table.Column<double>(type: "float", nullable: true),
                    ValueBool = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStatScalars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterStatScalars_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterStoryHooks_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTerritoryReputations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reputation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTerritoryReputations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTerritoryReputations_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTerritoryZones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTerritoryZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTerritoryZones_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTimeline",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InWorldDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StoryId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Consequences = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusChange = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTimeline", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTimeline_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerGoodAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumerGoodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerGoodAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerGoodAliases_ConsumerGoods_ConsumerGoodId",
                        column: x => x.ConsumerGoodId,
                        principalTable: "ConsumerGoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerGoodStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumerGoodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerGoodStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerGoodStoryHooks_ConsumerGoods_ConsumerGoodId",
                        column: x => x.ConsumerGoodId,
                        principalTable: "ConsumerGoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractBonuses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    BonusType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Amount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractBonuses_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractComplications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractComplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractComplications_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorponationCommonNames",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorponationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorponationCommonNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorponationCommonNames_Corponations_CorponationId",
                        column: x => x.CorponationId,
                        principalTable: "Corponations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CyberwareItemAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CyberwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyberwareItemAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyberwareItemAliases_CyberwareItems_CyberwareId",
                        column: x => x.CyberwareId,
                        principalTable: "CyberwareItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CyberwareItemKnownUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CyberwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyberwareItemKnownUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyberwareItemKnownUsers_CyberwareItems_CyberwareId",
                        column: x => x.CyberwareId,
                        principalTable: "CyberwareItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CyberwareItemKnownUsers_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CyberwareItemSideEffects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CyberwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyberwareItemSideEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyberwareItemSideEffects_CyberwareItems_CyberwareId",
                        column: x => x.CyberwareId,
                        principalTable: "CyberwareItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CyberwareItemStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CyberwareId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyberwareItemStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyberwareItemStoryHooks_CyberwareItems_CyberwareId",
                        column: x => x.CyberwareId,
                        principalTable: "CyberwareItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentHeadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    HeadingText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHeadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentHeadings_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntertainmentAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntertainmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntertainmentAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntertainmentAliases_EntertainmentItems_EntertainmentId",
                        column: x => x.EntertainmentId,
                        principalTable: "EntertainmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntertainmentKnownFans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntertainmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntertainmentKnownFans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntertainmentKnownFans_EntertainmentItems_EntertainmentId",
                        column: x => x.EntertainmentId,
                        principalTable: "EntertainmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntertainmentKnownFans_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntertainmentStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntertainmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntertainmentStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntertainmentStoryHooks_EntertainmentItems_EntertainmentId",
                        column: x => x.EntertainmentId,
                        principalTable: "EntertainmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAliases_EquipmentItems_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "EquipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentBaseTechnologies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentBaseTechnologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentBaseTechnologies_Entities_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentBaseTechnologies_EquipmentItems_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "EquipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentKnownUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentKnownUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentKnownUsers_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentKnownUsers_EquipmentItems_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "EquipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentSpecifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentSpecifications_EquipmentItems_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "EquipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentStoryHooks_EquipmentItems_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "EquipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionAliases_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionGoals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionGoals_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MemberStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionMembers_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactionMembers_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionMethods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionMethods_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionRelationships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetFactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_Entities_TargetFactionId",
                        column: x => x.TargetFactionId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionResources_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionStoryHooks_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlyoverEntityAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlyoverEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlyoverEntityAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlyoverEntityAliases_FlyoverEntities_FlyoverEntityId",
                        column: x => x.FlyoverEntityId,
                        principalTable: "FlyoverEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlyoverEntityKnownLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlyoverEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlyoverEntityKnownLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlyoverEntityKnownLocations_Entities_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlyoverEntityKnownLocations_FlyoverEntities_FlyoverEntityId",
                        column: x => x.FlyoverEntityId,
                        principalTable: "FlyoverEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlyoverEntityStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlyoverEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlyoverEntityStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlyoverEntityStoryHooks_FlyoverEntities_FlyoverEntityId",
                        column: x => x.FlyoverEntityId,
                        principalTable: "FlyoverEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenemodAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GenemodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenemodAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenemodAliases_Genemods_GenemodId",
                        column: x => x.GenemodId,
                        principalTable: "Genemods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenemodSideEffects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GenemodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenemodSideEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenemodSideEffects_Genemods_GenemodId",
                        column: x => x.GenemodId,
                        principalTable: "Genemods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenemodStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GenemodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenemodStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenemodStoryHooks_Genemods_GenemodId",
                        column: x => x.GenemodId,
                        principalTable: "Genemods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabSpecimenAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabSpecimenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSpecimenAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSpecimenAliases_LabSpecimens_LabSpecimenId",
                        column: x => x.LabSpecimenId,
                        principalTable: "LabSpecimens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabSpecimenKnownLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabSpecimenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSpecimenKnownLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSpecimenKnownLocations_Entities_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabSpecimenKnownLocations_LabSpecimens_LabSpecimenId",
                        column: x => x.LabSpecimenId,
                        principalTable: "LabSpecimens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabSpecimenStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabSpecimenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSpecimenStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSpecimenStoryHooks_LabSpecimens_LabSpecimenId",
                        column: x => x.LabSpecimenId,
                        principalTable: "LabSpecimens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialAliases_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialApplications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialApplications_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialDevelopers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialDevelopers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialDevelopers_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialProperties_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialStoryHooks_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MotifAppearances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MotifId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Scene = table.Column<int>(type: "int", nullable: false),
                    Meaning = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotifAppearances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotifAppearances_Motifs_MotifId",
                        column: x => x.MotifId,
                        principalTable: "Motifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsEntitiesInvolved",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    InvolvedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsEntitiesInvolved", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsEntitiesInvolved_Entities_InvolvedEntityId",
                        column: x => x.InvolvedEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsEntitiesInvolved_News_NewsId",
                        column: x => x.NewsId,
                        principalTable: "News",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsLocations_Entities_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsLocations_News_NewsId",
                        column: x => x.NewsId,
                        principalTable: "News",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmaceuticalAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PharmaceuticalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmaceuticalAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmaceuticalAliases_Pharmaceuticals_PharmaceuticalId",
                        column: x => x.PharmaceuticalId,
                        principalTable: "Pharmaceuticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmaceuticalEffects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PharmaceuticalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmaceuticalEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmaceuticalEffects_Pharmaceuticals_PharmaceuticalId",
                        column: x => x.PharmaceuticalId,
                        principalTable: "Pharmaceuticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmaceuticalSideEffects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PharmaceuticalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmaceuticalSideEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmaceuticalSideEffects_Pharmaceuticals_PharmaceuticalId",
                        column: x => x.PharmaceuticalId,
                        principalTable: "Pharmaceuticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmaceuticalStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PharmaceuticalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmaceuticalStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmaceuticalStoryHooks_Pharmaceuticals_PharmaceuticalId",
                        column: x => x.PharmaceuticalId,
                        principalTable: "Pharmaceuticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceAdjacencies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NeighborId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceAdjacencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceAdjacencies_Entities_NeighborId",
                        column: x => x.NeighborId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaceAdjacencies_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceAliases_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceAtmosphereItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Item = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceAtmosphereItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceAtmosphereItems_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceDangers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Danger = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceDangers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceDangers_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceExits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationAlias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExitType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Restricted = table.Column<bool>(type: "bit", nullable: false),
                    DangerLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceExits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceExits_Entities_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaceExits_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceFrequentedBy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceFrequentedBy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceFrequentedBy_Entities_TargetEntityId",
                        column: x => x.TargetEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaceFrequentedBy_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceNotableLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceNotableLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceNotableLocations_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceOpportunities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Opportunity = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceOpportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceOpportunities_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceRelatedEntities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceRelatedEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceRelatedEntities_Entities_RelatedEntityId",
                        column: x => x.RelatedEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaceRelatedEntities_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceStoryHooks_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsionicAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsionicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsionicAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsionicAliases_Psionics_PsionicId",
                        column: x => x.PsionicId,
                        principalTable: "Psionics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsionicKnownPractitioners",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsionicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsionicKnownPractitioners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsionicKnownPractitioners_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PsionicKnownPractitioners_Psionics_PsionicId",
                        column: x => x.PsionicId,
                        principalTable: "Psionics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsionicStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsionicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsionicStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsionicStoryHooks_Psionics_PsionicId",
                        column: x => x.PsionicId,
                        principalTable: "Psionics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubsidiaryProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubsidiaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubsidiaryProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubsidiaryProducts_Entities_ProductEntityId",
                        column: x => x.ProductEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubsidiaryProducts_Subsidiaries_SubsidiaryId",
                        column: x => x.SubsidiaryId,
                        principalTable: "Subsidiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyntheticLifeAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyntheticLifeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyntheticLifeAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyntheticLifeAliases_SyntheticLives_SyntheticLifeId",
                        column: x => x.SyntheticLifeId,
                        principalTable: "SyntheticLives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyntheticLifeKnownAssociations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyntheticLifeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssociateEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyntheticLifeKnownAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyntheticLifeKnownAssociations_Entities_AssociateEntityId",
                        column: x => x.AssociateEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyntheticLifeKnownAssociations_SyntheticLives_SyntheticLifeId",
                        column: x => x.SyntheticLifeId,
                        principalTable: "SyntheticLives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyntheticLifeStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyntheticLifeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyntheticLifeStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyntheticLifeStoryHooks_SyntheticLives_SyntheticLifeId",
                        column: x => x.SyntheticLifeId,
                        principalTable: "SyntheticLives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyAliases_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyBaseTechnologies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    BaseTechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyBaseTechnologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyBaseTechnologies_Entities_BaseTechnologyId",
                        column: x => x.BaseTechnologyId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnologyBaseTechnologies_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyDevelopers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    DeveloperEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyDevelopers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyDevelopers_Entities_DeveloperEntityId",
                        column: x => x.DeveloperEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnologyDevelopers_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyEnabledList",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    EnabledEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyEnabledList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyEnabledList_Entities_EnabledEntityId",
                        column: x => x.EnabledEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnologyEnabledList_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyStoryHooks_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportationAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransportationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportationAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportationAliases_Transportations_TransportationId",
                        column: x => x.TransportationId,
                        principalTable: "Transportations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportationStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransportationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportationStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportationStoryHooks_Transportations_TransportationId",
                        column: x => x.TransportationId,
                        principalTable: "Transportations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponAliases_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponAmmunitionTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    AmmunitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponAmmunitionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponAmmunitionTypes_Entities_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeaponAmmunitionTypes_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponBaseTechnologies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponBaseTechnologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponBaseTechnologies_Entities_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeaponBaseTechnologies_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponKnownUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponKnownUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponKnownUsers_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeaponKnownUsers_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponSpecs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SpecValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponSpecs_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponStoryHooks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeaponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponStoryHooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponStoryHooks_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoverImagePrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Generator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PromptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NegativePrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverImagePrompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverImagePrompts_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CoverImagePrompts_Strands_StrandId",
                        column: x => x.StrandId,
                        principalTable: "Strands",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmotionalBeatScores",
                columns: table => new
                {
                    ExaminationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatNumber = table.Column<int>(type: "int", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmotionalBeatScores", x => new { x.ExaminationId, x.BeatNumber });
                    table.ForeignKey(
                        name: "FK_EmotionalBeatScores_EmotionalExaminations_ExaminationId",
                        column: x => x.ExaminationId,
                        principalTable: "EmotionalExaminations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmotionalDimensionResults",
                columns: table => new
                {
                    ExaminationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dimension = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    StrongestEvidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeakestEvidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeakestBeatNumber = table.Column<int>(type: "int", nullable: true),
                    Fix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CraftLaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBlocking = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmotionalDimensionResults", x => new { x.ExaminationId, x.Dimension });
                    table.ForeignKey(
                        name: "FK_EmotionalDimensionResults_EmotionalExaminations_ExaminationId",
                        column: x => x.ExaminationId,
                        principalTable: "EmotionalExaminations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrandReviewBeatScores",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatNumber = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Gripes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contradictions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrandReviewBeatScores", x => new { x.ReviewId, x.BeatNumber });
                    table.ForeignKey(
                        name: "FK_StrandReviewBeatScores_StrandReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "StrandReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterKnowledgeEntities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnowledgeId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    EntityRef = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterKnowledgeEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterKnowledgeEntities_CharacterKnowledge_KnowledgeId",
                        column: x => x.KnowledgeId,
                        principalTable: "CharacterKnowledge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTimelineBodyChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimelineEventId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    BodyChange = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTimelineBodyChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTimelineBodyChanges_CharacterTimeline_TimelineEventId",
                        column: x => x.TimelineEventId,
                        principalTable: "CharacterTimeline",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionRelationshipTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactionRelationshipRowId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionRelationshipTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionRelationshipTags_FactionRelationships_FactionRelationshipRowId",
                        column: x => x.FactionRelationshipRowId,
                        principalTable: "FactionRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmmunitionAliases_AmmunitionId_Position",
                table: "AmmunitionAliases",
                columns: new[] { "AmmunitionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AmmunitionAliases_Value",
                table: "AmmunitionAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_AmmunitionCompatibleWeapons_AmmunitionId_Position",
                table: "AmmunitionCompatibleWeapons",
                columns: new[] { "AmmunitionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AmmunitionCompatibleWeapons_WeaponId",
                table: "AmmunitionCompatibleWeapons",
                column: "WeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_Ammunitions_Caliber",
                table: "Ammunitions",
                column: "Caliber");

            migrationBuilder.CreateIndex(
                name: "IX_Ammunitions_Manufacturer",
                table: "Ammunitions",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Ammunitions_Name",
                table: "Ammunitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AmmunitionStoryHooks_AmmunitionId_Position",
                table: "AmmunitionStoryHooks",
                columns: new[] { "AmmunitionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AmmunitionVariants_AmmunitionId_Position",
                table: "AmmunitionVariants",
                columns: new[] { "AmmunitionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ApparelAliases_ApparelId_Position",
                table: "ApparelAliases",
                columns: new[] { "ApparelId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ApparelAliases_Value",
                table: "ApparelAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_ApparelMaterials_ApparelId_Position",
                table: "ApparelMaterials",
                columns: new[] { "ApparelId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Apparels_Category",
                table: "Apparels",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Apparels_Manufacturer",
                table: "Apparels",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Apparels_Name",
                table: "Apparels",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApparelStoryHooks_ApparelId_Position",
                table: "ApparelStoryHooks",
                columns: new[] { "ApparelId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ApparelWornBy_Alias",
                table: "ApparelWornBy",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_ApparelWornBy_ApparelId_Position",
                table: "ApparelWornBy",
                columns: new[] { "ApparelId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ApparelWornBy_CharacterEntityId",
                table: "ApparelWornBy",
                column: "CharacterEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeOpposites_ArchetypeId_Position",
                table: "ArchetypeOpposites",
                columns: new[] { "ArchetypeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeOpposites_OppositeArchetypeId",
                table: "ArchetypeOpposites",
                column: "OppositeArchetypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Archetypes_Category",
                table: "Archetypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Archetypes_Family",
                table: "Archetypes",
                column: "Family");

            migrationBuilder.CreateIndex(
                name: "IX_Archetypes_Name",
                table: "Archetypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeSimilars_ArchetypeId_Position",
                table: "ArchetypeSimilars",
                columns: new[] { "ArchetypeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeSimilars_SimilarArchetypeId",
                table: "ArchetypeSimilars",
                column: "SimilarArchetypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeUnless_ArchetypeId_Position",
                table: "ArchetypeUnless",
                columns: new[] { "ArchetypeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeWillAlways_ArchetypeId_Position",
                table: "ArchetypeWillAlways",
                columns: new[] { "ArchetypeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeWillNever_ArchetypeId_Position",
                table: "ArchetypeWillNever",
                columns: new[] { "ArchetypeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_StrandId",
                table: "Assets",
                column: "StrandId",
                filter: "[StrandId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Type",
                table: "Assets",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Automata_Classification",
                table: "Automata",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_Automata_KindOfBeing",
                table: "Automata",
                column: "KindOfBeing");

            migrationBuilder.CreateIndex(
                name: "IX_Automata_Manufacturer",
                table: "Automata",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Automata_Name",
                table: "Automata",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Automata_Operator",
                table: "Automata",
                column: "Operator");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonAliases_AutomatonId_Position",
                table: "AutomatonAliases",
                columns: new[] { "AutomatonId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonAliases_Value",
                table: "AutomatonAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonArmament_Alias",
                table: "AutomatonArmament",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonArmament_AutomatonId_Position",
                table: "AutomatonArmament",
                columns: new[] { "AutomatonId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonArmament_WeaponId",
                table: "AutomatonArmament",
                column: "WeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonDeployments_AutomatonId_Position",
                table: "AutomatonDeployments",
                columns: new[] { "AutomatonId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonDeployments_DeploymentEntityId",
                table: "AutomatonDeployments",
                column: "DeploymentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonSensors_AutomatonId_Position",
                table: "AutomatonSensors",
                columns: new[] { "AutomatonId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomatonStoryHooks_AutomatonId_Position",
                table: "AutomatonStoryHooks",
                columns: new[] { "AutomatonId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_BeatEntityMentions_EntityId",
                table: "BeatEntityMentions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Beats_Number",
                table: "Beats",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beats_Slug",
                table: "Beats",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_BeatServiceLog_BeatId",
                table: "BeatServiceLog",
                column: "BeatId",
                filter: "[BeatId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BeatServiceLog_StrandId",
                table: "BeatServiceLog",
                column: "StrandId");

            migrationBuilder.CreateIndex(
                name: "IX_BookChapterOrder_BookId_Position",
                table: "BookChapterOrder",
                columns: new[] { "BookId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookChapterOrder_ChapterId",
                table: "BookChapterOrder",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_BookProtagonists_BookId_Position",
                table: "BookProtagonists",
                columns: new[] { "BookId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_BookProtagonists_CharacterId",
                table: "BookProtagonists",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_SeriesId",
                table: "Books",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "UX_Books_Universe_Slug",
                table: "Books",
                columns: new[] { "UniverseId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChapterBeats_BeatGuid",
                table: "ChapterBeats",
                column: "BeatGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChapterBeats_ChapterId_Index",
                table: "ChapterBeats",
                columns: new[] { "ChapterId", "Index" });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterBeats_InWorldDate",
                table: "ChapterBeats",
                column: "InWorldDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterCharacters_ChapterId_Position",
                table: "ChapterCharacters",
                columns: new[] { "ChapterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterCharacters_CharacterId",
                table: "ChapterCharacters",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_BookId",
                table: "Chapters",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_BookId_Number",
                table: "Chapters",
                columns: new[] { "BookId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_InWorldDate",
                table: "Chapters",
                column: "InWorldDate");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAffiliations_Alias",
                table: "CharacterAffiliations",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAffiliations_CharacterId_Position",
                table: "CharacterAffiliations",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAffiliations_FactionId",
                table: "CharacterAffiliations",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAliases_CharacterId_Position",
                table: "CharacterAliases",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAliases_Value",
                table: "CharacterAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAncestryDetails_CharacterId_Region_SubRegion_Nationality",
                table: "CharacterAncestryDetails",
                columns: new[] { "CharacterId", "Region", "SubRegion", "Nationality" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAncestryDetails_Nationality",
                table: "CharacterAncestryDetails",
                column: "Nationality");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterArchetypeScores_ArchetypeName",
                table: "CharacterArchetypeScores",
                column: "ArchetypeName");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterArchetypeScores_CharacterId_ArchetypeName",
                table: "CharacterArchetypeScores",
                columns: new[] { "CharacterId", "ArchetypeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBehavioralMaps_CharacterId_Bucket_KeyName",
                table: "CharacterBehavioralMaps",
                columns: new[] { "CharacterId", "Bucket", "KeyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBehavioralRules_CharacterId_Bucket_Position",
                table: "CharacterBehavioralRules",
                columns: new[] { "CharacterId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBelongingsExtras_CharacterId_KeyName",
                table: "CharacterBelongingsExtras",
                columns: new[] { "CharacterId", "KeyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBelongingsGear_CharacterId_Bucket_Position",
                table: "CharacterBelongingsGear",
                columns: new[] { "CharacterId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBelongingsGear_GearEntityId",
                table: "CharacterBelongingsGear",
                column: "GearEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBioBatteryThresholds_CharacterId_Threshold",
                table: "CharacterBioBatteryThresholds",
                columns: new[] { "CharacterId", "Threshold" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterChangelog_CharacterId_InWorldDate",
                table: "CharacterChangelog",
                columns: new[] { "CharacterId", "InWorldDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterChangelog_CharacterId_Position",
                table: "CharacterChangelog",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterChangelog_StoryId",
                table: "CharacterChangelog",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConditions_CharacterId_Kind",
                table: "CharacterConditions",
                columns: new[] { "CharacterId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCyberware_CharacterId",
                table: "CharacterCyberware",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCyberware_Name",
                table: "CharacterCyberware",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEmotionalLedgers_StrandId_Character",
                table: "CharacterEmotionalLedgers",
                columns: new[] { "StrandId", "Character" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterGeneticAncestries_CharacterId_Region",
                table: "CharacterGeneticAncestries",
                columns: new[] { "CharacterId", "Region" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterGeneticAncestries_Region",
                table: "CharacterGeneticAncestries",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHomeTurfs_Alias",
                table: "CharacterHomeTurfs",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHomeTurfs_CharacterId_Position",
                table: "CharacterHomeTurfs",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHomeTurfs_PlaceId",
                table: "CharacterHomeTurfs",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledge_CharacterId_Topic",
                table: "CharacterKnowledge",
                columns: new[] { "CharacterId", "Topic" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledge_LearnedChapter",
                table: "CharacterKnowledge",
                column: "LearnedChapter");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledgeEntities_EntityRef",
                table: "CharacterKnowledgeEntities",
                column: "EntityRef");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledgeEntities_KnowledgeId_Position",
                table: "CharacterKnowledgeEntities",
                columns: new[] { "KnowledgeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterNeuralAbilities_CharacterId_Position",
                table: "CharacterNeuralAbilities",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterNeuralAbilities_Name",
                table: "CharacterNeuralAbilities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterPhysicalMarks_CharacterId_Position",
                table: "CharacterPhysicalMarks",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterPsychologyTraits_CharacterId_Bucket_Position",
                table: "CharacterPsychologyTraits",
                columns: new[] { "CharacterId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterReadModels_UniverseId",
                table: "CharacterReadModels",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterReadModels_Version",
                table: "CharacterReadModels",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_CharacterId_TargetName",
                table: "CharacterRelationships",
                columns: new[] { "CharacterId", "TargetName" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_TargetEntityId",
                table: "CharacterRelationships",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_FirstName",
                table: "Characters",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_KindOfBeing",
                table: "Characters",
                column: "KindOfBeing");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_LastFirst",
                table: "Characters",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_LastName",
                table: "Characters",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Species",
                table: "Characters",
                column: "Species");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSpeechPhrases_CharacterId_Bucket_Position",
                table: "CharacterSpeechPhrases",
                columns: new[] { "CharacterId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStatPhrases_CharacterId_Bucket_Position",
                table: "CharacterStatPhrases",
                columns: new[] { "CharacterId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStatScalars_Bucket_KeyName",
                table: "CharacterStatScalars",
                columns: new[] { "Bucket", "KeyName" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStatScalars_CharacterId_Bucket_KeyName",
                table: "CharacterStatScalars",
                columns: new[] { "CharacterId", "Bucket", "KeyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStoryHooks_CharacterId_Position",
                table: "CharacterStoryHooks",
                columns: new[] { "CharacterId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTerritoryReputations_CharacterId_Zone",
                table: "CharacterTerritoryReputations",
                columns: new[] { "CharacterId", "Zone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTerritoryZones_CharacterId_Bucket_Position",
                table: "CharacterTerritoryZones",
                columns: new[] { "CharacterId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTerritoryZones_Zone",
                table: "CharacterTerritoryZones",
                column: "Zone");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTimeline_CharacterId_InWorldDate",
                table: "CharacterTimeline",
                columns: new[] { "CharacterId", "InWorldDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTimeline_CharacterId_StoryId",
                table: "CharacterTimeline",
                columns: new[] { "CharacterId", "StoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTimelineBodyChanges_TimelineEventId_Position",
                table: "CharacterTimelineBodyChanges",
                columns: new[] { "TimelineEventId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerGoodAliases_ConsumerGoodId_Position",
                table: "ConsumerGoodAliases",
                columns: new[] { "ConsumerGoodId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerGoodAliases_Value",
                table: "ConsumerGoodAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerGoods_Category",
                table: "ConsumerGoods",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerGoods_Manufacturer",
                table: "ConsumerGoods",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerGoods_Name",
                table: "ConsumerGoods",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerGoodStoryHooks_ConsumerGoodId_Position",
                table: "ConsumerGoodStoryHooks",
                columns: new[] { "ConsumerGoodId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityClaims_EntityId_Predicate",
                table: "ContinuityClaims",
                columns: new[] { "EntityId", "Predicate" });

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityClaims_SourceType",
                table: "ContinuityClaims",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityClaims_Status",
                table: "ContinuityClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityClaims_StoryDate",
                table: "ContinuityClaims",
                column: "StoryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractBonuses_ContractId_Position",
                table: "ContractBonuses",
                columns: new[] { "ContractId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractComplications_ContractId_Position",
                table: "ContractComplications",
                columns: new[] { "ContractId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ClientEntityId",
                table: "Contracts",
                column: "ClientEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_Codename",
                table: "Contracts",
                column: "Codename");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractStatus",
                table: "Contracts",
                column: "ContractStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_LocationPlaceId",
                table: "Contracts",
                column: "LocationPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CorponationCommonNames_CorponationId_Position",
                table: "CorponationCommonNames",
                columns: new[] { "CorponationId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CorponationCommonNames_Value",
                table: "CorponationCommonNames",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Corponations_Name",
                table: "Corponations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CoverImagePrompts_AssetId",
                table: "CoverImagePrompts",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverImagePrompts_Generator",
                table: "CoverImagePrompts",
                column: "Generator");

            migrationBuilder.CreateIndex(
                name: "IX_CoverImagePrompts_StrandId",
                table: "CoverImagePrompts",
                column: "StrandId",
                filter: "[StrandId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItemAliases_CyberwareId_Position",
                table: "CyberwareItemAliases",
                columns: new[] { "CyberwareId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItemAliases_Value",
                table: "CyberwareItemAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItemKnownUsers_CharacterId",
                table: "CyberwareItemKnownUsers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItemKnownUsers_CyberwareId_Position",
                table: "CyberwareItemKnownUsers",
                columns: new[] { "CyberwareId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItems_Manufacturer",
                table: "CyberwareItems",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItems_Name",
                table: "CyberwareItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItemSideEffects_CyberwareId_Position",
                table: "CyberwareItemSideEffects",
                columns: new[] { "CyberwareId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CyberwareItemStoryHooks_CyberwareId_Position",
                table: "CyberwareItemStoryHooks",
                columns: new[] { "CyberwareId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedWorkQueue_TargetId",
                table: "DistributedWorkQueue",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedWorkQueue_WorkType_Status_ClaimedAt",
                table: "DistributedWorkQueue",
                columns: new[] { "WorkType", "Status", "ClaimedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHeadings_DocumentId_Position",
                table: "DocumentHeadings",
                columns: new[] { "DocumentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Category",
                table: "Documents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileName",
                table: "Documents",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Title",
                table: "Documents",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_Source_Current",
                table: "Edges",
                columns: new[] { "SourceId", "RelationType" },
                filter: "[StoryValidUntil] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_SourceId_RelationType_StoryValidFrom",
                table: "Edges",
                columns: new[] { "SourceId", "RelationType", "StoryValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Edges_Target_Current",
                table: "Edges",
                columns: new[] { "TargetId", "RelationType" },
                filter: "[StoryValidUntil] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_TargetId_RelationType_StoryValidFrom",
                table: "Edges",
                columns: new[] { "TargetId", "RelationType", "StoryValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Edges_UniverseId",
                table: "Edges",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_EmotionalExaminations_StrandId_ExaminedAt",
                table: "EmotionalExaminations",
                columns: new[] { "StrandId", "ExaminedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentAliases_EntertainmentId_Position",
                table: "EntertainmentAliases",
                columns: new[] { "EntertainmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentAliases_Value",
                table: "EntertainmentAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentItems_Category",
                table: "EntertainmentItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentItems_Genre",
                table: "EntertainmentItems",
                column: "Genre");

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentItems_Name",
                table: "EntertainmentItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentKnownFans_CharacterId",
                table: "EntertainmentKnownFans",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentKnownFans_EntertainmentId_Position",
                table: "EntertainmentKnownFans",
                columns: new[] { "EntertainmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_EntertainmentStoryHooks_EntertainmentId_Position",
                table: "EntertainmentStoryHooks",
                columns: new[] { "EntertainmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Entities_EntityType",
                table: "Entities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_EntityType_IsActive",
                table: "Entities",
                columns: new[] { "EntityType", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_InWorldCreatedDate",
                table: "Entities",
                column: "InWorldCreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_ModifiedAt_Active",
                table: "Entities",
                column: "ModifiedAt",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Slug",
                table: "Entities",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_UniverseId",
                table: "Entities",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "UX_Entities_Universe_Type_Slug",
                table: "Entities",
                columns: new[] { "UniverseId", "EntityType", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityEmbeddings_EmbeddedAt",
                table: "EntityEmbeddings",
                column: "EmbeddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EntityProperties_EntityId_PropertyKey_StoryValidFrom",
                table: "EntityProperties",
                columns: new[] { "EntityId", "PropertyKey", "StoryValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityProperties_PropertyKey",
                table: "EntityProperties",
                column: "PropertyKey");

            migrationBuilder.CreateIndex(
                name: "IX_EntityReviewQueue_EntityId",
                table: "EntityReviewQueue",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityReviewQueue_Status_ClaimedAt",
                table: "EntityReviewQueue",
                columns: new[] { "Status", "ClaimedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityReviews_EntityId_EntityType_ReviewedAt",
                table: "EntityReviews",
                columns: new[] { "EntityId", "EntityType", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityReviews_EntityType_ReviewedAt",
                table: "EntityReviews",
                columns: new[] { "EntityType", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityReviewSummaries_EntityId_EntityType",
                table: "EntityReviewSummaries",
                columns: new[] { "EntityId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_AtStoryTime",
                table: "EntityStateEvents",
                column: "AtStoryTime");

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_BeatGuid",
                table: "EntityStateEvents",
                column: "BeatGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_ChapterId",
                table: "EntityStateEvents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_EntityId_AspectKey_AtStoryTime",
                table: "EntityStateEvents",
                columns: new[] { "EntityId", "AspectKey", "AtStoryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_EntityId_AspectKey_InWorldValidFrom",
                table: "EntityStateEvents",
                columns: new[] { "EntityId", "AspectKey", "InWorldValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_UniverseId",
                table: "EntityStateEvents",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityTags_TagId",
                table: "EntityTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityTaxonomies_TaxonomyId",
                table: "EntityTaxonomies",
                column: "TaxonomyId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeBeats_EpisodeId_Index",
                table: "EpisodeBeats",
                columns: new[] { "EpisodeId", "Index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrections_Applied",
                table: "EpisodeCorrections",
                column: "Applied");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeCorrections_EpisodeId",
                table: "EpisodeCorrections",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_StartedAt",
                table: "Episodes",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_Status",
                table: "Episodes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeSurveys_EpisodeId",
                table: "EpisodeSurveys",
                column: "EpisodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAliases_EquipmentId_Position",
                table: "EquipmentAliases",
                columns: new[] { "EquipmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAliases_Value",
                table: "EquipmentAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentBaseTechnologies_EquipmentId_Position",
                table: "EquipmentBaseTechnologies",
                columns: new[] { "EquipmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentBaseTechnologies_TechnologyId",
                table: "EquipmentBaseTechnologies",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_BrandName",
                table: "EquipmentItems",
                column: "BrandName");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_Category",
                table: "EquipmentItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_Manufacturer",
                table: "EquipmentItems",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_Name",
                table: "EquipmentItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKnownUsers_CharacterId",
                table: "EquipmentKnownUsers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKnownUsers_EquipmentId_Position",
                table: "EquipmentKnownUsers",
                columns: new[] { "EquipmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentSpecifications_EquipmentId_KeyName",
                table: "EquipmentSpecifications",
                columns: new[] { "EquipmentId", "KeyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentStoryHooks_EquipmentId_Position",
                table: "EquipmentStoryHooks",
                columns: new[] { "EquipmentId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionRuns_StartedAt",
                table: "ExtractionRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FactionAliases_FactionId_Position",
                table: "FactionAliases",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionAliases_Value",
                table: "FactionAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_FactionGoals_FactionId_Position",
                table: "FactionGoals",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionMembers_Alias",
                table: "FactionMembers",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_FactionMembers_CharacterId",
                table: "FactionMembers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionMembers_FactionId_Position",
                table: "FactionMembers",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionMethods_FactionId_Position",
                table: "FactionMethods",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_Alias",
                table: "FactionRelationships",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_FactionId_Position",
                table: "FactionRelationships",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_TargetFactionId",
                table: "FactionRelationships",
                column: "TargetFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationshipTags_FactionRelationshipRowId_Position",
                table: "FactionRelationshipTags",
                columns: new[] { "FactionRelationshipRowId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionResources_FactionId_Position",
                table: "FactionResources",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Factions_Allegiance",
                table: "Factions",
                column: "Allegiance");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_Name",
                table: "Factions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FactionStoryHooks_FactionId_Position",
                table: "FactionStoryHooks",
                columns: new[] { "FactionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_ChapterId",
                table: "Findings",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_FilePath",
                table: "Findings",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Status",
                table: "Findings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ_Findings_DedupKey",
                table: "Findings",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntities_Classification",
                table: "FlyoverEntities",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntities_Name",
                table: "FlyoverEntities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntityAliases_FlyoverEntityId_Position",
                table: "FlyoverEntityAliases",
                columns: new[] { "FlyoverEntityId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntityAliases_Value",
                table: "FlyoverEntityAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntityKnownLocations_FlyoverEntityId_Position",
                table: "FlyoverEntityKnownLocations",
                columns: new[] { "FlyoverEntityId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntityKnownLocations_PlaceId",
                table: "FlyoverEntityKnownLocations",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_FlyoverEntityStoryHooks_FlyoverEntityId_Position",
                table: "FlyoverEntityStoryHooks",
                columns: new[] { "FlyoverEntityId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FocusGroups_Name",
                table: "FocusGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GenemodAliases_GenemodId_Position",
                table: "GenemodAliases",
                columns: new[] { "GenemodId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_GenemodAliases_Value",
                table: "GenemodAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Genemods_Category",
                table: "Genemods",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Genemods_Manufacturer",
                table: "Genemods",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Genemods_Name",
                table: "Genemods",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Genemods_TargetSystem",
                table: "Genemods",
                column: "TargetSystem");

            migrationBuilder.CreateIndex(
                name: "IX_GenemodSideEffects_GenemodId_Position",
                table: "GenemodSideEffects",
                columns: new[] { "GenemodId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_GenemodStoryHooks_GenemodId_Position",
                table: "GenemodStoryHooks",
                columns: new[] { "GenemodId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimenAliases_LabSpecimenId_Position",
                table: "LabSpecimenAliases",
                columns: new[] { "LabSpecimenId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimenAliases_Value",
                table: "LabSpecimenAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimenKnownLocations_LabSpecimenId_Position",
                table: "LabSpecimenKnownLocations",
                columns: new[] { "LabSpecimenId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimenKnownLocations_PlaceId",
                table: "LabSpecimenKnownLocations",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimens_Classification",
                table: "LabSpecimens",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimens_Name",
                table: "LabSpecimens",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LabSpecimenStoryHooks_LabSpecimenId_Position",
                table: "LabSpecimenStoryHooks",
                columns: new[] { "LabSpecimenId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MarkdownFiles_Category",
                table: "MarkdownFiles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MarkdownFiles_FileRoot_RelativePath",
                table: "MarkdownFiles",
                columns: new[] { "FileRoot", "RelativePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkdownFiles_LastSyncedAt",
                table: "MarkdownFiles",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarkdownFiles_Tier",
                table: "MarkdownFiles",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAliases_MaterialId_Position",
                table: "MaterialAliases",
                columns: new[] { "MaterialId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAliases_Value",
                table: "MaterialAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialApplications_MaterialId_Position",
                table: "MaterialApplications",
                columns: new[] { "MaterialId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDevelopers_MaterialId_Position",
                table: "MaterialDevelopers",
                columns: new[] { "MaterialId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialProperties_MaterialId_Position",
                table: "MaterialProperties",
                columns: new[] { "MaterialId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Category",
                table: "Materials",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Name",
                table: "Materials",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStoryHooks_MaterialId_Position",
                table: "MaterialStoryHooks",
                columns: new[] { "MaterialId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MotifAppearances_MotifId_Position",
                table: "MotifAppearances",
                columns: new[] { "MotifId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Motifs_Name",
                table: "Motifs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_News_Category",
                table: "News",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_News_Name",
                table: "News",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_News_Outlet",
                table: "News",
                column: "Outlet");

            migrationBuilder.CreateIndex(
                name: "IX_News_PublishedDate",
                table: "News",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_NewsEntitiesInvolved_InvolvedEntityId",
                table: "NewsEntitiesInvolved",
                column: "InvolvedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsEntitiesInvolved_NewsId_Position",
                table: "NewsEntitiesInvolved",
                columns: new[] { "NewsId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsLocations_NewsId_Position",
                table: "NewsLocations",
                columns: new[] { "NewsId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsLocations_PlaceId",
                table: "NewsLocations",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaceuticalAliases_PharmaceuticalId_Position",
                table: "PharmaceuticalAliases",
                columns: new[] { "PharmaceuticalId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmaceuticalAliases_Value",
                table: "PharmaceuticalAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaceuticalEffects_PharmaceuticalId_Position",
                table: "PharmaceuticalEffects",
                columns: new[] { "PharmaceuticalId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Pharmaceuticals_Category",
                table: "Pharmaceuticals",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmaceuticals_Manufacturer",
                table: "Pharmaceuticals",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmaceuticals_Name",
                table: "Pharmaceuticals",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaceuticalSideEffects_PharmaceuticalId_Position",
                table: "PharmaceuticalSideEffects",
                columns: new[] { "PharmaceuticalId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmaceuticalStoryHooks_PharmaceuticalId_Position",
                table: "PharmaceuticalStoryHooks",
                columns: new[] { "PharmaceuticalId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceAdjacencies_Alias",
                table: "PlaceAdjacencies",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceAdjacencies_NeighborId",
                table: "PlaceAdjacencies",
                column: "NeighborId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceAdjacencies_PlaceId_Position",
                table: "PlaceAdjacencies",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceAliases_PlaceId_Position",
                table: "PlaceAliases",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceAliases_Value",
                table: "PlaceAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceAtmosphereItems_PlaceId_Bucket_Position",
                table: "PlaceAtmosphereItems",
                columns: new[] { "PlaceId", "Bucket", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceDangers_PlaceId_Position",
                table: "PlaceDangers",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceExits_DestinationId",
                table: "PlaceExits",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceExits_Direction",
                table: "PlaceExits",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceExits_PlaceId_Position",
                table: "PlaceExits",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceFrequentedBy_Alias",
                table: "PlaceFrequentedBy",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceFrequentedBy_PlaceId_Position",
                table: "PlaceFrequentedBy",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceFrequentedBy_TargetEntityId",
                table: "PlaceFrequentedBy",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceNotableLocations_LocationName",
                table: "PlaceNotableLocations",
                column: "LocationName");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceNotableLocations_PlaceId_Position",
                table: "PlaceNotableLocations",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceOpportunities_PlaceId_Position",
                table: "PlaceOpportunities",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceRelatedEntities_PlaceId_Position",
                table: "PlaceRelatedEntities",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceRelatedEntities_RelatedEntityId",
                table: "PlaceRelatedEntities",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Climate",
                table: "Places",
                column: "Climate");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Name",
                table: "Places",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceStoryHooks_PlaceId_Position",
                table: "PlaceStoryHooks",
                columns: new[] { "PlaceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantPayoffs_PayoffBeatId",
                table: "PlantPayoffs",
                column: "PayoffBeatId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantPayoffs_PlantBeatId",
                table: "PlantPayoffs",
                column: "PlantBeatId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantPayoffs_StrandId",
                table: "PlantPayoffs",
                column: "StrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ProseEmbeddings_EmbeddedAt",
                table: "ProseEmbeddings",
                column: "EmbeddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProseEmbeddings_ScopeKind_EmbeddedAt",
                table: "ProseEmbeddings",
                columns: new[] { "ScopeKind", "EmbeddedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PsionicAliases_PsionicId_Position",
                table: "PsionicAliases",
                columns: new[] { "PsionicId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PsionicAliases_Value",
                table: "PsionicAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_PsionicKnownPractitioners_CharacterId",
                table: "PsionicKnownPractitioners",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PsionicKnownPractitioners_PsionicId_Position",
                table: "PsionicKnownPractitioners",
                columns: new[] { "PsionicId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Psionics_Discipline",
                table: "Psionics",
                column: "Discipline");

            migrationBuilder.CreateIndex(
                name: "IX_Psionics_Name",
                table: "Psionics",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PsionicStoryHooks_PsionicId_Position",
                table: "PsionicStoryHooks",
                columns: new[] { "PsionicId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Attribution",
                table: "Quotes",
                column: "Attribution");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Category",
                table: "Quotes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Theme",
                table: "Quotes",
                column: "Theme");

            migrationBuilder.CreateIndex(
                name: "IX_Records_UpdatedAt",
                table: "Records",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryDefinitions_Slug",
                table: "RepositoryDefinitions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeriesItems_Name",
                table: "SeriesItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesItems_Slug",
                table: "SeriesItems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Species_Universe_Name",
                table: "Species",
                columns: new[] { "UniverseId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrandAmendments_StrandId",
                table: "StrandAmendments",
                column: "StrandId");

            migrationBuilder.CreateIndex(
                name: "IX_StrandAmendments_StrandId_SequenceNo",
                table: "StrandAmendments",
                columns: new[] { "StrandId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrandAudioEvents_PublicationId",
                table: "StrandAudioEvents",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_StrandAudioEvents_StrandId_At",
                table: "StrandAudioEvents",
                columns: new[] { "StrandId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_StrandBeats_BeatId",
                table: "StrandBeats",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_StrandBeats_StrandId_SortKey",
                table: "StrandBeats",
                columns: new[] { "StrandId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_StrandPublications_StrandId_StartedAt",
                table: "StrandPublications",
                columns: new[] { "StrandId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StrandReviews_StrandId_ReviewedAt",
                table: "StrandReviews",
                columns: new[] { "StrandId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StrandReviewSummaries_StrandId",
                table: "StrandReviewSummaries",
                column: "StrandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Strands_IsDraft",
                table: "Strands",
                column: "IsDraft");

            migrationBuilder.CreateIndex(
                name: "IX_Strands_Kind",
                table: "Strands",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Strands_ParentStrandId_SortKey",
                table: "Strands",
                columns: new[] { "ParentStrandId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Strands_PreviousStrandId",
                table: "Strands",
                column: "PreviousStrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Strands_UniverseId",
                table: "Strands",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "UX_Strands_Universe_Slug",
                table: "Strands",
                columns: new[] { "UniverseId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrandScoreHistories_StrandId_RecordedAt",
                table: "StrandScoreHistories",
                columns: new[] { "StrandId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StrandSpineVersions_StrandId",
                table: "StrandSpineVersions",
                column: "StrandId");

            migrationBuilder.CreateIndex(
                name: "IX_StrandSpineVersions_StrandId_StrandVersion",
                table: "StrandSpineVersions",
                columns: new[] { "StrandId", "StrandVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subsidiaries_Name",
                table: "Subsidiaries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Subsidiaries_ParentCorponationAlias",
                table: "Subsidiaries",
                column: "ParentCorponationAlias");

            migrationBuilder.CreateIndex(
                name: "IX_Subsidiaries_ParentCorponationId",
                table: "Subsidiaries",
                column: "ParentCorponationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubsidiaryProducts_Alias",
                table: "SubsidiaryProducts",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_SubsidiaryProducts_ProductEntityId",
                table: "SubsidiaryProducts",
                column: "ProductEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SubsidiaryProducts_SubsidiaryId_Position",
                table: "SubsidiaryProducts",
                columns: new[] { "SubsidiaryId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLifeAliases_SyntheticLifeId_Position",
                table: "SyntheticLifeAliases",
                columns: new[] { "SyntheticLifeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLifeAliases_Value",
                table: "SyntheticLifeAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLifeKnownAssociations_Alias",
                table: "SyntheticLifeKnownAssociations",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLifeKnownAssociations_AssociateEntityId",
                table: "SyntheticLifeKnownAssociations",
                column: "AssociateEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLifeKnownAssociations_SyntheticLifeId_Position",
                table: "SyntheticLifeKnownAssociations",
                columns: new[] { "SyntheticLifeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLifeStoryHooks_SyntheticLifeId_Position",
                table: "SyntheticLifeStoryHooks",
                columns: new[] { "SyntheticLifeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLives_Classification",
                table: "SyntheticLives",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLives_Disposition",
                table: "SyntheticLives",
                column: "Disposition");

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticLives_Name",
                table: "SyntheticLives",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxonomies_Domain_Code",
                table: "Taxonomies",
                columns: new[] { "Domain", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxonomies_ParentId",
                table: "Taxonomies",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_BrandName",
                table: "Technologies",
                column: "BrandName");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Category",
                table: "Technologies",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Name",
                table: "Technologies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAliases_TechnologyId_Position",
                table: "TechnologyAliases",
                columns: new[] { "TechnologyId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAliases_Value",
                table: "TechnologyAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyBaseTechnologies_BaseTechnologyId",
                table: "TechnologyBaseTechnologies",
                column: "BaseTechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyBaseTechnologies_TechnologyId_Position",
                table: "TechnologyBaseTechnologies",
                columns: new[] { "TechnologyId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyDevelopers_DeveloperEntityId",
                table: "TechnologyDevelopers",
                column: "DeveloperEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyDevelopers_TechnologyId_Position",
                table: "TechnologyDevelopers",
                columns: new[] { "TechnologyId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyEnabledList_EnabledEntityId",
                table: "TechnologyEnabledList",
                column: "EnabledEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyEnabledList_TechnologyId_Position",
                table: "TechnologyEnabledList",
                columns: new[] { "TechnologyId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyStoryHooks_TechnologyId_Position",
                table: "TechnologyStoryHooks",
                columns: new[] { "TechnologyId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TransportationAliases_TransportationId_Position",
                table: "TransportationAliases",
                columns: new[] { "TransportationId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TransportationAliases_Value",
                table: "TransportationAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Transportations_Category",
                table: "Transportations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Transportations_Manufacturer",
                table: "Transportations",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Transportations_Name",
                table: "Transportations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Transportations_Propulsion",
                table: "Transportations",
                column: "Propulsion");

            migrationBuilder.CreateIndex(
                name: "IX_TransportationStoryHooks_TransportationId_Position",
                table: "TransportationStoryHooks",
                columns: new[] { "TransportationId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Universe_Slug",
                table: "Universe",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_Category",
                table: "VocabularyEntries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_Domain",
                table: "VocabularyEntries",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_Term",
                table: "VocabularyEntries",
                column: "Term");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChangeLog_Status_CreatedAt",
                table: "VoiceChangeLog",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChangeLog_StrandId",
                table: "VoiceChangeLog",
                column: "StrandId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponAliases_Value",
                table: "WeaponAliases",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponAliases_WeaponId_Position",
                table: "WeaponAliases",
                columns: new[] { "WeaponId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WeaponAmmunitionTypes_AmmunitionId",
                table: "WeaponAmmunitionTypes",
                column: "AmmunitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponAmmunitionTypes_WeaponId_Position",
                table: "WeaponAmmunitionTypes",
                columns: new[] { "WeaponId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WeaponBaseTechnologies_TechnologyId",
                table: "WeaponBaseTechnologies",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponBaseTechnologies_WeaponId_Position",
                table: "WeaponBaseTechnologies",
                columns: new[] { "WeaponId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WeaponKnownUsers_CharacterId",
                table: "WeaponKnownUsers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponKnownUsers_WeaponId_Position",
                table: "WeaponKnownUsers",
                columns: new[] { "WeaponId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Weapons_Category",
                table: "Weapons",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Weapons_Manufacturer",
                table: "Weapons",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Weapons_Name",
                table: "Weapons",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponSpecs_SpecKey",
                table: "WeaponSpecs",
                column: "SpecKey");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponSpecs_WeaponId_SpecKey",
                table: "WeaponSpecs",
                columns: new[] { "WeaponId", "SpecKey" });

            migrationBuilder.CreateIndex(
                name: "IX_WeaponStoryHooks_WeaponId_Position",
                table: "WeaponStoryHooks",
                columns: new[] { "WeaponId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmmunitionAliases");

            migrationBuilder.DropTable(
                name: "AmmunitionCompatibleWeapons");

            migrationBuilder.DropTable(
                name: "AmmunitionStoryHooks");

            migrationBuilder.DropTable(
                name: "AmmunitionVariants");

            migrationBuilder.DropTable(
                name: "ApparelAliases");

            migrationBuilder.DropTable(
                name: "ApparelMaterials");

            migrationBuilder.DropTable(
                name: "ApparelStoryHooks");

            migrationBuilder.DropTable(
                name: "ApparelWornBy");

            migrationBuilder.DropTable(
                name: "ArchetypeOpposites");

            migrationBuilder.DropTable(
                name: "ArchetypeSimilars");

            migrationBuilder.DropTable(
                name: "ArchetypeUnless");

            migrationBuilder.DropTable(
                name: "ArchetypeWillAlways");

            migrationBuilder.DropTable(
                name: "ArchetypeWillNever");

            migrationBuilder.DropTable(
                name: "AutomatonAliases");

            migrationBuilder.DropTable(
                name: "AutomatonArmament");

            migrationBuilder.DropTable(
                name: "AutomatonDeployments");

            migrationBuilder.DropTable(
                name: "AutomatonSensors");

            migrationBuilder.DropTable(
                name: "AutomatonStoryHooks");

            migrationBuilder.DropTable(
                name: "BeatEntityMentions");

            migrationBuilder.DropTable(
                name: "BeatModeLog");

            migrationBuilder.DropTable(
                name: "BeatServiceLog");

            migrationBuilder.DropTable(
                name: "BookChapterOrder");

            migrationBuilder.DropTable(
                name: "BookProtagonists");

            migrationBuilder.DropTable(
                name: "ChapterBeats");

            migrationBuilder.DropTable(
                name: "ChapterCharacters");

            migrationBuilder.DropTable(
                name: "CharacterAffiliations");

            migrationBuilder.DropTable(
                name: "CharacterAliases");

            migrationBuilder.DropTable(
                name: "CharacterAncestryDetails");

            migrationBuilder.DropTable(
                name: "CharacterArchetypeScores");

            migrationBuilder.DropTable(
                name: "CharacterBehavioralMaps");

            migrationBuilder.DropTable(
                name: "CharacterBehavioralRules");

            migrationBuilder.DropTable(
                name: "CharacterBelongingsExtras");

            migrationBuilder.DropTable(
                name: "CharacterBelongingsGear");

            migrationBuilder.DropTable(
                name: "CharacterBioBatteryThresholds");

            migrationBuilder.DropTable(
                name: "CharacterChangelog");

            migrationBuilder.DropTable(
                name: "CharacterConditions");

            migrationBuilder.DropTable(
                name: "CharacterCyberware");

            migrationBuilder.DropTable(
                name: "CharacterEmotionalLedgers");

            migrationBuilder.DropTable(
                name: "CharacterGeneticAncestries");

            migrationBuilder.DropTable(
                name: "CharacterHomeTurfs");

            migrationBuilder.DropTable(
                name: "CharacterKnowledgeEntities");

            migrationBuilder.DropTable(
                name: "CharacterNeuralAbilities");

            migrationBuilder.DropTable(
                name: "CharacterPhysicalMarks");

            migrationBuilder.DropTable(
                name: "CharacterPsychologyTraits");

            migrationBuilder.DropTable(
                name: "CharacterReadModels");

            migrationBuilder.DropTable(
                name: "CharacterRelationships");

            migrationBuilder.DropTable(
                name: "CharacterSpeechPhrases");

            migrationBuilder.DropTable(
                name: "CharacterStatPhrases");

            migrationBuilder.DropTable(
                name: "CharacterStatScalars");

            migrationBuilder.DropTable(
                name: "CharacterStoryHooks");

            migrationBuilder.DropTable(
                name: "CharacterTerritoryReputations");

            migrationBuilder.DropTable(
                name: "CharacterTerritoryZones");

            migrationBuilder.DropTable(
                name: "CharacterTimelineBodyChanges");

            migrationBuilder.DropTable(
                name: "ClaimConfirmations");

            migrationBuilder.DropTable(
                name: "ClaimContradictions");

            migrationBuilder.DropTable(
                name: "ConsumerGoodAliases");

            migrationBuilder.DropTable(
                name: "ConsumerGoodStoryHooks");

            migrationBuilder.DropTable(
                name: "ContinuityClaims");

            migrationBuilder.DropTable(
                name: "ContractBonuses");

            migrationBuilder.DropTable(
                name: "ContractComplications");

            migrationBuilder.DropTable(
                name: "CorponationCommonNames");

            migrationBuilder.DropTable(
                name: "CoverImagePrompts");

            migrationBuilder.DropTable(
                name: "CyberwareItemAliases");

            migrationBuilder.DropTable(
                name: "CyberwareItemKnownUsers");

            migrationBuilder.DropTable(
                name: "CyberwareItemSideEffects");

            migrationBuilder.DropTable(
                name: "CyberwareItemStoryHooks");

            migrationBuilder.DropTable(
                name: "DistributedWorkQueue");

            migrationBuilder.DropTable(
                name: "DocumentHeadings");

            migrationBuilder.DropTable(
                name: "Edges");

            migrationBuilder.DropTable(
                name: "EmotionalBeatScores");

            migrationBuilder.DropTable(
                name: "EmotionalDimensionResults");

            migrationBuilder.DropTable(
                name: "EntertainmentAliases");

            migrationBuilder.DropTable(
                name: "EntertainmentKnownFans");

            migrationBuilder.DropTable(
                name: "EntertainmentStoryHooks");

            migrationBuilder.DropTable(
                name: "EntityEmbeddings");

            migrationBuilder.DropTable(
                name: "EntityProperties");

            migrationBuilder.DropTable(
                name: "EntityReviewQueue");

            migrationBuilder.DropTable(
                name: "EntityReviews");

            migrationBuilder.DropTable(
                name: "EntityReviewSummaries");

            migrationBuilder.DropTable(
                name: "EntityStateEvents");

            migrationBuilder.DropTable(
                name: "EntityTags");

            migrationBuilder.DropTable(
                name: "EntityTaxonomies");

            migrationBuilder.DropTable(
                name: "EpisodeBeats");

            migrationBuilder.DropTable(
                name: "EpisodeCorrections");

            migrationBuilder.DropTable(
                name: "EpisodeSurveys");

            migrationBuilder.DropTable(
                name: "EquipmentAliases");

            migrationBuilder.DropTable(
                name: "EquipmentBaseTechnologies");

            migrationBuilder.DropTable(
                name: "EquipmentKnownUsers");

            migrationBuilder.DropTable(
                name: "EquipmentSpecifications");

            migrationBuilder.DropTable(
                name: "EquipmentStoryHooks");

            migrationBuilder.DropTable(
                name: "ExtractionRuns");

            migrationBuilder.DropTable(
                name: "FactionAliases");

            migrationBuilder.DropTable(
                name: "FactionGoals");

            migrationBuilder.DropTable(
                name: "FactionMembers");

            migrationBuilder.DropTable(
                name: "FactionMethods");

            migrationBuilder.DropTable(
                name: "FactionRelationshipTags");

            migrationBuilder.DropTable(
                name: "FactionResources");

            migrationBuilder.DropTable(
                name: "FactionStoryHooks");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "FlyoverEntityAliases");

            migrationBuilder.DropTable(
                name: "FlyoverEntityKnownLocations");

            migrationBuilder.DropTable(
                name: "FlyoverEntityStoryHooks");

            migrationBuilder.DropTable(
                name: "FocusGroupMembers");

            migrationBuilder.DropTable(
                name: "GenemodAliases");

            migrationBuilder.DropTable(
                name: "GenemodSideEffects");

            migrationBuilder.DropTable(
                name: "GenemodStoryHooks");

            migrationBuilder.DropTable(
                name: "LabSpecimenAliases");

            migrationBuilder.DropTable(
                name: "LabSpecimenKnownLocations");

            migrationBuilder.DropTable(
                name: "LabSpecimenStoryHooks");

            migrationBuilder.DropTable(
                name: "MarkdownFiles");

            migrationBuilder.DropTable(
                name: "MaterialAliases");

            migrationBuilder.DropTable(
                name: "MaterialApplications");

            migrationBuilder.DropTable(
                name: "MaterialDevelopers");

            migrationBuilder.DropTable(
                name: "MaterialProperties");

            migrationBuilder.DropTable(
                name: "MaterialStoryHooks");

            migrationBuilder.DropTable(
                name: "MotifAppearances");

            migrationBuilder.DropTable(
                name: "NewsEntitiesInvolved");

            migrationBuilder.DropTable(
                name: "NewsLocations");

            migrationBuilder.DropTable(
                name: "PharmaceuticalAliases");

            migrationBuilder.DropTable(
                name: "PharmaceuticalEffects");

            migrationBuilder.DropTable(
                name: "PharmaceuticalSideEffects");

            migrationBuilder.DropTable(
                name: "PharmaceuticalStoryHooks");

            migrationBuilder.DropTable(
                name: "PlaceAdjacencies");

            migrationBuilder.DropTable(
                name: "PlaceAliases");

            migrationBuilder.DropTable(
                name: "PlaceAtmosphereItems");

            migrationBuilder.DropTable(
                name: "PlaceDangers");

            migrationBuilder.DropTable(
                name: "PlaceExits");

            migrationBuilder.DropTable(
                name: "PlaceFrequentedBy");

            migrationBuilder.DropTable(
                name: "PlaceNotableLocations");

            migrationBuilder.DropTable(
                name: "PlaceOpportunities");

            migrationBuilder.DropTable(
                name: "PlaceRelatedEntities");

            migrationBuilder.DropTable(
                name: "PlaceStoryHooks");

            migrationBuilder.DropTable(
                name: "PlantPayoffs");

            migrationBuilder.DropTable(
                name: "ProseEmbeddings");

            migrationBuilder.DropTable(
                name: "PsionicAliases");

            migrationBuilder.DropTable(
                name: "PsionicKnownPractitioners");

            migrationBuilder.DropTable(
                name: "PsionicStoryHooks");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Records");

            migrationBuilder.DropTable(
                name: "RepositoryDefinitions");

            migrationBuilder.DropTable(
                name: "SeriesItems");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Species");

            migrationBuilder.DropTable(
                name: "StrandAmendments");

            migrationBuilder.DropTable(
                name: "StrandAudioEvents");

            migrationBuilder.DropTable(
                name: "StrandBeats");

            migrationBuilder.DropTable(
                name: "StrandPublications");

            migrationBuilder.DropTable(
                name: "StrandReviewBeatScores");

            migrationBuilder.DropTable(
                name: "StrandReviewSummaries");

            migrationBuilder.DropTable(
                name: "StrandScoreHistories");

            migrationBuilder.DropTable(
                name: "StrandSpineVersions");

            migrationBuilder.DropTable(
                name: "SubsidiaryProducts");

            migrationBuilder.DropTable(
                name: "SyntheticLifeAliases");

            migrationBuilder.DropTable(
                name: "SyntheticLifeKnownAssociations");

            migrationBuilder.DropTable(
                name: "SyntheticLifeStoryHooks");

            migrationBuilder.DropTable(
                name: "TechnologyAliases");

            migrationBuilder.DropTable(
                name: "TechnologyBaseTechnologies");

            migrationBuilder.DropTable(
                name: "TechnologyDevelopers");

            migrationBuilder.DropTable(
                name: "TechnologyEnabledList");

            migrationBuilder.DropTable(
                name: "TechnologyStoryHooks");

            migrationBuilder.DropTable(
                name: "TransportationAliases");

            migrationBuilder.DropTable(
                name: "TransportationStoryHooks");

            migrationBuilder.DropTable(
                name: "Universe");

            migrationBuilder.DropTable(
                name: "VocabularyEntries");

            migrationBuilder.DropTable(
                name: "VoiceChangeLog");

            migrationBuilder.DropTable(
                name: "WeaponAliases");

            migrationBuilder.DropTable(
                name: "WeaponAmmunitionTypes");

            migrationBuilder.DropTable(
                name: "WeaponBaseTechnologies");

            migrationBuilder.DropTable(
                name: "WeaponKnownUsers");

            migrationBuilder.DropTable(
                name: "WeaponSpecs");

            migrationBuilder.DropTable(
                name: "WeaponStoryHooks");

            migrationBuilder.DropTable(
                name: "Ammunitions");

            migrationBuilder.DropTable(
                name: "Apparels");

            migrationBuilder.DropTable(
                name: "Archetypes");

            migrationBuilder.DropTable(
                name: "Automata");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "CharacterKnowledge");

            migrationBuilder.DropTable(
                name: "CharacterTimeline");

            migrationBuilder.DropTable(
                name: "ConsumerGoods");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Corponations");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "CyberwareItems");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "EmotionalExaminations");

            migrationBuilder.DropTable(
                name: "EntertainmentItems");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Taxonomies");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "EquipmentItems");

            migrationBuilder.DropTable(
                name: "FactionRelationships");

            migrationBuilder.DropTable(
                name: "FlyoverEntities");

            migrationBuilder.DropTable(
                name: "FocusGroups");

            migrationBuilder.DropTable(
                name: "Genemods");

            migrationBuilder.DropTable(
                name: "LabSpecimens");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Motifs");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "Pharmaceuticals");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "Psionics");

            migrationBuilder.DropTable(
                name: "Beats");

            migrationBuilder.DropTable(
                name: "StrandReviews");

            migrationBuilder.DropTable(
                name: "Subsidiaries");

            migrationBuilder.DropTable(
                name: "SyntheticLives");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "Transportations");

            migrationBuilder.DropTable(
                name: "Weapons");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "Strands");

            migrationBuilder.DropTable(
                name: "Entities");
        }
    }
}
