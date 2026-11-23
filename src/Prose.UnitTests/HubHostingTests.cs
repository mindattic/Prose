using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MindAttic.Authentication;
using Prose.Core.Services;
using Prose.Hub.Hosting;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T1004: the Hub's hosted mode. Off by default; when on, it refuses to start half-configured and
/// lets nothing through without a signed-in, allow-listed user except the sign-in pages and /healthz.
/// </summary>
[TestFixture]
public class HubHostingTests
{
    private static readonly Func<string, string?> NoEnv = _ => null;

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            // Later values win (an override passed to Hosted() replaces its default), as configuration
            // layers do; AddInMemoryCollection itself throws on a repeated key.
            .AddInMemoryCollection(values.GroupBy(v => v.Key).Select(g => new KeyValuePair<string, string?>(g.Key, g.Last().Value)))
            .Build();

    private static HubHosting Hosted(params (string Key, string Value)[] extra) =>
        HubHosting.Read(Config([
            (HubHosting.ModeKey, "Public"),
            (HubHosting.AllowedHostsKey, "prose.example.net"),
            (HubHosting.AllowedUsersKey, "ryan"),
            (HubHosting.KeyRingPathKey, @"C:\keys"),
            .. extra]), NoEnv);

    private static ClaimsPrincipal User(string name, bool mfa)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, name), new(MaClaims.Amr, "pwd") };
        if (mfa) claims.Add(new Claim(MaClaims.Amr, "mfa"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    // ── Mode ─────────────────────────────────────────────────────────────────

    [Test]
    public void NoConfiguration_IsLocalMode() =>
        Assert.That(HubHosting.Read(Config(), NoEnv).IsPublic, Is.False);

    [Test]
    public void LocalMode_IgnoresEveryOtherHostingKey() =>
        Assert.That(HubHosting.Read(Config((HubHosting.ModeKey, "local"), (HubHosting.AllowedHostsKey, "x")), NoEnv), Is.SameAs(HubHosting.Local));

    [Test]
    public void UnknownMode_RefusesToStart() =>
        Assert.That(() => HubHosting.Read(Config((HubHosting.ModeKey, "Pubic")), NoEnv), Throws.TypeOf<HubHostingException>());

    [Test]
    public void PublicMode_WithNothingElse_RefusesToStart_AndNamesEveryMissingKey()
    {
        var ex = Assert.Throws<HubHostingException>(() => HubHosting.Read(Config((HubHosting.ModeKey, "Public")), NoEnv))!;
        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain(HubHosting.AllowedHostsKey));
            Assert.That(ex.Message, Does.Contain(HubHosting.AllowedUsersKey));
            Assert.That(ex.Message, Does.Contain(HubHosting.KeyRingPathKey));
        });
    }

    [TestCase(HubHosting.AllowedHostsKey)]
    [TestCase(HubHosting.AllowedUsersKey)]
    [TestCase(HubHosting.KeyRingPathKey)]
    public void PublicMode_MissingOneRequiredKey_RefusesToStart(string missing)
    {
        var values = new List<(string, string)>
        {
            (HubHosting.ModeKey, "Public"),
            (HubHosting.AllowedHostsKey, "prose.example.net"),
            (HubHosting.AllowedUsersKey, "ryan"),
            (HubHosting.KeyRingPathKey, @"C:\keys"),
        };
        values.RemoveAll(v => v.Item1 == missing);
        Assert.That(() => HubHosting.Read(Config([.. values]), NoEnv), Throws.TypeOf<HubHostingException>().With.Message.Contains(missing));
    }

    [TestCase("*")]
    [TestCase("https://prose.example.net")]
    [TestCase("prose.example.net:443")]
    public void AllowedHosts_MustBeBareNames(string host) =>
        Assert.That(() => Hosted((HubHosting.AllowedHostsKey, host)), Throws.TypeOf<HubHostingException>());

    [Test]
    public void PublicMode_Defaults_AreTheStrictOnes()
    {
        var h = Hosted();
        Assert.Multiple(() =>
        {
            Assert.That(h.IsPublic, Is.True);
            Assert.That(h.RequireMfa, Is.True);
            Assert.That(h.RequireHttps, Is.True);
            Assert.That(h.HstsMaxAgeDays, Is.EqualTo(365));
            Assert.That(h.AllowedOrigins, Is.EqualTo(new[] { "https://prose.example.net" }));
            Assert.That(h.InternalToken, Has.Length.EqualTo(64));
        });
    }

    [Test]
    public void ABadNumberOrFlag_RefusesToStart_InsteadOfFallingBackToTheDefault() =>
        Assert.Multiple(() =>
        {
            Assert.That(() => Hosted((HubHosting.RequireMfaKey, "nope")), Throws.TypeOf<HubHostingException>());
            Assert.That(() => Hosted((HubHosting.LoginPermitLimitKey, "0")), Throws.TypeOf<HubHostingException>());
        });

    [Test]
    public void OnAppService_TheKeyRingDefaultsToThePersistentHomeShare()
    {
        var env = new Dictionary<string, string?> { ["WEBSITE_SITE_NAME"] = "mindattic-prose", ["HOME"] = @"D:\home" };
        var h = HubHosting.Read(Config(
            (HubHosting.ModeKey, "Public"), (HubHosting.AllowedHostsKey, "prose.example.net"), (HubHosting.AllowedUsersKey, "ryan")),
            k => env.GetValueOrDefault(k));
        Assert.That(h.KeyRingPath, Is.EqualTo(Path.Combine(@"D:\home", "data", "Prose", "DataProtection-Keys")));
    }

    // ── Binding ──────────────────────────────────────────────────────────────

    [Test]
    public void Binding_ExplicitUrlsWin() =>
        Assert.That(HubHosting.ResolveBinding(Config((HubHosting.UrlsKey, "http://0.0.0.0:9000")), _ => "8080"),
            Is.EqualTo(("http://0.0.0.0:9000", (int?)9000)));

    [Test]
    public void Binding_AspNetCoreUrls_IsLeftToTheHost() =>
        Assert.That(HubHosting.ResolveBinding(Config(), k => k == "ASPNETCORE_URLS" ? "http://*:8080" : null),
            Is.EqualTo(((string?)null, (int?)8080)));

    [Test]
    public void Binding_PlatformPort_IsBoundOnEveryAddress() =>
        Assert.That(HubHosting.ResolveBinding(Config(), k => k == "PORT" ? "8181" : null),
            Is.EqualTo(("http://0.0.0.0:8181", (int?)8181)));

    [Test]
    public void Binding_NothingGiven_IsLeftToTheHost_AndNeverTheLocalLoopbackPort() =>
        Assert.That(HubHosting.ResolveBinding(Config(), NoEnv), Is.EqualTo(((string?)null, (int?)null)));

    // ── The gate ─────────────────────────────────────────────────────────────

    [TestCase("GET", "/healthz")]
    [TestCase("GET", "/login")]
    [TestCase("GET", "/LOGIN/")]
    [TestCase("GET", "/mfa")]
    [TestCase("POST", "/_ma-auth/login")]
    [TestCase("POST", "/_ma-auth/mfa-challenge")]
    public void Anonymous_MayReachOnlyTheSignInSurfaceAndHealth(string method, string path) =>
        Assert.That(HostedGate.Decide(method, path, false, Anonymous, Hosted()), Is.EqualTo(GateDecision.Allow));

    [TestCase("GET", "/")]
    [TestCase("GET", "/index.html")]
    [TestCase("GET", "/writer")]
    [TestCase("GET", "/repo/people/someone")]
    [TestCase("GET", "/texts")]
    [TestCase("GET", "/read")]
    [TestCase("GET", "/app")]
    [TestCase("GET", "/api/health")]
    [TestCase("GET", "/api/universes")]
    [TestCase("GET", "/api/agent/bootstrap")]
    [TestCase("POST", "/api/mcp-invoke")]
    [TestCase("POST", "/api/cli-invoke")]
    [TestCase("GET", "/hubs/observability")]
    [TestCase("POST", "/hubs/observability/negotiate")]
    [TestCase("GET", "/_blazor")]
    [TestCase("GET", "/_framework/blazor.web.js")]
    [TestCase("GET", "/_content/Prose.WriterUi/writer.css")]
    [TestCase("GET", "/Hub.styles.css")]
    [TestCase("GET", "/account")]
    [TestCase("GET", "/account/mfa/setup")]
    [TestCase("POST", "/_ma-auth/change-password")]
    [TestCase("POST", "/_ma-auth/reset/request")]
    [TestCase("POST", "/_ma-auth/reset/confirm")]
    [TestCase("POST", "/login")]
    [TestCase("GET", "/_ma-auth/login")]
    [TestCase("GET", "/healthz/extra")]
    [TestCase("GET", "/login/../writer")]
    public void Anonymous_IsChallengedEverywhereElse(string method, string path) =>
        Assert.That(HostedGate.Decide(method, path, false, Anonymous, Hosted()), Is.EqualTo(GateDecision.Challenge));

    [Test]
    public void TheHubApiKey_IsNotALogin()
    {
        // Decide takes no key at all: a request with a valid X-Prose-Key and no session is still anonymous.
        Assert.That(HostedGate.Decide("POST", "/api/mcp-invoke", false, Anonymous, Hosted()), Is.EqualTo(GateDecision.Challenge));
    }

    [Test]
    public void ASignedInUserNotOnTheAllowList_IsForbidden_EvenWithMfa() =>
        Assert.That(HostedGate.Decide("GET", "/writer", false, User("someone-else", mfa: true), Hosted()), Is.EqualTo(GateDecision.Forbidden));

    [Test]
    public void AnAllowedUserWithoutMfa_ReachesOnlyTheAccountPages() =>
        Assert.Multiple(() =>
        {
            var h = Hosted();
            Assert.That(HostedGate.Decide("GET", "/writer", false, User("Ryan", mfa: false), h), Is.EqualTo(GateDecision.EnrollMfa));
            Assert.That(HostedGate.Decide("GET", "/_blazor", false, User("ryan", mfa: false), h), Is.EqualTo(GateDecision.EnrollMfa));
            Assert.That(HostedGate.Decide("GET", "/account/mfa/setup", false, User("ryan", mfa: false), h), Is.EqualTo(GateDecision.Allow));
            Assert.That(HostedGate.Decide("POST", "/_ma-auth/logout", false, User("ryan", mfa: false), h), Is.EqualTo(GateDecision.Allow));
        });

    [Test]
    public void AnAllowedUserWithMfa_ReachesEverything() =>
        Assert.That(HostedGate.Decide("GET", "/writer", false, User("ryan", mfa: true), Hosted()), Is.EqualTo(GateDecision.Allow));

    [Test]
    public void WithMfaTurnedOff_APasswordSessionIsEnough() =>
        Assert.That(HostedGate.Decide("GET", "/writer", false, User("ryan", mfa: false), Hosted((HubHosting.RequireMfaKey, "false"))),
            Is.EqualTo(GateDecision.Allow));

    [Test]
    public void TheHubsOwnLoopbackCall_IsAllowed() =>
        Assert.That(HostedGate.Decide("POST", "/api/mcp-invoke", true, Anonymous, Hosted()), Is.EqualTo(GateDecision.Allow));

    [TestCase("prose.example.net", true)]
    [TestCase("PROSE.example.net", true)]
    [TestCase("evil.example.net", false)]
    [TestCase("127.0.0.1", false)]
    [TestCase("", false)]
    public void OnlyConfiguredHostsAreServed(string host, bool expected) =>
        Assert.That(HostedGate.IsHostAllowed(host, Hosted().AllowedHosts), Is.EqualTo(expected));

    [TestCase("/", true)]
    [TestCase("/writer?node=1", true)]
    [TestCase("//evil.example", false)]
    [TestCase("/\\evil.example", false)]
    [TestCase("~//evil.example", false)]
    [TestCase("https://evil.example", false)]
    [TestCase("/a\r\nb", false)]
    [TestCase("", false)]
    public void ReturnUrl_MustBeAPlainSameSitePath(string url, bool expected) =>
        Assert.That(HostedGate.IsSafeReturnUrl(url), Is.EqualTo(expected));

    // ── Idle background work ─────────────────────────────────────────────────

    [Test]
    public void HomeStats_RefreshesWhileAClientIsActive_AndStandsDownWhenIdle()
    {
        var now = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.Multiple(() =>
        {
            Assert.That(HomeStatsRefreshService.ShouldRefresh(5, 15, now.AddMinutes(-1), now), Is.True);
            Assert.That(HomeStatsRefreshService.ShouldRefresh(5, 15, now.AddMinutes(-15), now), Is.False);
            Assert.That(HomeStatsRefreshService.ShouldRefresh(5, 0, now.AddDays(-1), now), Is.True, "0 = never stand down");
            Assert.That(HomeStatsRefreshService.ShouldRefresh(0, 15, now, now), Is.False, "0 = off");
        });
    }
}
