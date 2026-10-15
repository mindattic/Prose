using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;
using Prose.Hub;

namespace Prose.Mcp
{
    /// <summary>Probe tool class. ToolDispatch resolves tool classes by simple name within the
    /// Prose.Mcp namespace across loaded assemblies, so this one is found like a real tool.</summary>
    public sealed class DispatchProbeTools
    {
        public string EchoImpl(string text, int count = 2) => string.Concat(Enumerable.Repeat(text, count));
        public string SetThingImpl(string value) => "set:" + value;
        public Task<string> ReadAsyncThingImpl() => Task.FromResult("async-ok");
        public string BoomImpl() => throw new InvalidOperationException("kaboom");
    }
}

namespace Prose.UnitTests
{
    [TestFixture, NonParallelizable]
    public class ToolDispatchTests
    {
        private sealed class RecordingUniverse : IUniverseContext
        {
            public static readonly Guid Glmz = new("0197e9c9-0001-7000-8000-000000000001");
            public List<Guid?> FlowCalls { get; } = [];
            public Guid CurrentId => Glmz;
            public bool IsExplicitlyScoped => true;
            public string CurrentSlug => "glmz";
            public UniverseInfo? CurrentUniverse => new(Glmz, "glmz", "GLMZ", null, null, true, 100);
            public IReadOnlyList<UniverseInfo> ListUniverses() => [CurrentUniverse!];
            public bool IsGlmz => true;
            public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
            public void UseUniverse(Guid id) { }
            public bool UseUniverseBySlug(string slug) => false;
            public void SetFlowUniverse(Guid? id) => FlowCalls.Add(id);
            public void PersistAsDefault(Guid id) { }
            public void Refresh() { }
        }

        private RecordingUniverse universe = null!;
        private ServiceProvider sp = null!;

        [SetUp]
        public void SetUp()
        {
            universe = new RecordingUniverse();
            sp = new ServiceCollection().AddSingleton<IUniverseContext>(universe).BuildServiceProvider();
        }

        [TearDown]
        public void TearDown() => sp.Dispose();

        private Task<(Microsoft.AspNetCore.Http.IResult Result, bool Success, string? Output, string? Error)> Invoke(
            string method, string? argsJson = null, bool scopeExplicit = false, string? universeSlug = null, string toolClass = "DispatchProbeTools")
        {
            JsonElement? args = argsJson == null ? null : JsonDocument.Parse(argsJson).RootElement;
            return ToolDispatch.InvokeCoreAsync(new ToolDispatch.InvokeRequest(toolClass, method, args, universeSlug, scopeExplicit), sp);
        }

        // ── LooksMutating ───────────────────────────────────────────────────

        [TestCase("CreateCharacter")]
        [TestCase("updateBeatText")]
        [TestCase("DeleteBeat")]
        [TestCase("SetEntityFieldsImpl")]
        [TestCase("RenameNode")]
        public void LooksMutating_VerbPrefixes(string method) =>
            Assert.That(ToolDispatch.LooksMutating(method), Is.True);

        [TestCase("SpliceBeats")]
        [TestCase("ReadBeats")]
        [TestCase("RecordRuling")]
        [TestCase("ExportNode")]
        [TestCase("lint_prose")]
        [TestCase("VerifyEntityCommitImpl")]
        public void LooksMutating_ExplicitWriteToolsWithoutAVerb(string method) =>
            Assert.That(ToolDispatch.LooksMutating(method), Is.True);

        [TestCase("GetCharacter")]
        [TestCase("ListBooks")]
        [TestCase("FindEntities")]
        [TestCase("FactoryStatus")]
        public void LooksMutating_ReadTools_AreNot(string method) =>
            Assert.That(ToolDispatch.LooksMutating(method), Is.False);

        [TestCase("WorkOrderAdd")]
        [TestCase("SwitchUniverse")]
        [TestCase("AddDictionaryWord")]
        [TestCase("SetOperatorByoKeysImpl")]
        public void LooksMutating_UniverseFreeWrites_NeedNoScope(string method) =>
            Assert.That(ToolDispatch.LooksMutating(method), Is.False);

        // ── the universe gate ───────────────────────────────────────────────

        [Test]
        public async Task MutatingTool_WithoutExplicitScope_IsRefusedBeforeItRuns()
        {
            var r = await Invoke("SetThing", """{"value":"x"}""", scopeExplicit: false);

            Assert.That(r.Success, Is.False);
            Assert.That(r.Error, Is.EqualTo("missing_universe"));
        }

        [Test]
        public async Task MutatingTool_WithExplicitScope_Runs()
        {
            var r = await Invoke("SetThing", """{"value":"x"}""", scopeExplicit: true);

            Assert.That(r.Success, Is.True, r.Error);
            Assert.That(r.Output, Is.EqualTo("set:x"));
        }

        [Test]
        public async Task ReadTool_WithoutScope_Runs()
        {
            var r = await Invoke("Echo", """{"text":"ab"}""");

            Assert.That(r.Success, Is.True, r.Error);
            Assert.That(r.Output, Is.EqualTo("abab"));
        }

        [Test]
        public async Task NamedUniverse_IsAppliedForTheCallThenCleared()
        {
            var r = await Invoke("Echo", """{"text":"a"}""", universeSlug: "GLMZ");

            Assert.That(r.Success, Is.True, r.Error);
            Assert.That(universe.FlowCalls, Is.EqualTo(new Guid?[] { RecordingUniverse.Glmz, null }));
        }

        [Test]
        public async Task UnknownUniverse_IsRefused()
        {
            var r = await Invoke("Echo", """{"text":"a"}""", universeSlug: "nowhere");

            Assert.That(r.Error, Is.EqualTo("unknown_universe"));
            Assert.That(universe.FlowCalls, Is.Empty);
        }

        // ── resolution and binding ──────────────────────────────────────────

        [Test]
        public async Task Args_BindCaseInsensitively_AndOmittedArgsTakeTheirDefault()
        {
            var r = await Invoke("EchoImpl", """{"TEXT":"z","Count":3}""");
            var d = await Invoke("Echo", """{"text":"z"}""");

            Assert.That(r.Output, Is.EqualTo("zzz"));
            Assert.That(d.Output, Is.EqualTo("zz"));
        }

        [Test]
        public async Task AsyncTool_IsAwaited()
        {
            var r = await Invoke("ReadAsyncThing");

            Assert.That(r.Output, Is.EqualTo("async-ok"));
        }

        [Test]
        public async Task UnknownToolClassOrMethod_IsNamed()
        {
            Assert.That((await Invoke("Echo", toolClass: "NoSuchTools")).Error, Is.EqualTo("unknown_tool_class"));
            Assert.That((await Invoke("NoSuchMethod")).Error, Is.EqualTo("unknown_method"));
        }

        [Test]
        public async Task MissingToolClassOrMethod_IsABadRequestNotACrash()
        {
            Assert.That((await Invoke("", toolClass: "DispatchProbeTools")).Error, Is.EqualTo("missing_tool"));
            Assert.That((await Invoke("Echo", toolClass: null!)).Error, Is.EqualTo("missing_tool"));
        }

        [Test]
        public async Task UnknownArguments_AreRefused_NotSilentlyDropped()
        {
            var r = await Invoke("Echo", """{"text":"a","colour":"red"}""");

            Assert.That(r.Success, Is.False);
            Assert.That(r.Error, Does.StartWith("unknown_arguments").And.Contain("colour"));
        }

        [Test]
        public async Task WrongShapedArgument_IsNamed()
        {
            var r = await Invoke("Echo", """{"text":"a","count":"three"}""");

            Assert.That(r.Error, Does.StartWith("bad_argument: count"));
        }

        [Test]
        public async Task ToolException_IsReportedAsToolThrew()
        {
            var r = await Invoke("Boom");

            Assert.That(r.Error, Is.EqualTo("tool_threw: kaboom"));
        }
    }
}
