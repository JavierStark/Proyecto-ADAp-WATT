namespace Backend;

public class Test
{
    static public string GetEvents(string numeroEventosString)
    {
        int numeroEventos;
        if (!int.TryParse(numeroEventosString, out numeroEventos) || numeroEventos < 1)
        {
            return "Por favor, ingrese un número válido de eventos mayor que 0.";
        }

        var eventos = new List<string>();
        for (int i = 1; i <= numeroEventos; i++)
        {
            eventos.Add($"Evento {i}: Detalles del evento {i}");
        }

        return string.Join("\n", eventos);
    }
}