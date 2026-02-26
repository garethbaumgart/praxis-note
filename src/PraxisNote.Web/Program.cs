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
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ModelContextProtocol.AspNetCore;
using PraxisNote.Web.Auth;
using PraxisNote.Web.Endpoints;
using PraxisNote.Web.Middleware;
using PraxisNote.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Cloud Run (SSL termination at load balancer)
// Cloud Run uses dynamic IPs so KnownProxies/KnownNetworks must be cleared per Google's guidance.
// ForwardLimit = 1 restricts processing to the single Cloud Run proxy hop.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = 1;
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

// HttpContextAccessor (needed by MCP tools and API key auth)
builder.Services.AddHttpContextAccessor();

// SSE Manager for real-time notifications (singleton for connection tracking)
builder.Services.AddSingleton<NotificationSseManager>();

// Background service for periodic Drive folder sync
builder.Services.AddHostedService<DriveSyncBackgroundJob>();

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
    options.Cookie.Name = "__session";
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

    // Forward to mock auth or API key auth based on request headers
    options.ForwardDefaultSelector = context =>
    {
        if (enableMockAuth && context.Request.Headers.ContainsKey(MockAuthenticationOptions.HeaderName))
            return MockAuthenticationOptions.SchemeName;

        if (context.Request.Path.StartsWithSegments("/mcp"))
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer pn_", StringComparison.Ordinal))
                return ApiKeyAuthenticationOptions.SchemeName;
        }

        return null; // Use default (cookie) scheme
    };
});

// Add Google authentication only if credentials are configured
var googleAuth = builder.Configuration.GetSection("Authentication:Google");
var clientId = googleAuth["ClientId"];
var clientSecret = googleAuth["ClientSecret"];

if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
{
    authBuilder.AddOAuth<GoogleOptions, FirebaseProxyGoogleHandler>(
        GoogleDefaults.AuthenticationScheme,
        GoogleDefaults.DisplayName,
        options =>
    {
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = false;

        // Map the picture claim from Google's user info response
        options.ClaimActions.MapJsonKey("picture", "picture");

        // Correlation cookie settings removed — FirebaseProxyGoogleHandler
        // stores the correlation ID in the encrypted state parameter instead,
        // bypassing the cookie that Firebase Hosting would strip.

        // Force account selection on each login (useful after logout)
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uri = QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "select_account");
            context.Response.Redirect(uri);
            return Task.CompletedTask;
        };

        // Handle remote authentication failures gracefully instead of returning 500.
        // Distinguishes user cancellation (access_denied) from real errors.
        options.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("GoogleAuth");

            var errorCode = context.HttpContext.Request.Query["error"].FirstOrDefault();
            if (string.Equals(errorCode, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                // User cancelled the login — redirect home silently
                logger.LogInformation("User cancelled Google login");
                context.Response.Redirect("/");
            }
            else
            {
                var message = context.Failure?.Message ?? "Unknown";
                var isRateLimited = message.Contains("429", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("rate-limited", StringComparison.OrdinalIgnoreCase);

                if (isRateLimited)
                {
                    logger.LogWarning(context.Failure,
                        "Google OAuth rate limited (429). User should wait before retrying. Message: {Message}",
                        message);
                    context.Response.Redirect("/?error=rate_limited");
                }
                else
                {
                    logger.LogWarning(context.Failure, "Google OAuth remote failure: {Message}", message);
                    context.Response.Redirect("/?error=auth_failed");
                }
            }

            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

// Add mock authentication scheme (Development/E2E only)
if (enableMockAuth)
{
    authBuilder.AddMockAuthentication();
}

// Add API key authentication (always available)
authBuilder.AddApiKeyAuthentication();

// Rate limiting for MCP endpoint (per API key)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("mcp", context =>
    {
        var apiKeyId = context.User?.FindFirst("api_key_id")?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(apiKeyId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
        });
    });
});

// MCP Server for OpenClaw and other MCP clients
builder.Services.AddScoped<PraxisNote.Web.Mcp.McpUserContext>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

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

        // InMemory provider doesn't support migrations — use EnsureCreated instead
        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            db.Database.EnsureCreated();
            app.Logger.LogInformation("In-memory database created successfully");
            break;
        }

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
var webRootPath = app.Environment.WebRootPath;
var angularAppExists = false;
var browserPath = string.Empty;

if (!string.IsNullOrEmpty(webRootPath))
{
    browserPath = Path.Combine(webRootPath, "browser");
    angularAppExists = Directory.Exists(browserPath);
}

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
    KeepAliveInterval = TimeSpan.FromSeconds(20)
});

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting middleware (after auth so context.User is populated for per-key partitioning)
app.UseRateLimiter();

// Profile validation middleware (extracts X-Profile-Id header, falls back to default profile)
app.UseMiddleware<ProfileValidationMiddleware>();

// Minimal API endpoints
app.MapGet("/api/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });
app.MapAuthEndpoints();
app.MapAccountLinkEndpoints();
app.MapProfileEndpoints();
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
app.MapDriveEndpoints();
app.MapJiraEndpoints();
app.MapInsightEndpoints();
app.MapActionItemEndpoints();
app.MapTranscriptionEndpoints();
app.MapApiKeyEndpoints();

// MCP endpoint for OpenClaw and other MCP clients
app.MapMcp("/mcp").RequireAuthorization().RequireRateLimiting("mcp");

// SPA fallback - serves index.html for client-side routing
if (angularAppExists)
{
    app.MapFallbackToFile("browser/index.html");
}

app.Run();
