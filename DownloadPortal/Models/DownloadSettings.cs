namespace DownloadPortal.Models;

public sealed class DownloadSettings
{
    public const string SectionName =
        "DownloadSettings";

    public string RootPath { get; init; } =
        string.Empty;

    public string[] AllowedExtensions { get; init; } =
        Array.Empty<string>();
}
