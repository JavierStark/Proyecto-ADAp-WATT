using Backend.Models;
using Supabase.Postgrest;

namespace Backend;

static class Admin
{
    public static async Task<IResult> AdminListEvents(Supabase.Client client)
    {
        try
        {
            var eventos = (await client
                .From<Evento>()
                .Order("fecha_y_hora", Constants.Ordering.Descending)
                .Get()).Models;
            
            var entradas = (await client
                .From<EntradaEvento>()
                .Get()).Models; 

            var eventosDto = eventos.Select(e => {
                
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
                    e.EntradaValida,
                    e.ObjetoRecaudacion ?? "Sin especificar",
                    
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
    
    public static async Task<IResult> AdminCreateEvent(EventoCreateDto dto, Supabase.Client client)
    {
        try
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Results.BadRequest(new { error = "El título del evento es obligatorio." });

            if (dto.Fecha < DateTime.UtcNow)
                return Results.BadRequest(new { error = "La fecha del evento no puede ser en el pasado." });

            if (dto.CantidadGeneral <= 0)
                return Results.BadRequest(new { error = "Debes crear al menos 1 entrada General." });

            if (dto.PrecioGeneral < 0)
                return Results.BadRequest(new { error = "El precio General no puede ser negativo." });

            // Validación VIP 
            bool tieneVip = dto.PrecioVip.HasValue && dto.CantidadVip.HasValue && dto.CantidadVip.Value > 0;
            if ((dto.PrecioVip.HasValue && !dto.CantidadVip.HasValue) || (!dto.PrecioVip.HasValue && dto.CantidadVip.HasValue))
            {
                return Results.BadRequest(new { error = "Para crear entradas VIP debes indicar tanto el precio como la cantidad." });
            }
            
            int aforoTotal = dto.CantidadGeneral + (tieneVip ? dto.CantidadVip!.Value : 0);
            
            var nuevoEvento = new Evento
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                FechaEvento = dto.Fecha,
                Ubicacion = dto.Ubicacion,
                Aforo = aforoTotal,
                EntradaValida = dto.EntradaValida,
                ObjetoRecaudacion = dto.ObjetoRecaudacion
            };

            var eventResponse = await client
                .From<Evento>()
                .Insert(nuevoEvento);

            var eventoCreado = eventResponse.Models.First();

            // Insertar las entradas
            var entradasAInsertar = new List<EntradaEvento>();

            // Entrada General
            entradasAInsertar.Add(new EntradaEvento
            {
                FkEvento = eventoCreado.Id,
                Tipo = "General",
                Precio = dto.PrecioGeneral,
                Cantidad = dto.CantidadGeneral
            });

            // Entrada VIP
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
                    eventoCreado.EntradaValida,
                    eventoCreado.ObjetoRecaudacion ?? "Sin especificar",
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
    
    public static async Task<IResult> AdminUpdateEvent(int eventId, EventoModifyDto dto, Supabase.Client client)
{
    try
    {
        var eventResponse = await client
            .From<Evento>()
            .Filter("id_evento", Constants.Operator.Equals, eventId)
            .Single();

        var eventoDb = eventResponse;

        if (eventoDb == null)
            return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}." });
        
        var ticketsResponse = await client
            .From<EntradaEvento>()
            .Filter("id_evento", Constants.Operator.Equals, eventId)
            .Get();

        var ticketsDb = ticketsResponse.Models;
        
        var general = ticketsDb.FirstOrDefault(t => t.Tipo == "General");
        var vip = ticketsDb.FirstOrDefault(t => t.Tipo == "VIP");
        
        bool huboCambiosEvento = false;

        if (!string.IsNullOrWhiteSpace(dto.Nombre)) { eventoDb.Nombre = dto.Nombre; huboCambiosEvento = true; }
        if (dto.Descripcion != null) { eventoDb.Descripcion = dto.Descripcion; huboCambiosEvento = true; }
        
        if (dto.Fecha.HasValue) {
            if (dto.Fecha.Value < DateTime.UtcNow) return Results.BadRequest(new { error = "Fecha inválida." });
            eventoDb.FechaEvento = dto.Fecha.Value; huboCambiosEvento = true;
        }
        
        if (dto.Ubicacion != null) { eventoDb.Ubicacion = dto.Ubicacion; huboCambiosEvento = true; }
        if (dto.EntradaValida.HasValue) { eventoDb.EntradaValida = dto.EntradaValida.Value; huboCambiosEvento = true; }
        if (dto.ObjetoRecaudacion != null) { eventoDb.ObjetoRecaudacion = dto.ObjetoRecaudacion; huboCambiosEvento = true; }

        if (general != null)
        {
            bool cambioG = false;
            if (dto.PrecioGeneral.HasValue) { general.Precio = dto.PrecioGeneral.Value; cambioG = true; }
            if (dto.CantidadGeneral.HasValue) { general.Cantidad = dto.CantidadGeneral.Value; cambioG = true; }
            
            if (cambioG) await client.From<EntradaEvento>().Update(general);
        }
        
        if (vip != null)
        {
            bool cambioV = false;
            if (dto.PrecioVip.HasValue) { vip.Precio = dto.PrecioVip.Value; cambioV = true; }
            if (dto.CantidadVip.HasValue) { vip.Cantidad = dto.CantidadVip.Value; cambioV = true; }
            
            if (cambioV) await client.From<EntradaEvento>().Update(vip);
        }
        else 
        {
            if (dto.PrecioVip.HasValue && dto.CantidadVip.HasValue)
            {
                var nuevaVip = new EntradaEvento
                {
                    FkEvento = eventoDb.Id,
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
            
            eventoDb.Aforo = nuevoGen + nuevoVip + eventoDb.EntradasVendidas;
            huboCambiosEvento = true;
        }
        
        if (huboCambiosEvento)
        {
            await client.From<Evento>().Update(eventoDb);
        }
        
        return Results.Ok(new
        {
            status = "success",
            message = "Evento actualizado correctamente.",
            evento = new EventoAdminDto(
                eventoDb.Id,
                eventoDb.Nombre,
                eventoDb.Descripcion,
                eventoDb.FechaEvento,
                eventoDb.Ubicacion,
                eventoDb.Aforo ?? 0,
                eventoDb.EntradasVendidas,
                eventoDb.EntradaValida,
                eventoDb.ObjetoRecaudacion ?? "Sin especificar",
                
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
    
    public static async Task<IResult> AdminDeleteEvent(int eventId, Supabase.Client client)
    {
        try
        {
            // Verificar que el evento existe
            var response = await client
                .From<Evento>()
                .Filter("id_evento", Constants.Operator.Equals, eventId)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}." });

            // Eliminar el evento
            await client
                .From<Evento>()
                .Filter("id_evento", Constants.Operator.Equals, eventId)
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
        bool? EntradaValida,
        string ObjetoRecaudacion,
        
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
        bool EntradaValida,
        string ObjetoRecaudacion,
        
        decimal PrecioGeneral,
        int CantidadGeneral,
        
        decimal? PrecioVip,
        int? CantidadVip
    );
    public record EventoModifyDto(
        Guid Id,
        string? Nombre,
        string? Descripcion,
        DateTime? Fecha,
        string? Ubicacion,
        int? Aforo,
        bool? EntradaValida,
        string? ObjetoRecaudacion,
        
        decimal? PrecioGeneral,
        int? CantidadGeneral,
            
        decimal? PrecioVip,
        int? CantidadVip
    );
}