namespace Prose.UnitTests;

/// <summary>
/// <see cref="RepoPaths"/> finds the repo root from any build output location: the env var when set,
/// else a walk up from the test assembly, else a walk up from the compile-time source path. Each case
/// uses throwaway directories, so the outcome never depends on where this assembly was built.
/// </summary>
[TestFixture]
public class RepoPathsTests
{
    private string temp = null!;

    [SetUp]
    public void SetUp()
    {
        temp = Path.Combine(Path.GetTempPath(), "prose-repopaths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(temp, recursive: true); } catch { }
    }

    private string FakeRepo(string name)
    {
        var root = Path.Combine(temp, name);
        Directory.CreateDirectory(Path.Combine(root, "src", "Prose.Core"));
        File.WriteAllText(Path.Combine(root, "legion.json"), "{}");
        return root;
    }

    private string OutsideDir()
    {
        var dir = Path.Combine(temp, "elsewhere", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Test]
    public void AssemblyBuiltInsideTheRepo_IsFoundByWalkingUp()
    {
        var repo = FakeRepo("repo");
        var bin = Path.Combine(repo, "src", "Prose.UnitTests", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(bin);

        Assert.That(RepoPaths.Resolve(null, bin, sourceFile: ""), Is.EqualTo(repo));
    }

    [Test]
    public void AssemblyBuiltOutsideTheRepo_FallsBackToTheSourceFileAnchor()
    {
        var repo = FakeRepo("repo");
        var source = Path.Combine(repo, "src", "Prose.UnitTests", "RepoPaths.cs");

        Assert.That(RepoPaths.Resolve(null, OutsideDir(), source), Is.EqualTo(repo));
    }

    [Test]
    public void EnvVar_WinsOverBothWalks()
    {
        var walked = FakeRepo("walked");
        var pinned = FakeRepo("pinned");
        var bin = Path.Combine(walked, "src", "Prose.UnitTests", "bin");
        Directory.CreateDirectory(bin);

        Assert.That(RepoPaths.Resolve(pinned, bin, Path.Combine(walked, "src", "x.cs")), Is.EqualTo(pinned));
    }

    [Test]
    public void EnvVar_ThatIsNotARepoRoot_FailsLoudly()
    {
        FakeRepo("repo");
        Assert.Throws<InvalidOperationException>(() => RepoPaths.Resolve(OutsideDir(), OutsideDir(), ""));
    }

    [Test]
    public void NoAnchorAnywhere_FailsNamingTheEnvVar()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            RepoPaths.Resolve(null, OutsideDir(), Path.Combine(temp, "nowhere", "x.cs")));
        Assert.That(ex!.Message, Does.Contain(RepoPaths.EnvVar));
    }

    [Test]
    public void TheRealRoot_HoldsTheCommittedFilesTheTestsRead()
    {
        Assert.That(RepoPaths.IsRepoRoot(RepoPaths.Root), Is.True);
        Assert.That(File.Exists(RepoPaths.Combine("legion.json")), Is.True);
        Assert.That(Directory.Exists(RepoPaths.Combine("engine", "data")), Is.True);
    }
}
