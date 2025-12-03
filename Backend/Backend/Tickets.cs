using Backend.Models;
using Supabase.Gotrue;
using static Supabase.Postgrest.Constants;

namespace Backend;

static class Tickets
{
    public static async Task<IResult> GetMyTickets(string? ticketId, Supabase.Client client)
    {
        try
        {
            var parsed = Guid.Parse(client.Auth.CurrentUser!.Id!);

            var usuarioDb = await client
                .From<Usuario>()
                .Where(u => u.Id == parsed)
                .Single();
            
            if (usuarioDb == null) return Results.Unauthorized();
            
            var query = client
                .From<Entrada>()
                .Select("*, Evento(*)")
                .Where(e => e.FkUsuario == usuarioDb.Id); 

            if (ticketId != null)
            {
                var parsedTicketId = Guid.Parse(ticketId);
                query.Where(e => e.Id == parsedTicketId);
            }
            
            var result = await query.Get();
                
            return Results.Ok(result);
        }
        catch (Exception)
        {
            return Results.NotFound(new { error = "Ticket no encontrado." });
        }
    }
}