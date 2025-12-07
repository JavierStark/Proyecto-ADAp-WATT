using RestSharp;

namespace Backend.Services;

public interface IEmailService
{
    Task<RestResponse> SendEmailAsync(string to, string subject, string htmlBody, byte[] qrBytes,
        IConfiguration config);
}