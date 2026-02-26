using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Download;
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

    public async Task<DriveFileListResult> ListFilesAsync(
        string accessToken,
        string folderId,
        DateTimeOffset? modifiedAfter,
        string? pageToken,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);

        var request = service.Files.List();

        // Build query: files in folder, not trashed, supported types only
        var supportedTypes = new[]
        {
            "text/plain",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.google-apps.document"
        };

        var typeFilter = string.Join(" or ", supportedTypes.Select(t => $"mimeType='{t}'"));
        var query = $"'{folderId}' in parents and trashed=false and ({typeFilter})";

        if (modifiedAfter.HasValue)
        {
            var modifiedAfterUtc = modifiedAfter.Value.ToUniversalTime();
            query += $" and modifiedTime > '{modifiedAfterUtc:yyyy-MM-dd'T'HH:mm:ss'Z'}'";
        }

        request.Q = query;
        request.Fields = "nextPageToken, files(id, name, mimeType, modifiedTime)";
        request.PageSize = 100;
        request.OrderBy = "modifiedTime desc";

        if (!string.IsNullOrEmpty(pageToken))
            request.PageToken = pageToken;

        var result = await request.ExecuteAsync(cancellationToken);

        var files = result.Files?.Select(f => new DriveFile(
            f.Id,
            f.Name,
            f.MimeType,
            f.ModifiedTimeDateTimeOffset
        )).ToList() ?? [];

        _logger.LogInformation("Listed {Count} files in Drive folder {FolderId}", files.Count, folderId);

        return new DriveFileListResult(files, result.NextPageToken);
    }

    public async Task<Stream> DownloadFileAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);
        var request = service.Files.Get(fileId);
        var stream = new MemoryStream();
        var progress = await request.DownloadAsync(stream, cancellationToken);
        if (progress.Status != DownloadStatus.Completed)
            throw new InvalidOperationException(
                $"Drive file download failed for '{fileId}'. Status: {progress.Status}");
        stream.Position = 0;
        return stream;
    }

    public async Task<string> ExportGoogleDocAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);
        var request = service.Files.Export(fileId, "text/plain");
        var stream = new MemoryStream();
        var progress = await request.DownloadAsync(stream, cancellationToken);
        if (progress.Status != DownloadStatus.Completed)
            throw new InvalidOperationException(
                $"Drive Google Doc export failed for '{fileId}'. Status: {progress.Status}");
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
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
