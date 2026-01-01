using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Serve static files from wwwroot (for favicon, etc.)
app.UseStaticFiles();

// Serve static files from wwwroot/browser (Angular 21 output)
var browserPath = Path.Combine(app.Environment.WebRootPath, "browser");
var angularAppExists = Directory.Exists(browserPath);

if (angularAppExists)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(browserPath)
    });
}
else
{
    app.Logger.LogWarning(
        "Angular app not found at '{BrowserPath}'. Run 'npm run build' in ClientApp first.",
        browserPath);
}

// Minimal API endpoint
app.MapGet("/api/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// SPA fallback - serves index.html for client-side routing
if (angularAppExists)
{
    app.MapFallbackToFile("browser/index.html");
}

app.Run();
