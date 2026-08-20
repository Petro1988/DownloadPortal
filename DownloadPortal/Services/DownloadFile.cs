namespace DownloadPortal.Services;

public sealed class DownloadFile
{
    public required string FullPath { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}