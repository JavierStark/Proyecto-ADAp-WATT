namespace Backend;

static class Events
{
    public static async Task<IResult> ListEvents(string? query, Supabase.Client client)
    {
        try
        {
            // Consulta a la tabla eventos
            var dbQuery = client.From<Evento>().Select("*");

            if (!string.IsNullOrEmpty(query))
            {
                dbQuery = dbQuery.Filter("nombre", Supabase.Postgrest.Constants.Operator.ILike, $"%{query}%");
            }

            dbQuery = dbQuery.Order("fecha_y_hora", Supabase.Postgrest.Constants.Ordering.Ascending);

            // Ejecutar consulta
            var response = await dbQuery.Get();
        
            // Convertimos la lista de modelos de Supabase a una lista de DTOs simples
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

            // Devolver la lista limpia
            return Results.Ok(eventos);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al obtener eventos: " + ex.Message);
        }
    }
    public static IResult ListEvents(string? query) => Results.Ok();
    public static IResult GetEvent(int eventId) => Results.Ok();
    public static IResult StartPurchase(PurchaseStartDto dto) => Results.Ok();
    public static IResult ConfirmPurchase(PurchaseConfirmDto dto) => Results.Ok();
    
    public static IResult GetPaymentMethods() => Results.Ok();
    public static IResult ValidateDiscount(DiscountCheckDto dto) => Results.Ok();
    
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