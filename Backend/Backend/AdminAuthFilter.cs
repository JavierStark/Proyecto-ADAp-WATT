using Backend.Models;
using Supabase;

namespace Backend;

/// <summary>
/// AdminAuthFilter assumes SupabaseAuthFilter has already run and validated the JWT token.
/// This filter only checks if the authenticated user has admin permissions.
/// Apply filters in order: .AddEndpointFilter&lt;SupabaseAuthFilter&gt;().AddEndpointFilter&lt;AdminAuthFilter&gt;()
/// </summary>
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

        // Check if SupabaseAuthFilter has already authenticated the user
        if (!http.Items.TryGetValue("user_id", out var userIdObj) || userIdObj is not string userIdStr)
        {
            return Results.Json(
                new { error = "Usuario no autenticado. Debe aplicar SupabaseAuthFilter primero." },
                statusCode: 401
            );
        }

        try
        {
            var userId = Guid.Parse(userIdStr);

            // Get the usuario from the database
            var usuarioResponse = await _supabase
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();
            if (usuario == null)
            {
                return Results.Json(
                    new { error = "Usuario no encontrado." },
                    statusCode: 404
                );
            }

            // Check if the user is an admin
            var adminResponse = await _supabase
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

            // Store additional context for endpoints
            http.Items["usuario"] = usuario;
            http.Items["admin"] = admin;
        }
        catch (FormatException)
        {
            return Results.Json(
                new { error = "ID de usuario inválido." },
                statusCode: 400
            );
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al verificar permisos de administrador: " + ex.Message);
        }

        return await next(ctx);
    }
}

