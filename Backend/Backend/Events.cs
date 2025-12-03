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
        
            var eventos = response.Models.Select(e => new EventoDto(
                e.IdEvento,
                e.Nombre,
                e.Descripcion,
                e.FechaEvento,
                e.Ubicacion,
                e.Aforo ?? 0,
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
    
    public static async Task<IResult> GetEvent(int eventId, Supabase.Client client)
    {
        try
        {
            var response = await client
                .From<Evento>()
                .Filter("id_evento", Operator.Equals, eventId)
                .Get();

            var eventoDb = response.Models.FirstOrDefault();

            if (eventoDb == null)
                return Results.NotFound(new { error = $"No se encontró ningún evento con el ID {eventId}" });

            var eventoDto = new EventoDto(
                eventoDb.IdEvento,
                eventoDb.Nombre,
                eventoDb.Descripcion,
                eventoDb.FechaEvento,
                eventoDb.Ubicacion,
                eventoDb.Aforo ?? 0, 
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
        if (dto.Quantity <= 0) 
            return Results.BadRequest(new { error = "La cantidad debe ser mayor a 0." });
        
        try
        {
            var userAuth = client.Auth.CurrentUser;
            if (userAuth == null) 
                return Results.Unauthorized();

            // Obtener evento para verificar que existe y obtener precio
            var eventoResponse = await client
                .From<Evento>()
                .Filter("id_evento", Operator.Equals, dto.EventId)
                .Get();

            var evento = eventoResponse.Models.FirstOrDefault();
            if (evento == null)
                return Results.NotFound(new { error = "El evento no existe." });

            // Verificar aforo disponible
            var ticketsExistentes = await client
                .From<Ticket>()
                .Filter("id_evento", Operator.Equals, dto.EventId)
                .Get();

            int totalVendidos = ticketsExistentes.Models.Sum(t => t.Cantidad);
            int disponibles = (evento.Aforo ?? 0) - totalVendidos;

            if (disponibles < dto.Quantity)
                return Results.BadRequest(new { 
                    error = $"No hay suficientes entradas disponibles. Disponibles: {disponibles}" 
                });

            // Obtener usuario para validaciones
            var usuarioResponse = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Operator.Equals, userAuth.Id)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();
            if (usuario == null)
                return Results.Unauthorized();

            // Calcular precio total (asumimos 50€ por entrada, o configurar según lógica de negocio)
            decimal precioUnitario = 50; // TODO: obtener del evento o configuración
            decimal importeTotal = precioUnitario * dto.Quantity;

            // Crear resumen del carrito (sin persistir aún, es el "carrito de compra")
            var resumenCarrito = new
            {
                status = "success",
                message = "Carrito de compra iniciado",
                carrito = new
                {
                    id_evento = evento.IdEvento,
                    nombre_evento = evento.Nombre,
                    cantidad_entradas = dto.Quantity,
                    precio_unitario = precioUnitario,
                    importe_total = importeTotal,
                    direccion_facturacion = dto.BillingAddress,
                    es_empresa = dto.IsCompany,
                    descuento_aplicado = dto.DiscountCode,
                    fecha_evento = evento.FechaEvento
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
        if (string.IsNullOrEmpty(dto.PaymentMethod) || string.IsNullOrEmpty(dto.PaymentToken))
            return Results.BadRequest(new { error = "Método de pago y token de pago son requeridos." });

        try
        {
            var userAuth = client.Auth.CurrentUser;
            if (userAuth == null)
                return Results.Unauthorized();

            // Obtener usuario
            var usuarioResponse = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Operator.Equals, userAuth.Id)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();
            if (usuario == null)
                return Results.Unauthorized();

            // TODO: Validar token de pago con proveedor (Stripe, PayPal, etc.)
            // Por ahora asumimos que es válido. En producción se debe verificar aquí.

            // Crear registro de pago
            var nuevoPago = new Pago
            {
                Monto = 0, // Se debe pasar desde el carrito guardado, pero aquí no tenemos persistencia de carrito
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                IdCliente = usuario.IdUsuario
            };

            var pagoResponse = await client
                .From<Pago>()
                .Insert(nuevoPago);

            var pagoCreado = pagoResponse.Models.First();

            // TODO: Crear ticket con los datos del carrito (necesitaríamos guardar el carrito en StartPurchase)
            // Por ahora retornamos confirmación exitosa del pago

            return Results.Ok(new
            {
                status = "success",
                message = "Pago confirmado exitosamente",
                pago = new
                {
                    id_pago = pagoCreado.IdPago,
                    monto = pagoCreado.Monto,
                    fecha = pagoCreado.Fecha,
                    estado = pagoCreado.Estado,
                    metodo_pago = pagoCreado.MetodoDePago
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error confirmando el pago: " + ex.Message);
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
    
    record EventoDto(
        long Id, 
        string Nombre, 
        string? Descripcion, 
        DateTime Fecha,
        string? Ubicacion,
        int Aforo, 
        bool EntradaValida,
        string ObjetoRecaudacion
    );
    public record PurchaseStartDto(
        int EventId,
        int Quantity,
        bool IsCompany,
        string BillingAddress,
        string? DiscountCode);

    
    public record PurchaseConfirmDto(
        string PaymentMethod,
        string PaymentToken);

    public record DiscountCheckDto(string Code);
}