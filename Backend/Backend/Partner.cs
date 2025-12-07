using static Supabase.Postgrest.Constants;

namespace Backend;

public class Partner
{
    public static async Task<IResult> BecomePartner(HttpContext httpContext, SuscripcionDto dto, Supabase.Client client)
{
    try
    {
        var userId = httpContext.Items["user_id"]?.ToString();
        var userIdGuid = Guid.Parse(userId);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        // Validaciones
        if (dto.Importe <= 0) return Results.BadRequest("El importe debe ser mayor a 0.");

        int meses = dto.Plan.ToLower() switch
        {
            "mensual" => 1,
            "trimestral" => 3,
            "anual" => 12,
            _ => 0
        };
        if (meses == 0) return Results.BadRequest("Plan inválido.");

        // Buscamos si ya existe
        var response = await client
            .From<Socio>()
            .Filter("fk_cliente", Operator.Equals, userId)
            .Get();

        var socioExistente = response.Models.FirstOrDefault();

        // Calculamos Fechas
        DateTime fechaInicio = DateTime.UtcNow;
        DateTime nuevaFechaFin = DateTime.UtcNow.AddMonths(meses);

        // Si existe y aún no caduca, extendemos
        if (socioExistente != null && socioExistente.FechaFin > DateTime.UtcNow)
        {
            fechaInicio = socioExistente.FechaInicio;
            nuevaFechaFin = socioExistente.FechaFin.AddMonths(meses);
        }

        if (socioExistente != null)
        {
            // ACTUALIZAR (RENOVACIÓN)
            // Usamos .Set() para modificar solo lo necesario
            await client.From<Socio>()
                .Filter("id", Operator.Equals, socioExistente.Id)
                .Set(s => s.TipoSuscripcion, dto.Plan.ToLower())
                .Set(s => s.Cuota, dto.Importe)
                .Set(s => s.FechaFin, nuevaFechaFin)
                .Set(s => s.FechaInicio, fechaInicio)
                .Update();
        }
        else
        {
            // INSERTAR (NUEVO SOCIO)
            var nuevoSocio = new Socio
            {
                FkCliente = userIdGuid,
                TipoSuscripcion = dto.Plan.ToLower(),
                Cuota = dto.Importe,
                FechaInicio = fechaInicio,
                FechaFin = nuevaFechaFin
            };

            await client.From<Socio>().Insert(nuevoSocio);
        }

        return Results.Ok(new
        {
            Mensaje = socioExistente != null ? "Suscripción renovada." : "¡Bienvenido nuevo socio!",
            Plan = dto.Plan,
            Vence = nuevaFechaFin
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error: " + ex.Message);
    }
}

    public record SuscripcionDto(string Plan, decimal Importe);
}