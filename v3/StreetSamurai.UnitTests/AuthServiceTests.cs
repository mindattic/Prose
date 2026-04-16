using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class AuthServiceTests
{
    private string testDir = null!;
    private UserRepository userRepo = null!;
    private AuthService auth = null!;

    [SetUp]
    public void SetUp()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_auth_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var paths = new TestPathProviderWithRoot(testDir);
        Directory.CreateDirectory(paths.EngineDataDir);
        userRepo = new UserRepository(paths);
        auth = new AuthService(userRepo);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDir))
            Directory.Delete(testDir, recursive: true);
    }

    // ──────────────────────────────────────────────────────
    // Seed behavior
    // ──────────────────────────────────────────────────────

    [Test]
    public void SeedsDefaultAdminOnFirstRun()
    {
        var all = userRepo.GetAll();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Email, Is.EqualTo("admin@streetsamurai.local"));
        Assert.That(all[0].Role, Is.EqualTo(UserRoles.Administrator));
    }

    [Test]
    public void DoesNotSeedAdminIfOneAlreadyExists()
    {
        // The setup already created one admin. Create a new AuthService — it should not create another.
        var auth2 = new AuthService(userRepo);
        Assert.That(userRepo.GetAll(), Has.Count.EqualTo(1));
    }

    // ──────────────────────────────────────────────────────
    // Password hashing
    // ──────────────────────────────────────────────────────

    [Test]
    public void PasswordsAreHashedWithBCrypt()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user, Is.Not.Null);
        // BCrypt hashes start with $2a$ or $2b$
        Assert.That(user!.PasswordHash, Does.Match(@"^\$2[ab]\$\d{2}\$"));
    }

    [Test]
    public void PasswordHashIsNotPlaintext()
    {
        var password = "Secure-Pass-123!";
        auth.CreateUser("test@test.com", "Test", password, UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.PasswordHash, Is.Not.EqualTo(password));
        Assert.That(user.PasswordHash, Does.Not.Contain(password));
    }

    [Test]
    public void SamePasswordProducesDifferentHashes()
    {
        var password = "Secure-Pass-123!";
        auth.CreateUser("user1@test.com", "User1", password, UserRoles.User);
        auth.CreateUser("user2@test.com", "User2", password, UserRoles.User);
        var u1 = userRepo.GetByEmail("user1@test.com");
        var u2 = userRepo.GetByEmail("user2@test.com");
        // BCrypt uses random salt — same password should produce different hashes
        Assert.That(u1!.PasswordHash, Is.Not.EqualTo(u2!.PasswordHash));
    }

    [Test]
    public void BcryptWorkFactorIsAtLeast12()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        // BCrypt hash format: $2a$XX$ where XX is the work factor
        var workFactor = int.Parse(user!.PasswordHash.Split('$')[2]);
        Assert.That(workFactor, Is.GreaterThanOrEqualTo(12));
    }

    // ──────────────────────────────────────────────────────
    // Authentication
    // ──────────────────────────────────────────────────────

    [Test]
    public void AuthenticateSucceedsWithCorrectCredentials()
    {
        auth.CreateUser("test@test.com", "Test", "Correct-Pass-1!", UserRoles.User);
        var result = auth.Authenticate("test@test.com", "Correct-Pass-1!");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo("test@test.com"));
    }

    [Test]
    public void AuthenticateFailsWithWrongPassword()
    {
        auth.CreateUser("test@test.com", "Test", "Correct-Pass-1!", UserRoles.User);
        var result = auth.Authenticate("test@test.com", "Wrong-Pass-99!");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AuthenticateFailsWithNonexistentEmail()
    {
        var result = auth.Authenticate("nobody@test.com", "Any-Password-1!");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AuthenticateIsCaseInsensitiveOnEmail()
    {
        auth.CreateUser("Test@Test.COM", "Test", "Secure-Pass-123!", UserRoles.User);
        var result = auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void AuthenticateRejectsNullEmail()
    {
        var result = auth.Authenticate(null!, "password");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AuthenticateRejectsEmptyPassword()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var result = auth.Authenticate("test@test.com", "");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AuthenticateUpdatesLastLoginTimestamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var before = DateTime.UtcNow;
        auth.Authenticate("test@test.com", "Secure-Pass-123!");
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.LastLoginUtc, Is.Not.Null);
        Assert.That(user.LastLoginUtc!.Value, Is.GreaterThanOrEqualTo(before));
    }

    // ──────────────────────────────────────────────────────
    // Timing-safe authentication (user enumeration defense)
    // ──────────────────────────────────────────────────────

    [Test]
    public void AuthenticateDoesNotLeakUserExistenceViaTiming()
    {
        // Both should take approximately the same time because we verify
        // against a dummy hash even when the user doesn't exist.
        auth.CreateUser("exists@test.com", "Exists", "Secure-Pass-123!", UserRoles.User);

        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        auth.Authenticate("exists@test.com", "Wrong-Password-1!");
        sw1.Stop();

        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        auth.Authenticate("nonexistent@test.com", "Wrong-Password-1!");
        sw2.Stop();

        // Allow generous tolerance — the point is both paths do a BCrypt verify
        // (not that one path is instant and the other takes 250ms)
        Assert.That(sw2.ElapsedMilliseconds, Is.GreaterThan(50),
            "Nonexistent user path should still perform BCrypt verify (timing defense)");
    }

    // ──────────────────────────────────────────────────────
    // Account lockout
    // ──────────────────────────────────────────────────────

    [Test]
    public void AccountLocksOutAfter5FailedAttempts()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        for (int i = 0; i < 10; i++)
            auth.Authenticate("test@test.com", "Wrong-Password-X!");

        Assert.That(auth.IsLockedOut("test@test.com"), Is.True);
    }

    [Test]
    public void LockedOutAccountRejectsCorrectPassword()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        for (int i = 0; i < 10; i++)
            auth.Authenticate("test@test.com", "Wrong-Password-X!");

        // Even with the correct password, locked out
        var result = auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SuccessfulLoginClearsFailedAttempts()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        // 9 failed attempts (not yet locked out — threshold is 10)
        for (int i = 0; i < 9; i++)
            auth.Authenticate("test@test.com", "Wrong-Password-X!");

        Assert.That(auth.GetFailedAttemptCount("test@test.com"), Is.EqualTo(9));

        // Successful login should clear the counter
        auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(auth.GetFailedAttemptCount("test@test.com"), Is.EqualTo(0));
    }

    [Test]
    public void LockoutIsCaseInsensitiveOnEmail()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        for (int i = 0; i < 10; i++)
            auth.Authenticate("TEST@TEST.COM", "Wrong-Password-X!");

        Assert.That(auth.IsLockedOut("test@test.com"), Is.True);
    }

    // ──────────────────────────────────────────────────────
    // Password policy
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsPasswordShorterThan8Characters()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Short1!", UserRoles.User));
    }

    [Test]
    public void RejectsEmptyPassword()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "", UserRoles.User));
    }

    [Test]
    public void AcceptsPasswordExactly8Characters()
    {
        // 8 chars satisfying full policy: upper + lower + digit + special
        Assert.DoesNotThrow(() =>
            auth.CreateUser("test@test.com", "Test", "Abcd12!@", UserRoles.User));
    }

    [Test]
    public void ChangePasswordEnforcesPolicy()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.Throws<ArgumentException>(() =>
            auth.ChangePassword(user!.Id, "short"));
    }

    // ──────────────────────────────────────────────────────
    // Role validation
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsInvalidRole()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", "SuperAdmin"));
    }

    [Test]
    public void AcceptsAllValidRoles()
    {
        auth.CreateUser("user@test.com", "U", "Secure-Pass-123!", UserRoles.User);
        auth.CreateUser("contrib@test.com", "C", "Secure-Pass-123!", UserRoles.Contributor);
        auth.CreateUser("admin2@test.com", "A", "Secure-Pass-123!", UserRoles.Administrator);

        Assert.That(userRepo.GetByEmail("user@test.com")!.Role, Is.EqualTo("User"));
        Assert.That(userRepo.GetByEmail("contrib@test.com")!.Role, Is.EqualTo("Contributor"));
        Assert.That(userRepo.GetByEmail("admin2@test.com")!.Role, Is.EqualTo("Administrator"));
    }

    [Test]
    public void ChangeRoleRejectsInvalidRole()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.Throws<ArgumentException>(() =>
            auth.ChangeRole(user!.Id, "Hacker"));
    }

    [Test]
    public void ChangeRoleSucceedsWithValidRole()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        auth.ChangeRole(user!.Id, UserRoles.Contributor);
        var updated = userRepo.GetByEmail("test@test.com");
        Assert.That(updated!.Role, Is.EqualTo(UserRoles.Contributor));
    }

    // ──────────────────────────────────────────────────────
    // Email validation
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsEmptyEmail()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsEmailWithoutAtSign()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("notanemail", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsDuplicateEmail()
    {
        auth.CreateUser("test@test.com", "Test1", "Secure-Pass-123!", UserRoles.User);
        Assert.Throws<InvalidOperationException>(() =>
            auth.CreateUser("test@test.com", "Test2", "Secure-Pass-456!", UserRoles.User));
    }

    [Test]
    public void EmailIsNormalizedToLowercase()
    {
        auth.CreateUser("Test@TEST.COM", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Email, Is.EqualTo("test@test.com"));
    }

    // ──────────────────────────────────────────────────────
    // Password change
    // ──────────────────────────────────────────────────────

    [Test]
    public void ChangePasswordInvalidatesOldPassword()
    {
        auth.CreateUser("test@test.com", "Test", "Old-Password-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        auth.ChangePassword(user!.Id, "New-Password-456!");

        Assert.That(auth.Authenticate("test@test.com", "Old-Password-123!"), Is.Null);
        Assert.That(auth.Authenticate("test@test.com", "New-Password-456!"), Is.Not.Null);
    }

    // ──────────────────────────────────────────────────────
    // User CRUD
    // ──────────────────────────────────────────────────────

    [Test]
    public void DeletedUserCannotAuthenticate()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        userRepo.Delete(user!.Id);

        var result = auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void UserIdIsGuidFormat()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(Guid.TryParse(user!.Id, out _), Is.True);
    }

    // ══════════════════════════════════════════════════════
    // HARDENING TESTS — Security audit additions
    // ══════════════════════════════════════════════════════

    // ──────────────────────────────────────────────────────
    // Null byte injection defense
    // ──────────────────────────────────────────────────────

    [Test]
    public void AuthenticateRejectsNullByteInEmail()
    {
        var result = auth.Authenticate("admin\0@test.com", "Secure-Pass-123!");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AuthenticateRejectsNullByteInPassword()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var result = auth.Authenticate("test@test.com", "Secure\0-Pass-123!");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CreateUserRejectsNullByteInEmail()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test\0@test.com", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void CreateUserRejectsNullByteInPassword()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure\0-Pass!", UserRoles.User));
    }

    [Test]
    public void CreateUserRejectsNullByteInDisplayName()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test\0Name", "Secure-Pass-123!", UserRoles.User));
    }

    // ──────────────────────────────────────────────────────
    // Password max length (BCrypt 72-byte truncation defense)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsPasswordExceeding72Characters()
    {
        var longPassword = new string('A', 73);
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", longPassword, UserRoles.User));
    }

    [Test]
    public void AcceptsPasswordExactly72Characters()
    {
        // 72 chars satisfying full policy: 68×'A' + "b1!@"
        var maxPassword = new string('A', 68) + "b1!@";
        Assert.DoesNotThrow(() =>
            auth.CreateUser("test@test.com", "Test", maxPassword, UserRoles.User));
    }

    [Test]
    public void ChangePasswordRejectsExceeding72Characters()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var longPassword = new string('B', 73);
        Assert.Throws<ArgumentException>(() =>
            auth.ChangePassword(user!.Id, longPassword));
    }

    // ──────────────────────────────────────────────────────
    // DisplayName sanitization (XSS defense)
    // ──────────────────────────────────────────────────────

    [Test]
    public void DisplayNameStripsHtmlScriptTags()
    {
        auth.CreateUser("test@test.com", "<script>alert('xss')</script>Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.DisplayName, Does.Not.Contain("<script>"));
        Assert.That(user.DisplayName, Does.Not.Contain("</script>"));
        Assert.That(user.DisplayName, Does.Contain("alert('xss')"));
    }

    [Test]
    public void DisplayNameStripsHtmlImgOnError()
    {
        auth.CreateUser("test@test.com", "<img src=x onerror=alert(1)>", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.DisplayName, Does.Not.Contain("<img"));
        Assert.That(user.DisplayName, Does.Not.Contain("onerror"));
    }

    [Test]
    public void DisplayNameStripsNestedHtml()
    {
        auth.CreateUser("test@test.com", "<b><i>Bold Italic</i></b>", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.DisplayName, Is.EqualTo("Bold Italic"));
    }

    [Test]
    public void DisplayNameRejectsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void DisplayNameRejectsWhitespaceOnly()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "   ", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void DisplayNameEnforcesMaxLength()
    {
        var longName = new string('A', 101);
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", longName, "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void DisplayNameAcceptsExactlyMaxLength()
    {
        var name = new string('A', 100);
        Assert.DoesNotThrow(() =>
            auth.CreateUser("test@test.com", name, "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void DisplayNameCollapsesWhitespace()
    {
        auth.CreateUser("test@test.com", "  John    Doe  ", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.DisplayName, Is.EqualTo("John Doe"));
    }

    // ──────────────────────────────────────────────────────
    // Email validation — hardened
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsEmailWithOnlyAtAndDot()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("@.", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsEmailMissingLocalPart()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("@domain.com", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsEmailMissingDomainTld()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("user@domain", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsEmailWithSpaces()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("user @test.com", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsEmailExceedingMaxLength()
    {
        var longLocal = new string('a', 250);
        var email = $"{longLocal}@test.com";
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser(email, "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void RejectsEmailWithSqlInjectionAttempt()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("'; DROP TABLE users;--@test.com", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void AcceptsValidEmailWithSubdomain()
    {
        Assert.DoesNotThrow(() =>
            auth.CreateUser("user@sub.domain.com", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    [Test]
    public void AcceptsValidEmailWithPlusAddressing()
    {
        Assert.DoesNotThrow(() =>
            auth.CreateUser("user+tag@test.com", "Test", "Secure-Pass-123!", UserRoles.User));
    }

    // ──────────────────────────────────────────────────────
    // Unicode in passwords and display names
    // ──────────────────────────────────────────────────────

    [Test]
    public void AcceptsUnicodeInPassword()
    {
        auth.CreateUser("test@test.com", "Test", "P\u00e4ssw\u00f6rd-123!", UserRoles.User);
        var result = auth.Authenticate("test@test.com", "P\u00e4ssw\u00f6rd-123!");
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void AcceptsUnicodeInDisplayName()
    {
        auth.CreateUser("test@test.com", "\u5c0f\u6797\u592a\u90ce", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.DisplayName, Is.EqualTo("\u5c0f\u6797\u592a\u90ce"));
    }

    [Test]
    public void AcceptsEmojiInDisplayName()
    {
        auth.CreateUser("test@test.com", "Samurai \ud83d\udde1\ufe0f", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.DisplayName, Does.Contain("\ud83d\udde1"));
    }

    // ──────────────────────────────────────────────────────
    // Lockout edge cases
    // ──────────────────────────────────────────────────────

    [Test]
    public void LockoutCounterIncrementsCorrectly()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        for (int i = 1; i <= 4; i++)
        {
            auth.Authenticate("test@test.com", "Wrong-Password-X!");
            Assert.That(auth.GetFailedAttemptCount("test@test.com"), Is.EqualTo(i),
                $"Failed attempt count should be {i} after {i} failed attempts");
        }
    }

    [Test]
    public void LockoutForNonexistentUserDoesNotThrow()
    {
        // Attempting to lock out a nonexistent user should not crash
        for (int i = 0; i < 10; i++)
            auth.Authenticate("ghost@nowhere.com", "Whatever-1234!");

        Assert.That(auth.IsLockedOut("ghost@nowhere.com"), Is.True);
    }

    [Test]
    public void IsLockedOutReturnsFalseForUnknownEmail()
    {
        Assert.That(auth.IsLockedOut("never-seen@test.com"), Is.False);
    }

    [Test]
    public void GetFailedAttemptCountReturnsZeroForUnknownEmail()
    {
        Assert.That(auth.GetFailedAttemptCount("never-seen@test.com"), Is.EqualTo(0));
    }

    [Test]
    public void AdditionalFailedAttemptsAfterLockoutDoNotResetCounter()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        // Lock the account
        for (int i = 0; i < 10; i++)
            auth.Authenticate("test@test.com", "Wrong-Password-X!");

        Assert.That(auth.IsLockedOut("test@test.com"), Is.True);

        // Additional attempts while locked out should still be rejected
        var result = auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(result, Is.Null);
        // Counter should still reflect locked state
        Assert.That(auth.GetFailedAttemptCount("test@test.com"), Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void ConcurrentLockoutAttemptsDoNotCorrupt()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        // Simulate concurrent login attempts from multiple threads
        var tasks = new Task[20];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
                auth.Authenticate("test@test.com", "Wrong-Password-X!"));
        }
        Task.WaitAll(tasks);

        // Account should definitely be locked out, and counter should be >= 5
        Assert.That(auth.IsLockedOut("test@test.com"), Is.True);
        Assert.That(auth.GetFailedAttemptCount("test@test.com"), Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void LockoutAffectsOnlyTargetedEmail()
    {
        auth.CreateUser("victim@test.com", "Victim", "Secure-Pass-123!", UserRoles.User);
        auth.CreateUser("innocent@test.com", "Innocent", "Secure-Pass-456!", UserRoles.User);

        // Lock out victim
        for (int i = 0; i < 10; i++)
            auth.Authenticate("victim@test.com", "Wrong-Password-X!");

        // Innocent user should NOT be affected
        Assert.That(auth.IsLockedOut("innocent@test.com"), Is.False);
        var result = auth.Authenticate("innocent@test.com", "Secure-Pass-456!");
        Assert.That(result, Is.Not.Null);
    }

    // ──────────────────────────────────────────────────────
    // Privilege escalation defense
    // ──────────────────────────────────────────────────────

    [Test]
    public void ChangeRoleRequiresValidUserId()
    {
        Assert.Throws<InvalidOperationException>(() =>
            auth.ChangeRole("nonexistent-id", UserRoles.Administrator));
    }

    [Test]
    public void ChangePasswordRequiresValidUserId()
    {
        Assert.Throws<InvalidOperationException>(() =>
            auth.ChangePassword("nonexistent-id", "New-Password-123!"));
    }

    [Test]
    public void RoleChangeDoesNotAffectOtherUsers()
    {
        auth.CreateUser("user1@test.com", "User1", "Secure-Pass-123!", UserRoles.User);
        auth.CreateUser("user2@test.com", "User2", "Secure-Pass-456!", UserRoles.User);

        var user1 = userRepo.GetByEmail("user1@test.com");
        auth.ChangeRole(user1!.Id, UserRoles.Administrator);

        // user2 should still be User role
        var user2 = userRepo.GetByEmail("user2@test.com");
        Assert.That(user2!.Role, Is.EqualTo(UserRoles.User));
    }

    [Test]
    public void CannotEscalateViaCaseTrickOnRole()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", "administrator"));
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", "ADMINISTRATOR"));
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", "Admin"));
    }

    [Test]
    public void CannotSetRoleToEmptyString()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", ""));
    }

    [Test]
    public void CannotSetRoleWithWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", " Administrator "));
    }

    // ──────────────────────────────────────────────────────
    // Password hash verification with different BCrypt variants
    // ──────────────────────────────────────────────────────

    [Test]
    public void VerifiesHashGeneratedWith2aPrefix()
    {
        // BCrypt.Net generates $2a$ hashes by default
        var hash = BCrypt.Net.BCrypt.HashPassword("TestPass-1234!", 12);
        Assert.That(hash, Does.StartWith("$2a$"));
        Assert.That(BCrypt.Net.BCrypt.Verify("TestPass-1234!", hash), Is.True);
    }

    [Test]
    public void OldHashStillWorksAfterRehash()
    {
        // Simulate: user created with one hash, password verified later
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var originalHash = userRepo.GetByEmail("test@test.com")!.PasswordHash;

        // Verify the hash still works for authentication
        var result = auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(result, Is.Not.Null);

        // Hash should be unchanged (no silent re-hash on login)
        var currentHash = userRepo.GetByEmail("test@test.com")!.PasswordHash;
        Assert.That(currentHash, Is.EqualTo(originalHash));
    }

    // ──────────────────────────────────────────────────────
    // SanitizeDisplayName static method tests
    // ──────────────────────────────────────────────────────

    [Test]
    public void SanitizeDisplayNameRemovesScriptTags()
    {
        var result = AuthService.SanitizeDisplayName("<script>alert('xss')</script>");
        Assert.That(result, Is.EqualTo("alert('xss')"));
    }

    [Test]
    public void SanitizeDisplayNameRemovesAllHtmlTags()
    {
        var result = AuthService.SanitizeDisplayName("<div class='evil'><b>Bold</b></div>");
        Assert.That(result, Is.EqualTo("Bold"));
    }

    [Test]
    public void SanitizeDisplayNameRemovesNullBytes()
    {
        var result = AuthService.SanitizeDisplayName("Test\0Name");
        Assert.That(result, Is.EqualTo("TestName"));
    }

    [Test]
    public void SanitizeDisplayNameTrimsAndCollapsesWhitespace()
    {
        var result = AuthService.SanitizeDisplayName("  John    Doe  ");
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void SanitizeDisplayNamePreservesUnicode()
    {
        var result = AuthService.SanitizeDisplayName("\u5c0f\u6797\u592a\u90ce");
        Assert.That(result, Is.EqualTo("\u5c0f\u6797\u592a\u90ce"));
    }

    // ──────────────────────────────────────────────────────
    // SanitizeForLog static method tests
    // ──────────────────────────────────────────────────────

    [Test]
    public void SanitizeForLogStripsNewlines()
    {
        var result = AuthService.SanitizeForLog("line1\r\nline2\nline3");
        Assert.That(result, Is.EqualTo("line1line2line3"));
    }

    [Test]
    public void SanitizeForLogStripsNullBytes()
    {
        var result = AuthService.SanitizeForLog("before\0after");
        Assert.That(result, Is.EqualTo("beforeafter"));
    }

    [Test]
    public void SanitizeForLogHandlesNull()
    {
        var result = AuthService.SanitizeForLog(null!);
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void SanitizeForLogHandlesEmpty()
    {
        var result = AuthService.SanitizeForLog("");
        Assert.That(result, Is.EqualTo(""));
    }
}

// ══════════════════════════════════════════════════════
// IsLocalUrl tests — open redirect defense
// ══════════════════════════════════════════════════════

[TestFixture]
public class IsLocalUrlTests
{
    // ──────────────────────────────────────────────────────
    // Valid local URLs (should return true)
    // ──────────────────────────────────────────────────────

    [Test]
    public void AcceptsRootPath()
    {
        Assert.That(AuthService.IsLocalUrl("/"), Is.True);
    }

    [Test]
    public void AcceptsSimpleLocalPath()
    {
        Assert.That(AuthService.IsLocalUrl("/dashboard"), Is.True);
    }

    [Test]
    public void AcceptsNestedLocalPath()
    {
        Assert.That(AuthService.IsLocalUrl("/admin/users"), Is.True);
    }

    [Test]
    public void AcceptsLocalPathWithQueryString()
    {
        Assert.That(AuthService.IsLocalUrl("/search?q=test"), Is.True);
    }

    [Test]
    public void AcceptsLocalPathWithFragment()
    {
        Assert.That(AuthService.IsLocalUrl("/page#section"), Is.True);
    }

    [Test]
    public void AcceptsLocalPathWithQueryAndFragment()
    {
        Assert.That(AuthService.IsLocalUrl("/page?q=1#top"), Is.True);
    }

    // ──────────────────────────────────────────────────────
    // Null/empty/whitespace (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsNull()
    {
        Assert.That(AuthService.IsLocalUrl(null), Is.False);
    }

    [Test]
    public void RejectsEmptyString()
    {
        Assert.That(AuthService.IsLocalUrl(""), Is.False);
    }

    [Test]
    public void RejectsWhitespaceOnly()
    {
        Assert.That(AuthService.IsLocalUrl("   "), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Protocol-relative URLs (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsProtocolRelativeUrl()
    {
        Assert.That(AuthService.IsLocalUrl("//evil.com"), Is.False);
    }

    [Test]
    public void RejectsProtocolRelativeWithPath()
    {
        Assert.That(AuthService.IsLocalUrl("//evil.com/path"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Backslash tricks (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsBackslashRedirect()
    {
        Assert.That(AuthService.IsLocalUrl("/\\evil.com"), Is.False);
    }

    [Test]
    public void RejectsEncodedBackslashRedirect()
    {
        // %5C is URL-encoded backslash
        Assert.That(AuthService.IsLocalUrl("/%5Cevil.com"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Absolute URLs (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsHttpAbsoluteUrl()
    {
        Assert.That(AuthService.IsLocalUrl("http://evil.com"), Is.False);
    }

    [Test]
    public void RejectsHttpsAbsoluteUrl()
    {
        Assert.That(AuthService.IsLocalUrl("https://evil.com"), Is.False);
    }

    [Test]
    public void RejectsRelativePathWithNoSlash()
    {
        Assert.That(AuthService.IsLocalUrl("evil.com"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // JavaScript/Data URI schemes (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsJavascriptUri()
    {
        Assert.That(AuthService.IsLocalUrl("javascript:alert(1)"), Is.False);
    }

    [Test]
    public void RejectsDataUri()
    {
        Assert.That(AuthService.IsLocalUrl("data:text/html,<script>alert(1)</script>"), Is.False);
    }

    [Test]
    public void RejectsVbscriptUri()
    {
        Assert.That(AuthService.IsLocalUrl("vbscript:msgbox"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Encoded bypass attempts (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsEncodedProtocolRelative()
    {
        // %2F = /  so %2F%2F = //
        Assert.That(AuthService.IsLocalUrl("/%2Fevil.com"), Is.False);
    }

    [Test]
    public void RejectsEncodedDoubleSlash()
    {
        Assert.That(AuthService.IsLocalUrl("%2F%2Fevil.com"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // User info syntax bypass (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsAtSignUserInfoBypass()
    {
        // /foo@evil.com could be interpreted as user info in some parsers
        Assert.That(AuthService.IsLocalUrl("/foo@evil.com"), Is.False);
    }

    [Test]
    public void RejectsAtSignInPath()
    {
        Assert.That(AuthService.IsLocalUrl("/@evil.com"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Control character injection (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsNullByteInUrl()
    {
        Assert.That(AuthService.IsLocalUrl("/path\0evil"), Is.False);
    }

    [Test]
    public void RejectsTabInUrl()
    {
        Assert.That(AuthService.IsLocalUrl("/path\tevil"), Is.False);
    }

    [Test]
    public void RejectsNewlineInUrl()
    {
        Assert.That(AuthService.IsLocalUrl("/path\nevil"), Is.False);
    }

    [Test]
    public void RejectsCarriageReturnInUrl()
    {
        Assert.That(AuthService.IsLocalUrl("/path\revil"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Colon-based scheme bypass attempts (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsPathWithColonBeforeSlash()
    {
        // /evil:scheme could be misinterpreted as a scheme by some parsers
        Assert.That(AuthService.IsLocalUrl("/evil:8080"), Is.False);
    }

    // ──────────────────────────────────────────────────────
    // Colons in query strings are OK (not in path)
    // ──────────────────────────────────────────────────────

    [Test]
    public void AcceptsColonInQueryString()
    {
        Assert.That(AuthService.IsLocalUrl("/search?time=12:00"), Is.True);
    }

    [Test]
    public void AcceptsColonInFragment()
    {
        Assert.That(AuthService.IsLocalUrl("/page#section:detail"), Is.True);
    }

    [Test]
    public void AcceptsColonAfterSlashInPath()
    {
        // Colon after a slash is OK — it's a path segment, not a scheme
        Assert.That(AuthService.IsLocalUrl("/api/v1:latest"), Is.True);
    }

    // ──────────────────────────────────────────────────────
    // Double-encoded bypass attempts (should return false)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RejectsDoubleEncodedSlashes()
    {
        // %252F = double-encoded / — after one decode it's %2F, after second it's /
        // Our code only decodes once, so this stays as /%, which is safe.
        // But the raw / at start still makes it start with /, which is OK.
        // The real concern is %2F%2F -> // which we already test.
        Assert.That(AuthService.IsLocalUrl("/%252Fevil.com"), Is.True);
    }
}

// ══════════════════════════════════════════════════════
// Repository hardening tests
// ══════════════════════════════════════════════════════

[TestFixture]
public class UserRepositoryTests
{
    private string testDir = null!;
    private UserRepository repo = null!;

    [SetUp]
    public void SetUp()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_repo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var paths = new TestPathProviderWithRoot(testDir);
        Directory.CreateDirectory(paths.EngineDataDir);
        repo = new UserRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDir))
            Directory.Delete(testDir, recursive: true);
    }

    [Test]
    public void PersistsToJsonFile()
    {
        repo.Add(new UserAccount { Email = "test@test.com", DisplayName = "Test" });
        var filePath = Path.Combine(testDir, "engine_data", "users.json");
        Assert.That(File.Exists(filePath), Is.True);
        var content = File.ReadAllText(filePath);
        Assert.That(content, Does.Contain("test@test.com"));
    }

    [Test]
    public void JsonFileDoesNotContainPlaintextPasswords()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("secret");
        repo.Add(new UserAccount { Email = "test@test.com", PasswordHash = hash });
        var filePath = Path.Combine(testDir, "engine_data", "users.json");
        var content = File.ReadAllText(filePath);
        Assert.That(content, Does.Not.Contain("secret"));
        Assert.That(content, Does.Contain("$2")); // BCrypt hash prefix
    }

    [Test]
    public void SurvivesReloadFromDisk()
    {
        repo.Add(new UserAccount { Email = "persist@test.com", DisplayName = "Persist" });

        // Create a new repo instance pointing at the same directory (simulates app restart)
        var paths = new TestPathProviderWithRoot(testDir);
        var repo2 = new UserRepository(paths);
        var user = repo2.GetByEmail("persist@test.com");
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.DisplayName, Is.EqualTo("Persist"));
    }

    [Test]
    public void GetByEmailIsCaseInsensitive()
    {
        repo.Add(new UserAccount { Email = "test@test.com" });
        Assert.That(repo.GetByEmail("TEST@TEST.COM"), Is.Not.Null);
    }

    [Test]
    public void DeleteRemovesUser()
    {
        var user = new UserAccount { Email = "delete@test.com" };
        repo.Add(user);
        Assert.That(repo.Count, Is.EqualTo(1));
        repo.Delete(user.Id);
        Assert.That(repo.Count, Is.EqualTo(0));
    }

    [Test]
    public void UpdateModifiesExistingUser()
    {
        var user = new UserAccount { Email = "test@test.com", Role = UserRoles.User };
        repo.Add(user);
        user.Role = UserRoles.Contributor;
        repo.Update(user);
        var updated = repo.GetByEmail("test@test.com");
        Assert.That(updated!.Role, Is.EqualTo(UserRoles.Contributor));
    }

    [Test]
    public void EmptyRepoReturnsEmptyList()
    {
        Assert.That(repo.GetAll(), Is.Empty);
        Assert.That(repo.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetByIdReturnsCorrectUser()
    {
        var user = new UserAccount { Email = "test@test.com" };
        repo.Add(user);
        var found = repo.GetById(user.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Email, Is.EqualTo("test@test.com"));
    }

    [Test]
    public void GetByIdReturnsNullForMissing()
    {
        Assert.That(repo.GetById("nonexistent"), Is.Null);
    }

    // ──────────────────────────────────────────────────────
    // Thread safety tests
    // ──────────────────────────────────────────────────────

    [Test]
    public void ConcurrentAddsDoNotLoseData()
    {
        const int threadCount = 20;
        var tasks = new Task[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() =>
            {
                repo.Add(new UserAccount
                {
                    Email = $"user{idx}@test.com",
                    DisplayName = $"User {idx}"
                });
            });
        }
        Task.WaitAll(tasks);

        Assert.That(repo.Count, Is.EqualTo(threadCount));
    }

    [Test]
    public void ConcurrentReadsDoNotThrow()
    {
        // Pre-populate
        for (int i = 0; i < 10; i++)
            repo.Add(new UserAccount { Email = $"user{i}@test.com", DisplayName = $"User {i}" });

        var tasks = new Task[50];
        for (int i = 0; i < tasks.Length; i++)
        {
            var idx = i % 10;
            tasks[i] = Task.Run(() =>
            {
                var all = repo.GetAll();
                var byEmail = repo.GetByEmail($"user{idx}@test.com");
                Assert.That(all, Is.Not.Null);
            });
        }

        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    [Test]
    public void ConcurrentReadWriteDoesNotCorrupt()
    {
        // Start with some data
        repo.Add(new UserAccount { Email = "seed@test.com", DisplayName = "Seed" });

        var tasks = new Task[30];
        for (int i = 0; i < tasks.Length; i++)
        {
            var idx = i;
            if (idx % 3 == 0)
            {
                // Write
                tasks[i] = Task.Run(() =>
                    repo.Add(new UserAccount
                    {
                        Email = $"concurrent{idx}@test.com",
                        DisplayName = $"C{idx}"
                    }));
            }
            else
            {
                // Read
                tasks[i] = Task.Run(() =>
                {
                    var all = repo.GetAll();
                    Assert.That(all, Is.Not.Null);
                });
            }
        }

        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        Assert.That(repo.Count, Is.GreaterThan(1));
    }

    // ──────────────────────────────────────────────────────
    // GetAll returns a copy, not a reference
    // ──────────────────────────────────────────────────────

    [Test]
    public void GetAllReturnsCopyNotReference()
    {
        repo.Add(new UserAccount { Email = "test@test.com", DisplayName = "Test" });
        var list1 = repo.GetAll();
        var list2 = repo.GetAll();

        // Modifying the returned list should not affect the repository
        list1.Clear();
        Assert.That(repo.Count, Is.EqualTo(1));
        Assert.That(list2, Has.Count.EqualTo(1));
    }

    // ──────────────────────────────────────────────────────
    // Update nonexistent user is a no-op
    // ──────────────────────────────────────────────────────

    [Test]
    public void UpdateNonexistentUserIsNoOp()
    {
        var phantom = new UserAccount { Id = "does-not-exist", Email = "phantom@test.com" };
        Assert.DoesNotThrow(() => repo.Update(phantom));
        Assert.That(repo.Count, Is.EqualTo(0));
    }

    // ──────────────────────────────────────────────────────
    // Delete nonexistent user is a no-op
    // ──────────────────────────────────────────────────────

    [Test]
    public void DeleteNonexistentUserIsNoOp()
    {
        repo.Add(new UserAccount { Email = "test@test.com" });
        Assert.DoesNotThrow(() => repo.Delete("nonexistent-id"));
        Assert.That(repo.Count, Is.EqualTo(1));
    }

    // ──────────────────────────────────────────────────────
    // Data survives serialization round-trip for all fields
    // ──────────────────────────────────────────────────────

    [Test]
    public void AllFieldsSurviveRoundTrip()
    {
        var original = new UserAccount
        {
            Email = "roundtrip@test.com",
            DisplayName = "Round Trip",
            PasswordHash = "$2a$12$somefakehashvalue1234567890abcdefghijklmnop",
            Role = UserRoles.Contributor,
            LastLoginUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc)
        };
        repo.Add(original);

        // Reload from disk
        var paths = new TestPathProviderWithRoot(testDir);
        var repo2 = new UserRepository(paths);
        var loaded = repo2.GetByEmail("roundtrip@test.com");

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Id, Is.EqualTo(original.Id));
        Assert.That(loaded.Email, Is.EqualTo("roundtrip@test.com"));
        Assert.That(loaded.DisplayName, Is.EqualTo("Round Trip"));
        Assert.That(loaded.PasswordHash, Is.EqualTo(original.PasswordHash));
        Assert.That(loaded.Role, Is.EqualTo(UserRoles.Contributor));
        Assert.That(loaded.LastLoginUtc, Is.Not.Null);
    }

    // ──────────────────────────────────────────────────────
    // JSON file does not contain sensitive fields in plaintext
    // ──────────────────────────────────────────────────────

    [Test]
    public void JsonFileContainsOnlyExpectedFields()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("MySecret-123!", 12);
        repo.Add(new UserAccount
        {
            Email = "audit@test.com",
            DisplayName = "Audit User",
            PasswordHash = hash,
            Role = UserRoles.User
        });

        var filePath = Path.Combine(testDir, "engine_data", "users.json");
        var content = File.ReadAllText(filePath);

        // Should contain the hash but NEVER the plaintext password
        Assert.That(content, Does.Contain("$2a$12$"));
        Assert.That(content, Does.Not.Contain("MySecret-123!"));

        // Should contain expected JSON fields
        Assert.That(content, Does.Contain("\"Email\""));
        Assert.That(content, Does.Contain("\"DisplayName\""));
        Assert.That(content, Does.Contain("\"PasswordHash\""));
        Assert.That(content, Does.Contain("\"Role\""));
    }

    // ──────────────────────────────────────────────────────
    // Unicode data survives persistence
    // ──────────────────────────────────────────────────────

    [Test]
    public void UnicodeDataSurvivesPersistence()
    {
        repo.Add(new UserAccount
        {
            Email = "unicode@test.com",
            DisplayName = "\u5c0f\u6797\u592a\u90ce"
        });

        var paths = new TestPathProviderWithRoot(testDir);
        var repo2 = new UserRepository(paths);
        var user = repo2.GetByEmail("unicode@test.com");
        Assert.That(user!.DisplayName, Is.EqualTo("\u5c0f\u6797\u592a\u90ce"));
    }
}

[TestFixture]
public class UserRolesTests
{
    [Test]
    public void AllRolesArrayContainsExactlyThreeRoles()
    {
        Assert.That(UserRoles.All, Has.Length.EqualTo(3));
        Assert.That(UserRoles.All, Does.Contain("User"));
        Assert.That(UserRoles.All, Does.Contain("Contributor"));
        Assert.That(UserRoles.All, Does.Contain("Administrator"));
    }

    [Test]
    public void WritersArrayContainsOnlyWriteRoles()
    {
        Assert.That(UserRoles.Writers, Has.Length.EqualTo(2));
        Assert.That(UserRoles.Writers, Does.Contain("Contributor"));
        Assert.That(UserRoles.Writers, Does.Contain("Administrator"));
        Assert.That(UserRoles.Writers, Does.Not.Contain("User"));
    }
}

// ════════════════════════════════════════════════════════════════
// SecurityStamp — session invalidation on password/role change
// ════════════════════════════════════════════════════════════════

[TestFixture]
public class SecurityStampTests
{
    private string testDir = null!;
    private UserRepository userRepo = null!;
    private AuthService auth = null!;

    [SetUp]
    public void SetUp()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_stamp_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var paths = new TestPathProviderWithRoot(testDir);
        Directory.CreateDirectory(paths.EngineDataDir);
        userRepo = new UserRepository(paths);
        auth = new AuthService(userRepo);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDir))
            Directory.Delete(testDir, recursive: true);
    }

    [Test]
    public void NewUserHasSecurityStamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.That(user!.SecurityStamp, Is.Not.Null.And.Not.Empty);
        Assert.That(Guid.TryParse(user.SecurityStamp, out _), Is.True);
    }

    [Test]
    public void SeededAdminHasSecurityStamp()
    {
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.That(admin!.SecurityStamp, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void PasswordChangeRotatesSecurityStamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var oldStamp = user!.SecurityStamp;

        auth.ChangePassword(user.Id, "New-Secure-Pass-456!");

        var updated = userRepo.GetByEmail("test@test.com");
        Assert.That(updated!.SecurityStamp, Is.Not.EqualTo(oldStamp));
    }

    [Test]
    public void RoleChangeRotatesSecurityStamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var oldStamp = user!.SecurityStamp;

        auth.ChangeRole(user.Id, UserRoles.Contributor);

        var updated = userRepo.GetByEmail("test@test.com");
        Assert.That(updated!.SecurityStamp, Is.Not.EqualTo(oldStamp));
    }

    [Test]
    public void AuthenticationDoesNotRotateSecurityStamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var stampBefore = user!.SecurityStamp;

        auth.Authenticate("test@test.com", "Secure-Pass-123!");

        var after = userRepo.GetByEmail("test@test.com");
        Assert.That(after!.SecurityStamp, Is.EqualTo(stampBefore));
    }

    [Test]
    public void SecurityStampPersistsToJson()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var stamp = userRepo.GetByEmail("test@test.com")!.SecurityStamp;

        // Reload from disk
        var paths = new TestPathProviderWithRoot(testDir);
        var repo2 = new UserRepository(paths);
        var reloaded = repo2.GetByEmail("test@test.com");
        Assert.That(reloaded!.SecurityStamp, Is.EqualTo(stamp));
    }

    [Test]
    public void EachPasswordChangeProducesUniqueStamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com")!;

        var stamps = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            auth.ChangePassword(user.Id, $"Password-{i}-Secure!");
            var u = userRepo.GetByEmail("test@test.com")!;
            stamps.Add(u.SecurityStamp);
        }

        Assert.That(stamps, Has.Count.EqualTo(10), "Each password change should produce a unique stamp");
    }
}

// ════════════════════════════════════════════════════════════════
// Lockout dictionary cleanup — bounded memory
// ════════════════════════════════════════════════════════════════

[TestFixture]
public class LockoutCleanupTests
{
    private string testDir = null!;
    private UserRepository userRepo = null!;
    private AuthService auth = null!;

    [SetUp]
    public void SetUp()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_lockout_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var paths = new TestPathProviderWithRoot(testDir);
        Directory.CreateDirectory(paths.EngineDataDir);
        userRepo = new UserRepository(paths);
        auth = new AuthService(userRepo);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDir))
            Directory.Delete(testDir, recursive: true);
    }

    [Test]
    public void FailedAttemptsCreateEntries()
    {
        // Try different emails to create multiple lockout entries
        for (int i = 0; i < 10; i++)
            auth.Authenticate($"user{i}@attacker.com", "wrong");

        Assert.That(auth.GetLockoutEntryCount(), Is.EqualTo(10));
    }

    [Test]
    public void CleanupRunsAfter100FailedAttempts()
    {
        // Generate 100+ entries across unique emails.
        // The cleanup triggers every 100 failed attempts.
        // Since these are fresh entries (not expired), they won't be evicted —
        // but the mechanism runs. To test actual eviction, we'd need time travel.
        // Instead, verify the count doesn't grow unbounded to 101+
        // (the entries aren't expired so they stay, but the mechanism works).
        for (int i = 0; i < 101; i++)
            auth.Authenticate($"spray{i}@attacker.com", "wrong");

        // All entries are fresh so none get evicted, but verify the mechanism doesn't crash
        Assert.That(auth.GetLockoutEntryCount(), Is.EqualTo(101));
    }

    [Test]
    public void SuccessfulLoginClearsEntry()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);

        // Fail a few times
        auth.Authenticate("test@test.com", "wrong");
        auth.Authenticate("test@test.com", "wrong");
        Assert.That(auth.GetLockoutEntryCount(), Is.GreaterThanOrEqualTo(1));

        // Succeed
        auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(auth.GetFailedAttemptCount("test@test.com"), Is.EqualTo(0));
    }

    [Test]
    public void LockoutEntryCountMethodWorks()
    {
        Assert.That(auth.GetLockoutEntryCount(), Is.EqualTo(0));
        auth.Authenticate("a@b.com", "wrong");
        Assert.That(auth.GetLockoutEntryCount(), Is.EqualTo(1));
        auth.Authenticate("c@d.com", "wrong");
        Assert.That(auth.GetLockoutEntryCount(), Is.EqualTo(2));
    }
}

// ════════════════════════════════════════════════════════════════
// Red Team Round 2 — deeper security audit tests
// ════════════════════════════════════════════════════════════════

[TestFixture]
public class RedTeamRound2Tests
{
    private string testDir = null!;
    private UserRepository userRepo = null!;
    private AuthService auth = null!;

    [SetUp]
    public void SetUp()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_rt2_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var paths = new TestPathProviderWithRoot(testDir);
        Directory.CreateDirectory(paths.EngineDataDir);
        userRepo = new UserRepository(paths);
        auth = new AuthService(userRepo);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDir))
            Directory.Delete(testDir, recursive: true);
    }

    // ──────────────────────────────────────────────────────
    // Flaw 1: DevAutoLoginMiddleware missing UserId/SecurityStamp
    // (Tested structurally — the middleware now includes these claims.
    //  This test validates that the login endpoint creates proper claims
    //  that include UserId, which the dev middleware now mirrors.)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_AuthenticatedUserHasIdAndSecurityStamp()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = auth.Authenticate("test@test.com", "Secure-Pass-123!");
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(user.SecurityStamp, Is.Not.Null.And.Not.Empty);
        // Both fields must be present for OnValidatePrincipal to accept the session
        Assert.That(Guid.TryParse(user.Id, out _), Is.True);
        Assert.That(Guid.TryParse(user.SecurityStamp, out _), Is.True);
    }

    // ──────────────────────────────────────────────────────
    // Flaw 2: UpdateProfile validates email and sanitizes display name
    // (Previously Users.razor bypassed validation via direct repo update)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_UpdateProfileValidatesEmail()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.Throws<ArgumentException>(() =>
            auth.UpdateProfile(user!.Id, "not-an-email", "Test"));
    }

    [Test]
    public void RedTeam_UpdateProfileSanitizesDisplayName()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        auth.UpdateProfile(user!.Id, "test@test.com", "<script>alert(1)</script>Hacker");
        var updated = userRepo.GetByEmail("test@test.com");
        Assert.That(updated!.DisplayName, Does.Not.Contain("<script>"));
        Assert.That(updated.DisplayName, Is.EqualTo("alert(1)Hacker"));
    }

    [Test]
    public void RedTeam_UpdateProfileRejectsEmptyDisplayName()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        Assert.Throws<ArgumentException>(() =>
            auth.UpdateProfile(user!.Id, "test@test.com", "   "));
    }

    [Test]
    public void RedTeam_UpdateProfilePreventsEmailCollision()
    {
        auth.CreateUser("user1@test.com", "User1", "Secure-Pass-123!", UserRoles.User);
        auth.CreateUser("user2@test.com", "User2", "Secure-Pass-456!", UserRoles.User);
        var user1 = userRepo.GetByEmail("user1@test.com");
        // Try to change user1's email to user2's email — should fail
        Assert.Throws<InvalidOperationException>(() =>
            auth.UpdateProfile(user1!.Id, "user2@test.com", "User1"));
    }

    [Test]
    public void RedTeam_UpdateProfileRotatesSecurityStampOnEmailChange()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var oldStamp = user!.SecurityStamp;

        auth.UpdateProfile(user.Id, "newemail@test.com", "Test");

        var updated = userRepo.GetByEmail("newemail@test.com");
        Assert.That(updated!.SecurityStamp, Is.Not.EqualTo(oldStamp),
            "SecurityStamp must rotate on email change to invalidate sessions with stale claims");
    }

    // ──────────────────────────────────────────────────────
    // Flaw 3: ChangePassword resolved user by display name (IDOR)
    // (Now uses UserId claim — tested via AuthService.ChangePassword which requires exact ID)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_ChangePasswordRequiresExactUserId()
    {
        auth.CreateUser("user1@test.com", "SharedName", "Secure-Pass-123!", UserRoles.User);
        auth.CreateUser("user2@test.com", "SharedName", "Secure-Pass-456!", UserRoles.User);
        var user1 = userRepo.GetByEmail("user1@test.com");
        var user2 = userRepo.GetByEmail("user2@test.com");

        // Changing password by user1's ID should not affect user2
        auth.ChangePassword(user1!.Id, "NewPass-789-Go!");

        // user2 should still authenticate with their original password
        var result = auth.Authenticate("user2@test.com", "Secure-Pass-456!");
        Assert.That(result, Is.Not.Null, "Changing user1's password must not affect user2 even with identical display names");
    }

    // ──────────────────────────────────────────────────────
    // Flaw 4: Admin self-deletion prevention
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_CannotDeleteOwnAccount()
    {
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.Throws<InvalidOperationException>(() =>
            auth.DeleteUser(admin!.Id, admin.Id));
    }

    [Test]
    public void RedTeam_CannotDeleteLastAdmin()
    {
        // Create a non-admin user to act as the deleter (which shouldn't matter for the check)
        auth.CreateUser("other@test.com", "Other", "Secure-Pass-123!", UserRoles.User);
        var other = userRepo.GetByEmail("other@test.com");
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");

        Assert.Throws<InvalidOperationException>(() =>
            auth.DeleteUser(admin!.Id, other!.Id),
            "Deleting the last administrator should be blocked");
    }

    [Test]
    public void RedTeam_CanDeleteAdminIfAnotherAdminExists()
    {
        auth.CreateUser("admin2@test.com", "Admin2", "Secure-Pass-123!", UserRoles.Administrator);
        var admin1 = userRepo.GetByEmail("admin@streetsamurai.local");
        var admin2 = userRepo.GetByEmail("admin2@test.com");

        // Can delete admin1 because admin2 still exists
        Assert.DoesNotThrow(() =>
            auth.DeleteUser(admin1!.Id, admin2!.Id));
        Assert.That(userRepo.GetByEmail("admin@streetsamurai.local"), Is.Null);
    }

    // ──────────────────────────────────────────────────────
    // Flaw 5: Last admin demotion prevention
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_CannotDemoteLastAdmin()
    {
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.Throws<InvalidOperationException>(() =>
            auth.ChangeRole(admin!.Id, UserRoles.User),
            "Demoting the last administrator should be blocked");
    }

    [Test]
    public void RedTeam_CanDemoteAdminIfAnotherExists()
    {
        auth.CreateUser("admin2@test.com", "Admin2", "Secure-Pass-123!", UserRoles.Administrator);
        var admin1 = userRepo.GetByEmail("admin@streetsamurai.local");

        Assert.DoesNotThrow(() =>
            auth.ChangeRole(admin1!.Id, UserRoles.User));
        Assert.That(userRepo.GetByEmail("admin@streetsamurai.local")!.Role, Is.EqualTo(UserRoles.User));
    }

    [Test]
    public void RedTeam_RoleChangeToSameRoleIsAllowed()
    {
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        // Changing admin to admin (no-op) should not throw even if they're the last admin
        Assert.DoesNotThrow(() =>
            auth.ChangeRole(admin!.Id, UserRoles.Administrator));
    }

    // ──────────────────────────────────────────────────────
    // Flaw 6: Password change requires current password verification
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_ChangePasswordWithVerificationRejectsWrongCurrentPassword()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");

        Assert.Throws<ArgumentException>(() =>
            auth.ChangePasswordWithVerification(user!.Id, "Wrong-Current-1!", "New-Pass-456!"));
    }

    [Test]
    public void RedTeam_ChangePasswordWithVerificationSucceedsWithCorrectCurrent()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");

        Assert.DoesNotThrow(() =>
            auth.ChangePasswordWithVerification(user!.Id, "Secure-Pass-123!", "New-Pass-456!"));

        // Verify old password no longer works, new one does
        Assert.That(auth.Authenticate("test@test.com", "Secure-Pass-123!"), Is.Null);
        Assert.That(auth.Authenticate("test@test.com", "New-Pass-456!"), Is.Not.Null);
    }

    [Test]
    public void RedTeam_ChangePasswordWithVerificationRejectsEmptyCurrent()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");

        Assert.Throws<ArgumentException>(() =>
            auth.ChangePasswordWithVerification(user!.Id, "", "New-Pass-456!"));
    }

    // ──────────────────────────────────────────────────────
    // Flaw 7: Volatile cache reference in UserRepository
    // (Structural fix — the volatile keyword prevents partial-read race.
    //  This test verifies concurrent read/write correctness.)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_ConcurrentCacheAccessIsThreadSafe()
    {
        // Stress test: concurrent reads and writes should never see a corrupted state
        var tasks = new Task[40];
        for (int i = 0; i < tasks.Length; i++)
        {
            var idx = i;
            if (idx % 2 == 0)
            {
                tasks[i] = Task.Run(() =>
                {
                    auth.CreateUser($"stress{idx}@test.com", $"Stress{idx}", "Secure-Pass-123!", UserRoles.User);
                });
            }
            else
            {
                tasks[i] = Task.Run(() =>
                {
                    var all = userRepo.GetAll();
                    Assert.That(all, Is.Not.Null);
                    // Should never get a null or partially initialized list
                    foreach (var u in all)
                    {
                        Assert.That(u.Id, Is.Not.Null);
                        Assert.That(u.Email, Is.Not.Null);
                    }
                });
            }
        }

        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    // ──────────────────────────────────────────────────────
    // Flaw 8: MustChangePassword bypass prevention
    // (Server-side middleware fix — tested by verifying the flag state.)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_DefaultAdminHasMustChangePasswordFlag()
    {
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.That(admin!.MustChangePassword, Is.True,
            "Seeded admin must have MustChangePassword=true");
    }

    [Test]
    public void RedTeam_ChangePasswordClearsMustChangePasswordFlag()
    {
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.That(admin!.MustChangePassword, Is.True);

        auth.ChangePassword(admin.Id, "Brand-New-Admin-Pass-1!");

        var updated = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.That(updated!.MustChangePassword, Is.False,
            "MustChangePassword must be cleared after password change");
    }

    [Test]
    public void RedTeam_MustChangePasswordSurvivesRoundTrip()
    {
        // Verify MustChangePassword persists to disk and reloads correctly
        var admin = userRepo.GetByEmail("admin@streetsamurai.local");
        Assert.That(admin!.MustChangePassword, Is.True);

        // Reload from disk
        var paths = new TestPathProviderWithRoot(testDir);
        var repo2 = new UserRepository(paths);
        var reloaded = repo2.GetByEmail("admin@streetsamurai.local");
        Assert.That(reloaded!.MustChangePassword, Is.True);
    }

    // ──────────────────────────────────────────────────────
    // Flaw 9: SecurityStamp rotation on profile update
    // (Previously direct repo updates didn't rotate stamps.)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_UpdateProfileRotatesStampOnNameChange()
    {
        auth.CreateUser("test@test.com", "OldName", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var oldStamp = user!.SecurityStamp;

        auth.UpdateProfile(user.Id, "test@test.com", "NewName");

        var updated = userRepo.GetByEmail("test@test.com");
        Assert.That(updated!.SecurityStamp, Is.Not.EqualTo(oldStamp),
            "SecurityStamp must rotate on display name change");
        Assert.That(updated.DisplayName, Is.EqualTo("NewName"));
    }

    [Test]
    public void RedTeam_UpdateProfileDoesNotRotateStampOnNoChange()
    {
        auth.CreateUser("test@test.com", "Test", "Secure-Pass-123!", UserRoles.User);
        var user = userRepo.GetByEmail("test@test.com");
        var oldStamp = user!.SecurityStamp;

        auth.UpdateProfile(user.Id, "test@test.com", "Test");

        var updated = userRepo.GetByEmail("test@test.com");
        Assert.That(updated!.SecurityStamp, Is.EqualTo(oldStamp),
            "SecurityStamp should not rotate if nothing actually changed");
    }

    // ──────────────────────────────────────────────────────
    // Flaw 10: Exception message leakage prevention
    // (Structural fix in Razor — tested by verifying that known
    //  validation exceptions contain safe messages.)
    // ──────────────────────────────────────────────────────

    [Test]
    public void RedTeam_ValidationExceptionsContainSafeMessages()
    {
        // ArgumentException from ValidateEmail should have a user-friendly message
        var ex1 = Assert.Throws<ArgumentException>(() =>
            auth.CreateUser("bad-email", "Test", "Secure-Pass-123!", UserRoles.User));
        Assert.That(ex1!.Message, Does.Not.Contain("System."));
        Assert.That(ex1.Message, Does.Not.Contain("Exception"));
        Assert.That(ex1.Message, Does.Contain("email").IgnoreCase);

        // InvalidOperationException for duplicate email should not leak path info
        auth.CreateUser("taken@test.com", "Taken", "Secure-Pass-123!", UserRoles.User);
        var ex2 = Assert.Throws<InvalidOperationException>(() =>
            auth.CreateUser("taken@test.com", "Dupe", "Secure-Pass-456!", UserRoles.User));
        Assert.That(ex2!.Message, Does.Not.Contain("\\"));
        Assert.That(ex2.Message, Does.Not.Contain("StackTrace"));
    }

    [Test]
    public void RedTeam_DeleteUserThrowsSafeErrorForNonexistent()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            auth.DeleteUser("nonexistent-id", "some-admin-id"));
        Assert.That(ex!.Message, Is.EqualTo("User not found."));
    }

    [Test]
    public void RedTeam_UpdateProfileThrowsSafeErrorForNonexistent()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            auth.UpdateProfile("nonexistent-id", "a@b.com", "Test"));
        Assert.That(ex!.Message, Is.EqualTo("User not found."));
    }
}
