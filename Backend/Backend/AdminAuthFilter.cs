using Backend.Models;
using Supabase;

namespace Backend;

public class AdminAuthFilter(Client supabase) : IEndpointFilter
{
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
            await supabase.Auth.SetSession(token, "dummy");

            var user = supabase.Auth.CurrentUser;
            if (user == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(user.Id);

            // Get the usuario from the database
            var usuarioResponse = await supabase
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();
            if (usuario == null)
                return Results.Unauthorized();

            var adminResponse = await supabase
                .From<Admin>()
                .Where(a => a.fkUsuario == usuario.Id)
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