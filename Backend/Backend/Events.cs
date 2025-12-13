﻿using Backend.Models;
using Backend.Services;
using static Supabase.Postgrest.Constants;

namespace Backend;

static class Events
{
    public static async Task<IResult> ListEvents(string? query, Supabase.Client client)
    {
        try
        {
            var dbQuery =
                client.From<Evento>()
                    .Filter("evento_visible", Operator.Equals, "true")
                    .Order(e => e.FechaEvento!, Ordering.Ascending);

            if (!string.IsNullOrEmpty(query))
                dbQuery = dbQuery.Filter("nombre", Operator.ILike, $"%{query}%");

            var response = await dbQuery.Get();
            var eventosDb = response.Models;
            
            if (!eventosDb.Any()) 
                return Results.Ok(new List<EventoListDto>());
            
            // Obtenemos solo los IDs de los eventos cargados
            var eventIds = eventosDb.Select(e => e.Id.ToString()).ToList();
            
            // Traemos solo el precio y la FK de las entradas que pertenecen a estos eventos.
            var entradasResponse = await client.From<Entrada>()
                .Select("fk_evento, precio") 
                .Filter("fk_evento", Operator.In, eventIds)
                .Get();
            
            var todasLasEntradas = entradasResponse.Models;

            // Mapeamos y calculamos
            var eventos = eventosDb.Select(e =>
            {
                // Sumamos los precios de las entradas de este evento específico
                decimal recaudadoEntradas = todasLasEntradas
                    .Where(t => t.FkEvento == e.Id)
                    .Sum(t => t.Precio);

                // Sumamos lo recaudado por entradas + lo extra (ej. patrocinios)
                decimal totalRecaudado = recaudadoEntradas + (e.RecaudacionExtra ?? 0);

                return new EventoListDto(
                    e.Id,
                    e.Nombre,
                    e.Descripcion,
                    e.FechaEvento,
                    e.Ubicacion,
                    e.Aforo ?? 0,
                    e.EntradasVendidas,
                    e.ObjetivoRecaudacion ?? 0,
                    totalRecaudado,
                    e.ImagenUrl
                );
            });

            return Results.Ok(eventos);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al obtener eventos: " + ex.Message);
        }
    }

    public static async Task<IResult> GetEvent(string eventId, Supabase.Client client)
    {
        try
        {
            var parsed = Guid.Parse(eventId);

            var response = await client
                .From<Evento>()
                .Where(e => e.Id == parsed)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}" });
            
            var entradasResponse = await client.From<Entrada>()
                .Select("precio") // Solo necesitamos el precio
                .Filter("fk_evento", Operator.Equals, eventId)
                .Get();

            decimal recaudadoEntradas = entradasResponse.Models.Sum(t => t.Precio);
            
            decimal totalRecaudado = recaudadoEntradas + (eventoDb.RecaudacionExtra ?? 0);

            var eventoDto = new EventoListDto(
                eventoDb.Id,
                eventoDb.Nombre,
                eventoDb.Descripcion,
                eventoDb.FechaEvento,
                eventoDb.Ubicacion,
                eventoDb.Aforo ?? 0,
                eventoDb.EntradasVendidas,
                eventoDb.ObjetivoRecaudacion ?? 0,
                totalRecaudado,
                eventoDb.ImagenUrl
            );

            return Results.Ok(eventoDto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error interno: " + ex.Message);
        }
    }

    public record EventoListDto(
        Guid Id,
        string? Nombre,
        string? Descripcion,
        DateTimeOffset? Fecha,
        string? Ubicacion,
        int Aforo,
        int EntradasVendidas,
        decimal ObjetivoRecaudacion,
        decimal TotalRecaudado,
        string? ImagenUrl
    );

    public record TipoEntradaPublicoDto(
        Guid Id,
        string Nombre,
        decimal Precio,
        int Stock
    );
}