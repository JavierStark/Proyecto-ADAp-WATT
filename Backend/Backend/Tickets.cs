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
                .Order("precio", Ordering.Ascending)
                .Get();

            var tiposEntrada = response.Models;

            // Mapear a un objeto anónimo o DTO para el frontend
            var resultado = tiposEntrada.Select(t => new TicketTypeDto(
                t.Id,
                t.Tipo!,
                t.Precio,
                t.Cantidad
            ));

            return Results.Ok(new TicketTypesResponseDto(
                "success",
                resultado
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error al obtener los tipos de entrada: " + ex.Message);
        }
    }

    public static async Task<IResult> PurchaseTickets(BuyTicketDto dto, Client client,
        IPaymentService paymentService, HttpContext httpContext, IEmailService emailService, IConfiguration config)
    {
        // Router method: check if user is authenticated
        var userIdString = httpContext.Items["user_id"]?.ToString();
        
        if (!string.IsNullOrEmpty(userIdString))
        {
            // Authenticated purchase
            return await PurchaseTicketsAuthenticated(dto, client, paymentService, emailService, config, Guid.Parse(userIdString));
        }
        else
        {
            // Anonymous purchase
            return await PurchaseTicketsAnonymous(dto, client, paymentService, emailService, config);
        }
    }

    private static async Task<IResult> PurchaseTicketsAuthenticated(BuyTicketDto dto, Client client,
        IPaymentService paymentService, IEmailService emailService, IConfiguration config, Guid userGuid)
    {
        try
        {
            // Validaciones básicas
            if (dto.Items.Count == 0)
                return Results.BadRequest(new { error = "El carrito está vacío." });

            if (dto.Items.Any(i => i.Quantity <= 0))
                return Results.BadRequest(new { error = "La cantidad de cada entrada debe ser al menos 1." });

            // Obtener tipos de entrada y validar stock
            var ticketTypeIds = dto.Items.Select(i => i.TicketEventId).ToList();
            
            var responseTipos = await client.From<EntradaEvento>()
                .Filter("id", Operator.In, ticketTypeIds)
                .Get();

            var tiposDb = responseTipos.Models;

            // Validaciones lógicas y cálculo de total
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

                totalPagar += tipoDb.Precio * item.Quantity;
                cantidadTotalEntradas += item.Quantity;
            }
            
            // Aplicar descuento si existe
            decimal descuentoAplicado = 0;
        
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                var codigoUpper = dto.DiscountCode.ToUpper();
                var now = DateTime.Now;

                var descuento = await client.From<ValeDescuento>()
                    .Filter("codigo", Operator.Equals, codigoUpper)
                    .Single();
                
                if (descuento == null ||
                    (descuento.FechaExpiracion != null && descuento.FechaExpiracion < now) || 
                    (descuento.Cantidad != null && descuento.Cantidad <= 0))
                {
                    return Results.BadRequest(new { error = "Código de descuento inválido o expirado." });
                }

                descuentoAplicado = descuento.Descuento * totalPagar;
                totalPagar -= descuentoAplicado;

                descuento.Cantidad -= 1;
                await client.From<ValeDescuento>().Update(descuento);
            }

            // Obtener y actualizar datos del usuario
            var usuario = await client.From<Usuario>().Where(u => u.Id == userGuid).Single();
            var clienteRes = await client.From<Cliente>().Where(c => c.Id == userGuid).Get();
            var cliente = clienteRes.Models.FirstOrDefault() ?? new Cliente { Id = userGuid };

            bool datosClienteActualizados = false;
            bool datosUsuarioActualizados = false;
            
            // Actualizar datos de Usuario
            if (!string.IsNullOrEmpty(dto.Dni) && usuario.Dni != dto.Dni)
            {
                usuario.Dni = dto.Dni;
                datosUsuarioActualizados = true;
            }
            
            if (!string.IsNullOrEmpty(dto.Nombre) && usuario.Nombre != dto.Nombre)
            {
                usuario.Nombre = dto.Nombre;
                datosUsuarioActualizados = true;
            }
            
            if (!string.IsNullOrEmpty(dto.Apellidos) && usuario.Apellidos != dto.Apellidos)
            {
                usuario.Apellidos = dto.Apellidos;
                datosUsuarioActualizados = true;
            }
            
            if (!string.IsNullOrEmpty(dto.Telefono) && usuario.Telefono != dto.Telefono)
            {
                usuario.Telefono = dto.Telefono;
                datosUsuarioActualizados = true;
            }
            
            // Actualizar dirección de cliente
            if (!string.IsNullOrEmpty(dto.Calle) && cliente.Calle != dto.Calle) 
            { 
                cliente.Calle = dto.Calle; 
                datosClienteActualizados = true; 
            }

            if (!string.IsNullOrEmpty(dto.Numero) && cliente.Numero != dto.Numero) 
            { 
                cliente.Numero = dto.Numero; 
                datosClienteActualizados = true; 
            }

            if (!string.IsNullOrEmpty(dto.PisoPuerta) && cliente.PisoPuerta != dto.PisoPuerta) 
            { 
                cliente.PisoPuerta = dto.PisoPuerta; 
                datosClienteActualizados = true; 
            }
            
            // Validar datos fiscales requeridos
            var camposFaltantes = new List<string>();
            
            if (string.IsNullOrEmpty(usuario.Dni)) camposFaltantes.Add("DNI");
            
            if (string.IsNullOrEmpty(cliente.Calle) || string.IsNullOrEmpty(cliente.Numero)) 
                camposFaltantes.Add("Dirección completa");
            
            if (camposFaltantes.Count != 0)
            {
                return Results.BadRequest(new
                {
                    error = "Faltan datos fiscales necesarios.",
                    missing_fields = camposFaltantes,
                    code = "MISSING_DATA"
                });
            }

            if (datosUsuarioActualizados) 
                await client.From<Usuario>().Update(usuario);
            
            if (datosClienteActualizados) 
                await client.From<Cliente>().Upsert(cliente);

            // Procesar pago
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
            
            // Crear registro de pago
            var nuevoPago = new Pago
            {
                Monto = totalPagar,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                FkUsuario = userGuid,
                FkUsuarioNoRegistrado = null
            };
            var pagoRes = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoRes.Models.First();

            var ticketsGenerados = new List<Entrada>();

            // Obtener nombre del evento para email
            var evento = await client.From<Evento>()
                .Filter("id", Operator.Equals, dto.EventId.ToString())
                .Single();
            
            var nombreEvento = evento?.Nombre ?? "Evento";

            // Generar entradas y enviar emails
            foreach (var item in dto.Items)
            {
                var tipoDb = tiposDb.First(t => t.Id == item.TicketEventId);

                // Descontar stock
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

                    // Enviar email individual
                    await emailService.SendEmailAsync(
                        usuario.Email!,
                        $"Tu Entrada ({tipoDb.Tipo}) - {nombreEvento}",
                        GetTicketEmailHtml(nuevaEntrada.CodigoQr),
                        GenerateQr(nuevaEntrada.CodigoQr),
                        config
                    );
                }
            }

            // Insertar todas las entradas
            await client.From<Entrada>().Insert(ticketsGenerados);

            // Actualizar estadísticas del evento
            if (evento != null)
            {
                evento.EntradasVendidas += cantidadTotalEntradas;
                await client.From<Evento>().Update(evento);
            }

            return Results.Ok(new PurchaseTicketsResponseDto(
                "success",
                $"Compra realizada. Se han generado {cantidadTotalEntradas} entradas.",
                totalPagar
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error procesando la compra: " + ex.Message);
        }
    }

    private static async Task<IResult> PurchaseTicketsAnonymous(BuyTicketDto dto, Client client,
        IPaymentService paymentService, IEmailService emailService, IConfiguration config)
    {
        try
        {
            // Validaciones básicas
            if (dto.Items.Count == 0)
                return Results.BadRequest(new { error = "El carrito está vacío." });

            if (dto.Items.Any(i => i.Quantity <= 0))
                return Results.BadRequest(new { error = "La cantidad de cada entrada debe ser al menos 1." });

            // Validar email para compra anónima
            if (string.IsNullOrEmpty(dto.Email))
                return Results.BadRequest(new { error = "El email es obligatorio para compras sin cuenta." });

            // Validar datos fiscales requeridos para compra anónima
            var camposFaltantes = new List<string>();
            
            if (string.IsNullOrEmpty(dto.Email)) camposFaltantes.Add("Email");
            if (string.IsNullOrEmpty(dto.Dni)) camposFaltantes.Add("DNI");
            if (string.IsNullOrEmpty(dto.Nombre)) camposFaltantes.Add("Nombre");
            if (string.IsNullOrEmpty(dto.Apellidos)) camposFaltantes.Add("Apellidos");
            
            if (camposFaltantes.Count != 0)
            {
                return Results.BadRequest(new
                {
                    error = "Faltan datos necesarios para la compra.",
                    missing_fields = camposFaltantes,
                    code = "MISSING_DATA"
                });
            }

            // Obtener tipos de entrada y validar stock
            var ticketTypeIds = dto.Items.Select(i => i.TicketEventId).ToList();
            
            var responseTipos = await client.From<EntradaEvento>()
                .Filter("id", Operator.In, ticketTypeIds)
                .Get();

            var tiposDb = responseTipos.Models;

            // Validaciones lógicas y cálculo de total
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

                totalPagar += tipoDb.Precio * item.Quantity;
                cantidadTotalEntradas += item.Quantity;
            }
            
            // Aplicar descuento si existe
            decimal descuentoAplicado = 0;
        
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                var codigoUpper = dto.DiscountCode.ToUpper();
                var now = DateTime.Now;

                var descuento = await client.From<ValeDescuento>()
                    .Filter("codigo", Operator.Equals, codigoUpper)
                    .Single();
                
                if (descuento == null ||
                    (descuento.FechaExpiracion != null && descuento.FechaExpiracion < now) || 
                    (descuento.Cantidad != null && descuento.Cantidad <= 0))
                {
                    return Results.BadRequest(new { error = "Código de descuento inválido o expirado." });
                }

                descuentoAplicado = descuento.Descuento * totalPagar;
                totalPagar -= descuentoAplicado;

                descuento.Cantidad -= 1;
                await client.From<ValeDescuento>().Update(descuento);
            }

            // Crear o actualizar usuario no registrado usando upsert con OnConflict en email
            var nuevoUsuarioNoRegistrado = new UsuarioNoRegistrado
            {
                Email = dto.Email,
                Dni = dto.Dni,
                Nombre = dto.Nombre,
                Apellidos = dto.Apellidos,
                Telefono = dto.Telefono
            };

            var upsertOptions = new Supabase.Postgrest.QueryOptions
            {
                OnConflict = "email"
            };

            var upsertRes = await client.From<UsuarioNoRegistrado>()
                .Upsert(nuevoUsuarioNoRegistrado, upsertOptions);
            var usuarioNoRegistrado = upsertRes.Models.First();

            // Procesar pago
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
            
            // Crear registro de pago vinculado al usuario no registrado
            var nuevoPago = new Pago
            {
                Monto = totalPagar,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                FkUsuario = null,
                FkUsuarioNoRegistrado = usuarioNoRegistrado.Id
            };
            var pagoRes = await client.From<Pago>().Insert(nuevoPago);
            var pagoCreado = pagoRes.Models.First();

            var ticketsGenerados = new List<Entrada>();

            // Obtener nombre del evento para email
            var evento = await client.From<Evento>()
                .Filter("id", Operator.Equals, dto.EventId.ToString())
                .Single();
            
            var nombreEvento = evento?.Nombre ?? "Evento";

            // Generar entradas y enviar emails
            foreach (var item in dto.Items)
            {
                var tipoDb = tiposDb.First(t => t.Id == item.TicketEventId);

                // Descontar stock
                tipoDb.Cantidad -= item.Quantity;
                await client.From<EntradaEvento>().Update(tipoDb);

                // Generar N entradas individuales
                for (int i = 0; i < item.Quantity; i++)
                {
                    var nuevaEntrada = new Entrada
                    {
                        FkUsuario = null,
                        FkUsuarioNoRegistrado = usuarioNoRegistrado.Id,
                        FkEvento = dto.EventId,
                        FkPago = pagoCreado.Id,
                        FechaCompra = DateTime.UtcNow,
                        Precio = tipoDb.Precio,
                        FkEntradaEvento = tipoDb.Id,
                        CodigoQr = Guid.NewGuid().ToString()
                    };
                    ticketsGenerados.Add(nuevaEntrada);

                    // Enviar email individual al email proporcionado
                    await emailService.SendEmailAsync(
                        dto.Email!,
                        $"Tu Entrada ({tipoDb.Tipo}) - {nombreEvento}",
                        GetTicketEmailHtml(nuevaEntrada.CodigoQr),
                        GenerateQr(nuevaEntrada.CodigoQr),
                        config
                    );
                }
            }

            // Insertar todas las entradas
            await client.From<Entrada>().Insert(ticketsGenerados);

            // Actualizar estadísticas del evento
            if (evento != null)
            {
                evento.EntradasVendidas += cantidadTotalEntradas;
                await client.From<Evento>().Update(evento);
            }

            return Results.Ok(new PurchaseTicketsResponseDto(
                "success",
                $"Compra realizada. Se han enviado {cantidadTotalEntradas} entradas a {dto.Email}.",
                totalPagar
            ));
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
            new PaymentMethodDto(1, "Tarjeta de Crédito", "CARD"),
            new PaymentMethodDto(2, "Tarjeta de Débito", "DEBIT"),
            new PaymentMethodDto(3, "PayPal", "PAYPAL"),
            new PaymentMethodDto(4, "Transferencia Bancaria", "BANK")
        };

        return Results.Ok(new PaymentMethodsResponseDto(
            "success",
            "Métodos de pago disponibles",
            metodos
        ));
    }

    public static async Task<IResult> ValidateDiscount(DiscountCheckDto dto, Client client)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Code))
                return Results.BadRequest(new { error = "El código de descuento no puede estar vacío." });

            var codigoUpper = dto.Code.ToUpper();
            var now = DateTime.Now;
            
            var descuento = await client.From<ValeDescuento>()
                .Filter("codigo", Operator.Equals, codigoUpper)
                .Single();

            if (descuento == null)
            {
                return Results.NotFound(new { error = "Código de descuento no encontrado." });
            }

            if (descuento.FechaExpiracion != null && descuento.FechaExpiracion < now)
            {
                return Results.BadRequest(new { error = "El código de descuento ha expirado." });
            }

            if (descuento.Cantidad != null && descuento.Cantidad <= 0)
            {
                return Results.BadRequest(new { error = "El código de descuento ya no tiene usos disponibles." });
            }

            return Results.Ok(new DiscountValidationResponseDto(
                "success",
                "Código de descuento válido.",
                new DiscountDetailsDto(
                    descuento.Codigo,
                    descuento.Descuento,
                    descuento.Descuento * 100,
                    descuento.Cantidad,
                    "porcentaje",  
                    descuento.FechaExpiracion,
                    true
                )
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error validando código de descuento: " + ex.Message);
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

            return Results.Ok(new ValidateTicketQrResponseDto(
                "success",
                "Código QR válido.",
                new TicketValidationDataDto(
                    ticket.Id,
                    evento?.Nombre,
                    ticket.Precio,
                    ticket.FechaCompra,
                    ticket.Estado
                )
            ));
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
        string? Email,           // Required for anonymous purchases
        string? Dni,
        string? Nombre,
        string? Telefono,
        string? Apellidos,
        string? Calle,
        string? Numero,
        string? PisoPuerta,
        string? CodigoPostal,
        string? Ciudad,
        string? Provincia,
        string? Pais
    );

    public record DiscountCheckDto(string Code);
}