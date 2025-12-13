﻿﻿﻿using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;

namespace Backend;

public static class AdminEndpoints
{
    public static async Task<IResult> AdminListEvents(Supabase.Client client)
    {
        try
        {
            var eventos = (await client
                .From<Evento>()
                .Order(e => e.FechaEvento, Constants.Ordering.Descending)
                .Get()).Models;

            var entradasTipos = (await client
                .From<EntradaEvento>()
                .Get()).Models;
            
            var ventasResponse = await client
                .From<Entrada>()
                .Select("fk_evento, fk_entrada_evento, precio") 
                .Get();
            
            var todasLasVentas = ventasResponse.Models;

            var eventosDto = eventos.Select(e =>
            {
                // Filtros en memoria para este evento
                var tiposEvento = entradasTipos.Where(en => en.FkEvento == e.Id).ToList();
                var ventasEvento = todasLasVentas.Where(v => v.FkEvento == e.Id).ToList();
                
                var general = tiposEvento.FirstOrDefault(en => en.Tipo == "General");
                var vip = tiposEvento.FirstOrDefault(en => en.Tipo == "VIP");

                // Cálculo de dinero real ingresado por entradas
                decimal dineroEntradas = ventasEvento.Sum(v => v.Precio);

                // Cálculo del TOTAL GLOBAL (Entradas + Extra)
                decimal totalRecaudado = dineroEntradas + (e.RecaudacionExtra ?? 0);

                return new EventoAdminDto(
                    e.Id,
                    e.Nombre,
                    e.Descripcion,
                    e.FechaEvento,
                    e.Ubicacion,
                    e.Aforo ?? 0,
                    e.EntradasVendidas,
                    e.EventoVisible,
                    e.ObjetivoRecaudacion ?? 0,
                    e.RecaudacionExtra ?? 0,
                    totalRecaudado, // <--- TOTAL CALCULADO
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
                
                var bucket = client.Storage.From("eventos");

                _ = Task.Run(async () =>
                    {
                        try
                        {
                            await bucket
                                .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { Upsert = false });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                );

                imagenUrlFinal = bucket.GetPublicUrl(fileName);
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
                ObjetivoRecaudacion = dto.ObjetivoRecaudacion,
                RecaudacionExtra = dto.RecaudacionExtra,
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
            
            decimal totalInicial = eventoCreado.RecaudacionExtra ?? 0;

            return Results.Created($"/events/{eventoCreado.Id}", new AdminEventCreateResponseDto(
                "success",
                "Evento y tickets creados correctamente.",
                new EventoAdminDto(
                    eventoCreado.Id,
                    eventoCreado.Nombre,
                    eventoCreado.Descripcion,
                    eventoCreado.FechaEvento,
                    eventoCreado.Ubicacion,
                    eventoCreado.Aforo ?? 0,
                    0,
                    eventoCreado.EventoVisible,
                    eventoCreado.ObjetivoRecaudacion ?? 0,
                    eventoCreado.RecaudacionExtra ?? 0,
                    totalInicial,
                    eventoCreado.ImagenUrl ?? "",
                    dto.PrecioGeneral,
                    dto.CantidadGeneral,
                    dto.PrecioVip,
                    dto.CantidadVip
                ),
                entradasAInsertar.Select(t => new TicketTypeCreatedDto(t.Tipo!, t.Precio, t.Cantidad))
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al crear el evento y sus entradas: " + ex.Message);
        }
    }

    public static async Task<IResult> AdminUpdateEvent(string eventId, [FromForm] EventoModifyDto dto, Supabase.Client client)
{
    try
    {
        var parsedId = Guid.Parse(eventId);

        // Validaciones
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Results.BadRequest(new { error = "El nombre del evento es obligatorio." });

        if (!dto.Fecha.HasValue)
            return Results.BadRequest(new { error = "La fecha es obligatoria." });
            
        if (dto.Fecha < DateTime.UtcNow)
            return Results.BadRequest(new { error = "La fecha del evento no puede ser en el pasado." });

        if ((dto.PrecioGeneral ?? 0) < 0 || (dto.PrecioVip ?? 0) < 0)
            return Results.BadRequest(new { error = "Los precios no pueden ser negativos." });

        if ((dto.CantidadGeneral ?? 0) < 0 || (dto.CantidadVip ?? 0) < 0)
            return Results.BadRequest(new { error = "Las cantidades no pueden ser negativas." });


        // Obtener datos actuales
        var eventoModel = await client
            .From<Evento>()
            .Where(e => e.Id == parsedId)
            .Single();

        if (eventoModel == null)
            return Results.NotFound(new { error = "Evento no encontrado." });

        var ticketsResponse = await client
            .From<EntradaEvento>()
            .Filter("fk_evento", Constants.Operator.Equals, eventId)
            .Get();
        
        var ticketsDb = ticketsResponse.Models;
        var ticketGeneral = ticketsDb.FirstOrDefault(t => t.Tipo == "General");
        var ticketVip = ticketsDb.FirstOrDefault(t => t.Tipo == "VIP");


        // Imagen
        if (dto.Imagen != null && dto.Imagen.Length > 0)
        {
            var extension = Path.GetExtension(dto.Imagen.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var bucket = client.Storage.From("eventos");
            
            var urlAntigua = eventoModel.ImagenUrl;
            
            using var memoryStream = new MemoryStream();
            await dto.Imagen.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    // Subir la nueva imagen
                    await bucket.Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { Upsert = false });

                    // Borrar la antigua si existía
                    if (!string.IsNullOrEmpty(urlAntigua))
                    {
                        var nombreViejo = GetFileNameFromUrl(urlAntigua);
                        if (nombreViejo != null)
                        {
                            await bucket.Remove(new List<string> { nombreViejo });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error subiendo imagen en background: {ex.Message}");
                }
            });

            // Asignar la nueva URL
            eventoModel.ImagenUrl = bucket.GetPublicUrl(fileName);
        }
        
        // Update
        eventoModel.Nombre = dto.Nombre;
        eventoModel.FechaEvento = dto.Fecha.Value;
        eventoModel.Descripcion = dto.Descripcion ?? eventoModel.Descripcion;
        eventoModel.Ubicacion = dto.Ubicacion ?? eventoModel.Ubicacion;
        eventoModel.ObjetivoRecaudacion = dto.ObjetivoRecaudacion ?? eventoModel.ObjetivoRecaudacion;
        
        if (dto.EventoVisible.HasValue) 
            eventoModel.EventoVisible = dto.EventoVisible.Value;
        
        if (dto.RecaudacionExtra.HasValue)
        {
            eventoModel.RecaudacionExtra = dto.RecaudacionExtra.Value;
        }

        // Entradas generales
        if (ticketGeneral != null)
        {
            ticketGeneral.Precio = dto.PrecioGeneral ?? ticketGeneral.Precio;
            ticketGeneral.Cantidad = dto.CantidadGeneral ?? ticketGeneral.Cantidad;
            await client.From<EntradaEvento>().Update(ticketGeneral);
        }
        else if (dto.CantidadGeneral.HasValue && dto.PrecioGeneral.HasValue)
        {
            ticketGeneral = new EntradaEvento 
            { 
                FkEvento = parsedId, Tipo = "General", 
                Precio = dto.PrecioGeneral.Value, Cantidad = dto.CantidadGeneral.Value 
            };
            await client.From<EntradaEvento>().Insert(ticketGeneral);
        }

        // Entradas VIP
        if (ticketVip != null)
        {
            ticketVip.Precio = dto.PrecioVip ?? ticketVip.Precio;
            ticketVip.Cantidad = dto.CantidadVip ?? ticketVip.Cantidad;
            await client.From<EntradaEvento>().Update(ticketVip);
        }
        else if (dto.PrecioVip.HasValue && dto.CantidadVip.HasValue && dto.CantidadVip > 0)
        {
            ticketVip = new EntradaEvento 
            { 
                FkEvento = parsedId, Tipo = "VIP", 
                Precio = dto.PrecioVip.Value, Cantidad = dto.CantidadVip.Value 
            };
            await client.From<EntradaEvento>().Insert(ticketVip);
        }

        // Aforo
        int stockGeneral = dto.CantidadGeneral ?? (ticketGeneral?.Cantidad ?? 0);
        int stockVip = dto.CantidadVip ?? (ticketVip?.Cantidad ?? 0);
        eventoModel.Aforo = stockGeneral + stockVip + eventoModel.EntradasVendidas;

        // Guardar evento
        await client.From<Evento>().Update(eventoModel);

        var ventasResponse = await client.From<Entrada>()
            .Select("precio")
            .Filter("fk_evento", Constants.Operator.Equals, eventId)
            .Get();
            
        decimal dineroEntradas = ventasResponse.Models.Sum(v => v.Precio);
        decimal totalRecaudado = dineroEntradas + (eventoModel.RecaudacionExtra ?? 0);

        return Results.Ok(new AdminEventUpdateResponseDto(
            "success",
            "Evento actualizado correctamente.",
            new EventoAdminDto(
                eventoModel.Id,
                eventoModel.Nombre,
                eventoModel.Descripcion,
                eventoModel.FechaEvento,
                eventoModel.Ubicacion,
                eventoModel.Aforo ?? 0,
                eventoModel.EntradasVendidas,
                eventoModel.EventoVisible,
                eventoModel.ObjetivoRecaudacion ?? 0,
                eventoModel.RecaudacionExtra ?? 0,
                totalRecaudado,
                eventoModel.ImagenUrl ?? "",
                ticketGeneral?.Precio ?? 0,
                ticketGeneral?.Cantidad ?? 0,
                ticketVip?.Precio,
                ticketVip?.Cantidad
            )
        ));
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

            return Results.Ok(new AdminEventDeleteResponseDto(
                "success",
                $"Evento '{eventoDb.Nombre}' eliminado correctamente."
            ));
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

    public record EventoAdminDto(
        Guid Id,
        string? Nombre,
        string? Descripcion,
        DateTimeOffset? Fecha,
        string? Ubicacion,
        int Aforo,
        int EntradasVendidas,
        bool? EventoVisible,
        decimal ObjetivoRecaudacion,
        decimal RecaudacionExtra,
        decimal TotalRecaudado,
        string ImagenURL,
        decimal PrecioGeneral,
        int CantidadGeneral,
        decimal? PrecioVip,
        int? CantidadVip
    );

    public class EventoCreateDto
    {
        public string Nombre { get; set; } = string.Empty; 
        public string? Descripcion { get; set; }
        public DateTimeOffset? Fecha { get; set; } 
        public string? Ubicacion { get; set; }
        public bool EventoVisible { get; set; }
        public decimal? ObjetivoRecaudacion { get; set; }
        public decimal? RecaudacionExtra { get; set; }
        public decimal PrecioGeneral { get; set; }
        public int CantidadGeneral { get; set; }
        public decimal? PrecioVip { get; set; }
        public int? CantidadVip { get; set; }
        public IFormFile? Imagen { get; set; }
    }

    public class EventoModifyDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public DateTimeOffset? Fecha { get; set; }
        public string? Ubicacion { get; set; }
        public bool? EventoVisible { get; set; }
        public decimal? ObjetivoRecaudacion { get; set; }
        public decimal? RecaudacionExtra { get; set; }
        public decimal? PrecioGeneral { get; set; }
        public int? CantidadGeneral { get; set; }
        public decimal? PrecioVip { get; set; }
        public int? CantidadVip { get; set; }
        public IFormFile? Imagen { get; set; }
    }
}