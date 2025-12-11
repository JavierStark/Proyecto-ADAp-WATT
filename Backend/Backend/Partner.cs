using Backend.Models;
using static Supabase.Postgrest.Constants;
using Backend.Services;

namespace Backend;

public class Partner
{
    public static async Task<IResult> BecomePartner(HttpContext httpContext, SuscripcionDto dto,
        Supabase.Client client, IPaymentService paymentService)
    {
        try
        {
            // Auth
            var userId = httpContext.Items["user_id"]?.ToString();
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            
            var userIdGuid = Guid.Parse(userId);

            // Validaciones básicas
            if (dto.Importe <= 0) return Results.BadRequest("El importe debe ser mayor a 0.");

            int meses = dto.Plan.ToLower() switch
            {
                "mensual" => 1,
                "trimestral" => 3,
                "anual" => 12,
                _ => 0
            };
            if (meses == 0) return Results.BadRequest("Plan inválido.");

            // Procesar pago
            await paymentService.ProcessPaymentAsync(dto.Importe, dto.PaymentToken);

            // Guardar el pago
            var nuevoPago = new Pago
            {
                Monto = dto.Importe,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.MetodoPago ?? "Tarjeta",
                FkCliente = userIdGuid,
            };

            var pagoResponse = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoResponse.Models.First();
            
            var response = await client
                .From<Socio>()
                .Filter("fk_cliente", Operator.Equals, userId)
                .Get();

            var socioExistente = response.Models.FirstOrDefault();

            // Calcular Fechas
            DateTime fechaInicio = DateTime.UtcNow;
            DateTime nuevaFechaFin = DateTime.UtcNow.AddMonths(meses);

            if (socioExistente != null && socioExistente.FechaFin > DateTime.UtcNow)
            {
                // Renovación: extendemos fecha
                fechaInicio = socioExistente.FechaInicio;
                nuevaFechaFin = socioExistente.FechaFin.AddMonths(meses);
            }
            
            Guid socioIdFinal;
            string conceptoHistorial;
            
            if (socioExistente != null)
            {
                // Actualizar
                socioIdFinal = socioExistente.Id!.Value;
                conceptoHistorial = $"Renovación {dto.Plan}";

                await client.From<Socio>()
                    .Filter("id", Operator.Equals, socioExistente.Id.ToString())
                    .Set(s => s.TipoSuscripcion, dto.Plan.ToLower())
                    .Set(s => s.Cuota, dto.Importe)
                    .Set(s => s.FechaFin, nuevaFechaFin)
                    .Set(s => s.FechaInicio, fechaInicio)
                    .Update();
            }
            else
            {
                // Insertar 
                conceptoHistorial = $"Alta {dto.Plan}";

                var nuevoSocio = new Socio
                {
                    Id = null,
                    FkCliente = userIdGuid,
                    TipoSuscripcion = dto.Plan.ToLower(),
                    Cuota = dto.Importe,
                    FechaInicio = fechaInicio,
                    FechaFin = nuevaFechaFin
                };
                
                var insertResponse = await client.From<Socio>().Insert(nuevoSocio);
                var socioCreado = insertResponse.Models.First();
                
                socioIdFinal = socioCreado.Id!.Value;
            }

            // Guardar periodo_socio
            var nuevaMensualidad = new PeriodoSocio
            {
                FkSocio = socioIdFinal,
                FkPago = pagoCreado.Id,
                Concepto = conceptoHistorial
            };

            await client.From<PeriodoSocio>().Insert(nuevaMensualidad);

            return Results.Ok(new PartnerSubscriptionResponseDto(
                "Suscripción activada/renovada con éxito.",
                nuevaFechaFin,
                pagoCreado.Id
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error: " + ex.Message);
        }
    }
    
    public static async Task<IResult> GetPartnerData(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = httpContext.Items["user_id"]?.ToString();
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            
            var response = await client
                .From<Socio>()
                .Filter("fk_cliente",Operator.Equals, userId)
                .Get();

            var socio = response.Models.FirstOrDefault();

            if (socio == null)
            {
                return Results.NotFound(new { error = "El usuario no es socio actualmente." });
            }

            // Devolvemos datos
            return Results.Ok(new PartnerDataDto(
                socio.TipoSuscripcion,
                socio.Cuota,
                socio.FechaInicio,
                socio.FechaFin,
                socio.FechaFin > DateTime.UtcNow,
                (socio.FechaFin - DateTime.UtcNow).Days
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo datos de socio: " + ex.Message);
        }
    }

    public record SuscripcionDto(
        string Plan,
        decimal Importe,
        string PaymentToken,
        string? MetodoPago
    );
}