using Supabase;

public class SupabaseAuthFilter : IEndpointFilter
{
    private readonly Client _supabase;

    public SupabaseAuthFilter(Client supabase)
    {
        _supabase = supabase;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;

        // Extract Authorization header
        if (!http.Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Results.Unauthorized();

        string token = authHeader.ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "")
            .Trim();

        if (string.IsNullOrEmpty(token))
            return Results.Unauthorized();

        try
        {
            // Set session for this request
            await _supabase.Auth.SetSession(token, "dummy");

            var user = _supabase.Auth.CurrentUser;
            if (user == null)
                return Results.Unauthorized();

            http.Items["supabase_user"] = user;
        }
        catch
        {
            return Results.Unauthorized();
        }

        return await next(ctx);
    }
}