using System.Diagnostics;
using DownloadPortal.Models;
using DownloadPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace DownloadPortal.Controllers;

public sealed class HomeController : Controller
{
    private readonly IFileDownloadService _fileDownloadService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IFileDownloadService fileDownloadService,
        ILogger<HomeController> logger)
    {
        _fileDownloadService = fileDownloadService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var viewModel = new DownloadIndexViewModel
        {
            Files = _fileDownloadService.GetFiles()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Download(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(
                "Es wurde kein Dateiname angegeben.");
        }

        var downloadFile =
            _fileDownloadService.GetFile(fileName);

        if (downloadFile is null)
        {
            _logger.LogWarning(
                "Datei {FileName} wurde nicht gefunden oder ist nicht erlaubt.",
                fileName);

            return NotFound(
                "Die angeforderte Datei wurde nicht gefunden.");
        }

        return PhysicalFile(
            downloadFile.FullPath,
            downloadFile.ContentType,
            downloadFile.FileName,
            enableRangeProcessing: true);
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
    }
}