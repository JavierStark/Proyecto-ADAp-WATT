using Backend.Models;
using static Supabase.Postgrest.Constants;

namespace Backend;

static class Admin
{
    public static async Task<IResult> AdminListEvents(Supabase.Client client)
    {
        try
        {
            var response = await client
                .From<Evento>()
                .Select("*")
                .Order("fecha_y_hora", Ordering.Descending)
                .Get();

            var eventos = response.Models.Select(e => new EventoAdminDto(
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
    
    public static async Task<IResult> AdminCreateEvent(EventoModificarDto dto, Supabase.Client client)
    {
        try
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Results.BadRequest(new { error = "El título del evento es obligatorio." });

            if (!dto.Fecha.HasValue)
                return Results.BadRequest(new { error = "La fecha del evento es obligatoria." });

            if (dto.Fecha.Value < DateTime.UtcNow)
                return Results.BadRequest(new { error = "La fecha del evento no puede ser en el pasado." });

            var nuevoEvento = new Evento
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                FechaEvento = dto.Fecha.Value,
                Ubicacion = dto.Ubicacion,
                Aforo = dto.Aforo ?? 0,
                EntradaValida = dto.EntradaValida ?? false,
                ObjetoRecaudacion = dto.ObjetoRecaudacion
            };

            var response = await client
                .From<Evento>()
                .Insert(nuevoEvento);

            var eventoCreado = response.Models.First();

            return Results.Created($"/events/{eventoCreado.IdEvento}", new
            {
                status = "success",
                message = "Evento creado correctamente.",
                evento = new EventoAdminDto(
                    eventoCreado.IdEvento,
                    eventoCreado.Nombre,
                    eventoCreado.Descripcion,
                    eventoCreado.FechaEvento,
                    eventoCreado.Ubicacion,
                    eventoCreado.Aforo ?? 0,
                    eventoCreado.EntradaValida,
                    eventoCreado.ObjetoRecaudacion ?? "Sin especificar"
                )
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al crear el evento: " + ex.Message);
        }
    }
    
    public static async Task<IResult> AdminUpdateEvent(int eventId, EventoModificarDto dto, Supabase.Client client)
    {
        try
        {
            // Verificar que el evento existe
            var response = await client
                .From<Evento>()
                .Filter("id_evento", Operator.Equals, eventId)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}." });

            // Actualizar solo los campos proporcionados
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
                eventoDb.Nombre = dto.Nombre;

            if (dto.Descripcion != null)
                eventoDb.Descripcion = dto.Descripcion;

            if (dto.Fecha.HasValue)
            {
                if (dto.Fecha.Value < DateTime.UtcNow)
                    return Results.BadRequest(new { error = "La fecha del evento no puede ser en el pasado." });
                
                eventoDb.FechaEvento = dto.Fecha.Value;
            }

            if (dto.Ubicacion != null)
                eventoDb.Ubicacion = dto.Ubicacion;

            if (dto.Aforo.HasValue)
            {
                if (dto.Aforo.Value < 0)
                    return Results.BadRequest(new { error = "El aforo no puede ser negativo." });
                
                eventoDb.Aforo = dto.Aforo.Value;
            }

            if (dto.EntradaValida.HasValue)
                eventoDb.EntradaValida = dto.EntradaValida.Value;

            if (dto.ObjetoRecaudacion != null)
                eventoDb.ObjetoRecaudacion = dto.ObjetoRecaudacion;

            // Realizar la actualización
            await client
                .From<Evento>()
                .Update(eventoDb);

            return Results.Ok(new
            {
                status = "success",
                message = "Evento actualizado correctamente.",
                evento = new EventoAdminDto(
                    eventoDb.IdEvento,
                    eventoDb.Nombre,
                    eventoDb.Descripcion,
                    eventoDb.FechaEvento,
                    eventoDb.Ubicacion,
                    eventoDb.Aforo ?? 0,
                    eventoDb.EntradaValida,
                    eventoDb.ObjetoRecaudacion ?? "Sin especificar"
                )
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al actualizar el evento: " + ex.Message);
        }
    }
    
    public static async Task<IResult> AdminDeleteEvent(int eventId, Supabase.Client client)
    {
        try
        {
            // Verificar que el evento existe
            var response = await client
                .From<Evento>()
                .Filter("id_evento", Operator.Equals, eventId)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}." });

            // Eliminar el evento
            await client
                .From<Evento>()
                .Filter("id_evento", Operator.Equals, eventId)
                .Delete();

            return Results.Ok(new
            {
                status = "success",
                message = $"Evento '{eventoDb.Nombre}' eliminado correctamente."
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al eliminar el evento: " + ex.Message);
        }
    }
    
    record EventoAdminDto(
        long Id,
        string Nombre,
        string? Descripcion,
        DateTime Fecha,
        string? Ubicacion,
        int Aforo,
        bool EntradaValida,
        string ObjetoRecaudacion
    );
    
    public record EventoModificarDto(
        string? Nombre,
        string? Descripcion,
        DateTime? Fecha,
        string? Ubicacion,
        int? Aforo,
        bool? EntradaValida,
        string? ObjetoRecaudacion
    );
}