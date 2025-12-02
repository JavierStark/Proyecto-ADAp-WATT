using Backend.Models;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace Backend;

public class AdminAuthFilter : IEndpointFilter
{
    private readonly Client _supabase;

    public AdminAuthFilter(Client supabase)
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

            // Get the usuario from the database
            var usuarioResponse = await _supabase
                .From<Usuario>()
                .Filter("id_auth_supabase", Operator.Equals, user.Id)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();
            if (usuario == null)
                return Results.Unauthorized();

            // Check if the user is an admin
            var adminResponse = await _supabase
                .From<Models.Admin>()
                .Filter("id_usuario", Operator.Equals, usuario.IdUsuario.ToString())
                .Get();

            var admin = adminResponse.Models.FirstOrDefault();
            if (admin == null)
            {
                return Results.Json(
                    new { error = "Acceso denegado. Se requieren permisos de administrador." },
                    statusCode: 403
                );
            }

            http.Items["supabase_user"] = user;
            http.Items["usuario"] = usuario;
            http.Items["admin"] = admin;
        }
        catch (Exception ex)
        {
            return Results.Problem("Error de autenticación: " + ex.Message);
        }

        return await next(ctx);
    }
}

