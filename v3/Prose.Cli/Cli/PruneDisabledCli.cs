namespace Prose.Cli;

/// <summary>
/// <c>prose --prune-disabled --slug &lt;slug&gt; [--dry-run]</c>
///
/// Retired. There is no more disabled/soft-deleted beat state to prune — a
/// BeatNode row exists or it doesn't (see ProseDbContext.SystemVersionedTables
/// and the removal of BeatNode.IsEnabled). The write paths that used to leave
/// disabled rows lying around (NodeWorkbenchService.DeleteBeatAsync,
/// ReimportNodeCli) now delete for real, so this command would always find
/// zero candidates. Kept as a stub so the flag doesn't silently do nothing
/// without explanation if an old script still calls it.
/// </summary>
public static class PruneDisabledCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Console.WriteLine("[prune-disabled] Retired — there is no disabled-beat state anymore.");
        Console.WriteLine("[prune-disabled] Beats are deleted for real when removed; nothing to prune.");
        return Task.FromResult(0);
    }
}
