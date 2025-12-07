using System.Drawing;
using System.Drawing.Imaging;
using Backend.Models;
using Backend.Services;
using QRCoder;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace Backend;

static class Tickets
{
    public static async Task<IResult> GetMyTickets(string? ticketId, HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;

            var usuarioDb = await client.From<Usuario>()
                .Filter("id", Operator.Equals, userId)
                .Single();

            if (usuarioDb == null) return Results.Unauthorized();

            var query = client.From<Entrada>()
                .Filter("fk_usuario", Operator.Equals, usuarioDb.Id.ToString());

            if (!string.IsNullOrEmpty(ticketId))
            {
                query = query.Filter("id", Operator.Equals, ticketId);
            }

            var responseTickets = await query.Order("fecha_compra", Ordering.Descending).Get();
            var misTickets = responseTickets.Models;

            if (!misTickets.Any())
            {
                return !string.IsNullOrEmpty(ticketId)
                    ? Results.NotFound(new { error = "Ticket no encontrado." })
                    : Results.Ok(new List<TicketDto>());
            }

            var eventosIds = misTickets.Select(t => t.FkEvento.ToString()).Distinct().ToList();
            var tiposIds = misTickets.Select(t => t.FkEntradaEvento.ToString()).Distinct().ToList();

            // Traer Eventos
            var eventosResponse = await client.From<Evento>()
                .Filter("id", Operator.In, eventosIds)
                .Get();
            
            var dictEventos = eventosResponse.Models.ToDictionary(e => e.Id);

            // Traer Tipos de Entrada
            var tiposResponse = await client.From<EntradaEvento>()
                .Filter("id", Operator.In, tiposIds)
                .Get();

            var dictTipos = tiposResponse.Models.ToDictionary(t => t.FkEntradaEvento);

            // Mapeo a DTO
            var listaFinal = misTickets.Select(t =>
            {
                var evento = dictEventos.ContainsKey(t.FkEvento) ? dictEventos[t.FkEvento] : null;

                var tipo = dictTipos.ContainsKey(t.FkEntradaEvento) ? dictTipos[t.FkEntradaEvento] : null;

                return new TicketDto(
                    t.Id,
                    evento?.Nombre ?? "Evento no disponible",
                    tipo?.Tipo ?? "Estándar",
                    t.Precio,
                    evento?.FechaEvento?.DateTime ?? DateTime.MinValue,
                    evento?.Ubicacion ?? "Ubicación desconocida",
                    t.CodigoQr,
                    t.Estado ?? "Activo"
                );
            }).ToList();

            return Results.Ok(listaFinal);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error recuperando tickets: " + ex.Message);
        }
    }

    
    public static async Task<IResult> SendTestEmail(string ticketId, string email, Client client, IEmailService emailService, IConfiguration config)
    {
        try
        {
            byte[] qr = GenerateQr(ticketId);

            var html = GetTicketEmailHtml(ticketId);

            var emailResponse = await emailService.SendEmailAsync(email, "Your Ticket", html, qr, config);

            return emailResponse.IsSuccessful ?
                Results.Ok(new { message = "Ticket enviado por email correctamente." }) : 
                Results.Problem("Error enviando el email: " + emailResponse.ErrorMessage);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error enviando el ticket por email: " + ex.Message);
        }
    }

    private static string GetTicketEmailHtml(string ticketId)
    {
        var html = $"""

                    <!DOCTYPE html>
                    <html lang="es" style="margin:0; padding:0; font-family: Arial, Helvetica, sans-serif;">
                      <body style="background-color:#f5f7fa; margin:0; padding:40px 0;">
                        <table width="100%" cellspacing="0" cellpadding="0" 
                               style="max-width:600px; margin:auto; background:white; border-radius:12px; 
                                      box-shadow:0 4px 16px rgba(0,0,0,0.08); padding:40px;">
                          <tr>
                            <td style="text-align:center;">
                    
                              <h1 style="font-size:28px; color:#1e1e1e; margin-bottom:10px; font-weight:600;">
                                ¡Aquí tienes tu entrada! 🎫
                              </h1>
                    
                              <p style="font-size:16px; color:#4a4a4a; margin-bottom:25px;">
                                Gracias por tu compra. Presenta este código QR en la entrada para acceder al evento.
                              </p>
                    
                              <div style="margin:30px 0;">
                                <img src="cid:qr.png" alt="QR Code" 
                                     style="width:180px; height:180px; border-radius:8px; 
                                            box-shadow:0 4px 12px rgba(0,0,0,0.15);" />
                              </div>
                    
                              <p style="font-size:16px; color:#1e1e1e; margin-bottom:10px; font-weight:600;">
                                Ticket ID:
                              </p>
                              <p style="font-size:18px; color:#4f46e5; margin-bottom:30px; font-weight:bold;">
                                {ticketId}
                              </p>
                    
                              <hr style="border:none; border-top:1px solid #e5e7eb; margin:40px 0;">
                    
                              <p style="font-size:14px; color:#6b7280;">
                                Si tienes algún problema con tu ticket o necesitas asistencia,
                                ponte en contacto con nuestro equipo de soporte.
                              </p>
                    
                              <p style="font-size:13px; color:#9ca3af; margin-top:25px;">
                                Gracias por confiar en nosotros. ¡Disfruta del evento!
                              </p>
                    
                            </td>
                          </tr>
                        </table>
                      </body>
                    </html>
                    """;
        return html;
    }

    private static byte[] GenerateQr(string ticketId)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);

        using var bitMap = qrCode.GetGraphic(20);
        using var ms = new MemoryStream();
        bitMap.Save(ms, ImageFormat.Png);

        return ms.ToArray(); // PNG bytes
    }
    
    public static async Task<IResult> ConfirmPurchase(PurchaseConfirmDto dto, Client client,
        IPaymentService paymentService, HttpContext httpContext, IEmailService emailService, IConfiguration config)
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
                    
                    await emailService.SendEmailAsync(
                        usuarioDb.Email!,
                        "Entrada Comprada",
                        GetTicketEmailHtml(ticketsNuevos.Last().CodigoQr!),
                        GenerateQr(ticketsNuevos.Last().CodigoQr!),
                        config
                    );
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

    public static async Task<IResult> StartPurchase(Tickets.PurchaseStartDto dto, Supabase.Client client, HttpContext httpContext)
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
            if (httpContext.Items["user_id"] is not string userAuth)
                return Results.Unauthorized();

            var usuarioDb = await client.From<Usuario>()
                .Filter("id", Operator.Equals, userAuth)
                .Single();

            var reservasResponse = await client.From<ReservaEntrada>()
                .Filter("fk_usuario", Operator.Equals, usuarioDb.Id.ToString())
                .Filter("fecha_expiracion", Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
                .Get();

            var misReservas = reservasResponse.Models;

            if (!misReservas.Any()) return Results.Ok(new List<Tickets.ReservationDto>());

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

                return new Tickets.ReservationDto(
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

    public static async Task<IResult> ValidateDiscount(Tickets.DiscountCheckDto dto, Supabase.Client client)
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
    
    public record TicketDto(
        Guid TicketId,
        string EventoNombre,
        string TipoEntrada,
        decimal PrecioPagado,
        DateTime FechaEvento,
        string Ubicacion,
        string? CodigoQrUrl,
        string Estado
    );

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

    public record PurchaseStartDto(
        Guid EventId,
        Guid TicketEventId,
        int Quantity,
        bool IsCompany,
        string BillingAddress,
        string? DiscountCode);
}