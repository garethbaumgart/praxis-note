using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PraxisNote.Application;
using PraxisNote.Application.Features.Tasks;
using PraxisNote.Infrastructure;
using PraxisNote.Infrastructure.Persistence;
using PraxisNote.Web.Auth;
using PraxisNote.Web.Endpoints;
using PraxisNote.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Cloud Run (SSL termination at load balancer)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Task settings
builder.Services.Configure<TaskSettings>(builder.Configuration.GetSection(TaskSettings.SectionName));

// HttpClient factory (used by external API services)
builder.Services.AddHttpClient();

// Add Application services (use cases)
builder.Services.AddApplication();

// Add Infrastructure services (DbContext, repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// SSE Manager for real-time notifications (singleton for connection tracking)
builder.Services.AddSingleton<NotificationSseManager>();

// Configure Data Protection to persist keys to database (survives cold starts)
builder.Services.AddDataProtection()
    .SetApplicationName("PraxisNote")
    .PersistKeysToDbContext<PraxisNoteDbContext>();

// Add CORS for development (ng serve)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Authorization
builder.Services.AddAuthorization();

// Check if mock auth should be enabled (Development only)
var enableMockAuth = builder.Environment.IsDevelopment();

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
});

// Add Google authentication only if credentials are configured
var googleAuth = builder.Configuration.GetSection("Authentication:Google");
var clientId = googleAuth["ClientId"];
var clientSecret = googleAuth["ClientSecret"];

if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = false;

        // Map the picture claim from Google's user info response
        options.ClaimActions.MapJsonKey("picture", "picture");

        // Force account selection on each login (useful after logout)
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uri = QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "select_account");
            context.Response.Redirect(uri);
            return Task.CompletedTask;
        };
    });
}

// Add mock authentication scheme (Development/E2E only)
if (enableMockAuth)
{
    authBuilder.AddMockAuthentication();
}

var app = builder.Build();

// Use forwarded headers (must be first - for Cloud Run / load balancers)
app.UseForwardedHeaders();

// Apply database migrations on startup with retry logic for Cloud Run cold starts
// Cloud Run may start the container before the database connection is fully ready
var maxRetries = 5;
var retryDelaySeconds = 3;

for (var attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PraxisNoteDbContext>();
        db.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully");
        break;
    }
    catch (Exception ex) when (attempt < maxRetries)
    {
        app.Logger.LogWarning(
            ex,
            "Database migration attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}s...",
            attempt, maxRetries, retryDelaySeconds);
        Thread.Sleep(TimeSpan.FromSeconds(retryDelaySeconds));
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration failed after {MaxRetries} attempts", maxRetries);
        throw; // Re-throw on final attempt to fail startup
    }
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

// WebSocket support (for real-time transcription proxy)
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Minimal API endpoints
app.MapGet("/api/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });
app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapCommentEndpoints();
app.MapDueDateEndpoints();
app.MapTagEndpoints();
app.MapTaskTagEndpoints();
app.MapNoteEndpoints();
app.MapNoteTagEndpoints();
app.MapMeetingEndpoints();
app.MapMeetingTagEndpoints();
app.MapNotificationEndpoints();
app.MapCalendarEndpoints();
app.MapInsightEndpoints();
app.MapSummaryEndpoints();
app.MapTranscriptionEndpoints();

// SPA fallback - serves index.html for client-side routing
if (angularAppExists)
{
    app.MapFallbackToFile("browser/index.html");
}

app.Run();
