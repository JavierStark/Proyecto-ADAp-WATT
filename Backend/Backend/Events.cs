using Backend.Models;
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
                eventoDb.ObjetoRecaudacion ?? "Sin especificar"
            );

            return Results.Ok(eventoDto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error interno: " + ex.Message);
        }
    }

    public static async Task<IResult> StartPurchase(PurchaseStartDto dto, Supabase.Client client, HttpContext httpContext)
    {
        if (dto.Quantity <= 0) return Results.BadRequest(new { error = "Cantidad mínima: 1." });

        try
        {
            var userId = (string)httpContext.Items["user_id"]!;
            
            var usuarioDb = await client.From<Usuario>()
                .Filter("id", Operator.Equals, userId)
                .Single();
            
            var tipoEntrada = await client.From<EntradaEvento>()
                .Filter("id", Operator.Equals, dto.TicketEventId.ToString()) 
                .Single();

            if (tipoEntrada == null) return Results.NotFound(new { error = "Entrada no encontrada." });
            if (tipoEntrada.FkEvento != dto.EventId)
                return Results.BadRequest(new { error = "La entrada no coincide con el evento." });

            // CALCULAR STOCK REAL
            var ahora = DateTime.UtcNow;
            
            var reservasActivas = await client.From<ReservaEntrada>()
                .Filter("fk_entrada_evento", Operator.Equals, dto.TicketEventId.ToString())
                .Filter("fecha_expiracion", Operator.GreaterThan, ahora.ToString("o"))
                .Get();

            int cantidadBloqueada = reservasActivas.Models.Sum(r => r.Cantidad);
            int stockDisponible = tipoEntrada.Cantidad - cantidadBloqueada;

            if (stockDisponible < dto.Quantity)
            {
                return Results.BadRequest(new { error = $"Solo quedan {stockDisponible} entradas disponibles." });
            }

            // CREAR RESERVA
            var nuevaReserva = new ReservaEntrada
            {
                FkEntradaEvento = tipoEntrada.FkEntradaEvento,
                FkUsuario = usuarioDb.Id,
                Cantidad = dto.Quantity,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(10)
            };

            await client.From<ReservaEntrada>().Insert(nuevaReserva);

            var evento = await client.From<Evento>().Filter("id", Operator.Equals, dto.EventId.ToString()).Single();

            return Results.Ok(new
            {
                status = "success",
                message = "Entradas reservadas por 10 minutos.",
                carrito = new
                {
                    evento = evento?.Nombre,
                    tipo = tipoEntrada.Tipo,
                    cantidad = dto.Quantity,
                    total = tipoEntrada.Precio * dto.Quantity,
                    expira_en = nuevaReserva.FechaExpiracion
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error en reserva: " + ex.Message);
        }
    }

    public static async Task<IResult> GetMyReservations(Supabase.Client client, HttpContext httpContext)
    {
        try
        {
            if (httpContext.Items["user_auth"] is not string userAuth)
                return Results.Unauthorized();

            var usuarioDb = await client.From<Usuario>()
                .Filter("id", Operator.Equals, userAuth)
                .Single();

            var reservasResponse = await client.From<ReservaEntrada>()
                .Filter("fk_usuario", Operator.Equals, usuarioDb.Id.ToString())
                .Filter("fecha_expiracion", Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
                .Get();

            var misReservas = reservasResponse.Models;

            if (!misReservas.Any()) return Results.Ok(new List<ReservationDto>());

            var idsTipos = misReservas.Select(r => r.FkEntradaEvento).Distinct().ToList();
            var infoTipos = new Dictionary<Guid, (string Tipo, decimal Precio, Guid IdEvento)>();
            var infoEventos = new Dictionary<Guid, string>();

            foreach (var idTipo in idsTipos)
            {
                var tipo = await client.From<EntradaEvento>().Filter("id", Operator.Equals, idTipo.ToString()).Single();
                
                if (tipo == null) continue;
                if (infoEventos.ContainsKey(tipo.FkEvento)) continue;
                
                
                infoTipos[idTipo] = (tipo.Tipo, tipo.Precio, tipo.FkEvento);


                var evt = await client.From<Evento>().Filter("id", Operator.Equals, tipo.FkEvento.ToString())
                    .Single();
                if (evt != null) infoEventos[tipo.FkEvento] = evt.Nombre ?? "Evento";
            }

            // Mapear a DTO
            var resultado = misReservas.Select(r =>
            {
                var info = infoTipos.ContainsKey(r.FkEntradaEvento)
                    ? infoTipos[r.FkEntradaEvento]
                    : (Tipo: "Desconocido", Precio: 0m, IdEvento: Guid.Empty);

                var nombreEvento = infoEventos.ContainsKey(info.IdEvento)
                    ? infoEventos[info.IdEvento]
                    : "Desconocido";

                // Cálculo del tiempo restante
                var expiracionUtc = r.FechaExpiracion.Kind == DateTimeKind.Utc
                    ? r.FechaExpiracion
                    : r.FechaExpiracion.ToUniversalTime();

                // 2. Restamos contra UTC real
                var tiempoRestante = expiracionUtc - DateTime.UtcNow;

                string reloj = tiempoRestante.TotalSeconds <= 0
                    ? "00:00"
                    : $"{(int)tiempoRestante.TotalMinutes:D2}:{tiempoRestante.Seconds:D2}";

                return new ReservationDto(
                    r.IdReserva,
                    nombreEvento,
                    info.Tipo,
                    r.Cantidad,
                    info.Precio,
                    info.Precio * r.Cantidad,
                    reloj
                );
            }).ToList();

            return Results.Ok(resultado);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo carrito: " + ex.Message);
        }
    }

    public static async Task<IResult> ConfirmPurchase(PurchaseConfirmDto dto, Supabase.Client client,
        IPaymentService paymentService, HttpContext httpContext)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;

            var usuarioDb = await client.From<Usuario>()
                .Filter("id", Operator.Equals, userId)
                .Single();


            if (!dto.ReservationIds.Any())
                return Results.BadRequest(new { error = "No has seleccionado ninguna reserva." });

            // Obtenemos las reseervas a partir de sus Ids
            var responseReservas = await client.From<ReservaEntrada>()
                .Filter("fk_usuario", Operator.Equals, usuarioDb.Id.ToString())
                .Filter("fecha_expiracion", Operator.GreaterThan, DateTime.UtcNow.ToString("o")) // Solo válidas
                .Get();

            var reservasAConfirmar = responseReservas.Models
                .Where(r => dto.ReservationIds.Contains(r.IdReserva))
                .ToList();

            if (reservasAConfirmar.Count != dto.ReservationIds.Count)
            {
                return Results.BadRequest(new
                    { error = "Algunas reservas han caducado o no existen. Vuelve a iniciar la compra." });
            }

            decimal totalPagar = 0;
            var eventosAfectados = new Dictionary<Guid, int>();

            var idsTipos = reservasAConfirmar.Select(r => r.FkEntradaEvento).Distinct().ToList();
            var preciosDict = new Dictionary<Guid, (decimal Precio, Guid IdEvento)>();

            foreach (var idTipo in idsTipos)
            {
                var tipo = await client.From<EntradaEvento>().Filter("id", Operator.Equals, idTipo.ToString()).Single();
                if (tipo != null) preciosDict.Add(idTipo, (tipo.Precio, tipo.FkEvento));
            }

            foreach (var reserva in reservasAConfirmar)
            {
                if (!preciosDict.TryGetValue(reserva.FkEntradaEvento, out var info)) continue;
                
                
                totalPagar += info.Precio * reserva.Cantidad;

                // Contabilizar para aforo global
                eventosAfectados.TryAdd(info.IdEvento, 0);
                eventosAfectados[info.IdEvento] += reserva.Cantidad;
            }

            if (totalPagar == 0) return Results.BadRequest(new { error = "Carrito vacío." });

            // PASARELA DE PAGO (STRIPE REAL + SIMULACIÓN)
            // Convertimos a céntimos
            long cantidadEnCentimos = (long)(totalPagar * 100);
            
            try 
            {
                await paymentService.ProcessPaymentAsync(totalPagar, dto.PaymentToken);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            // Crear Pago
            var nuevoPago = new Pago
            {
                Monto = totalPagar,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                FkCliente = usuarioDb.Id
            };
            var pagoRes = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoRes.Models.First();

            // Procesar Entradas y Stock
            var ticketsNuevos = new List<Entrada>();

            foreach (var reserva in reservasAConfirmar)
            {
                var infoTipo = preciosDict[reserva.FkEntradaEvento];

                // Restar Stock de la tabla 'entrada_evento'
                var tipoDb = await client.From<EntradaEvento>()
                    .Filter("id", Operator.Equals, reserva.FkEntradaEvento.ToString()).Single();
                if (tipoDb != null)
                {
                    tipoDb.Cantidad -= reserva.Cantidad; // Restamos lo que se ha comprado
                    await client.From<EntradaEvento>().Update(tipoDb);
                }

                // Crear entradas
                for (int i = 0; i < reserva.Cantidad; i++)
                {
                    ticketsNuevos.Add(new Entrada
                    {
                        FkUsuario = usuarioDb.Id,
                        FkEvento = infoTipo.IdEvento,
                        FkPago = pagoCreado.Id,
                        FechaCompra = DateTime.UtcNow,
                        Precio = infoTipo.Precio,
                        FkEntradaEvento = reserva.FkEntradaEvento,
                        CodigoQr = Guid.NewGuid().ToString()
                    });
                }

                // Borrar la reserva procesada
                await client.From<ReservaEntrada>().Delete(reserva);
            }

            await client.From<Entrada>().Insert(ticketsNuevos);

            // Actualizar las entradas vendidas
            foreach (var kvp in eventosAfectados)
            {
                var evento = await client.From<Evento>().Filter("id", Operator.Equals, kvp.Key.ToString()).Single();
                if (evento != null)
                {
                    evento.EntradasVendidas += kvp.Value;
                    await client.From<Evento>().Update(evento);
                }
            }

            return Results.Ok(new { status = "success", message = "Compra realizada." });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error: " + ex.Message);
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

            if (!codigosValidos.ContainsKey(codigoUpper))
                return await Task.FromResult(Results.NotFound(new { error = "El código de descuento no es válido." }));

            var (porcentaje, descripcion) = codigosValidos[codigoUpper];
            return await Task.FromResult(Results.Ok(new
            {
                status = "success",
                message = "Código de descuento válido",
                descuento = new
                {
                    codigo = codigoUpper,
                    porcentaje,
                    descripcion,
                    valido = true
                }
            }));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(Results.Problem("Error validando descuento: " + ex.Message));
        }
    }

    record EventoListDto(
        Guid Id,
        string? Nombre,
        string? Descripcion,
        DateTimeOffset? Fecha,
        string? Ubicacion,
        int Aforo,
        int EntradasVendidas,
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

    public record ReservationDto(
        Guid IdReserva,
        string NombreEvento,
        string TipoEntrada,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Total,
        string TiempoRestante
    );

    public record PurchaseConfirmDto(
        string PaymentMethod,
        string PaymentToken,
        List<Guid> ReservationIds
    );

    public record DiscountCheckDto(string Code);
}