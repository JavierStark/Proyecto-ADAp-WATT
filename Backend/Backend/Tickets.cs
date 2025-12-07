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

    
    public static async Task<IResult> SendTicketByEmail(string ticketId, string email, Client client, IEmailService emailService, IConfiguration config)
    {
        try
        {
            // var responseTicket = await client.From<Entrada>()
            //     .Filter("id", Operator.Equals, ticketId)
            //     .Single();

            // if (responseTicket == null)
            // {
            //     return Results.NotFound(new { error = "Ticket no encontrado." });
            // }

            // Aquí construiríamos el cuerpo del email con los detalles del ticket
            
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
        string html = $"""

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

    public static byte[] GenerateQr(string ticketId)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);

        using var bitMap = qrCode.GetGraphic(20);
        using var ms = new MemoryStream();
        bitMap.Save(ms, ImageFormat.Png);

        return ms.ToArray(); // PNG bytes
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
}