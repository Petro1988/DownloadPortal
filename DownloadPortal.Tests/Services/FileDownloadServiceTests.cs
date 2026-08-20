using DownloadPortal.Models;
using DownloadPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DownloadPortal.Tests.Services;

public sealed class FileDownloadServiceTests : IDisposable
{
    private readonly string _testDirectory;

    public FileDownloadServiceTests()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "DownloadPortalTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void GetFiles_ReturnsAllowedFiles()
    {
        CreateTestFile(
            "OpenVPN-Installer.msi",
            "MSI test content");

        CreateTestFile(
            "Anleitung.pdf",
            "PDF test content");

        var service = CreateService();

        var files = service.GetFiles();

        Assert.Equal(2, files.Count);

        Assert.Contains(
            files,
            file => file.Name == "OpenVPN-Installer.msi");

        Assert.Contains(
            files,
            file => file.Name == "Anleitung.pdf");
    }

    [Fact]
    public void GetFiles_ExcludesDisallowedExtensions()
    {
        CreateTestFile(
            "OpenVPN-Installer.msi",
            "Allowed content");

        CreateTestFile(
            "Geheimdatei.txt",
            "Disallowed content");

        var service = CreateService(
            allowedExtensions:
            [
                ".msi"
            ]);

        var files = service.GetFiles();

        var file = Assert.Single(files);

        Assert.Equal(
            "OpenVPN-Installer.msi",
            file.Name);

        Assert.DoesNotContain(
            files,
            item => item.Name == "Geheimdatei.txt");
    }

    [Fact]
    public void GetFiles_IgnoresFilesInSubdirectories()
    {
        var subdirectory = Path.Combine(
            _testDirectory,
            "Unterordner");

        Directory.CreateDirectory(subdirectory);

        File.WriteAllText(
            Path.Combine(
                subdirectory,
                "Unterordner-Datei.msi"),
            "Test content");

        CreateTestFile(
            "Direkte-Datei.msi",
            "Direct content");

        var service = CreateService();

        var files = service.GetFiles();

        Assert.Single(files);

        Assert.Equal(
            "Direkte-Datei.msi",
            files[0].Name);
    }

    [Fact]
    public void GetFiles_ReturnsEmptyList_WhenDirectoryDoesNotExist()
    {
        var missingDirectory = Path.Combine(
            _testDirectory,
            "NichtVorhanden");

        var service = CreateService(
            rootPath: missingDirectory);

        var files = service.GetFiles();

        Assert.Empty(files);
    }

    [Fact]
    public void GetFile_ReturnsFile_WhenFileExistsAndIsAllowed()
    {
        CreateTestFile(
            "OpenVPN-Installer.msi",
            "Installer content");

        var service = CreateService();

        var result = service.GetFile(
            "OpenVPN-Installer.msi");

        Assert.NotNull(result);

        Assert.Equal(
            "OpenVPN-Installer.msi",
            result.FileName);

        Assert.Equal(
            Path.Combine(
                _testDirectory,
                "OpenVPN-Installer.msi"),
            result.FullPath);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ContentType));
    }

    [Fact]
    public void GetFile_ReturnsNull_WhenFileDoesNotExist()
    {
        var service = CreateService();

        var result = service.GetFile(
            "NichtVorhanden.msi");

        Assert.Null(result);
    }

    [Fact]
    public void GetFile_ReturnsNull_WhenExtensionIsNotAllowed()
    {
        CreateTestFile(
            "NichtErlaubt.txt",
            "Text content");

        var service = CreateService(
            allowedExtensions:
            [
                ".msi"
            ]);

        var result = service.GetFile(
            "NichtErlaubt.txt");

        Assert.Null(result);
    }

    [Fact]
    public void GetFile_ReturnsNull_WhenFileNameIsEmpty()
    {
        var service = CreateService();

        var result = service.GetFile(
            string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void GetFile_ReturnsNull_ForParentDirectoryTraversal()
    {
        var service = CreateService();

        var result = service.GetFile(
            @"..\..\Windows\win.ini");

        Assert.Null(result);
    }

    [Fact]
    public void GetFile_ReturnsNull_ForForwardSlashTraversal()
    {
        var service = CreateService();

        var result = service.GetFile(
            "../../Windows/win.ini");

        Assert.Null(result);
    }

    [Fact]
    public void GetFile_ReturnsNull_ForAbsoluteExternalPath()
    {
        var externalFilePath = Path.Combine(
            Path.GetTempPath(),
            $"External-{Guid.NewGuid():N}.msi");

        try
        {
            File.WriteAllText(
                externalFilePath,
                "External content");

            var service = CreateService();

            var result = service.GetFile(
                externalFilePath);

            Assert.Null(result);
        }
        finally
        {
            if (File.Exists(externalFilePath))
            {
                File.Delete(externalFilePath);
            }
        }
    }

    [Fact]
    public void GetFile_AcceptsExtensionCaseInsensitively()
    {
        CreateTestFile(
            "OpenVPN-Installer.MSI",
            "Installer content");

        var service = CreateService();

        var result = service.GetFile(
            "OpenVPN-Installer.MSI");

        Assert.NotNull(result);

        Assert.Equal(
            "OpenVPN-Installer.MSI",
            result.FileName);
    }

    [Fact]
    public void GetFiles_ReturnsFilesAlphabetically()
    {
        CreateTestFile(
            "Zulu.zip",
            "Zulu content");

        CreateTestFile(
            "Alpha.msi",
            "Alpha content");

        CreateTestFile(
            "Beta.pdf",
            "Beta content");

        var service = CreateService();

        var files = service.GetFiles();

        Assert.Equal(
            [
                "Alpha.msi",
                "Beta.pdf",
                "Zulu.zip"
            ],
            files.Select(
                file => file.Name));
    }

    [Fact]
    public void GetFiles_ReturnsCorrectFileSize()
    {
        const string content =
            "1234567890";

        CreateTestFile(
            "Groesse.zip",
            content);

        var service = CreateService();

        var files = service.GetFiles();

        var file = Assert.Single(files);

        Assert.Equal(
            content.Length,
            file.SizeInBytes);
    }

    [Fact]
    public void Constructor_NormalizesExtensionWithoutLeadingDot()
    {
        CreateTestFile(
            "OpenVPN-Installer.msi",
            "Installer content");

        var service = CreateService(
            allowedExtensions:
            [
                "msi"
            ]);

        var files = service.GetFiles();

        Assert.Single(files);

        Assert.Equal(
            "OpenVPN-Installer.msi",
            files[0].Name);
    }

    [Fact]
    public void Constructor_Throws_WhenRootPathIsEmpty()
    {
        var action = () => CreateService(
            rootPath: string.Empty);

        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Contains(
            "RootPath",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_Throws_WhenNoExtensionsAreConfigured()
    {
        var action = () => CreateService(
            allowedExtensions:
            []);

        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Contains(
            "Dateiendungen",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(
                _testDirectory,
                recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private FileDownloadService CreateService(
        string? rootPath = null,
        string[]? allowedExtensions = null)
    {
        var settings = new DownloadSettings
        {
            RootPath = rootPath ?? _testDirectory,

            AllowedExtensions =
                allowedExtensions ??
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
                ]
        };

        var options = Options.Create(settings);

        return new FileDownloadService(
            options,
            NullLogger<FileDownloadService>.Instance);
    }

    private string CreateTestFile(
        string fileName,
        string content)
    {
        var fullPath = Path.Combine(
            _testDirectory,
            fileName);

        File.WriteAllText(
            fullPath,
            content);

        return fullPath;
    }
}