using Backend.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend;

static class Events
{
    public static async Task<IResult> ListEvents(string? query, Supabase.Client client)
    {
        try
        {
            var dbQuery = 
                client.From<Evento>()
                    .Select("*")
                    .Order("fecha_y_hora", Supabase.Postgrest.Constants.Ordering.Ascending);
            
            if (!string.IsNullOrEmpty(query))
                dbQuery = dbQuery.Filter("nombre", Supabase.Postgrest.Constants.Operator.ILike, $"%{query}%");

            var response = await dbQuery.Get();
        
            var eventos = response.Models.Select(e => new EventoDto(
                e.IdEvento,
                e.Nombre,
                e.Descripcion,
                e.FechaEvento,
                e.Ubicacion,
                e.Aforo ?? 0,
                e.EntradaValida,
                e.ObjetoRecaudacion ?? "Sin especificar"
            ));

            return Results.Ok(eventos);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al obtener eventos: " + ex.Message);
        }
    }
    
    public static async Task<IResult> GetEvent(int eventId, Supabase.Client client)
    {
        try
        {
            var response = await client
                .From<Evento>()
                .Filter("id_evento", Supabase.Postgrest.Constants.Operator.Equals, eventId)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}" });

            var eventoDto = new EventoDto(
                eventoDb.IdEvento,
                eventoDb.Nombre,
                eventoDb.Descripcion,
                eventoDb.FechaEvento,
                eventoDb.Ubicacion,
                eventoDb.Aforo ?? 0, 
                eventoDb.EntradaValida,
                eventoDb.ObjetoRecaudacion ?? "Sin especificar"
            );

            return Results.Ok(eventoDto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error interno: " + ex.Message);
        }
    }
    
    public static IResult StartPurchase(PurchaseStartDto dto) => Results.Ok();
    public static IResult ConfirmPurchase(PurchaseConfirmDto dto) => Results.Ok();
    
    public static IResult GetPaymentMethods() => Results.Ok();
    public static IResult ValidateDiscount(DiscountCheckDto dto) => Results.Ok();
    
    record EventoDto(
        long Id, 
        string Nombre, 
        string? Descripcion, 
        DateTime Fecha,
        string? Ubicacion,
        int Aforo, 
        bool EntradaValida,
        string ObjetoRecaudacion
    );
    public record PurchaseStartDto(
        int EventId,
        int Quantity,
        bool IsCompany,
        string BillingAddress,
        string? DiscountCode);

    
    public record PurchaseConfirmDto(
        string PaymentMethod,
        string PaymentToken);

    public record DiscountCheckDto(string Code);
}