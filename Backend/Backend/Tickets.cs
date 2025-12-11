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

            var dictTipos = tiposResponse.Models.ToDictionary(t => t.Id);

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


    public static async Task<IResult> SendTestEmail(string ticketId, string email, Client client,
        IEmailService emailService, IConfiguration config)
    {
        try
        {
            byte[] qr = GenerateQr(ticketId);

            var html = GetTicketEmailHtml(ticketId);

            var emailResponse = await emailService.SendEmailAsync(email, "Your Ticket", html, qr, config);

            return emailResponse.IsSuccessful
                ? Results.Ok(new { message = "Ticket enviado por email correctamente." })
                : Results.Problem("Error enviando el email: " + emailResponse.ErrorMessage);
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
        var value = $"https://cudeca-watt.es/validar-qr?qr={ticketId}";
        var qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);

        using var bitMap = qrCode.GetGraphic(20);
        using var ms = new MemoryStream();
        bitMap.Save(ms, ImageFormat.Png);

        return ms.ToArray(); // PNG bytes
    }
    
    public static async Task<IResult> GetEventTicketTypes(string eventId, Supabase.Client client)
    {
        try
        {
            // Validar que el ID sea un GUID válido
            if (!Guid.TryParse(eventId, out var guidEventId))
                return Results.BadRequest(new { error = "Formato de ID de evento inválido." });

            // Consultar la tabla EntradaEvento filtrando por el evento
            var response = await client.From<EntradaEvento>()
                .Filter("fk_evento", Operator.Equals, eventId)
                .Order("precio", Ordering.Ascending) // Ordenar por precio (opcional)
                .Get();

            var tiposEntrada = response.Models;

            // Mapear a un objeto anónimo o DTO para el frontend
            var resultado = tiposEntrada.Select(t => new 
            {
                TicketEventId = t.Id,       // <--- ESTE ES EL ID QUE NECESITAS PARA COMPRAR
                Nombre = t.Tipo,            // Ej: "General", "VIP"
                Precio = t.Precio,
                Stock = t.Cantidad          // Para que el frontend sepa si quedan entradas
            });

            return Results.Ok(new
            {
                status = "success",
                data = resultado
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al obtener los tipos de entrada: " + ex.Message);
        }
    }

    public static async Task<IResult> PurchaseTickets(BuyTicketDto dto, Supabase.Client client,
        IPaymentService paymentService, HttpContext httpContext, IEmailService emailService, IConfiguration config)
    {
        try
        {
            // VALIDACIONES
            if (dto.Items == null || !dto.Items.Any())
                return Results.BadRequest(new { error = "El carrito está vacío." });

            if (dto.Items.Any(i => i.Quantity <= 0))
                return Results.BadRequest(new { error = "La cantidad de cada entrada debe ser al menos 1." });

            var userIdString = httpContext.Items["user_id"]?.ToString();
            if (string.IsNullOrEmpty(userIdString)) return Results.Unauthorized();
            var userGuid = Guid.Parse(userIdString);
            
            // OBTENER TIPOS DE ENTRADA Y VALIDAR STOCK
            var ticketTypeIds = dto.Items.Select(i => i.TicketEventId).ToList();
            
            var responseTipos = await client.From<EntradaEvento>()
                .Filter("id", Operator.In, ticketTypeIds) // Traemos todos los tipos requeridos
                .Get();

            var tiposDb = responseTipos.Models;

            // Validaciones lógicas
            decimal totalPagar = 0;
            int cantidadTotalEntradas = 0;

            foreach (var item in dto.Items)
            {
                var tipoDb = tiposDb.FirstOrDefault(t => t.Id == item.TicketEventId);

                if (tipoDb == null)
                    return Results.NotFound(
                        new { error = $"El tipo de entrada con ID {item.TicketEventId} no existe." });

                if (tipoDb.FkEvento != dto.EventId)
                    return Results.BadRequest(new
                        { error = $"La entrada '{tipoDb.Tipo}' no pertenece al evento solicitado." });

                if (tipoDb.Cantidad < item.Quantity)
                    return Results.BadRequest(new
                        { error = $"Stock insuficiente para '{tipoDb.Tipo}'. Quedan {tipoDb.Cantidad}." });

                // Sumar al total
                totalPagar += tipoDb.Precio * item.Quantity;
                cantidadTotalEntradas += item.Quantity;
            }
            
            decimal descuentoAplicado = 0;
        
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                // Definir los códigos válidos (Idealmente, sacar esto a una constante o base de datos compartida)
                var codigosValidos = new Dictionary<string, decimal>
                {
                    { "DESCUENTO2025", 15m },
                    { "PROMO50", 25m },
                    { "NAVIDAD", 10m },
                    { "VIP", 30m }
                };

                var codigoUpper = dto.DiscountCode.ToUpper();

                if (codigosValidos.ContainsKey(codigoUpper))
                {
                    var porcentaje = codigosValidos[codigoUpper];
                
                    // Calcular la cantidad a descontar
                    descuentoAplicado = totalPagar * (porcentaje / 100m);
                
                    // Aplicar al total
                    totalPagar -= descuentoAplicado;

                    // Seguridad: evitar precios negativos
                    if (totalPagar < 0) totalPagar = 0;
                }
            }
            
            // ACTUALIZAR PERFIL DE USUARIO (Si faltan datos)
            var usuario = await client.From<Usuario>().Where(u => u.Id == userGuid).Single();
            var clienteRes = await client.From<Cliente>().Where(c => c.Id == userGuid).Get();
            var cliente = clienteRes.Models.FirstOrDefault() ?? new Cliente { Id = userGuid };

            bool datosActualizados = false;
            var camposFaltantes = new List<string>();

            if (string.IsNullOrEmpty(usuario.Dni))
            {
                if (!string.IsNullOrEmpty(dto.Dni))
                {
                    usuario.Dni = dto.Dni;
                    datosActualizados = true;
                }
                else camposFaltantes.Add("DNI");
            }

            if (string.IsNullOrEmpty(cliente.Calle))
            {
                if (!string.IsNullOrEmpty(dto.Direccion))
                {
                    cliente.Calle = dto.Direccion;
                    datosActualizados = true;
                }
                else camposFaltantes.Add("Dirección");
            }

            if (camposFaltantes.Count != 0)
            {
                return Results.BadRequest(new
                {
                    error = "Faltan datos fiscales necesarios.",
                    missing_fields = camposFaltantes,
                    code = "MISSING_DATA"
                });
            }

            if (datosActualizados) await client.From<Cliente>().Upsert(cliente);

            // PROCESAR PAGO
            if (totalPagar > 0)
            {
                try
                {
                    await paymentService.ProcessPaymentAsync(totalPagar, dto.PaymentToken);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = "Error en el pago: " + ex.Message });
                }
            }
            
            // Crear Pago 
            var nuevoPago = new Pago
            {
                Monto = totalPagar,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                FkCliente = userGuid
            };
            var pagoRes = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoRes.Models.First();

            var ticketsGenerados = new List<Entrada>();

            // Procesamiento de cada TIPO de entrada comprado
            foreach (var item in dto.Items)
            {
                var tipoDb = tiposDb.First(t => t.Id == item.TicketEventId);

                // Descontar Stock
                tipoDb.Cantidad -= item.Quantity;
                await client.From<EntradaEvento>().Update(tipoDb);

                // Generar N entradas individuales
                for (int i = 0; i < item.Quantity; i++)
                {
                    var nuevaEntrada = new Entrada
                    {
                        FkUsuario = userGuid,
                        FkEvento = dto.EventId,
                        FkPago = pagoCreado.Id,
                        FechaCompra = DateTime.UtcNow,
                        Precio = tipoDb.Precio,
                        FkEntradaEvento = tipoDb.Id,
                        CodigoQr = Guid.NewGuid().ToString()
                    };
                    ticketsGenerados.Add(nuevaEntrada);

                    // Enviar Email Individual
                    await emailService.SendEmailAsync(
                        usuario.Email!,
                        $"Tu Entrada ({tipoDb.Tipo}) - " + (await client.From<Evento>()
                            .Filter("id", Operator.Equals, dto.EventId.ToString()).Single())?.Nombre,
                        GetTicketEmailHtml(nuevaEntrada.CodigoQr),
                        GenerateQr(nuevaEntrada.CodigoQr),
                        config
                    );
                }
            }

            // Insertar todas las entradas de golpe
            await client.From<Entrada>().Insert(ticketsGenerados);

            // Actualizar estadísticas del evento (Total vendido)
            var evento = await client.From<Evento>().Filter("id", Operator.Equals, dto.EventId.ToString()).Single();
            if (evento != null)
            {
                evento.EntradasVendidas += cantidadTotalEntradas;
                await client.From<Evento>().Update(evento);
            }

            return Results.Ok(new
            {
                status = "success",
                message = $"Compra realizada. Se han generado {cantidadTotalEntradas} entradas.",
                totalPagado = totalPagar
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error procesando la compra: " + ex.Message);
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

    public static async Task<IResult> ValidateTicketQr(string qrCode, Client client)
    {
        try
        {
            var ticket = await client.From<Entrada>()
                .Filter("codigo_qr", Operator.Equals, qrCode)
                .Single();

            if (ticket == null)
            {
                return Results.NotFound(new { error = "Código QR no válido." });
            }

            var evento = await client.From<Evento>()
                .Filter("id", Operator.Equals, ticket.FkEvento.ToString())
                .Single();

            return Results.Ok(new
            {
                status = "success",
                message = "Código QR válido.",
                ticket = new
                {
                    ticket.Id,
                    EventoNombre = evento?.Nombre,
                    ticket.Precio,
                    ticket.FechaCompra,
                    ticket.Estado
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error validando código QR: " + ex.Message);
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

    public record BuyTicketDto(
        Guid EventId,
        List<PurchaseItemDto> Items,
        string PaymentToken,
        string PaymentMethod,
        string? DiscountCode,
        string? Dni,
        string? Nombre,
        string? Apellidos,
        string? Direccion,
        string? Ciudad,
        string? CodigoPostal,
        string? Provincia
    );

    public record DiscountCheckDto(string Code);
}