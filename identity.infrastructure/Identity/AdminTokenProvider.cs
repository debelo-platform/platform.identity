using Microsoft.Extensions.Logging;
using Platform.Identity.Application.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Platform.Identity.Infrastructure.Identity;

public sealed class AdminTokenProvider : IAdminTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IdentityOptions _options;
    private readonly ILogger<AdminTokenProvider> _logger;

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AdminTokenProvider(IHttpClientFactory httpClientFactory, IdentityOptions options, ILogger<AdminTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) &&
                DateTimeOffset.UtcNow < _expiresAt)
            {
                return _cachedToken;
            }

            return await RequestAdminTokenAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) &&
                DateTimeOffset.UtcNow < _expiresAt)
            {
                return _cachedToken;
            }

            return await RequestAdminTokenAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> RequestAdminTokenAsync(CancellationToken cancellationToken)
    {
        var tokenUrl = BuildTokenUrl();

        ValidateAdminCredentials();

        var requestContent = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", _options.AdminClientId!),
                new KeyValuePair<string, string>("username", _options.AdminUsername!),
                new KeyValuePair<string, string>("password", _options.AdminPassword!)
            });

        var client = _httpClientFactory.CreateClient();

        using var response = await client.PostAsync(tokenUrl, requestContent, cancellationToken).ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to obtain admin token from dentity. Status: {StatusCode}.", response.StatusCode);

            throw new IdentityException($"Unable to obtain Identity admin token. Status: {response.StatusCode}.Response: {responseContent}");
        }

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, serializerOptions);

        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new IdentityException("Received empty token from Identity.");
        }

        var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;

        _cachedToken = tokenResponse.AccessToken;

        // Refresh 30 seconds before actual expiration.
        var cacheLifetime = Math.Max(expiresIn - 30, 1);

        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(cacheLifetime);

        return _cachedToken;
    }

    private string BuildTokenUrl()
    {
        var baseUrl = _options.BaseUrl?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new IdentityException("Identity BaseUrl is not configured.");
        }

        if (baseUrl.Contains("/realms/", StringComparison.OrdinalIgnoreCase))
        {
            return $"{baseUrl}/protocol/openid-connect/token";
        }

        if (string.IsNullOrWhiteSpace(_options.AdminRealm))
        {
            throw new IdentityException("Identity AdminRealm is not configured.");
        }

        return $"{baseUrl}/realms/{_options.AdminRealm}/protocol/openid-connect/token";
    }

    private void ValidateAdminCredentials()
    {
        if (string.IsNullOrWhiteSpace(_options.AdminClientId))
        {
            _logger.LogError("Identity AdminClientId is not configured.");

            throw new IdentityException("Identity admin client ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.AdminUsername))
        {
            _logger.LogError("Identity AdminUsername is not configured.");

            throw new IdentityException("Identity admin username is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            _logger.LogError("Identity AdminPassword is not configured.");

            throw new IdentityException("Identity admin password is not configured.");
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}