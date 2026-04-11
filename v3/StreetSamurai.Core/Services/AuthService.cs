using System.Text.RegularExpressions;
using Serilog;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Handles password hashing (BCrypt) and user authentication.
/// Seeds a default admin account on first run if no administrators exist.
/// </summary>
public class AuthService
{
    private readonly UserRepository users;

    // BCrypt work factor — 12 = ~250ms per hash on modern hardware.
    // High enough to resist brute force, low enough for interactive login.
    private const int BcryptWorkFactor = 12;

    // Account lockout: 10 attempts before lockout, 5 minute duration.
    // Threshold is high enough that DoS-by-lockout requires sustained effort,
    // while the global rate limiter (20 req/min) is the primary brute-force defense.
    private const int MaxFailedAttempts = 10;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    // Password policy
    public const int MinPasswordLength = 8;

    // BCrypt silently truncates passwords at 72 bytes. Enforce a sane max to prevent
    // users from unknowingly relying on truncated-password collisions.
    public const int MaxPasswordLength = 72;

    // DisplayName length limit to prevent abuse
    public const int MaxDisplayNameLength = 100;

    // Email length limit (RFC 5321: 254 chars max for email address)
    public const int MaxEmailLength = 254;

    // Track failed login attempts in memory (resets on app restart, which is acceptable)
    private readonly Dictionary<string, (int count, DateTime lastAttempt)> failedAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock lockoutLock = new();

    // Regex for basic email format validation (RFC 5322 simplified)
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AuthService(UserRepository users)
    {
        this.users = users;
        SeedDefaultAdmin();
    }

    public UserAccount? Authenticate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        // Reject null bytes in input (potential injection vector)
        if (email.Contains('\0') || password.Contains('\0'))
            return null;

        var user = users.GetByEmail(email);

        // ALWAYS run BCrypt.Verify regardless of lockout or user existence.
        // This ensures constant response time — prevents timing-based enumeration
        // of both account existence and lockout state.
        var hashToVerify = user?.PasswordHash ?? "$2a$12$invalidhashpaddingtomatchlength00000000000000000000";
        var isValid = BCrypt.Net.BCrypt.Verify(password, hashToVerify);

        // Check lockout AFTER BCrypt (timing-safe)
        if (IsLockedOut(email))
        {
            Log.Warning("Login attempt for locked-out account {Email}", SanitizeForLog(email));
            return null;
        }

        if (user == null || !isValid)
        {
            RecordFailedAttempt(email);
            return null;
        }

        // Success — clear failed attempts and update last login
        ClearFailedAttempts(email);
        user.LastLoginUtc = DateTime.UtcNow;
        users.Update(user);
        return user;
    }

    public UserAccount CreateUser(string email, string displayName, string password, string role)
    {
        ValidateEmail(email);
        ValidateDisplayName(displayName);
        ValidatePassword(password);
        ValidateRole(role);

        if (users.GetByEmail(email) != null)
            throw new InvalidOperationException($"User with email '{SanitizeForLog(email)}' already exists.");

        var user = new UserAccount
        {
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = SanitizeDisplayName(displayName),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor),
            Role = role
        };

        users.Add(user);
        Log.Information("Created user {Email} with role {Role}", SanitizeForLog(email), role);
        return user;
    }

    public void ChangePassword(string userId, string newPassword)
    {
        ValidatePassword(newPassword);

        var user = users.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.MustChangePassword = false;
        users.Update(user);
        Log.Information("Password changed for user {UserId} — active sessions invalidated", user.Id);
    }

    /// <summary>
    /// Changes password with verification of the current password.
    /// Used for self-service password change (not admin reset).
    /// </summary>
    public void ChangePasswordWithVerification(string userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(currentPassword))
            throw new ArgumentException("Current password is required.");

        var user = users.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");

        // Verify current password before allowing change
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new ArgumentException("Current password is incorrect.");

        ChangePassword(userId, newPassword);
    }

    public void ChangeRole(string userId, string newRole)
    {
        ValidateRole(newRole);

        var user = users.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");

        // Prevent demoting the last administrator — would lock out admin access entirely
        if (user.Role == UserRoles.Administrator && newRole != UserRoles.Administrator)
        {
            var adminCount = users.GetAll().Count(u => u.Role == UserRoles.Administrator);
            if (adminCount <= 1)
                throw new InvalidOperationException("Cannot demote the last administrator.");
        }

        var oldRole = user.Role;
        user.Role = newRole;
        user.SecurityStamp = Guid.NewGuid().ToString();
        users.Update(user);
        Log.Information("Changed role for user {UserId} from {OldRole} to {NewRole} — active sessions invalidated", user.Id, oldRole, newRole);
    }

    /// <summary>
    /// Deletes a user account with safety checks.
    /// Prevents deleting the last administrator (would lock out admin access).
    /// </summary>
    public void DeleteUser(string userId, string actingUserId)
    {
        if (userId == actingUserId)
            throw new InvalidOperationException("Cannot delete your own account.");

        var user = users.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role == UserRoles.Administrator)
        {
            var adminCount = users.GetAll().Count(u => u.Role == UserRoles.Administrator);
            if (adminCount <= 1)
                throw new InvalidOperationException("Cannot delete the last administrator.");
        }

        users.Delete(userId);
        Log.Information("Deleted user {UserId} ({Email})", userId, SanitizeForLog(user.Email));
    }

    /// <summary>
    /// Updates a user's profile (email, display name) with full validation and sanitization.
    /// Rotates SecurityStamp to invalidate active sessions (claims are now stale).
    /// </summary>
    public void UpdateProfile(string userId, string newEmail, string newDisplayName)
    {
        ValidateEmail(newEmail);
        ValidateDisplayName(newDisplayName);

        var user = users.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");

        // Check for email collision with another user
        var normalizedEmail = newEmail.Trim().ToLowerInvariant();
        var existing = users.GetByEmail(normalizedEmail);
        if (existing != null && existing.Id != userId)
            throw new InvalidOperationException($"Email '{SanitizeForLog(newEmail)}' is already in use.");

        var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var nameChanged = user.DisplayName != SanitizeDisplayName(newDisplayName);

        user.Email = normalizedEmail;
        user.DisplayName = SanitizeDisplayName(newDisplayName);

        // Rotate SecurityStamp if email or name changed — claims in the cookie are now stale
        if (emailChanged || nameChanged)
            user.SecurityStamp = Guid.NewGuid().ToString();

        users.Update(user);
        Log.Information("Updated profile for user {UserId}", user.Id);
    }

    public bool IsLockedOut(string email)
    {
        lock (lockoutLock)
        {
            if (!failedAttempts.TryGetValue(email, out var record))
                return false;

            if (record.count >= MaxFailedAttempts)
            {
                // Check if lockout period has expired
                if (DateTime.UtcNow - record.lastAttempt < LockoutDuration)
                    return true;

                // Lockout expired — clear
                failedAttempts.Remove(email);
                return false;
            }

            return false;
        }
    }

    // Exposed for testing
    public int GetLockoutEntryCount()
    {
        lock (lockoutLock) { return failedAttempts.Count; }
    }

    public int GetFailedAttemptCount(string email)
    {
        lock (lockoutLock)
        {
            return failedAttempts.TryGetValue(email, out var record) ? record.count : 0;
        }
    }

    // Max entries before triggering cleanup to cap memory usage
    private const int MaxLockoutEntries = 10_000;
    private int recordsSinceCleanup;

    private void RecordFailedAttempt(string email)
    {
        lock (lockoutLock)
        {
            var current = failedAttempts.TryGetValue(email, out var record) ? record.count : 0;
            failedAttempts[email] = (current + 1, DateTime.UtcNow);

            if (current + 1 >= MaxFailedAttempts)
                Log.Warning("Account locked out after {Count} failed attempts for {Email}", current + 1, SanitizeForLog(email));

            // Lazy eviction: purge expired entries periodically to cap memory growth.
            // Runs every 100 failed attempts OR when the dictionary exceeds MaxLockoutEntries.
            recordsSinceCleanup++;
            if (recordsSinceCleanup >= 100 || failedAttempts.Count > MaxLockoutEntries)
            {
                EvictExpiredEntries();
                recordsSinceCleanup = 0;
            }
        }
    }

    private void EvictExpiredEntries()
    {
        // Must be called under lockoutLock
        var now = DateTime.UtcNow;
        var expired = failedAttempts
            .Where(kv => now - kv.Value.lastAttempt >= LockoutDuration)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expired)
            failedAttempts.Remove(key);
    }

    private void ClearFailedAttempts(string email)
    {
        lock (lockoutLock)
        {
            failedAttempts.Remove(email);
        }
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");
        if (email.Length > MaxEmailLength)
            throw new ArgumentException($"Email must not exceed {MaxEmailLength} characters.");
        if (email.Contains('\0'))
            throw new ArgumentException("Email contains invalid characters.");
        if (!EmailRegex.IsMatch(email.Trim()))
            throw new ArgumentException("Invalid email format.");
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required.");
        if (password.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters.");
        if (password.Length > MaxPasswordLength)
            throw new ArgumentException($"Password must not exceed {MaxPasswordLength} characters (BCrypt limit).");
        if (password.Contains('\0'))
            throw new ArgumentException("Password contains invalid characters.");
    }

    public static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");
        if (displayName.Length > MaxDisplayNameLength)
            throw new ArgumentException($"Display name must not exceed {MaxDisplayNameLength} characters.");
        if (displayName.Contains('\0'))
            throw new ArgumentException("Display name contains invalid characters.");
    }

    private static void ValidateRole(string role)
    {
        if (!UserRoles.All.Contains(role))
            throw new ArgumentException($"Invalid role '{role}'. Must be one of: {string.Join(", ", UserRoles.All)}");
    }

    /// <summary>
    /// Strips HTML tags from the display name to prevent stored XSS.
    /// Also trims and collapses whitespace.
    /// </summary>
    public static string SanitizeDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name.Trim();

        // Strip HTML tags
        var sanitized = Regex.Replace(name, @"<[^>]*>", "", RegexOptions.Compiled);
        // Strip null bytes
        sanitized = sanitized.Replace("\0", "");
        // Collapse whitespace
        sanitized = Regex.Replace(sanitized.Trim(), @"\s+", " ");
        return sanitized;
    }

    /// <summary>
    /// Sanitizes a value for safe inclusion in structured log messages.
    /// Strips newlines to prevent log injection.
    /// </summary>
    public static string SanitizeForLog(string value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        return value.Replace("\r", "").Replace("\n", "").Replace("\0", "");
    }

    /// <summary>
    /// Validates a URL is a safe local redirect target.
    /// Prevents open redirect attacks via absolute URLs, protocol-relative URLs,
    /// backslash tricks, javascript: URIs, data: URIs, and encoded variants.
    /// </summary>
    public static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // Reject any control characters or null bytes
        if (url.Any(c => char.IsControl(c))) return false;

        // Decode to catch encoded bypass attempts (e.g., %2F%5C)
        string decoded;
        try { decoded = Uri.UnescapeDataString(url); }
        catch { return false; }

        // Must start with a single forward slash (relative path)
        if (!decoded.StartsWith('/')) return false;

        // Reject protocol-relative URLs (//evil.com)
        if (decoded.StartsWith("//")) return false;

        // Reject backslash tricks (/\evil.com) — browsers normalize \ to /
        if (decoded.StartsWith("/\\")) return false;

        // Extract only the path portion (before ? or #) for scheme/authority checks
        var pathPortion = decoded;
        var queryIdx = decoded.IndexOf('?');
        var fragIdx = decoded.IndexOf('#');
        var delimIdx = (queryIdx >= 0, fragIdx >= 0) switch
        {
            (true, true) => Math.Min(queryIdx, fragIdx),
            (true, false) => queryIdx,
            (false, true) => fragIdx,
            _ => -1
        };
        if (delimIdx >= 0) pathPortion = decoded[..delimIdx];

        // Reject path containing : before the first / after the leading slash
        // (catches javascript:, data:, scheme:// embedded in path)
        var afterSlash = pathPortion[1..];
        var colonIdx = afterSlash.IndexOf(':');
        var slashIdx = afterSlash.IndexOf('/');
        if (colonIdx >= 0 && (slashIdx < 0 || colonIdx < slashIdx)) return false;

        // Reject @ in path portion (user info syntax: /foo@evil.com)
        if (pathPortion.Contains('@')) return false;

        return true;
    }

    private void SeedDefaultAdmin()
    {
        var all = users.GetAll();
        if (all.Any(u => u.Role == UserRoles.Administrator))
            return;

        var admin = new UserAccount
        {
            Email = "ryandebraal@mindattic.com",
            DisplayName = "Ryan",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Change-Me-123!", BcryptWorkFactor),
            Role = UserRoles.Administrator,
            MustChangePassword = true,
        };

        users.Add(admin);
        // Do NOT log the default password — it ends up in log files on disk
        Log.Warning("Created default admin account (admin@streetsamurai.local) — change the password immediately!");
    }
}
