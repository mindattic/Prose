using Serilog;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Manages single-use verification codes for self-service password reset.
/// Codes are 6-digit, in-memory only, and expire after 15 minutes.
/// </summary>
public class PasswordResetService
{
    private record PendingReset(string Code, DateTime ExpiresUtc);

    private readonly Dictionary<string, PendingReset> pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock resetLock = new();

    private static readonly TimeSpan CodeExpiry = TimeSpan.FromMinutes(15);

    /// <summary>Generates a new 6-digit code for the given user and returns it for sending.</summary>
    public string GenerateCode(string userId)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        lock (resetLock)
        {
            pending[userId] = new PendingReset(code, DateTime.UtcNow + CodeExpiry);
        }
        Log.Information("Password reset code generated for user {UserId}", userId);
        return code;
    }

    /// <summary>
    /// Validates a submitted code. Returns true and removes the code on success.
    /// Returns false if the code is wrong, expired, or no code exists for the user.
    /// </summary>
    public bool ValidateCode(string userId, string submittedCode)
    {
        lock (resetLock)
        {
            if (!pending.TryGetValue(userId, out var reset))
                return false;

            if (DateTime.UtcNow > reset.ExpiresUtc)
            {
                pending.Remove(userId);
                Log.Information("Password reset code expired for user {UserId}", userId);
                return false;
            }

            if (!string.Equals(reset.Code, submittedCode.Trim(), StringComparison.Ordinal))
                return false;

            // One-time use — remove immediately after validation
            pending.Remove(userId);
            Log.Information("Password reset code validated for user {UserId}", userId);
            return true;
        }
    }

    /// <summary>Cancels any pending reset code for the user (e.g., user cancels the flow).</summary>
    public void CancelCode(string userId)
    {
        lock (resetLock) { pending.Remove(userId); }
    }

    /// <summary>Returns true if a non-expired code is pending for the user.</summary>
    public bool HasPendingCode(string userId)
    {
        lock (resetLock)
        {
            if (!pending.TryGetValue(userId, out var reset)) return false;
            if (DateTime.UtcNow > reset.ExpiresUtc) { pending.Remove(userId); return false; }
            return true;
        }
    }
}
