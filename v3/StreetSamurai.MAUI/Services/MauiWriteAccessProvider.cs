using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.MAUI.Services;

/// <summary>
/// MAUI implementation of IWriteAccessProvider.
/// Always returns full access — MAUI is a local desktop app with no auth.
/// </summary>
public class MauiWriteAccessProvider : IWriteAccessProvider
{
    public bool CanWrite => true;
    public bool CanAdminister => true;
    public bool IsReadOnlyMode => false;
    public string CurrentUserName => "Local";
    public string CurrentUserRole => "Administrator";
}
