using DownloadPortal.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace DownloadPortal.Services;

public sealed class FileDownloadService : IFileDownloadService
{
    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly HashSet<string> _allowedExtensions;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;
    private readonly ILogger<FileDownloadService> _logger;

    public FileDownloadService(
        IOptions<DownloadSettings> options,
        ILogger<FileDownloadService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.RootPath))
        {
            throw new InvalidOperationException(
                "DownloadSettings:RootPath wurde nicht konfiguriert.");
        }

        _rootPath = Path.GetFullPath(settings.RootPath);

        _rootPathWithSeparator =
            _rootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        _allowedExtensions = settings.AllowedExtensions
            .Where(extension =>
                !string.IsNullOrWhiteSpace(extension))
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_allowedExtensions.Count == 0)
        {
            throw new InvalidOperationException(
                "Es wurden keine erlaubten Dateiendungen konfiguriert.");
        }

        _contentTypeProvider =
            new FileExtensionContentTypeProvider();

        _logger = logger;
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
                .Select(CreateViewModel)
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

        if (!IsSafeFileName(fileName))
        {
            _logger.LogWarning(
                "Ein ungültiger Dateiname wurde angefordert: {FileName}",
                fileName);

            return null;
        }

        if (!HasAllowedExtension(fileName))
        {
            _logger.LogWarning(
                "Der Dateityp von {FileName} ist nicht freigegeben.",
                fileName);

            return null;
        }

        var fullPath = GetFullFilePath(fileName);

        if (fullPath is null)
        {
            _logger.LogWarning(
                "Der angeforderte Dateipfad liegt außerhalb " +
                "des Downloadordners: {FileName}",
                fileName);

            return null;
        }

        try
        {
            if (!File.Exists(fullPath))
            {
                _logger.LogInformation(
                    "Die angeforderte Datei wurde nicht gefunden: {FileName}",
                    fileName);

                return null;
            }

            var contentType = GetContentType(fullPath);

            return new DownloadFile
            {
                FullPath = fullPath,
                FileName = fileName,
                ContentType = contentType
            };
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(
                exception,
                "Kein Zugriff auf die Datei {FileName}.",
                fileName);

            return null;
        }
        catch (IOException exception)
        {
            _logger.LogError(
                exception,
                "Die Datei {FileName} konnte nicht gelesen werden.",
                fileName);

            return null;
        }
    }

    private DownloadFileViewModel CreateViewModel(string path)
    {
        var fileInfo = new FileInfo(path);

        return new DownloadFileViewModel
        {
            Name = fileInfo.Name,
            SizeInBytes = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc
        };
    }

    private bool IsAllowedFile(string path)
    {
        return HasAllowedExtension(path);
    }

    private bool HasAllowedExtension(string path)
    {
        var extension = Path.GetExtension(path);

        return !string.IsNullOrWhiteSpace(extension)
               && _allowedExtensions.Contains(extension);
    }

    private static bool IsSafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);

        return string.Equals(
            safeFileName,
            fileName,
            StringComparison.Ordinal);
    }

    private string? GetFullFilePath(string fileName)
    {
        var combinedPath = Path.Combine(
            _rootPath,
            fileName);

        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(
                _rootPathWithSeparator,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private string GetContentType(string fullPath)
    {
        if (_contentTypeProvider.TryGetContentType(
                fullPath,
                out var contentType))
        {
            return contentType;
        }

        return "application/octet-stream";
    }

    private static string NormalizeExtension(string extension)
    {
        var normalizedExtension =
            extension.Trim();

        if (!normalizedExtension.StartsWith('.'))
        {
            normalizedExtension =
                $".{normalizedExtension}";
        }

        return normalizedExtension;
    }
}