using RestSharp;
using RestSharp.Authenticators;

namespace Backend.Services;

public class MailGunService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _domain;

    public MailGunService(Backend.MailGunSettings settings)
    {
        _apiKey = settings.ApiKey;
        _domain = settings.Domain;
    }

    public async Task<RestResponse> SendEmailAsync(string to, string subject, string htmlBody, byte[] qrBytes)
    {
        var options = new RestClientOptions("https://api.mailgun.net")
        {
            Authenticator = new HttpBasicAuthenticator("api", _apiKey)
        };

        var client = new RestClient(options);
        var request = new RestRequest($"/v3/{_domain}/messages", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddParameter("from", $"Tickets <postmaster@{_domain}>");
        request.AddParameter("to", to);
        request.AddParameter("subject", subject);

        // Send HTML instead of plain text
        request.AddParameter("html", htmlBody);

        // Attach the QR code as inline image
        request.AddFile("inline", qrBytes, "qr.png", "image/png");

        return await client.ExecuteAsync(request);
    }
}