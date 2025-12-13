using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Filters;

/// <summary>
/// Optional authentication filter that extracts user information if a valid token is present,
/// but allows the request to proceed even without authentication.
/// Use this for endpoints that support both authenticated and anonymous access.
/// </summary>
public class OptionalAuthFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private static JsonWebKeySet? _cachedJwks;
    private static DateTime _jwksCacheExpiry = DateTime.MinValue;

    public OptionalAuthFilter(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;

        // Try to extract Authorization header - if not present, continue without auth
        if (!http.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            // No authorization header - continue as anonymous
            return await next(ctx);
        }

        string token = authHeader.ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "")
            .Trim();

        if (string.IsNullOrEmpty(token))
        {
            // Empty token - continue as anonymous
            return await next(ctx);
        }

        try
        {
            // Get Supabase URL from configuration
            var supabaseUrl = _configuration["Supabase:Url"];
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                // Configuration issue - continue as anonymous rather than failing
                return await next(ctx);
            }

            // Get JWKS (cache for 1 hour)
            if (_cachedJwks == null || DateTime.UtcNow > _jwksCacheExpiry)
            {
                var jwksUrl = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";
                _cachedJwks = await _httpClient.GetFromJsonAsync<JsonWebKeySet>(jwksUrl);
                _jwksCacheExpiry = DateTime.UtcNow.AddHours(1);
            }

            if (_cachedJwks == null)
            {
                // JWKS not available - continue as anonymous
                return await next(ctx);
            }

            // Validate the JWT token using JWKS
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                
                // RS256 public keys come from JWKS
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    _cachedJwks.Keys.Where(k => k.Kid == kid)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            
            // Extract user ID from the user_metadata claim
            var userMetadataClaim = principal.FindFirst("user_metadata")?.Value;
            string? userId = null;
            
            if (!string.IsNullOrEmpty(userMetadataClaim))
            {
                try
                {
                    var userMetadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(userMetadataClaim);
                    if (userMetadata != null && userMetadata.TryGetValue("sub", out var subValue))
                    {
                        userId = subValue?.ToString();
                    }
                }
                catch
                {
                    // If parsing fails, try to get sub from root claims as fallback
                    userId = principal.FindFirst("sub")?.Value;
                }
            }
            else
            {
                // Fallback: try to get sub from root claims
                userId = principal.FindFirst("sub")?.Value;
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Store user information in HttpContext for use in endpoints
                http.Items["user_id"] = userId;
                http.Items["user_claims"] = principal;
                
                // Optional: Extract other useful claims
                var email = principal.FindFirst("email")?.Value;
                var role = principal.FindFirst("role")?.Value;
                
                if (!string.IsNullOrEmpty(email))
                    http.Items["user_email"] = email;
                if (!string.IsNullOrEmpty(role))
                    http.Items["user_role"] = role;
            }
            // If userId is empty, just continue without setting it (anonymous request)
        }
        catch (SecurityTokenException)
        {
            // Invalid token - continue as anonymous rather than rejecting
        }
        catch (Exception)
        {
            // Any other error - continue as anonymous
        }

        // Always proceed to the endpoint (with or without user context)
        return await next(ctx);
    }
}

