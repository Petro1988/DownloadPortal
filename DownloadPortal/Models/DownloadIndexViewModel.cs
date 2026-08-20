namespace DownloadPortal.Models;

public sealed class DownloadIndexViewModel
{
    public required IReadOnlyList<DownloadFileViewModel> Files
    {
        get;
        init;
    }

    public bool HasFiles => Files.Count > 0;

    public string? ErrorMessage { get; init; }
}