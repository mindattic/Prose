namespace Prose.Core.Services;

/// <summary>
/// Simple event bus for triggering the global search overlay from anywhere
/// (e.g. NavMenu button, keyboard shortcut) without requiring a component reference.
/// </summary>
public class SearchTriggerService
{
    public event Func<Task>? OnOpenRequested;

    public async Task RequestOpenAsync()
    {
        if (OnOpenRequested != null)
            await OnOpenRequested.Invoke();
    }
}
