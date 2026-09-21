using System.Reflection;

namespace Prose.Hub.Contracts;

/// <summary>
/// The rules by which a forwarded CLI command finds the handler method it will run.
///
/// <para>This lives here, in the zero-dependency contracts assembly, for one reason: it has to be
/// callable by <c>Prose.Hub.CliDispatch</c> (which does the dispatching) and by the unit tests
/// (which prove every advertised command can be dispatched) from the same source. A test that
/// re-implements these rules is worse than no test — it goes green against its own copy while the
/// Hub answers <c>unknown_handler_class</c>, which is precisely the failure it was written to
/// prevent.</para>
///
/// <para>Why a guard is needed at all: <c>Prose.Cli</c>'s dispatch chain names its handler classes
/// as <em>string literals</em> (<c>ForwardAsync("SeedCli", args)</c>), resolved by reflection here
/// on the Hub side. Nothing checks those strings at compile time, so renaming a handler class
/// breaks exactly one command, only at runtime, and only for whoever happens to run it.</para>
/// </summary>
public static class DispatchResolution
{
    /// <summary>Where a handler parameter's value comes from when the Hub invokes it.</summary>
    public enum ParamSource
    {
        /// <summary>The Hub's <see cref="IServiceProvider"/>.</summary>
        Services,
        /// <summary>The caller's argv.</summary>
        Args,
        /// <summary>An enum the caller resolved client-side, passed as <c>ExtraParamValue</c>.</summary>
        ExtraEnum,
        /// <summary>A plain string passed as <c>ExtraParamValue</c>.</summary>
        ExtraString,
        /// <summary>
        /// Nothing matches, so the Hub passes <c>null</c>. This is never correct: it does not throw
        /// at invoke time, it surfaces later as an <see cref="ArgumentNullException"/> from inside
        /// the handler, once the returned Task is awaited. Real instance, 2026-09: handlers typing
        /// args as <c>IReadOnlyList&lt;string&gt;</c> silently bound null under a strict type-equality
        /// check and every one of them was broken until it was found by hand.
        /// </summary>
        Unbound,
    }

    /// <summary>A resolved handler method and where each of its parameters will come from.</summary>
    public sealed record CliBinding(MethodInfo Method, ParamSource[] Parameters)
    {
        /// <summary>True when some parameter would be passed null — see <see cref="ParamSource.Unbound"/>.</summary>
        public bool HasUnboundParameter => Array.IndexOf(Parameters, ParamSource.Unbound) >= 0;
    }

    /// <summary>
    /// Find a CLI handler class by simple name. Matches on <see cref="Type.Namespace"/> as well as
    /// name because the name alone ("BeatCli") is not unique across the loaded assemblies.
    /// </summary>
    public static Type? ResolveCliHandlerType(IEnumerable<Assembly> assemblies, string? handlerClass) =>
        // A request body that omits or misspells HandlerClass deserialises it as null. Returning
        // null rather than throwing lets the caller answer "unknown_handler_class" instead of
        // surfacing an unhandled 500 with a stack trace.
        string.IsNullOrWhiteSpace(handlerClass)
            ? null
            : assemblies
                // Deliberately catch-all. A partially-loadable assembly among the Hub's loaded set
                // must not take down handler resolution for every other command; the only sane
                // answer for an assembly we cannot enumerate is "no handlers in it".
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .FirstOrDefault(t => t.Name == handlerClass && t.Namespace == "Prose.Cli");

    /// <summary>
    /// Pick the method the Hub would invoke on <paramref name="type"/>, and work out where each
    /// parameter's value would come from.
    /// </summary>
    /// <param name="method">
    /// The explicit entry-point name, or null to try "RunAsync" then "Run" — the common case that
    /// the overwhelming majority of handlers use.
    /// </param>
    /// <param name="hasExtraParamValue">
    /// Whether the caller supplies the third, non-args/services argument. It changes the binding:
    /// without it, an enum or string parameter has no source and falls through to null.
    /// </param>
    /// <param name="binding">The resolved binding, or null when <paramref name="error"/> is set.</param>
    /// <param name="error">
    /// The same error code the Hub returns to the caller — <c>no_run_method</c> or
    /// <c>unsupported_signature</c> — or null on success.
    /// </param>
    /// <returns>True when a method was resolved.</returns>
    public static bool TryBindCli(
        Type type,
        string? method,
        bool hasExtraParamValue,
        out CliBinding? binding,
        out string? error)
    {
        binding = null;
        error = null;

        string[] candidateNames = string.IsNullOrWhiteSpace(method) ? ["RunAsync", "Run"] : [method];
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => candidateNames.Contains(m.Name))
            .ToList();
        if (methods.Count == 0)
        {
            error = "no_run_method";
            return false;
        }

        // Matched by parameter TYPE rather than position or count: handlers genuinely vary — some
        // take (args, services), some the reverse, some only one, one takes a third enum.
        var chosen = methods.FirstOrDefault(m => m.GetParameters().Length <= 3);
        if (chosen == null)
        {
            error = "unsupported_signature";
            return false;
        }

        var sources = Array.ConvertAll(
            chosen.GetParameters(),
            p => ClassifyParameter(p.ParameterType, hasExtraParamValue));
        binding = new CliBinding(chosen, sources);
        return true;
    }

    /// <summary>
    /// Where a single parameter's value comes from. The order of these tests is the contract — it
    /// mirrors the binding expression in <c>CliDispatch</c> exactly, and both must change together.
    /// </summary>
    public static ParamSource ClassifyParameter(Type parameterType, bool hasExtraParamValue) =>
        parameterType == typeof(IServiceProvider) ? ParamSource.Services
        // IsAssignableFrom rather than type equality: handlers that type args as
        // IReadOnlyList<string> or IEnumerable<string> must still bind to the argv array.
        : parameterType.IsAssignableFrom(typeof(string[])) ? ParamSource.Args
        : parameterType.IsEnum && hasExtraParamValue ? ParamSource.ExtraEnum
        : parameterType == typeof(string) && hasExtraParamValue ? ParamSource.ExtraString
        : ParamSource.Unbound;
}
