namespace DownloadPortal.Models;

public sealed class DownloadSettings
{
    public const string SectionName = "DownloadSettings";

    public string RootPath { get; init; } = string.Empty;

    public string[] AllowedExtensions { get; init; } =
    [
      ".pdf",
      ".docx",
      ".xlsx",
      ".txt",
      ".config",
      ".xml",
      ".json",
      ".pfx",
      ".p12",
      ".key",
      ".exe",
      ".msi",
      ".zip",
      ".tar",
      ".ovpn"
    ];
}