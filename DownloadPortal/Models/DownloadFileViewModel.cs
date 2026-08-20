namespace DownloadPortal.Models;

public sealed class DownloadFileViewModel
{
    public required string Name { get; init; }

    public long SizeInBytes { get; init; }

    public DateTime LastModifiedUtc { get; init; }

    public string FormattedSize => FormatSize(SizeInBytes);

    public string FormattedLastModified =>
        LastModifiedUtc
            .ToLocalTime()
            .ToString("dd.MM.yyyy HH:mm");

    private static string FormatSize(long bytes)
    {
        string[] units =
        [
            "B",
            "KB",
            "MB",
            "GB"
        ];

        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 &&
               unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}