using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PraxisNote.Application;
using PraxisNote.Infrastructure;
using PraxisNote.Infrastructure.Persistence;
using PraxisNote.Web.Auth;
using PraxisNote.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Cloud Run (SSL termination at load balancer)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Application services (use cases)
builder.Services.AddApplication();

// Add Infrastructure services (DbContext, repositories)
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

// Check if mock auth should be enabled (Development or E2E only)
var enableMockAuth = builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "E2E";

// Add Authentication
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };

    // Forward to mock auth if header is present (Dev/E2E only)
    if (enableMockAuth)
    {
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.ContainsKey(MockAuthenticationOptions.HeaderName))
            {
                return MockAuthenticationOptions.SchemeName;
            }
            return null; // Use default (cookie) scheme
        };
    }
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

// Add mock authentication scheme (Development/E2E only)
if (enableMockAuth)
{
    authBuilder.AddMockAuthentication();
}

var app = builder.Build();

// Use forwarded headers (must be first - for Cloud Run / load balancers)
app.UseForwardedHeaders();

// Apply database migrations (Development and E2E only)
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "E2E")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PraxisNoteDbContext>();
    db.Database.Migrate();
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
app.MapTaskEndpoints();

// SPA fallback - serves index.html for client-side routing
if (angularAppExists)
{
    app.MapFallbackToFile("browser/index.html");
}

app.Run();
