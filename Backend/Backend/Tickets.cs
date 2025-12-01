using Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend;

static class Tickets
{
    public static async Task<IResult> GetMyTickets(int? ticketId, [FromHeader(Name = "Authorization")] string authHeader,
        Supabase.Client client)
    {
        if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();
        string token = authHeader.Replace("Bearer ", "").Replace("\"", "").Trim();

        try
        {
            await client.Auth.SetSession(token, "dummy");
            var currentUser = client.Auth.CurrentUser;
            
            if (currentUser == null) return Results.Unauthorized();

            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, currentUser.Id)
                .Single();
            
            if (usuarioDb == null) return Results.Unauthorized();
            
            var query = client
                .From<Ticket>()
                .Select("*, evento(*)")
                .Filter("id_usuario", Supabase.Postgrest.Constants.Operator.Equals,
                    usuarioDb.IdUsuario); // ¡Seguridad clave!

            if (ticketId != null)
                query.Filter("id_ticket", Supabase.Postgrest.Constants.Operator.Equals, ticketId);
            
            var result = await query.Get();
                
            return Results.Ok(result);
        }
        catch (Exception)
        {
            return Results.NotFound(new { error = "Ticket no encontrado." });
        }
    }
}