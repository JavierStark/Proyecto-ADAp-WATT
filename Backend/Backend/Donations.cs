using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend;

static class Donations
{
    public static async Task<IResult> GetMyDonations([FromHeader(Name = "Authorization")] string? authHeader, 
        Supabase.Client client)
{
    // Validar Token
    if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();
    string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("\"", "")
                             .Trim();

    try
    {
        // Identificar al usuario
        await client.Auth.SetSession(token, "dummy");
        var userAuth = client.Auth.CurrentUser;
        if (userAuth == null) return Results.Unauthorized();

        // Buscar su ID numérico en tabla Usuario
        var usuarioDb = await client
            .From<Usuario>()
            .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
            .Single();

        // Consulta JOIN (id_cliente del pago == mi id"
        var response = await client
            .From<Donacion>()
            .Select("*, Pago:fk_don_pago!inner(*)")
            .Filter("pago.id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario.ToString())
            .Get();

        // Mapear a DTO
        // Ordenamos en memoria por fecha
        var historial = response.Models
            .Select(d => new DonationHistoryDto(
                d.IdDonacion,
                d.Pago != null ? d.Pago.Monto : 0, // Protección por si pago viniera nulo
                d.Pago != null ? d.Pago.Fecha : DateTime.MinValue,
                d.Pago != null ? d.Pago.Estado : "Desconocido",
                d.Pago?.MetodoDePago
            ))
            .OrderByDescending(x => x.Fecha)
            .ToList();

        return Results.Ok(historial);
    }
    catch (Exception ex)
    {
        return Results.Problem("Error obteniendo donaciones: " + ex.Message);
    }
}
    public static IResult GetMyDonationSummary() => Results.Ok();
    public static IResult CreateDonation(DonationDto dto) => Results.Ok();
    public static IResult GetDonationCertificate(int donationId) => Results.File("dummy.pdf");
    
    record DonationHistoryDto(
        long IdDonacion, 
        decimal Monto, 
        DateTime Fecha, 
        string Estado, 
        string? MetodoPago
    );
    public record DonationDto(decimal Amount);
    
    [Table("pago")]
    public class Pago : BaseModel
    {
        [PrimaryKey("id_pago")]
        public long IdPago { get; set; }

        [Column("monto")]
        public decimal Monto { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("estado")]
        public string Estado { get; set; } // "Pendiente", "Pagado"
    
        [Column("metododepago")]
        public string? MetodoDePago { get; set; }

        [Column("id_cliente")]
        public long IdCliente { get; set; }
    }

    [Table("donacion")]
    public class Donacion : BaseModel
    {
        [PrimaryKey("id_donacion")]
        public long IdDonacion { get; set; }

        [Column("id_pago")]
        public long IdPago { get; set; }
        
        // Una Donacion tiene un Pago asociado.
        [Reference(typeof(Pago))]
        public Pago Pago { get; set; }
    }
}