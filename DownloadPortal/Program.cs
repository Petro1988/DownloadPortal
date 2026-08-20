using DownloadPortal.Models;
using DownloadPortal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services
    .AddOptions<DownloadSettings>()
    .Bind(
        builder.Configuration.GetSection(
            DownloadSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(settings.RootPath),
        "DownloadSettings:RootPath muss konfiguriert sein.")
    .Validate(
        settings =>
            settings.AllowedExtensions is { Length: > 0 },
        "Mindestens eine Dateiendung muss erlaubt sein.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    IFileDownloadService,
    FileDownloadService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();