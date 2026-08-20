using DownloadPortal.Models;
using DownloadPortal.Services;

namespace DownloadPortal.Services;

public interface IFileDownloadService
{
    IReadOnlyList<DownloadFileViewModel> GetFiles();

    DownloadFile? GetFile(string fileName);
}