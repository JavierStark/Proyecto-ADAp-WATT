using Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend;

static class Tickets
{
    public static async Task<IResult> GetMyTickets([FromHeader(Name = "Authorization")] string authHeader, Supabase.Client client)
    {
        // Limpieza y validación del Token
        if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();
        string token = authHeader.Replace("Bearer ", "").Replace("\"", "").Trim();

        try
        {
            // Autenticar al usuario en Supabase
            await client.Auth.SetSession(token, "dummy");
            var currentUser = client.Auth.CurrentUser;

            if (currentUser == null) return Results.Unauthorized();

            // Obtener el ID numérico del usuario (id_usuario)
            // Buscamos en la tabla 'usuario' usando el UUID de Auth
            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, currentUser.Id)
                .Single();

            if (usuarioDb == null) return Results.Problem("Usuario no encontrado en base de datos.");

            // Obtener los tickets
            // Usamos .Select("*, evento(*)") para hacer un JOIN y traer los datos del evento
            var result = await client
                .From<Ticket>()
                .Select("*, evento(*)")
                .Filter("id_usuario", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario)
                .Order("fecha_compra", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return Results.Ok(result.Models);
        }
        catch (Exception ex)
        {
            // Manejo de errores (ej. token expirado)
            return Results.Unauthorized();
        }
    }

    public static async Task<IResult> GetMyTicketById(int ticketId, [FromHeader(Name = "Authorization")] string authHeader,
        Supabase.Client client)
    {
        // Validación del Token
        if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();
        string token = authHeader.Replace("Bearer ", "").Replace("\"", "").Trim();

        try
        {
            // Autenticar
            await client.Auth.SetSession(token, "dummy");
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null) return Results.Unauthorized();

            // Obtener ID numérico del usuario
            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, currentUser.Id)
                .Single();

            // Buscar el Ticket específico
            // Filtramos por ID del ticket Y por ID del usuario (Seguridad)
            var result = await client
                .From<Ticket>()
                .Select("*, evento(*)")
                .Filter("id_ticket", Supabase.Postgrest.Constants.Operator.Equals, ticketId)
                .Filter("id_usuario", Supabase.Postgrest.Constants.Operator.Equals,
                    usuarioDb.IdUsuario) // ¡Seguridad clave!
                .Single();

            return result == null
                ? Results.NotFound(new { error = "Ticket no encontrado o no te pertenece." })
                : Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = "Ticket no encontrado." });
        }
    }
}