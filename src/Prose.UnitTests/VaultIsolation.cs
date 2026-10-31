// No namespace on purpose: a [SetUpFixture] covers only its own namespace, so this one covers the whole assembly.

/// <summary>
/// PRS-T829 (LAMP): SettingsService reads and writes MindAttic Vault (settings, named keys, key health, key
/// assignments) and Legion resolves keys through Vault too. Point both Vault roots at temp folders for the whole
/// assembly so no test can touch the real %APPDATA%\MindAttic or %LOCALAPPDATA%\MindAttic (M.VALT-LAW-6).
/// </summary>
[SetUpFixture]
public sealed class ProseVaultIsolation
{
    private string root = "";
    private string? previousRoaming;
    private string? previousLocal;

    [OneTimeSetUp]
    public void Isolate()
    {
        root = Path.Combine(Path.GetTempPath(), "prose-vault-" + Guid.NewGuid().ToString("N"));
        previousRoaming = Environment.GetEnvironmentVariable("MINDATTIC_VAULT_ROAMING_ROOT");
        previousLocal = Environment.GetEnvironmentVariable("MINDATTIC_VAULT_LOCAL_ROOT");
        Environment.SetEnvironmentVariable("MINDATTIC_VAULT_ROAMING_ROOT", Path.Combine(root, "roaming"));
        Environment.SetEnvironmentVariable("MINDATTIC_VAULT_LOCAL_ROOT", Path.Combine(root, "local"));
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("MINDATTIC_VAULT_ROAMING_ROOT", previousRoaming);
        Environment.SetEnvironmentVariable("MINDATTIC_VAULT_LOCAL_ROOT", previousLocal);
        try { Directory.Delete(root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
