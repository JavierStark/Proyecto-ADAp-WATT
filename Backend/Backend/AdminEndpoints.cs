using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;

namespace Backend;

static class AdminEndpoints
{
    public static async Task<IResult> AdminListEvents(Supabase.Client client)
    {
        try
        {
            var eventos = (await client
                .From<Evento>()
                .Order(e => e.FechaEvento, Constants.Ordering.Descending)
                .Get()).Models;

            var entradas = (await client
                .From<EntradaEvento>()
                .Get()).Models;

            var eventosDto = eventos.Select(e =>
            {
                var entradasEvento = entradas.Where(en => en.FkEvento == e.Id).ToList();
                var general = entradasEvento.FirstOrDefault(en => en.Tipo == "General");
                var vip = entradasEvento.FirstOrDefault(en => en.Tipo == "VIP");

                return new EventoAdminDto(
                    e.Id,
                    e.Nombre,
                    e.Descripcion,
                    e.FechaEvento,
                    e.Ubicacion,
                    e.Aforo ?? 0,
                    e.EntradasVendidas,
                    e.EventoVisible,
                    e.ObjetoRecaudacion ?? "Sin especificar",
                    e.ImagenUrl,
                    general?.Precio ?? 0,
                    general?.Cantidad ?? 0,
                    vip?.Precio,
                    vip?.Cantidad
                );
            });

            return Results.Ok(eventosDto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al obtener eventos: " + ex.Message);
        }
    }

    public static async Task<IResult> AdminCreateEvent([FromForm] EventoCreateDto dto, Supabase.Client client)
{
    try
    {
        // 1. Validación: Nombre obligatorio
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Results.BadRequest(new { error = "El nombre del evento es obligatorio." });

        // 2. Validación: Fecha (si se proporciona)
        if (dto.Fecha.HasValue && dto.Fecha.Value < DateTime.UtcNow)
            return Results.BadRequest(new { error = "La fecha del evento no puede ser en el pasado." });

        // 3. Validación: Entradas Generales (Opcionales, pero si se ponen, deben estar completas y positivas)
        bool crearGeneral = dto.PrecioGeneral.HasValue && dto.CantidadGeneral.HasValue;
        if ((dto.PrecioGeneral.HasValue && !dto.CantidadGeneral.HasValue) || (!dto.PrecioGeneral.HasValue && dto.CantidadGeneral.HasValue))
             return Results.BadRequest(new { error = "Para crear entradas Generales debes indicar tanto precio como cantidad." });
        
        if (dto.PrecioGeneral.HasValue && dto.PrecioGeneral < 0)
            return Results.BadRequest(new { error = "El precio General no puede ser negativo." });

        // 4. Validación: Entradas VIP (Opcionales)
        bool crearVip = dto.PrecioVip.HasValue && dto.CantidadVip.HasValue;
        if ((dto.PrecioVip.HasValue && !dto.CantidadVip.HasValue) || (!dto.PrecioVip.HasValue && dto.CantidadVip.HasValue))
            return Results.BadRequest(new { error = "Para crear entradas VIP debes indicar tanto precio como cantidad." });

        if (dto.PrecioVip.HasValue && dto.PrecioVip < 0)
            return Results.BadRequest(new { error = "El precio VIP no puede ser negativo." });


        // 5. Gestión de Imagen
        string? imagenUrlFinal = null;
        if (dto.Imagen != null)
        {
            var extension = Path.GetExtension(dto.Imagen.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            using var memoryStream = new MemoryStream();
            await dto.Imagen.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            await client.Storage.From("eventos").Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { Upsert = false });
            imagenUrlFinal = client.Storage.From("eventos").GetPublicUrl(fileName);
        }

        // 6. Calcular Aforo inicial
        int aforoTotal = (crearGeneral ? dto.CantidadGeneral!.Value : 0) + (crearVip ? dto.CantidadVip!.Value : 0);

        var nuevoEvento = new Evento
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            FechaEvento = dto.Fecha, // Puede ser null ahora
            Ubicacion = dto.Ubicacion,
            Aforo = aforoTotal,
            EventoVisible = dto.EventoVisible,
            ObjetoRecaudacion = dto.ObjetoRecaudacion,
            ImagenUrl = imagenUrlFinal
        };

        var eventResponse = await client.From<Evento>().Insert(nuevoEvento);
        var eventoCreado = eventResponse.Models.First();

        // 7. Insertar Entradas (si aplica)
        var entradasAInsertar = new List<EntradaEvento>();

        if (crearGeneral)
        {
            entradasAInsertar.Add(new EntradaEvento
            {
                FkEvento = eventoCreado.Id,
                Tipo = "General",
                Precio = dto.PrecioGeneral!.Value,
                Cantidad = dto.CantidadGeneral!.Value
            });
        }

        if (crearVip)
        {
            entradasAInsertar.Add(new EntradaEvento
            {
                FkEvento = eventoCreado.Id,
                Tipo = "VIP",
                Precio = dto.PrecioVip!.Value,
                Cantidad = dto.CantidadVip!.Value
            });
        }

        if (entradasAInsertar.Any())
        {
            await client.From<EntradaEvento>().Insert(entradasAInsertar);
        }

        return Results.Created($"/events/{eventoCreado.Id}", new
        {
            status = "success",
            message = "Evento creado correctamente.",
            evento = eventoCreado,
            tickets_creados = entradasAInsertar.Select(t => new { t.Tipo, t.Precio, Stock = t.Cantidad })
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error al crear el evento: " + ex.Message);
    }
}

public static async Task<IResult> AdminUpdateEvent(string eventId, [FromForm] EventoModifyDto dto, Supabase.Client client)
{
    try
    {
        var parsed = Guid.Parse(eventId);

        // --- VALIDACIONES ---
        // 1. Nombre obligatorio
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Results.BadRequest(new { error = "El nombre del evento no puede estar vacío." });

        // 2. Fecha no pasada (solo si se envía)
        if (dto.Fecha.HasValue && dto.Fecha.Value < DateTimeOffset.UtcNow)
            return Results.BadRequest(new { error = "La fecha debe ser posterior a hoy." });

        // 3. Precios no negativos (solo si se envían)
        if (dto.PrecioGeneral.HasValue && dto.PrecioGeneral < 0)
            return Results.BadRequest(new { error = "El precio General no puede ser negativo." });
        if (dto.PrecioVip.HasValue && dto.PrecioVip < 0)
            return Results.BadRequest(new { error = "El precio VIP no puede ser negativo." });


        // --- OBTENCIÓN DE DATOS ---
        var evento = await client.From<Evento>().Where(e => e.Id == parsed).Single();
        if (evento == null)
            return Results.NotFound(new { error = $"No se encontró evento con ID {eventId}." });

        var updateQuery = client.From<Evento>().Where(x => x.Id == parsed);

        // --- ACTUALIZACIÓN DE IMAGEN ---
        if (dto.Imagen != null && dto.Imagen.Length > 0)
        {
            if (!string.IsNullOrEmpty(evento.ImagenUrl))
            {
                var nombreArchivoViejo = GetFileNameFromUrl(evento.ImagenUrl);
                if (nombreArchivoViejo != null)
                     _ = client.Storage.From("eventos").Remove(new List<string> { nombreArchivoViejo });
            }

            var extension = Path.GetExtension(dto.Imagen.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            using var memoryStream = new MemoryStream();
            await dto.Imagen.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            await client.Storage.From("eventos").Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { Upsert = false });
            var nuevaUrl = client.Storage.From("eventos").GetPublicUrl(fileName);

            updateQuery = updateQuery.Set(x => x.ImagenUrl, nuevaUrl);
            evento.ImagenUrl = nuevaUrl;
        }

        // --- ACTUALIZACIÓN DE CAMPOS DEL EVENTO ---
        updateQuery = updateQuery.Set(x => x.Nombre, dto.Nombre);
        evento.Nombre = dto.Nombre;

        if (dto.Fecha.HasValue)
        {
            updateQuery = updateQuery.Set(x => x.FechaEvento, dto.Fecha.Value);
            evento.FechaEvento = dto.Fecha.Value;
        }

        if (dto.Descripcion != null) { updateQuery = updateQuery.Set(x => x.Descripcion, dto.Descripcion); evento.Descripcion = dto.Descripcion; }
        if (dto.Ubicacion != null) { updateQuery = updateQuery.Set(x => x.Ubicacion, dto.Ubicacion); evento.Ubicacion = dto.Ubicacion; }
        if (dto.ObjetoRecaudacion != null) { updateQuery = updateQuery.Set(x => x.ObjetoRecaudacion, dto.ObjetoRecaudacion); evento.ObjetoRecaudacion = dto.ObjetoRecaudacion; }
        if (dto.EventoVisible.HasValue) { updateQuery = updateQuery.Set(x => x.EventoVisible, dto.EventoVisible.Value); evento.EventoVisible = dto.EventoVisible.Value; }

        // --- GESTIÓN DE TICKETS (Crear si no existen, Actualizar si existen) ---
        var ticketsResponse = await client.From<EntradaEvento>().Filter("fk_evento", Constants.Operator.Equals, eventId).Get();
        var ticketsDb = ticketsResponse.Models;
        var general = ticketsDb.FirstOrDefault(t => t.Tipo == "General");
        var vip = ticketsDb.FirstOrDefault(t => t.Tipo == "VIP");

        // 1. Gestión GENERAL
        if (general != null)
        {
            // Actualizar existente
            bool cambio = false;
            if (dto.PrecioGeneral.HasValue) { general.Precio = dto.PrecioGeneral.Value; cambio = true; }
            if (dto.CantidadGeneral.HasValue) { general.Cantidad = dto.CantidadGeneral.Value; cambio = true; }
            
            if(cambio) await client.From<EntradaEvento>().Update(general);
        }
        else if (dto.PrecioGeneral.HasValue && dto.CantidadGeneral.HasValue)
        {
            // Crear nueva General (porque no existía)
            var nuevaGeneral = new EntradaEvento
            {
                FkEvento = evento.Id,
                Tipo = "General",
                Precio = dto.PrecioGeneral.Value,
                Cantidad = dto.CantidadGeneral.Value
            };
            await client.From<EntradaEvento>().Insert(nuevaGeneral);
            general = nuevaGeneral; // Asignamos para el cálculo del aforo
        }

        // 2. Gestión VIP
        if (vip != null)
        {
            // Actualizar existente
            bool cambio = false;
            if (dto.PrecioVip.HasValue) { vip.Precio = dto.PrecioVip.Value; cambio = true; }
            if (dto.CantidadVip.HasValue) { vip.Cantidad = dto.CantidadVip.Value; cambio = true; }

            if(cambio) await client.From<EntradaEvento>().Update(vip);
        }
        else if (dto.PrecioVip.HasValue && dto.CantidadVip.HasValue)
        {
            // Crear nueva VIP (porque no existía)
            var nuevaVip = new EntradaEvento
            {
                FkEvento = evento.Id,
                Tipo = "VIP",
                Precio = dto.PrecioVip.Value,
                Cantidad = dto.CantidadVip.Value
            };
            await client.From<EntradaEvento>().Insert(nuevaVip);
            vip = nuevaVip; // Asignamos para el cálculo del aforo
        }

        // --- RECALCULAR AFORO ---
        // Tomamos el valor nuevo del DTO, si no, el valor de la base de datos (o recién creado), si no, 0.
        int cantGen = dto.CantidadGeneral ?? (general?.Cantidad ?? 0);
        int cantVip = dto.CantidadVip ?? (vip?.Cantidad ?? 0);
        
        // Asumimos que el aforo es la suma de capacidades actuales + entradas vendidas históricas (según tu lógica original)
        int nuevoAforo = cantGen + cantVip + evento.EntradasVendidas;

        updateQuery = updateQuery.Set(x => x.Aforo, nuevoAforo);
        evento.Aforo = nuevoAforo;

        await updateQuery.Update();

        return Results.Ok(new
        {
            status = "success",
            message = "Evento actualizado correctamente.",
            evento = new EventoAdminDto(
                evento.Id,
                evento.Nombre,
                evento.Descripcion,
                evento.FechaEvento,
                evento.Ubicacion,
                evento.Aforo ?? 0,
                evento.EntradasVendidas,
                evento.EventoVisible,
                evento.ObjetoRecaudacion ?? "Sin especificar",
                evento.ImagenUrl,
                general?.Precio ?? 0, // Si es null devuelve 0 para visualización
                general?.Cantidad ?? 0,
                vip?.Precio,
                vip?.Cantidad
            )
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error al actualizar el evento: " + ex.Message);
    }
}

    public static async Task<IResult> AdminDeleteEvent(string eventId, Supabase.Client client)
    {
        try
        {
            var parse = Guid.Parse(eventId);
            // Verificar que el evento existe
            var response = await client
                .From<Evento>()
                .Where(e => e.Id == parse)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}." });

            // Borrar foto de supabase
            if (!string.IsNullOrEmpty(eventoDb.ImagenUrl))
            {
                var nombreArchivo = GetFileNameFromUrl(eventoDb.ImagenUrl);
                if (nombreArchivo != null)
                {
                    await client.Storage.From("eventos").Remove(new List<string> { nombreArchivo });
                }
            }

            // Eliminar el evento
            await client
                .From<Evento>()
                .Where(e => e.Id == parse)
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

    private static string? GetFileNameFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try
        {
            var uri = new Uri(url);
            return uri.Segments.Last();
        }
        catch
        {
            return null;
        }
    }

    record EventoAdminDto(
        Guid Id,
        string? Nombre,
        string? Descripcion,
        DateTimeOffset? Fecha,
        string? Ubicacion,
        int Aforo,
        int EntradasVendidas,
        bool? EventoVisible,
        string ObjetoRecaudacion,
        string ImagenURL,
        decimal PrecioGeneral,
        int CantidadGeneral,
        decimal? PrecioVip,
        int? CantidadVip
    );

    public record EventoCreateDto(
        string Nombre,
        string? Descripcion,
        DateTimeOffset? Fecha,
        string? Ubicacion,
        bool EventoVisible,
        string? ObjetoRecaudacion,
        decimal? PrecioGeneral,
        int? CantidadGeneral,
        decimal? PrecioVip,
        int? CantidadVip,
        IFormFile? Imagen
    );

    public class EventoModifyDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public DateTimeOffset? Fecha { get; set; }
        public string? Ubicacion { get; set; }
        public bool? EventoVisible { get; set; }
        public string? ObjetoRecaudacion { get; set; }
        public decimal? PrecioGeneral { get; set; }
        public int? CantidadGeneral { get; set; }
        public decimal? PrecioVip { get; set; }
        public int? CantidadVip { get; set; }
        public IFormFile? Imagen { get; set; }
    }
}