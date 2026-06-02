using HomeMaids.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class LogsController : Controller
{
    private readonly IWebHostEnvironment _env;
    public LogsController(IWebHostEnvironment env) => _env = env;

    public IActionResult Index(string? file, int tail = 200, string level = "all")
    {
        var logsRoot = Path.Combine(_env.ContentRootPath, "Logs");
        var files = new List<FileInfo>();
        if (Directory.Exists(logsRoot))
        {
            files = Directory.EnumerateFiles(logsRoot, "*.log", SearchOption.AllDirectories)
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(50)
                .ToList();
        }

        var selected = file ?? files.FirstOrDefault()?.FullName;
        string[] lines = Array.Empty<string>();
        if (selected != null && System.IO.File.Exists(selected))
        {
            // Safety: only allow files under the Logs directory
            var fullPath = Path.GetFullPath(selected);
            var rootPath = Path.GetFullPath(logsRoot);
            if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    var all = new List<string>();
                    string? line;
                    while ((line = sr.ReadLine()) != null) all.Add(line);
                    lines = all.TakeLast(Math.Max(50, Math.Min(2000, tail))).ToArray();
                }
                catch (Exception ex)
                {
                    lines = new[] { $"تعذر قراءة الملف: {ex.Message}" };
                }
            }
        }

        if (!string.IsNullOrEmpty(level) && level != "all")
        {
            var token = level.ToUpperInvariant() switch
            {
                "ERROR" => new[] { "ERR", "ERROR", "FAIL", "EXCEPTION" },
                "WARN" => new[] { "WRN", "WARN" },
                "INFO" => new[] { "INF", "INFO" },
                _ => Array.Empty<string>()
            };
            if (token.Length > 0)
                lines = lines.Where(l => token.Any(t => l.Contains(t, StringComparison.OrdinalIgnoreCase))).ToArray();
        }

        ViewBag.Files = files;
        ViewBag.Selected = selected;
        ViewBag.Tail = tail;
        ViewBag.Level = level;
        return View(lines);
    }
}
