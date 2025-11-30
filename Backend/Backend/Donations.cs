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
    public static async Task<IResult> GetMyDonationSummary([FromHeader(Name = "Authorization")] string? authHeader, 
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

            // Buscar su ID numérico
            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
                .Single();

            // CONSULTA: Traer donaciones + pago
            var response = await client
                .From<Donacion>()
                .Select("*, Pago:fk_don_pago!inner(*)") 
                .Filter("Pago.id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario.ToString())
                .Get();
            
            // Sumamos los montos (usamos 0 si por error algún pago viene nulo)
            decimal total = response.Models.Sum(d => d.Pago?.Monto ?? 0);

            // Devolver el resultado simple
            return Results.Ok(new DonationSummaryDto(total));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error calculando el total: " + ex.Message);
        }
    }
    public static async Task<IResult> CreateDonation(DonationDto dto, [FromHeader(Name = "Authorization")] string? authHeader, 
    Supabase.Client client)
{
    // Validaciones
    if (dto.Amount <= 0) return Results.BadRequest(new { error = "El monto debe ser mayor a 0." });
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

        // Buscar su ID numérico (id_cliente)
        var usuarioDb = await client
            .From<Usuario>()
            .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
            .Single();

        // Crear el registro en la tabla PAGO
        var nuevoPago = new Pago
        {
            Monto = dto.Amount,
            Fecha = DateTime.UtcNow,
            Estado = "Pagado", // Asumimos que el pago es inmediato para simplificar
            MetodoDePago = dto.PaymentMethod ?? "Tarjeta",
            IdCliente = usuarioDb.IdUsuario // Vinculamos el pago al usuario
        };

        // Insertamos y pedimos que nos devuelva el objeto creado (para obtener el ID)
        var pagoResponse = await client
            .From<Pago>()
            .Insert(nuevoPago);

        var pagoCreado = pagoResponse.Models.First();

        // Crear el registro en la tabla DONACION
        // Vinculamos esta donación al pago que acabamos de crear
        var nuevaDonacion = new Donacion
        {
            IdPago = pagoCreado.IdPago
        };

        await client
            .From<Donacion>()
            .Insert(nuevaDonacion);

        // Respuesta de éxito
        return Results.Ok(new 
        { 
            status = "success", 
            message = $"¡Gracias! Donación de {dto.Amount}€ realizada correctamente.",
            id_donacion = pagoCreado.IdPago
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error procesando la donación: " + ex.Message);
    }
}

    public static async Task<IResult> GetDonationCertificate(
        int? year, // Opcional: si es null, usaremos el año pasado
        [FromHeader(Name = "Authorization")] string? authHeader,
        Supabase.Client client)
    {
        // Validar Token
        if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();
        string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "")
            .Trim();

        try
        {
            // Auth
            await client.Auth.SetSession(token, "dummy");
            var userAuth = client.Auth.CurrentUser;
            if (userAuth == null) return Results.Unauthorized();

            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
                .Single();

            // Si no nos pasan año, asumimos el año anterior (para la renta)
            int targetYear = year ?? DateTime.Now.Year - 1;

            // Formato ISO 8601 para Supabase: "YYYY-MM-DD"
            string fechaInicio = $"{targetYear}-01-01T00:00:00";
            string fechaFin = $"{targetYear}-12-31T23:59:59";

            // CONSULTA CON FILTRO DE FECHAS
            var response = await client
                .From<Donacion>()
                .Select("*, Pago:fk_don_pago!inner(*)")
                .Filter("Pago.id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario.ToString())
                // Filtros de fecha (Mayor o igual a Enero 1, Menor o igual a Dic 31)
                .Filter("Pago.fecha", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, fechaInicio)
                .Filter("Pago.fecha", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, fechaFin)
                .Get();

            var donacionesAnuales = response.Models
                .OrderBy(d => d.Pago?.Fecha) // Ordenamos aquí
                .ToList();

            if (donacionesAnuales.Count == 0)
            {
                return Results.NotFound(new { error = $"No se encontraron donaciones en el año fiscal {targetYear}." });
            }

            // CÁLCULOS
            decimal totalAnual = donacionesAnuales.Sum(d => d.Pago?.Monto ?? 0);

            // GENERAR DOCUMENTO (Texto simulando PDF)
            // Usamos StringBuilder para construir una tabla de texto
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("-------------------------------------------------------");
            sb.AppendLine("         CERTIFICADO FISCAL DE DONACIONES");
            sb.AppendLine("                FUNDACIÓN CUDECA");
            sb.AppendLine("-------------------------------------------------------");
            sb.AppendLine("");
            sb.AppendLine($"EJERCICIO FISCAL: {targetYear}");
            sb.AppendLine($"FECHA DE EMISIÓN: {DateTime.Now.ToShortDateString()}");
            sb.AppendLine("");
            sb.AppendLine("DATOS DEL DONANTE:");
            sb.AppendLine($"Nombre:   {usuarioDb.Nombre} {usuarioDb.Apellidos}");
            sb.AppendLine($"NIF/DNI:  {usuarioDb.Dni ?? "NO INFORMADO"}");
            sb.AppendLine($"Email:    {usuarioDb.Email}");
            sb.AppendLine("");
            sb.AppendLine("DETALLE DE APORTACIONES:");
            sb.AppendLine("-------------------------------------------------------");
            sb.AppendLine(String.Format("{0,-12} | {1,-25} | {2,10}", "FECHA", "MÉTODO", "IMPORTE"));
            sb.AppendLine("-------------------------------------------------------");

            foreach (var d in donacionesAnuales)
            {
                if (d.Pago != null)
                {
                    sb.AppendLine(String.Format("{0,-12} | {1,-25} | {2,10} EUR",
                        d.Pago.Fecha.ToShortDateString(),
                        d.Pago.MetodoDePago ?? "General",
                        d.Pago.Monto));
                }
            }

            sb.AppendLine("-------------------------------------------------------");
            sb.AppendLine(String.Format("{0,-37} | {1,10} EUR", "TOTAL APORTADO:", totalAnual));
            sb.AppendLine("-------------------------------------------------------");
            sb.AppendLine("");
            sb.AppendLine("Fundación Cudeca certifica que los donativos arriba");
            sb.AppendLine("indicados se han realizado de forma irrevocable.");
            sb.AppendLine("");
            sb.AppendLine("Este documento es válido a efectos del Impuesto sobre");
            sb.AppendLine("la Renta de las Personas Físicas (IRPF).");

            // Retornar archivo
            var archivoBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

            return Results.File(
                archivoBytes,
                "text/plain",
                $"Certificado_Fiscal_{targetYear}_{usuarioDb.Dni ?? "Donante"}.txt"
            );
        }
        catch (Exception ex)
        {
            return Results.Problem("Error generando certificado fiscal: " + ex.Message);
        }
    }

    record DonationHistoryDto(
        long IdDonacion, 
        decimal Monto, 
        DateTime Fecha, 
        string Estado, 
        string? MetodoPago
    );
    record DonationSummaryDto(decimal TotalDonado);
    public record DonationDto(decimal Amount, String PaymentMethod); // Ej: "Tarjeta", "PayPal", "Bizum"
    
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