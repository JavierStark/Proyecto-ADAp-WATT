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
            var userId = httpContext.Items["user_id"]?.ToString();
            // Validación de seguridad antes de parsear
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

            // 1. PROCESAR PAGO (Pasarela externa)
            await paymentService.ProcessPaymentAsync(dto.Importe, dto.PaymentToken);

            // 2. GUARDAR EL PAGO EN BD
            var nuevoPago = new Pago
            {
                Monto = dto.Importe,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.MetodoPago ?? "Tarjeta",
                FkCliente = userIdGuid,
            };

            var pagoResponse = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoResponse.Models.First(); // Aquí tenemos el ID del pago (pagoCreado.Id)

            // 3. BUSCAR SOCIO
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

            // Variables para guardar datos finales y usarlos en el paso 5
            Guid socioIdFinal;
            string conceptoHistorial;

            // 4. INSERTAR O ACTUALIZAR SOCIO
            if (socioExistente != null)
            {
                // === CASO ACTUALIZAR ===
                socioIdFinal = socioExistente.Id!.Value; // Usamos el ID existente
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
                // === CASO INSERTAR ===
                conceptoHistorial = $"Alta {dto.Plan}";

                var nuevoSocio = new Socio
                {
                    Id = null, // Dejar null para que BD genere UUID
                    FkCliente = userIdGuid,
                    TipoSuscripcion = dto.Plan.ToLower(),
                    Cuota = dto.Importe,
                    FechaInicio = fechaInicio,
                    FechaFin = nuevaFechaFin
                };

                // CAMBIO CLAVE: Capturamos la respuesta para obtener el ID generado
                var insertResponse = await client.From<Socio>().Insert(nuevoSocio);
                var socioCreado = insertResponse.Models.First();
                
                socioIdFinal = socioCreado.Id!.Value; // Obtenemos el nuevo ID
            }

            // Guardar periodo_socio
            var nuevaMensualidad = new PeriodoSocio
            {
                FkSocio = socioIdFinal,
                FkPago = pagoCreado.Id,
                Concepto = conceptoHistorial
            };

            await client.From<PeriodoSocio>().Insert(nuevaMensualidad);

            return Results.Ok(new
            {
                Mensaje = "Suscripción activada/renovada con éxito.",
                Vence = nuevaFechaFin,
                PagoRef = pagoCreado.Id
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error: " + ex.Message);
        }
    }

    public record SuscripcionDto(
        string Plan,
        decimal Importe,
        string PaymentToken,
        string? MetodoPago
    );
}