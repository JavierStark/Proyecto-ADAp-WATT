namespace Backend.Services;

public class SimulatedPaymentService : IPaymentService
{
    public Task ProcessPaymentAsync(decimal amount, string token, string currency = "eur")
    {
        if (token.StartsWith("sim_error"))
        {
            throw new Exception("Simulación: El banco rechazó la tarjeta.");
        }

        if (!token.StartsWith("sim_"))
        {
            throw new Exception("En modo simulación debes usar tokens que empiecen por 'sim_'.");
        }

        // Si es "sim_ok", no hacemos nada (éxito)
        return Task.CompletedTask;
    }
}