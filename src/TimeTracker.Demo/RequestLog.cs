namespace TimeTracker.Demo;

/// <summary>
/// One request/response log per circuit, shared by both tabs (scoped). Newest entry first.
/// Components subscribe to <see cref="Changed"/> and re-render.
/// </summary>
public sealed class RequestLog
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public event Action? Changed;

    public void Add(string line)
    {
        _lines.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss}  {line}");
        Changed?.Invoke();
    }

    public void Clear()
    {
        _lines.Clear();
        Changed?.Invoke();
    }
}
