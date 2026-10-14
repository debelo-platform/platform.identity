using identity.api.Middleware;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.OrganizationManagement;
using Platform.Identity.Application.PlatformManagement;
using Platform.Identity.Infrastructure.Identity;
using identity.infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var environment = builder.Environment;

var identitySection = configuration.GetSection(IdentityOptions.SectionName);
var identityOptions = identitySection.Get<IdentityOptions>() ?? new IdentityOptions();

var realm = identityOptions.Realm;

// Internal URL: reachable by the API container over the Docker network.
var internalIssuer =
    $"{identityOptions.BaseUrl.TrimEnd('/')}/realms/{realm}";

// Public URL: matches the issuer returned in the JWT by Keycloak.
var publicKeycloakUrl = (
    configuration["IdentityProvider:SwaggerAuthority"]
    ?? "http://localhost:8081").TrimEnd('/');

var publicIssuer = $"{publicKeycloakUrl}/realms/{realm}";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

ConfigureSwagger(
    builder.Services,
    identityOptions,
    environment,
    configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        // Retrieves OIDC metadata and signing keys from Keycloak.
        options.Authority = internalIssuer;
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = publicIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddOptions<IdentityOptions>()
    .Bind(identitySection)
    .ValidateDataAnnotations()
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.AdminUsername) &&
            !string.IsNullOrWhiteSpace(options.AdminPassword),
        "Identity admin username and password are required.")
    .ValidateOnStart();

builder.Services.AddSingleton(
    serviceProvider =>
        serviceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value);

builder.Services.AddHttpClient();

builder.Services.AddScoped<IAdminTokenProvider, AdminTokenProvider>();
builder.Services.AddScoped<IIdentityClient, IdentityClient>();
builder.Services.AddScoped<IUserAccountActions, UserAccountActions>();

// Application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IGroupService, GroupService>();

// Organization-scoped services
builder.Services.AddScoped<IOrganizationRoleNameBuilder, OrganizationRoleNameBuilder>();
builder.Services.AddScoped<IOrganizationRoleService, OrganizationRoleService>();
builder.Services.AddScoped<IOrganizationGroupService, OrganizationGroupService>();
builder.Services.AddScoped<IOrganizationOnboardingService, OrganizationOnboardingService>();
builder.Services.AddScoped<IOrganizationUserService, OrganizationUserService>();
builder.Services.AddOptions<CorsOptions>().Bind(configuration.GetSection("Cors")).Validate(options =>
            options.AllowedOrigins.Length > 0 &&
            options.AllowedOrigins.All(
                origin => Uri.TryCreate(
                    origin,
                    UriKind.Absolute,
                    out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp ||
                     uri.Scheme == Uri.UriSchemeHttps) &&
                    string.IsNullOrEmpty(uri.PathAndQuery.Trim('/')) &&
                    string.IsNullOrEmpty(uri.Fragment)),
        "CORS must contain valid allowed origins.")
    .ValidateOnStart();

var corsOptions = configuration
    .GetSection("Cors")
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DebeloCors", policy =>
    {
        policy
            .WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (corsOptions.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});


builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Platform.Identity API v1");

        options.OAuthClientId(
            configuration["IdentityProvider:SwaggerClientId"] ?? "debelo");

        options.OAuthClientSecret(
            configuration["IdentityProvider:SwaggerClientSecret"] ?? string.Empty);

        options.OAuthUsePkce();

        options.OAuthScopes(
            "openid",
            "email",
            "profile",
            "organization");

        var redirectUrl =
            configuration["IdentityProvider:SwaggerRedirectUrl"];

        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            options.OAuth2RedirectUrl(redirectUrl);
        }
    });
}

app.UseMiddleware<GlobalExceptionHandler>();

app.UseRouting();

app.UseCors("DebeloCors");

app.UseAuthentication();

app.UseMiddleware<OrganizationMembershipMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static void ConfigureSwagger(IServiceCollection services, IdentityOptions identityOptions, IHostEnvironment environment, IConfiguration configuration)
{
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Platform.Identity API",
            Version = "v1"
        });

        if (!environment.IsDevelopment())
        {
            return;
        }

        var authority = (configuration["IdentityProvider:SwaggerAuthority"] ?? "http://localhost:8081").TrimEnd('/');

        var realm = identityOptions.Realm;

        options.AddSecurityDefinition(
            "oauth2",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(
                            $"{authority}/realms/{realm}/protocol/openid-connect/auth"),

                        TokenUrl = new Uri(
                            $"{authority}/realms/{realm}/protocol/openid-connect/token"),

                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "OpenID Connect authentication",
                            ["email"] = "Access email claims",
                            ["profile"] = "Access profile claims",
                            ["organization"] = "Organization access"
                        }
                    }
                }
            });

        options.AddSecurityRequirement(document =>
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference("oauth2", document)
                ] = new List<string>
                {
                    "openid",
                    "email",
                    "profile",
                    "organization"
                }
            });
    });
}