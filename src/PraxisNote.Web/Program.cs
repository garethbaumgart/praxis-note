using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using PraxisNote.Infrastructure;
using PraxisNote.Infrastructure.Persistence;
using PraxisNote.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (DbContext, repositories, use cases)
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS for development (ng serve on port 4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Authorization
builder.Services.AddAuthorization();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "PraxisNote.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
})
.AddGoogle(options =>
{
    var googleAuth = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleAuth["ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
    options.ClientSecret = googleAuth["ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured");
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.SaveTokens = false;

    // Force account selection on each login (useful after logout)
    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        var uri = QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "select_account");
        context.Response.Redirect(uri);
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PraxisNoteDbContext>();
    db.Database.EnsureCreated();
}

// HTTPS redirection (production security)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS for development
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}

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

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Minimal API endpoints
app.MapGet("/api/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });
app.MapAuthEndpoints();

// SPA fallback - serves index.html for client-side routing
if (angularAppExists)
{
    app.MapFallbackToFile("browser/index.html");
}

app.Run();
