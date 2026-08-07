namespace Prose.Core.Models;

public class UserAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public bool MustChangePassword { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }
}

public static class UserRoles
{
    public const string User = "User";
    public const string Contributor = "Contributor";
    public const string Administrator = "Administrator";

    public static readonly string[] All = [User, Contributor, Administrator];
    public static readonly string[] Writers = [Contributor, Administrator];
}
