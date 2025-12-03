using Backend.Models;
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
                    .Select("*")
                    .Order("fecha_y_hora", Ordering.Ascending);

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

    public static async Task<IResult> GetEvent(string eventId, Supabase.Client client)
    {
        try
        {
            var response = await client
                .From<Evento>()
                .Where(e => e.Id == Guid.Parse(eventId))
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
                eventoDb.EntradaValida,
                eventoDb.ObjetoRecaudacion ?? "Sin especificar"
            );

            return Results.Ok(eventoDto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error interno: " + ex.Message);
        }
    }

    
    
    
    public static async Task<IResult> StartPurchase(PurchaseStartDto dto, Supabase.Client client)
    {
        if (dto.Quantity <= 0) return Results.BadRequest(new { error = "Cantidad mínima: 1." });

        try
        {
            var tipoResponse = await client
                .From<EntradaEvento>()
                .Where(te => te.IdEntradaEvento == dto.TicketEventId && te.IdEvento == dto.EventId)
                .Single();

            var tipoEntrada = tipoResponse;

            if (tipoEntrada == null)
                return Results.NotFound(new { error = "Ese tipo de entrada no existe." });

            if (tipoEntrada.Cantidad < dto.Quantity)
            {
                return Results.BadRequest(new
                {
                    error = $"No hay suficiente stock para '{tipoEntrada.Tipo}'. Quedan: {tipoEntrada.Cantidad}"
                });
            }

            var evento = await client
                .From<Evento>()
                .Where(e => e.Id == dto.EventId)
                .Single();
            
            if (evento == null)
                return Results.NotFound(new { error = "El evento especificado no existe." });
            
            decimal precioUnitario = tipoEntrada.Precio;
            decimal importeTotal = precioUnitario * dto.Quantity;

            var resumenCarrito = new
            {
                status = "success",
                message = "Disponible. Puede proceder al pago.",
                carrito = new
                {
                    id_evento = evento.Id,
                    nombre_evento = evento.Nombre,

                    id_tipo_entrada = tipoEntrada.IdEntradaEvento,
                    tipo_nombre = tipoEntrada.Tipo,

                    cantidad = dto.Quantity,
                    precio_unitario = precioUnitario,
                    importe_total = importeTotal,

                    direccion_facturacion = dto.BillingAddress,
                    es_empresa = dto.IsCompany
                }
            };

            return Results.Ok(resumenCarrito);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error iniciando la compra: " + ex.Message);
        }
    }

    public static async Task<IResult> ConfirmPurchase(PurchaseConfirmDto dto, Supabase.Client client)
    {
        try
        {
            var userAuth = client.Auth.CurrentUser!;

            var usuarioDb = await client
                .From<Usuario>()
                .Where(u => u.Id == Guid.Parse(userAuth.Id!))
                .Single();

            decimal totalPagar = 0;
            int cantidadTotalTickets = 0;

            var tiposEnBd = new Dictionary<Guid, EntradaEvento>();

            // Diccionario para saber qué eventos actualizar al final
            var eventosAfectados = new Dictionary<Guid, int>();

            foreach (var item in dto.Items.Where(item => item.Quantity > 0))
            {
                var tipoDb = await client.From<EntradaEvento>()
                    .Filter("id_entrada_evento", Operator.Equals, item.TicketEventId)
                    .Single();

                if (tipoDb == null)
                    return Results.BadRequest(new { error = $"El tipo de entrada {item.TicketEventId} no existe." });

                if (tipoDb.Cantidad < item.Quantity)
                    return Results.BadRequest(new
                        { error = $"No hay stock para {tipoDb.Tipo} (Evento ID: {tipoDb.IdEvento})." });

                // Cálculos económicos
                totalPagar += tipoDb.Precio * item.Quantity;
                cantidadTotalTickets += item.Quantity;

                tiposEnBd.Add(item.TicketEventId, tipoDb);

                // Sumamos al contador de este evento específico
                if (!eventosAfectados.ContainsKey(tipoDb.IdEvento))
                    eventosAfectados[tipoDb.IdEvento] = 0;

                eventosAfectados[tipoDb.IdEvento] += item.Quantity;
            }

            if (cantidadTotalTickets == 0) return Results.BadRequest(new { error = "El carrito está vacío." });

            var nuevoPago = new Pago
            {
                Monto = totalPagar,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                IdCliente = usuarioDb.Id
            };

            var pagoResponse = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoResponse.Models.First();

            // Restar Stock y Crear Tickets
            var ticketsNuevos = new List<Entrada>();

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0) continue;

                var tipoDb = tiposEnBd[item.TicketEventId];

                tipoDb.Cantidad = tipoDb.Cantidad - item.Quantity;
                await client.From<EntradaEvento>().Update(tipoDb);

                for (int i = 0; i < item.Quantity; i++)
                {
                    ticketsNuevos.Add(new Entrada
                    {
                        IdUsuario = usuarioDb.Id,
                        IdEvento = tipoDb.IdEvento,
                        IdPago = pagoCreado.Id,
                        FechaCompra = DateTime.UtcNow,
                        Precio = tipoDb.Precio,

                        IdEntradaEvento = tipoDb.IdEntradaEvento,
                    });
                }
            }

            await client.From<Entrada>().Insert(ticketsNuevos);

            // Actualizamos el aforo vendido de cada evento involucrado
            foreach (var (idEvento, cantidadVendida) in eventosAfectados)
            {
                var evento = await client.From<Evento>()
                    .Filter("id_evento", Operator.Equals, idEvento)
                    .Single();

                if (evento != null)
                {
                    evento.EntradasVendidas += cantidadVendida;
                    await client.From<Evento>().Update(evento);
                }
            }

            return Results.Ok(new
            {
                status = "success",
                message = "Compra realizada correctamente.",
                total_pagado = totalPagar,
                eventos_afectados = eventosAfectados.Count
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error en la compra: " + ex.Message);
        }
    }

    public static IResult GetPaymentMethods()
    {
        var metodos = new[]
        {
            new { id = 1, nombre = "Tarjeta de Crédito", codigo = "CARD" },
            new { id = 2, nombre = "Tarjeta de Débito", codigo = "DEBIT" },
            new { id = 3, nombre = "PayPal", codigo = "PAYPAL" },
            new { id = 4, nombre = "Transferencia Bancaria", codigo = "BANK" }
        };

        return Results.Ok(new
        {
            status = "success",
            message = "Métodos de pago disponibles",
            metodos = metodos
        });
    }

    public static async Task<IResult> ValidateDiscount(DiscountCheckDto dto, Supabase.Client client)
    {
        if (string.IsNullOrEmpty(dto.Code))
            return Results.BadRequest(new { error = "El código de descuento no puede estar vacío." });

        try
        {
            // Validación básica de formato (ej: DESCUENTO2025, PROMO50)
            if (dto.Code.Length < 3 || dto.Code.Length > 20)
                return Results.BadRequest(new { error = "El código debe tener entre 3 y 20 caracteres." });

            // Simulación: descuentos válidos conocidos
            var codigosValidos = new Dictionary<string, (decimal porcentaje, string descripcion)>
            {
                { "DESCUENTO2025", (15m, "15% de descuento") },
                { "PROMO50", (25m, "25% de descuento especial") },
                { "NAVIDAD", (10m, "10% Navidad") },
                { "VIP", (30m, "30% VIP") }
            };

            var codigoUpper = dto.Code.ToUpper();

            if (codigosValidos.ContainsKey(codigoUpper))
            {
                var (porcentaje, descripcion) = codigosValidos[codigoUpper];
                return await Task.FromResult(Results.Ok(new
                {
                    status = "success",
                    message = "Código de descuento válido",
                    descuento = new
                    {
                        codigo = codigoUpper,
                        porcentaje = porcentaje,
                        descripcion = descripcion,
                        valido = true
                    }
                }));
            }

            return await Task.FromResult(Results.NotFound(new { error = "El código de descuento no es válido." }));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(Results.Problem("Error validando descuento: " + ex.Message));
        }
    }

    record EventoListDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        DateTime Fecha,
        string? Ubicacion,
        int Aforo,
        int EntradasVendidas,
        bool EntradaValida,
        string ObjetoRecaudacion
    );

    record EventoDto(
        EventoListDto Evento,
        List<TipoEntradaPublicoDto> TiposEntrada
    );

    public record TipoEntradaPublicoDto(
        Guid Id,
        string Nombre,
        decimal Precio,
        int Stock
    );

    public record PurchaseStartDto(
        Guid EventId,
        Guid TicketEventId,
        int Quantity,
        bool IsCompany,
        string BillingAddress,
        string? DiscountCode);

    public record PurchaseItemDto(
        Guid TicketEventId,
        int Quantity
    );

    public record PurchaseConfirmDto(
        string PaymentMethod,
        string PaymentToken,
        List<PurchaseItemDto> Items);

    public record DiscountCheckDto(string Code);
}