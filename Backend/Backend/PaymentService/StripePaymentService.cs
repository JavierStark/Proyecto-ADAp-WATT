namespace Backend.PaymentService;

using Stripe;

public class StripePaymentService : IPaymentService
{
    private readonly string _secretKey;

    public StripePaymentService(IConfiguration config)
    {
        _secretKey = config["Stripe:SecretKey"];
    }

    public async Task ProcessPaymentAsync(decimal amount, string token, string currency = "eur")
    {
        if (string.IsNullOrEmpty(_secretKey))
            throw new Exception("Falta la clave de Stripe en la configuración.");

        StripeConfiguration.ApiKey = _secretKey;

        var service = new PaymentIntentService();
        var paymentIntent = await service.GetAsync(token); // GetAsync es la versión asíncrona

        // Validaciones
        if (paymentIntent.Status != "succeeded")
            throw new Exception($"El pago no se completó. Estado: {paymentIntent.Status}");

        long amountInCents = (long)(amount * 100);
        if (paymentIntent.AmountReceived != amountInCents)
            throw new Exception("Fraude detectado: El monto no coincide.");
    }
}