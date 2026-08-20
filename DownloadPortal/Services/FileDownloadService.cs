using DownloadPortal.Models;
using DownloadPortal.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace DownloadPortal.Services;

public sealed class FileDownloadService : IFileDownloadService
{
    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly ILogger<FileDownloadService> _logger;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;
    private readonly HashSet<string> _allowedExtensions;

    public FileDownloadService(
    IConfiguration configuration,
    ILogger<FileDownloadService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        var configuredRootPath =
            configuration["DownloadSettings:RootPath"];

        if (string.IsNullOrWhiteSpace(configuredRootPath))
        {
            throw new InvalidOperationException(
                "DownloadSettings:RootPath wurde nicht konfiguriert.");
        }

        var configuredExtensions = configuration
            .GetSection("DownloadSettings:AllowedExtensions")
            .Get<string[]>();

        if (configuredExtensions is null ||
            configuredExtensions.Length == 0)
        {
            throw new InvalidOperationException(
                "Es wurden keine erlaubten Dateitypen konfiguriert.");
        }

        _allowedExtensions = new HashSet<string>(
            configuredExtensions,
            StringComparer.OrdinalIgnoreCase);

        _rootPath = Path.GetFullPath(configuredRootPath);

        _rootPathWithSeparator =
            _rootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        _logger = logger;
        _contentTypeProvider =
            new FileExtensionContentTypeProvider();
    }

    public IReadOnlyList<DownloadFileViewModel> GetFiles()
    {
        if (!Directory.Exists(_rootPath))
        {
            _logger.LogError(
                "Der Downloadordner {RootPath} wurde nicht gefunden.",
                _rootPath);

            return Array.Empty<DownloadFileViewModel>();
        }

        try
        {
            return Directory
                .EnumerateFiles(
                    _rootPath,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(IsAllowedFile)
                .Select(path =>
                {
                    var fileInfo = new FileInfo(path);

                    return new DownloadFileViewModel
                    {
                        Name = fileInfo.Name,
                        SizeInBytes = fileInfo.Length,
                        LastModifiedUtc = fileInfo.LastWriteTimeUtc
                    };
                })
                .OrderBy(
                    file => file.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(
                exception,
                "Kein Zugriff auf den Downloadordner {RootPath}.",
                _rootPath);

            return Array.Empty<DownloadFileViewModel>();
        }
        catch (IOException exception)
        {
            _logger.LogError(
                exception,
                "Der Downloadordner {RootPath} konnte nicht gelesen werden.",
                _rootPath);

            return Array.Empty<DownloadFileViewModel>();
        }
    }

    public DownloadFile? GetFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var safeFileName = Path.GetFileName(fileName);

        if (!string.Equals(
                safeFileName,
                fileName,
                StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Ein ungültiger Dateiname wurde angefordert: {FileName}",
                fileName);

            return null;
        }

        var extension = Path.GetExtension(safeFileName);

        if (!_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning(
                "Der Dateityp von {FileName} ist nicht freigegeben.",
                safeFileName);

            return null;
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(_rootPath, safeFileName));

        if (!fullPath.StartsWith(
                _rootPathWithSeparator,
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Der angeforderte Dateipfad liegt außerhalb " +
                "des Downloadordners.");

            return null;
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return null;
        }

        if (!_contentTypeProvider.TryGetContentType(
                fullPath,
                out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new DownloadFile
        {
            FullPath = fullPath,
            FileName = safeFileName,
            ContentType = contentType
        };
    }

    private bool IsAllowedFile(string path)
    {
        var extension = Path.GetExtension(path);

        return _allowedExtensions.Contains(extension);
    }
}