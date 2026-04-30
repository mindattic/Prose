using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Services;

/// <summary>
/// Blazor Server implementation of IWriteAccessProvider.
/// Checks both the global ReadOnly config flag and the current user's auth claims.
/// Registered as Scoped (one per Blazor circuit).
/// </summary>
public class BlazorWriteAccessProvider : IWriteAccessProvider
{
    private readonly AuthenticationStateProvider authProvider;
    private readonly ReadOnlyState readOnlyState;

    public BlazorWriteAccessProvider(AuthenticationStateProvider authProvider, ReadOnlyState readOnlyState)
    {
        this.authProvider = authProvider;
        this.readOnlyState = readOnlyState;
    }

    public bool IsReadOnlyMode => readOnlyState.IsReadOnly;

    public bool IsVisitor
    {
        get
        {
            if (readOnlyState.IsReadOnly) return true;
            var user = GetUser();
            return !user.IsInRole(UserRoles.Contributor) && !user.IsInRole(UserRoles.Administrator);
        }
    }

    public bool IsContributor
    {
        get
        {
            if (readOnlyState.IsReadOnly) return false;
            var user = GetUser();
            return user.IsInRole(UserRoles.Contributor) || user.IsInRole(UserRoles.Administrator);
        }
    }

    public bool IsAdministrator
    {
        get
        {
            if (readOnlyState.IsReadOnly) return false;
            return GetUser().IsInRole(UserRoles.Administrator);
        }
    }

    public string CurrentUserName
    {
        get
        {
            var user = GetUser();
            return user.Identity?.Name ?? "Visitor";
        }
    }

    public string CurrentUserRole
    {
        get
        {
            var user = GetUser();
            return user.FindFirst(ClaimTypes.Role)?.Value ?? "Visitor";
        }
    }

    private ClaimsPrincipal GetUser()
    {
        // GetAuthenticationStateAsync is already resolved by the time Blazor components render.
        // This synchronous access works because the auth state is cached per circuit.
        var task = authProvider.GetAuthenticationStateAsync();
        if (task.IsCompleted)
            return task.Result.User;

        // Fallback: block briefly (should not happen in practice)
        return task.GetAwaiter().GetResult().User;
    }
}
