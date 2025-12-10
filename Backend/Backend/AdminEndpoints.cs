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
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Results.BadRequest(new { error = "El título del evento es obligatorio." });

            if (dto.Fecha < DateTime.UtcNow)
                return Results.BadRequest(new { error = "La fecha del evento no puede ser en el pasado." });

            if (dto.CantidadGeneral <= 0)
                return Results.BadRequest(new { error = "Debes crear al menos 1 entrada General." });

            if (dto.PrecioGeneral < 0)
                return Results.BadRequest(new { error = "El precio General no puede ser negativo." });

            if (dto is { PrecioVip: not null, CantidadVip: null } ||
                (!dto.PrecioVip.HasValue && dto.CantidadVip.HasValue))
            {
                return Results.BadRequest(new
                    { error = "Para crear entradas VIP debes indicar tanto el precio como la cantidad." });
            }
            
            string? imagenUrlFinal = null;

            if (dto.Imagen != null)
            {
                // Validar nombre/extensión
                var extension = Path.GetExtension(dto.Imagen.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}"; // Nombre único

                // Convertir a bytes
                using var memoryStream = new MemoryStream();
                await dto.Imagen.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Subir a Supabase (Bucket "eventos")
                await client.Storage
                    .From("eventos")
                    .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { Upsert = false });

                // Obtener URL Pública
                imagenUrlFinal = client.Storage.From("eventos").GetPublicUrl(fileName);
            }

            bool tieneVip = dto is { PrecioVip: not null, CantidadVip: > 0 };
            int aforoTotal = dto.CantidadGeneral + (tieneVip ? dto.CantidadVip!.Value : 0);

            var nuevoEvento = new Evento
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                FechaEvento = dto.Fecha,
                Ubicacion = dto.Ubicacion,
                Aforo = aforoTotal,
                EventoVisible = dto.EventoVisible,
                ObjetoRecaudacion = dto.ObjetoRecaudacion,
                ImagenUrl = imagenUrlFinal
            };

            var eventResponse = await client
                .From<Evento>()
                .Insert(nuevoEvento);

            var eventoCreado = eventResponse.Models.First();

            var entradasAInsertar = new List<EntradaEvento>();

            entradasAInsertar.Add(new EntradaEvento
            {
                FkEvento = eventoCreado.Id,
                Tipo = "General",
                Precio = dto.PrecioGeneral,
                Cantidad = dto.CantidadGeneral
            });

            if (tieneVip)
            {
                entradasAInsertar.Add(new EntradaEvento
                {
                    FkEvento = eventoCreado.Id,
                    Tipo = "VIP",
                    Precio = dto.PrecioVip!.Value,
                    Cantidad = dto.CantidadVip!.Value
                });
            }

            await client.From<EntradaEvento>().Insert(entradasAInsertar);

            return Results.Created($"/events/{eventoCreado.Id}", new
            {
                status = "success",
                message = "Evento y tickets creados correctamente.",
                evento = new EventoAdminDto(
                    eventoCreado.Id,
                    eventoCreado.Nombre,
                    eventoCreado.Descripcion,
                    eventoCreado.FechaEvento,
                    eventoCreado.Ubicacion,
                    eventoCreado.Aforo ?? 0,
                    0,
                    eventoCreado.EventoVisible,
                    eventoCreado.ObjetoRecaudacion ?? "Sin especificar",
                    eventoCreado.ImagenUrl,
                    dto.PrecioGeneral,
                    dto.CantidadGeneral,
                    dto.PrecioVip,
                    dto.CantidadVip
                ),
                tickets_creados = entradasAInsertar.Select(t => new { t.Tipo, t.Precio, Stock = t.Cantidad })
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al crear el evento y sus entradas: " + ex.Message);
        }
    }

    public static async Task<IResult> AdminUpdateEvent(string eventId, [FromForm]EventoModifyDto dto, 
        Supabase.Client client)
    {
        try
        {
            var parsed = Guid.Parse(eventId);
            var evento = await client
                .From<Evento>()
                .Where(e => e.Id == parsed)
                .Single();

            if (evento == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}." });

            var ticketsResponse = await client
                .From<EntradaEvento>()
                .Filter("fk_evento", Constants.Operator.Equals, eventId)
                .Get();

            var ticketsDb = ticketsResponse.Models;

            var general = ticketsDb.FirstOrDefault(t => t.Tipo == "General");
            var vip = ticketsDb.FirstOrDefault(t => t.Tipo == "VIP");

            bool huboCambiosEvento = false;
            
            if (dto.Imagen != null)
            {
                var extension = Path.GetExtension(dto.Imagen.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";

                using var memoryStream = new MemoryStream();
                await dto.Imagen.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                await client.Storage
                    .From("eventos")
                    .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { Upsert = false });

                // Actualizamos la URL en el objeto evento
                evento.ImagenUrl = client.Storage.From("eventos").GetPublicUrl(fileName);
                huboCambiosEvento = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                evento.Nombre = dto.Nombre;
                huboCambiosEvento = true;
            }

            if (dto.Descripcion != null)
            {
                evento.Descripcion = dto.Descripcion;
                huboCambiosEvento = true;
            }

            if (dto.Fecha.HasValue)
            {
                if (dto.Fecha.Value < DateTime.UtcNow) return Results.BadRequest(new { error = "Fecha inválida." });
                evento.FechaEvento = dto.Fecha.Value;
                huboCambiosEvento = true;
            }

            if (dto.Ubicacion != null)
            {
                evento.Ubicacion = dto.Ubicacion;
                huboCambiosEvento = true;
            }

            if (dto.EventoVisible.HasValue)
            {
                evento.EventoVisible = dto.EventoVisible.Value;
                huboCambiosEvento = true;
            }

            if (dto.ObjetoRecaudacion != null)
            {
                evento.ObjetoRecaudacion = dto.ObjetoRecaudacion;
                huboCambiosEvento = true;
            }

            if (general != null)
            {
                bool cambioG = false;
                if (dto.PrecioGeneral.HasValue)
                {
                    general.Precio = dto.PrecioGeneral.Value;
                    cambioG = true;
                }

                if (dto.CantidadGeneral.HasValue)
                {
                    general.Cantidad = dto.CantidadGeneral.Value;
                    cambioG = true;
                }

                if (cambioG) await client.From<EntradaEvento>().Update(general);
            }

            if (vip != null)
            {
                bool cambioV = false;
                if (dto.PrecioVip.HasValue)
                {
                    vip.Precio = dto.PrecioVip.Value;
                    cambioV = true;
                }

                if (dto.CantidadVip.HasValue)
                {
                    vip.Cantidad = dto.CantidadVip.Value;
                    cambioV = true;
                }

                if (cambioV) await client.From<EntradaEvento>().Update(vip);
            }
            else
            {
                if (dto.PrecioVip.HasValue && dto.CantidadVip.HasValue)
                {
                    var nuevaVip = new EntradaEvento
                    {
                        FkEvento = evento.Id,
                        Tipo = "VIP",
                        Precio = dto.PrecioVip.Value,
                        Cantidad = dto.CantidadVip.Value
                    };

                    await client.From<EntradaEvento>().Insert(nuevaVip);
                    vip = nuevaVip; // Asignamos a la variable para usarla abajo
                }
            }

            if (dto.CantidadGeneral.HasValue || dto.CantidadVip.HasValue)
            {
                int nuevoGen = dto.CantidadGeneral ?? (general?.Cantidad ?? 0);
                int nuevoVip = dto.CantidadVip ?? (vip?.Cantidad ?? 0);

                evento.Aforo = nuevoGen + nuevoVip + evento.EntradasVendidas;
                huboCambiosEvento = true;
            }

            if (huboCambiosEvento)
            {
                await client.From<Evento>().Update(evento);
            }

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

                    // Datos planos
                    general?.Precio ?? 0,
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
        DateTime Fecha,
        string? Ubicacion,
        bool EventoVisible,
        string ObjetoRecaudacion,
        decimal PrecioGeneral,
        int CantidadGeneral,
        decimal? PrecioVip,
        int? CantidadVip,
        IFormFile? Imagen
    );

    public record EventoModifyDto(
        string? Nombre,
        string? Descripcion,
        DateTime? Fecha,
        string? Ubicacion,
        int? Aforo,
        bool? EventoVisible,
        string? ObjetoRecaudacion,
        decimal? PrecioGeneral,
        int? CantidadGeneral,
        decimal? PrecioVip,
        int? CantidadVip,
        IFormFile? Imagen
    );
}