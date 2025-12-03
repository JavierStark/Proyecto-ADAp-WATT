using Backend.Models;
using static Supabase.Postgrest.Constants;

namespace Backend;

static class Tickets
{
    public static async Task<IResult> GetMyTickets(int? ticketId, Supabase.Client client)
    {
        try
        {
            var currentUser = client.Auth.CurrentUser!;

            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Operator.Equals, currentUser.Id)
                .Single();
            
            if (usuarioDb == null) return Results.Unauthorized();
            
            var query = client
                .From<Ticket>()
                .Select("*, evento(*)")
                .Filter("id_usuario", Operator.Equals,
                    usuarioDb.IdUsuario); // ¡Seguridad clave!

            if (ticketId != null)
                query.Filter("id_ticket", Operator.Equals, ticketId);
            
            var result = await query.Get();
                
            return Results.Ok(result);
        }
        catch (Exception)
        {
            return Results.NotFound(new { error = "Ticket no encontrado." });
        }
    }
}