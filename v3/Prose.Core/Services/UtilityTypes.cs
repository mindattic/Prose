namespace Prose.Core.Services;

public record UtilityProgress(int FilesScanned, int TotalFiles, int FilesModified, int ChangesApplied, string? Status = null);
public record UtilityResult(int FilesScanned, int FilesModified, int ChangesApplied, List<string>? Warnings = null);
