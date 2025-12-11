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


            var eventos = response.Models.Select(e => new EventoListDto(
                e.Id,
                e.Nombre,
                e.Descripcion,
                e.FechaEvento,
                e.Ubicacion,
                e.Aforo ?? 0,
                e.EntradasVendidas,
                e.ObjetoRecaudacion ?? "Sin especificar",
                e.ImagenUrl
            ));

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

            var eventoDto = new EventoListDto(
                eventoDb.Id,
                eventoDb.Nombre,
                eventoDb.Descripcion,
                eventoDb.FechaEvento,
                eventoDb.Ubicacion,
                eventoDb.Aforo ?? 0,
                eventoDb.EntradasVendidas,
                eventoDb.ObjetoRecaudacion ?? "Sin especificar",
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
        string ObjetoRecaudacion,
        string? ImagenUrl
    );

    public record TipoEntradaPublicoDto(
        Guid Id,
        string Nombre,
        decimal Precio,
        int Stock
    );
}