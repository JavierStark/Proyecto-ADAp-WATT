using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using static Supabase.Postgrest.Constants;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend;

static class Donations
{
    public static async Task<IResult> GetMyDonations(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;
            
            var response = await client
                .From<Donacion>()
                .Select("*, Pago:fk_pago!inner(*)") 
                .Filter("Pago.fk_usuario", Operator.Equals, userId)
                .Get();
            
            var historial = response.Models
                .Select(d => new DonationHistoryDto(
                    d.Id,
                    d.Pago?.Monto ?? 0,
                    d.Pago?.Fecha ?? DateTime.MinValue,
                    d.Pago != null ? d.Pago.Estado : "Desconocido",
                    d.Pago?.MetodoDePago
                ))
                .OrderByDescending(x => x.Fecha)
                .ToList();

            return Results.Ok(historial);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo donaciones: " + ex.Message);
        }
    }

    public static async Task<IResult> GetMyDonationSummary(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;

            var usuario = await client
                .From<Usuario>()
                .Filter("id", Operator.Equals, userId)
                .Single();

            var response = await client
                .From<Donacion>()
                .Select("*, Pago:fk_pago!inner(*)")
                .Filter("Pago.fk_usuario", Operator.Equals, usuario!.Id.ToString())
                .Get();

            decimal total = response.Models.Sum(d => d.Pago?.Monto ?? 0);

            return Results.Ok(new DonationSummaryDto(total));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error calculando el total: " + ex.Message);
        }
    }

    // Main entry point - acts as a filter to route to authenticated or anonymous donation
    public static async Task<IResult> CreateDonation(DonationDto dto, HttpContext httpContext, Supabase.Client client, Services.IPaymentService paymentService)
    {
        if (dto.Amount <= 0)
            return Results.BadRequest(new { error = "El monto debe ser mayor a 0." });

        var userId = httpContext.Items["user_id"] as string;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return await CreateDonationAuthenticated(dto, userId, client, paymentService);
        }
        else
        {
            return await CreateDonationAnonymous(dto, client, paymentService);
        }
    }

    // Authenticated user donation
    private static async Task<IResult> CreateDonationAuthenticated(DonationDto dto, string userId, Supabase.Client client, Services.IPaymentService paymentService)
    {
        try
        {
            var userGuid = Guid.Parse(userId);

            var usuario = await client
                .From<Usuario>()
                .Where(u => u.Id == userGuid)
                .Single();

            if (usuario == null)
                return Results.NotFound(new { error = "Usuario no encontrado." });

            // Procesar pago
            if (dto.Amount > 0)
            {
                try
                {
                    await paymentService.ProcessPaymentAsync(dto.Amount, dto.PaymentToken);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = "Error en el pago: " + ex.Message });
                }
            }

            var nuevoPago = new Pago
            {
                Monto = dto.Amount,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                FkUsuario = usuario.Id,
                FkUsuarioNoRegistrado = null
            };

            var pagoResponse = await client
                .From<Pago>()
                .Insert(nuevoPago);

            var pagoCreado = pagoResponse.Models.First();

            var nuevaDonacion = new Donacion { FkPago = pagoCreado.Id };

            await client
                .From<Donacion>()
                .Insert(nuevaDonacion);

            return Results.Ok(new DonationCreatedResponseDto(
                "success",
                $"¡Gracias! Donación de {dto.Amount}€ realizada correctamente.",
                pagoCreado.Id
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error procesando la donación autenticada: " + ex.Message);
        }
    }

    // Anonymous user donation - completely anonymous, no personal data required
    private static async Task<IResult> CreateDonationAnonymous(DonationDto dto, Supabase.Client client, Services.IPaymentService paymentService)
    {
        try
        {
            // Si se proporcionan datos personales, crear usuario no registrado
            UsuarioNoRegistrado? usuarioNoRegistrado = null;
            
            if (!string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Nombre))
            {
                // Create or update unregistered user using upsert with OnConflict on email
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
                usuarioNoRegistrado = upsertRes.Models.First();
            }

            // Procesar pago
            if (dto.Amount > 0)
            {
                try
                {
                    await paymentService.ProcessPaymentAsync(dto.Amount, dto.PaymentToken);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = "Error en el pago: " + ex.Message });
                }
            }

            // Create payment - can be completely anonymous (both FKs null)
            var nuevoPago = new Pago
            {
                Monto = dto.Amount,
                Fecha = DateTime.UtcNow,
                Estado = "Pagado",
                MetodoDePago = dto.PaymentMethod,
                FkUsuario = null,
                FkUsuarioNoRegistrado = usuarioNoRegistrado?.Id
            };

            var pagoResponse = await client
                .From<Pago>()
                .Insert(nuevoPago);

            var pagoCreado = pagoResponse.Models.First();

            var nuevaDonacion = new Donacion { FkPago = pagoCreado.Id };

            await client
                .From<Donacion>()
                .Insert(nuevaDonacion);

            return Results.Ok(new DonationCreatedResponseDto(
                "success",
                $"¡Gracias! Donación de {dto.Amount}€ realizada correctamente.",
                pagoCreado.Id
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Error procesando la donación anónima: " + ex.Message);
        }
    }

    public static async Task<IResult> GetDonationCertificate([FromBody] CertificadoRequestDto request,
    HttpContext httpContext, Supabase.Client client)
{
    try
    {
        var userIdString = httpContext.Items["user_id"]?.ToString();
        if (string.IsNullOrEmpty(userIdString)) return Results.Unauthorized();
        var userGuid = Guid.Parse(userIdString);
        
        var usuario = await client
            .From<Usuario>()
            .Where(u => u.Id == userGuid)
            .Single();

        if (usuario == null) return Results.Unauthorized();
        
        var responseCliente = await client
            .From<Cliente>()
            .Where(c => c.Id == userGuid)
            .Get();
            
        var cliente = responseCliente.Models.FirstOrDefault();
        
        if (cliente == null) cliente = new Cliente { Id = userGuid };

        // 2. LÓGICA DE VALIDACIÓN Y ACTUALIZACIÓN
        bool needsUpdate = false;
        var missingFields = new List<string>();

        // DNI
        if (string.IsNullOrEmpty(usuario.Dni))
        {
            if (!string.IsNullOrEmpty(request.Dni)) { usuario.Dni = request.Dni; needsUpdate = true; }
            else missingFields.Add("DNI");
        }

        // DIRECCIÓN
        if (string.IsNullOrEmpty(cliente.Calle))
        {
            if (!string.IsNullOrEmpty(request.Calle)) { cliente.Calle = request.Calle; needsUpdate = true; }
            else missingFields.Add("Calle");
        }

        if (string.IsNullOrEmpty(cliente.Numero))
        {
            if (!string.IsNullOrEmpty(request.Numero)) { cliente.Numero = request.Numero; needsUpdate = true; }
        }

        if (string.IsNullOrEmpty(cliente.CodigoPostal))
        {
            if (!string.IsNullOrEmpty(request.CodigoPostal)) { cliente.CodigoPostal = request.CodigoPostal; needsUpdate = true; }
            else missingFields.Add("Código Postal");
        }

        if (string.IsNullOrEmpty(cliente.Ciudad))
        {
            if (!string.IsNullOrEmpty(request.Ciudad)) { cliente.Ciudad = request.Ciudad; needsUpdate = true; }
            else missingFields.Add("Ciudad");
        }
        
        if (string.IsNullOrEmpty(cliente.PisoPuerta) && !string.IsNullOrEmpty(request.PisoPuerta)) 
        { 
            cliente.PisoPuerta = request.PisoPuerta; needsUpdate = true; 
        }
        if (string.IsNullOrEmpty(cliente.Provincia) && !string.IsNullOrEmpty(request.Provincia)) 
        { 
            cliente.Provincia = request.Provincia; needsUpdate = true; 
        }
        if (string.IsNullOrEmpty(cliente.Pais) && !string.IsNullOrEmpty(request.Pais)) 
        { 
            cliente.Pais = request.Pais; needsUpdate = true; 
        }

        // Si faltan obligatorios...
        if (missingFields.Any())
        {
            return Results.BadRequest(new 
            { 
                error = "Datos fiscales incompletos.",
                missing_fields = missingFields,
                message = "Por favor, completa los datos de dirección y DNI para generar el certificado."
            });
        }

        // Guardar cambios en la tabla CLIENTE
        if (needsUpdate)
        {
            await client.From<Cliente>().Upsert(cliente);
        }

        // CONSTRUCCIÓN DE LA DIRECCIÓN COMPLETA (Para el PDF)
        string lineaDireccion1 = $"{cliente.Calle?.Trim()} {cliente.Numero?.Trim()} {cliente.PisoPuerta?.Trim()}".Trim();
        
        // Formato: "29000 Málaga (Málaga), España"
        string paisStr = !string.IsNullOrEmpty(cliente.Pais) ? cliente.Pais : "España";
        string provinciaStr = !string.IsNullOrEmpty(cliente.Provincia) ? $"({cliente.Provincia})" : "";
        string lineaDireccion2 = $"{cliente.CodigoPostal} {cliente.Ciudad} {provinciaStr}, {paisStr}".Trim();


        // OBTENER DONACIONES
        int targetYear = request.Year ?? DateTime.Now.Year - 1;
        string fechaInicio = $"{targetYear}-01-01T00:00:00";
        string fechaFin = $"{targetYear}-12-31T23:59:59";

        // Importante: Filtramos por fk_cliente en la tabla PAGO, que debe coincidir con el ID usuario
        var responseDonaciones = await client
            .From<Donacion>()
            .Select("*, Pago:fk_pago!inner(*)")
            .Filter("Pago.fk_usuario", Operator.Equals, userGuid.ToString())
            .Filter("Pago.fecha", Operator.GreaterThanOrEqual, fechaInicio)
            .Filter("Pago.fecha", Operator.LessThanOrEqual, fechaFin)
            .Get();

        var donacionesAnuales = responseDonaciones.Models
            .OrderBy(d => d.Pago?.Fecha)
            .ToList();

        if (donacionesAnuales.Count == 0)
            return Results.NotFound(new { error = $"No se encontraron donaciones en el ejercicio {targetYear}." });

        decimal totalAnual = donacionesAnuales.Sum(d => d.Pago?.Monto ?? 0);


        // GENERAR PDF CON QUESTPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // --- CABECERA ---
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FUNDACIÓN CUDECA").Bold().FontSize(20).FontColor(Colors.Green.Medium);
                        col.Item().Text("NIF: G-92182054"); 
                        col.Item().Text("Av. del Cosmos, s/n, 29631 Benalmádena (Málaga)");
                    });
                });

                // --- CONTENIDO ---
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Text("CERTIFICADO DE DONACIONES").FontSize(16).Bold().AlignCenter();
                    col.Item().PaddingTop(10).Text($"Ejercicio Fiscal: {targetYear}").FontSize(14);
                    col.Item().Text($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy}");

                    // Separador (Solución al error LineHorizontal)
                    col.Item().PaddingTop(20).BorderBottom(1);

                    // Datos del Donante
                    col.Item().PaddingTop(10).Text("DATOS DEL DONANTE:").Bold();
                    col.Item().Text($"{usuario.Nombre} {usuario.Apellidos}"); // Nombre de tabla Usuario
                    col.Item().Text($"NIF/DNI: {usuario.Dni}");             // DNI de tabla Cliente
                    
                    // Dirección de tabla Cliente
                    col.Item().PaddingTop(5).Text("Domicilio Fiscal:").Underline();
                    col.Item().Text(lineaDireccion1);
                    col.Item().Text(lineaDireccion2);

                    col.Item().PaddingTop(20)
                        .Text("La Fundación CUDECA certifica que ha recibido las siguientes donaciones de carácter irrevocable:")
                        .Italic();

                    // Tabla de Donaciones
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); 
                            columns.RelativeColumn(2); 
                            columns.RelativeColumn(); 
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("FECHA");
                            header.Cell().Element(HeaderStyle).Text("MÉTODO");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("IMPORTE");

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold())
                                    .BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                            }
                        });

                        foreach (var donacion in donacionesAnuales)
                        {
                            var pago = donacion.Pago;
                            if (pago == null) continue;

                            table.Cell().Element(CellStyle).Text(pago.Fecha.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text(pago.MetodoDePago ?? "Donación");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{pago.Monto:N2} €");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        }
                    });

                    // Total
                    col.Item().PaddingTop(10).AlignRight().Text(text =>
                    {
                        text.Span("TOTAL APORTADO: ").Bold();
                        text.Span($"{totalAnual:N2} €").Bold().FontSize(14);
                    });
                });

                // --- PIE DE PÁGINA ---
                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(1); // Usamos BorderTop en lugar de LineHorizontal
                    col.Item().PaddingTop(5).Text("Entidad beneficiaria del mecenazgo según la Ley 49/2002.")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        return Results.File(
            pdfBytes,
            "application/pdf",
            $"Certificado_Fiscal_{targetYear}_{usuario.Dni}.pdf"
        );
    }
    catch (Exception ex)
    {
        return Results.Problem("Error generando certificado: " + ex.Message);
    }
}

    public record DonationHistoryDto(
        Guid IdDonacion,
        decimal Monto,
        DateTime Fecha,
        string Estado,
        string? MetodoPago
    );

    public record CertificadoRequestDto(
        int? Year,
        string? Dni,
        string? Calle,
        string? Numero,
        string? PisoPuerta,
        string? CodigoPostal,
        string? Ciudad,
        string? Provincia,
        string? Pais
    );

    public record DonationSummaryDto(decimal TotalDonado);

    public record DonationDto(
        decimal Amount, 
        string PaymentToken,
        string PaymentMethod,
        string? Email,      // Optional for anonymous donations
        string? Dni,
        string? Nombre,
        string? Apellidos,
        string? Telefono
    ); // Ej: "Tarjeta", "PayPal", "Bizum"
}