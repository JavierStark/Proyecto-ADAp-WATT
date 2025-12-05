namespace Backend.PaymentService;

public interface IPaymentService
{
    // Devuelve true si el pago es correcto, o lanza excepción si falla
    Task ProcessPaymentAsync(decimal amount, string token, string currency = "eur");
}