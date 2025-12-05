using Backend.Models;
using static Supabase.Postgrest.Constants;

namespace Backend;

static class Tickets
{
    public static async Task<IResult> GetMyTickets(string? ticketId, Supabase.Client client)
    {
        try
        {
            var userAuth = client.Auth.CurrentUser;
            if (userAuth == null) return Results.Unauthorized();

            var usuarioDb = await client.From<Usuario>()
                .Filter("id", Operator.Equals, userAuth.Id)
                .Single();

            if (usuarioDb == null) return Results.Unauthorized();

            var query = client.From<Entrada>()
                .Filter("fk_usuario", Operator.Equals, usuarioDb.Id.ToString());

            if (!string.IsNullOrEmpty(ticketId))
            {
                query = query.Filter("id", Operator.Equals, ticketId);
            }

            var responseTickets = await query.Order("fecha_compra", Ordering.Descending).Get();
            var misTickets = responseTickets.Models;

            if (!misTickets.Any())
            {
                return !string.IsNullOrEmpty(ticketId)
                    ? Results.NotFound(new { error = "Ticket no encontrado." })
                    : Results.Ok(new List<TicketDto>());
            }

            var eventosIds = misTickets.Select(t => t.FkEvento.ToString()).Distinct().ToList();
            var tiposIds = misTickets.Select(t => t.FkEntradaEvento.ToString()).Distinct().ToList();

            // Traer Eventos
            var eventosResponse = await client.From<Evento>()
                .Filter("id", Operator.In, eventosIds)
                .Get();
            
            var dictEventos = eventosResponse.Models.ToDictionary(e => e.Id);

            // Traer Tipos de Entrada
            var tiposResponse = await client.From<EntradaEvento>()
                .Filter("id", Operator.In, tiposIds)
                .Get();

            var dictTipos = tiposResponse.Models.ToDictionary(t => t.FkEntradaEvento);

            // Mapeo a DTO
            var listaFinal = misTickets.Select(t =>
            {
                var evento = dictEventos.ContainsKey(t.FkEvento) ? dictEventos[t.FkEvento] : null;

                var tipo = dictTipos.ContainsKey(t.FkEntradaEvento) ? dictTipos[t.FkEntradaEvento] : null;

                return new TicketDto(
                    t.Id,
                    evento?.Nombre ?? "Evento no disponible",
                    tipo?.Tipo ?? "Estándar",
                    t.Precio,
                    evento?.FechaEvento?.DateTime ?? DateTime.MinValue,
                    evento?.Ubicacion ?? "Ubicación desconocida",
                    t.CodigoQr,
                    t.Estado ?? "Activo"
                );
            }).ToList();

            return Results.Ok(listaFinal);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error recuperando tickets: " + ex.Message);
        }
    }

    public record TicketDto(
        Guid TicketId,
        string EventoNombre,
        string TipoEntrada,
        decimal PrecioPagado,
        DateTime FechaEvento,
        string Ubicacion,
        string? CodigoQrUrl,
        string Estado
    );
}