using Microsoft.Extensions.Logging;
using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Exceptions;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Identity;
using System.Net;
using System.Net.Http.Json;

namespace Platform.Identity.Infrastructure.Identity
{
    public sealed class IdentityClient : IIdentityClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAdminTokenProvider _tokenProvider;
        private readonly IdentityOptions _options;
        private readonly ILogger<IdentityClient> _logger;
        private string? _clientUuidCache;

        private sealed class ClientRepresentation
        {
            public string Id { get; set; } = default!;
            public string ClientId { get; set; } = default!;
        }

        public async Task<bool> IsOrganizationMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/members/{userId}";
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            using var client = CreateClient(token);
            using var response = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                token = await _tokenProvider.RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                using var retryClient = CreateClient(token);
                using var retryResponse = await retryClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (retryResponse.StatusCode == HttpStatusCode.NotFound)
                    return false;
                if (!retryResponse.IsSuccessStatusCode)
                    throw new IdentityException($"Identity request failed with status {(int)retryResponse.StatusCode}", retryResponse.StatusCode);
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            if (!response.IsSuccessStatusCode)
                throw new IdentityException($"Identity request failed with status {(int)response.StatusCode}", response.StatusCode);

            return true;
        }

        public async Task<IReadOnlyList<IdentityUser>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/users";
            return await SendAsync<IReadOnlyList<IdentityUser>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityUser>();
        }

        public async Task<IReadOnlyList<IdentityGroup>> GetOrganizationGroupsAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups";
            return await SendAsync<IReadOnlyList<IdentityGroup>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityGroup>();
        }

        public async Task SendExecuteActionsEmailAsync(string userId, IReadOnlyList<string> actions, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}/execute-actions-email";
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            using var response = await client.PutAsJsonAsync(endpoint, actions, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("Send execute-actions email failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to send account setup email in Identity", response.StatusCode);
            }
        }

        public IdentityClient(IHttpClientFactory httpClientFactory, IAdminTokenProvider tokenProvider, IdentityOptions options, ILogger<IdentityClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tokenProvider = tokenProvider;
            _options = options;
            _logger = logger;
        }

        public async Task<IReadOnlyList<IdentityUser>> GetGroupMembersAsync(string groupId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}/members";
            return await SendAsync<IReadOnlyList<IdentityUser>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityUser>();
        }

        private HttpClient CreateClient(string token)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<T?> SendAsync<T>(Func<HttpClient, Task<HttpResponseMessage>> action, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);

            var response = await action(client).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                token = await _tokenProvider.RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                client = CreateClient(token);
                response = await action(client).ConfigureAwait(false);
            }

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("Identity request failed: {Status} {Body}", response.StatusCode, text);
                throw new IdentityException($"Identity request failed with status {(int)response.StatusCode}", response.StatusCode);
            }

            if (typeof(T) == typeof(string))
            {
                var s = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return (T)(object)s!;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Users
        public async Task<string> CreateUserAsync(IdentityCreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);

            var endpoint = $"/admin/realms/{_options.Realm}/users";
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var id = location.TrimEnd('/').Split('/')[^1];
                    return id;
                }
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("CreateUser failed: {Status} {Body}", response.StatusCode, body);
            throw new IdentityException("Failed to create user in Identity", response.StatusCode);
        }

        public async Task<IdentityUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}";
            return await SendAsync<IdentityUser>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateUserAsync(string userId, IdentityUpdateUser request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}";
            using var response = await client.PutAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("UpdateUser failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to update user in Identity", response.StatusCode);
            }
        }

        public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to delete user in Identity ");
        }

        public async Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
        {
            var existing = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"User '{userId}' was not found.");

            var update = new IdentityUpdateUser
            {
                Username = existing.Username,
                Email = existing.Email,
                FirstName = existing.FirstName,
                LastName = existing.LastName,
                Enabled = enabled,
                EmailVerified = existing.EmailVerified
            };

            await UpdateUserAsync(userId, update, cancellationToken).ConfigureAwait(false);
        }

        public async Task ResetPasswordAsync(string userId, IdentityResetPassword request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}/reset-password";
            using var response = await client.PutAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("ResetPassword failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to reset password in Identity", response.StatusCode);
            }
        }

        // Organizations
        public async Task<string> CreateOrganizationAsync(IdentityCreateOrganization request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);

            var endpoint = $"/admin/realms/{_options.Realm}/organizations";
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var id = location.TrimEnd('/').Split('/')[^1];
                    return id;
                }
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Create Organization failed: {Status} {Body}", response.StatusCode, body);
            throw new IdentityException("Failed to create organization in Identity.");
        }

        public async Task<IdentityOrganization?> GetOrganizationAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}";
            return await SendAsync<IdentityOrganization>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> CreateUserAsync(CreateUserRequest request, string organizationId, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);

            var client = CreateClient(token);

            var endpoint = $"/admin/realms/{_options.Realm}/users";

            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogError("Failed to create Identity user. Status: {Status}, Body: {Body}",  response.StatusCode, body);

                throw new IdentityException(
                    $"Failed to create user in Identity: {(int)response.StatusCode} {body}",
                    response.StatusCode);
            }

            // Identity returns the created user ID in the Location header.
            var userId = GetUserIdFromLocationHeader(response);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new IdentityException("User was created successfully, but Identity did not return the created user ID.",HttpStatusCode.InternalServerError);
            }

            _logger.LogInformation("Identity user created successfully. UserId={UserId}", userId);

            // Add the newly created user to the organization.
            await AddOrganizationMemberAsync(organizationId, userId, cancellationToken);

            return userId;
        }

        public async Task AddOrganizationMemberAsync(
            string organizationId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider
                .GetTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            var client = CreateClient(token);

            var endpoint =
                $"/admin/realms/{_options.Realm}/organizations/{organizationId}/members";

            _logger.LogDebug(
                "Adding user {UserId} to organization {OrganizationId}",
                userId,
                organizationId);

            using var content = JsonContent.Create(userId);

            using var response = await client
                .PostAsync(endpoint, content, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogError(
                    "Failed to add user {UserId} to organization {OrganizationId}. Status: {Status}, Body: {Body}",
                    userId,
                    organizationId,
                    response.StatusCode,
                    body);

                throw new IdentityException(
                    $"Failed to add user to organization in Identity: {(int)response.StatusCode} {body}",
                    response.StatusCode);
            }

            _logger.LogInformation(
                "User {UserId} successfully added to organization {OrganizationId}",
                userId,
                organizationId);
        }

        private static string? GetUserIdFromLocationHeader(HttpResponseMessage response)
        {
            if (response.Headers.Location is null)
                return null;

            return response.Headers.Location.Segments.LastOrDefault(
                segment => !string.IsNullOrWhiteSpace(segment));
        }

        public async Task RemoveOrganizationMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/members/{userId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to remove organization member in Identity");
        }

        public async Task<IReadOnlyList<IdentityUser>> GetOrganizationMembersAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/members";
            return await SendAsync<IReadOnlyList<IdentityUser>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityUser>();
        }

        public async Task<int?> CountOrgAsync(CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/count";
            return await SendAsync<int?>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public async Task<long?> CountOrgMemberAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/members/count";
            return await SendAsync<long?>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<IdentityGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/groups";
            return await SendAsync<IReadOnlyList<IdentityGroup>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityGroup>();
        }

        public async Task<IReadOnlyList<IdentityOrganization>> GetOrganizationsAsync(CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations";
            return await SendAsync<IReadOnlyList<IdentityOrganization>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityOrganization>();
        }

        private async Task<string> ResolveClientUuidAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_clientUuidCache))
                return _clientUuidCache;

            var q = System.Uri.EscapeDataString(_options.ClientId ?? string.Empty);
            var endpoint = $"/admin/realms/{_options.Realm}/clients?clientId={q}";
            var clients = await SendAsync<List<ClientRepresentation>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<ClientRepresentation>();
            if (clients.Count == 0)
            {
                _logger.LogError("Identity client '{ClientId}' not found in realm {Realm}", _options.ClientId, _options.Realm);
                throw new IdentityException($"Identity client '{_options.ClientId}' not found in realm {_options.Realm}");
            }

            _clientUuidCache = clients[0].Id;
            return _clientUuidCache;
        }

        public async Task UpdateOrganizationAsync(string organizationId, IdentityCreateOrganization request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}";
            using var response = await client.PutAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("UpdateOrganization failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to update organization in Identity", response.StatusCode);
            }
        }

        public async Task DeleteOrganizationAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to delete organization in Identity");
        }

        // Roles
        public async Task<string> CreateRoleAsync(IdentityCreateRole request, CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/clients/{clientUuid}/roles";
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var id = location.TrimEnd('/').Split('/')[^1];
                    return id; // role name returned in location
                }
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("CreateRole (client) failed: {Status} {Body}", response.StatusCode, body);
            throw new IdentityException("Failed to create client role in Identity", response.StatusCode);
        }

        public async Task<IdentityRole?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/clients/{clientUuid}/roles/{roleId}";
            return await SendAsync<IdentityRole>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateRoleAsync(string roleId, IdentityUpdateRole request, CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/clients/{clientUuid}/roles/{roleId}";
            using var response = await client.PutAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("UpdateRole (client) failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to update client role in Identity", response.StatusCode);
            }
        }

        public async Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/clients/{clientUuid}/roles/{roleId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to delete client role in Identity");
        }

        public async Task<IReadOnlyList<IdentityRole>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/clients/{clientUuid}/roles";
            return await SendAsync<IReadOnlyList<IdentityRole>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityRole>();
        }

        public async Task AssignRoleToUserAsync(string userId, IdentityRole role, CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}/role-mappings/clients/{clientUuid}";
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            using var response = await client.PostAsJsonAsync(endpoint, new[] { new { id = role.Id, name = role.Name } }, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("AssignRoleToUser failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to assign client role to user in Identity", response.StatusCode);
            }
        }

        public async Task RemoveRoleFromUserAsync(string userId, IdentityRole role, CancellationToken cancellationToken = default)
        {
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/users/{userId}/role-mappings/clients/{clientUuid}";
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(client.BaseAddress!, endpoint))
            {
                Content = JsonContent.Create(new[] { new { id = role.Id, name = role.Name } })
            };
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("RemoveRoleFromUser failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to remove client role from user in Identity", response.StatusCode);
            }
        }

        // Groups
        public async Task<string> CreateGroupAsync(IdentityCreateGroup request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/groups";
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var id = location.TrimEnd('/').Split('/')[^1];
                    return id;
                }
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("CreateGroup failed: {Status} {Body}", response.StatusCode, body);
            throw new IdentityException("Failed to create group in Identity", response.StatusCode);
        }

        public async Task<IdentityGroup?> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}";
            return await SendAsync<IdentityGroup>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        // Organization-scoped groups
        public async Task<string> CreateOrganizationGroupAsync(string organizationId, IdentityCreateGroup request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups";
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var id = location.TrimEnd('/').Split('/')[^1];
                    return id;
                }
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("CreateOrganizationGroup failed: {Status} {Body}", response.StatusCode, body);
            throw new IdentityException("Failed to create organization group in Identity", response.StatusCode);
        }

        public async Task<IdentityGroup?> GetOrganizationGroupAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}";
            return await SendAsync<IdentityGroup>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateOrganizationGroupAsync(string organizationId, string groupId, IdentityUpdateGroup request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}";
            using var response = await client.PutAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("UpdateOrganizationGroup failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to update organization group in Identity", response.StatusCode);
            }
        }

        public async Task DeleteOrganizationGroupAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to delete organization group in Identity");
        }

        public async Task AddOrganizationGroupMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}/members/{userId}";
            using var response = await client.PutAsync(endpoint, content: null, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("AddOrganizationGroupMember failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to add organization group member in Identity ", response.StatusCode);
            }
        }

        public async Task<IReadOnlyList<IdentityUser>> GetOrganizationGroupMembersAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}/members";
            return await SendAsync<IReadOnlyList<IdentityUser>>(c => c.GetAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false) ?? new List<IdentityUser>();
        }

        public async Task RemoveOrganizationGroupMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}/members/{userId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to remove organization group member in Identity");
        }

        public async Task AssignRoleToOrganizationGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            var role = await GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (role is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}/role-mappings/clients/{clientUuid}";
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var roles = new[] { new { id = role.Id, name = role.Name } };
            using var response = await client.PostAsJsonAsync(endpoint, roles, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("AssignRoleToOrganizationGroup failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to assign role to organization group in Identity", response.StatusCode);
            }
        }

        public async Task RemoveRoleFromOrganizationGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            var role = await GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (role is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/organizations/{organizationId}/groups/{groupId}/role-mappings/clients/{clientUuid}";
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(client.BaseAddress!, endpoint))
            {
                Content = JsonContent.Create(new[] { new { id = role.Id, name = role.Name } })
            };
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("RemoveRoleFromOrganizationGroup failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to remove role from organization group in Identity", response.StatusCode);
            }
        }

        public async Task UpdateGroupAsync(string groupId, IdentityUpdateGroup request, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}";
            using var response = await client.PutAsJsonAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("UpdateGroup failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to update group in Identity", response.StatusCode);
            }
        }

        public async Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to delete group in Identity");
        }

        public async Task AddGroupMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}/members";
            using var response = await client.PostAsJsonAsync(endpoint, new { id = userId }, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("AddGroupMember failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to add group member in Identity", response.StatusCode);
            }
        }

        public async Task RemoveGroupMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default)
        {
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}/members/{userId}";
            var result = await SendAsync<string>(c => c.DeleteAsync(endpoint, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is null)
                throw new IdentityException("Failed to remove group member in Identity");
        }

        public async Task AssignRoleToGroupAsync(string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            var role = await GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (role is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}/role-mappings/clients/{clientUuid}";
            var roles = new[] { new { id = role.Id, name = role.Name } };
            using var response = await client.PostAsJsonAsync(endpoint, roles, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("AssignRoleToGroup (client) failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to assign client role to group in Identity", response.StatusCode);
            }
        }

        public async Task RemoveRoleFromGroupAsync(string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            var role = await GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (role is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var client = CreateClient(token);
            var clientUuid = await ResolveClientUuidAsync(cancellationToken).ConfigureAwait(false);
            var endpoint = $"/admin/realms/{_options.Realm}/groups/{groupId}/role-mappings/clients/{clientUuid}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(client.BaseAddress!, endpoint))
            {
                Content = JsonContent.Create(new[] { role })
            };

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("RemoveRoleFromGroup failed: {Status} {Body}", response.StatusCode, body);
                throw new IdentityException("Failed to remove role from group in Identity", response.StatusCode);
            }
        }
    }
}
