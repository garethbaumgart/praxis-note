using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Drive.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class GoogleDriveService : IDriveService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleDriveService> _logger;

    public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DriveFolder>> ListFoldersAsync(
        string accessToken,
        string? searchQuery,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);

        var query = "mimeType='application/vnd.google-apps.folder' and trashed=false";
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Escape backslashes and single quotes in user input per Google Drive query requirements
            var sanitized = searchQuery.Replace("\\", "\\\\").Replace("'", "\\'");
            query += $" and name contains '{sanitized}'";
        }

        var request = service.Files.List();
        request.Q = query;
        request.Fields = "files(id, name, modifiedTime)";
        request.OrderBy = "modifiedTime desc";
        request.PageSize = 50;

        var response = await request.ExecuteAsync(cancellationToken);

        var result = new List<DriveFolder>();
        if (response.Files is null) return result;

        foreach (var file in response.Files)
        {
            result.Add(new DriveFolder(
                Id: file.Id,
                Name: file.Name,
                ModifiedTime: file.ModifiedTimeDateTimeOffset
            ));
        }

        _logger.LogInformation("Listed {Count} Drive folders{Search}",
            result.Count,
            string.IsNullOrWhiteSpace(searchQuery) ? "" : $" matching '{searchQuery}'");

        return result;
    }

    public async Task<TokenRefreshResult> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google OAuth ClientId not configured");
        var clientSecret = _configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google OAuth ClientSecret not configured");

        using var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        });

        var tokenResponse = await flow.RefreshTokenAsync("user", refreshToken, cancellationToken);

        _logger.LogInformation("Successfully refreshed Google Drive access token");

        return new TokenRefreshResult(
            AccessToken: tokenResponse.AccessToken,
            ExpiresAt: DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600),
            RefreshToken: tokenResponse.RefreshToken
        );
    }

    private DriveService CreateDriveService(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PraxisNote"
        });
    }
}
