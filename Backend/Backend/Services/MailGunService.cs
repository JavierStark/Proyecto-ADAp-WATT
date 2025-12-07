using RestSharp;
using RestSharp.Authenticators;

namespace Backend.Services;

public class MailGunService : IEmailService
{
    public async Task<RestResponse> SendEmailAsync(string to, string subject, string htmlBody, byte[] qrBytes, IConfiguration config)
    {
        var options = new RestClientOptions("https://api.mailgun.net")
        {
            Authenticator = new HttpBasicAuthenticator("api", config["MailGun:ApiKey"]!)
        };

        var client = new RestClient(options);
        var request = new RestRequest($"/v3/{config["MailGun:Domain"]}/messages", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddParameter("from", $"Tickets <postmaster@{config["MailGun:Domain"]}>");
        request.AddParameter("to", to);
        request.AddParameter("subject", subject);

        // Send HTML instead of plain text
        request.AddParameter("html", htmlBody);

        // Attach the QR code as inline image
        request.AddFile("inline", qrBytes, "qr.png", "image/png");

        return await client.ExecuteAsync(request);
    }
}