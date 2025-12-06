using Backend.Models;

namespace Backend;

static class Profile
{
    public static async Task<IResult> GetMyProfile(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;
            var parsed = Guid.Parse(userId);

            var usuario = await client
                .From<Usuario>()
                .Where(u => u.Id == parsed)
                .Single();

            var cliente = await client
                .From<Cliente>()
                .Where(c => c.Id == parsed)
                .Single();

            var perfilCompleto = new
            {
                // Datos de identificación
                id_interno = usuario.Id,
                email = usuario.Email,

                // Datos personales (Tabla Usuario)
                dni = usuario.Dni,
                nombre = usuario.Nombre,
                apellidos = usuario.Apellidos,
                telefono = usuario.Telefono,

                // Datos de cliente (Tabla Cliente)
                calle = cliente.Calle,
                numero = cliente.Numero,
                piso = cliente.PisoPuerta,
                cp = cliente.CodigoPostal,
                ciudad = cliente.Ciudad,
                provincia = cliente.Provincia,
                pais = cliente.Pais,
                suscrito_newsletter = cliente.SuscritoNewsletter
            };

            return Results.Ok(perfilCompleto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo el perfil completo: " + ex.Message);
        }
    }

    public static async Task<IResult> UpdateProfile(ProfileUpdateDto dto, HttpContext httpContext,
        Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;
            var parsed = Guid.Parse(userId);

            // 1. Optimización: Cargar ambos datos en paralelo para ser más rápido
            var usuarioTask = client.From<Usuario>().Where(u => u.Id == parsed).Single();
            var clienteTask = client.From<Cliente>().Where(c => c.Id == parsed).Single();

            await Task.WhenAll(usuarioTask, clienteTask);

            var usuario = usuarioTask.Result;
            var cliente = clienteTask.Result;

            if (usuario == null || cliente == null)
                return Results.NotFound("Usuario o Cliente no encontrados.");

            // 2. Actualizar Tabla USUARIO
            // Modificamos directamente el objeto 'usuario' descargado.
            // Si el DTO es nulo o vacío, NO tocamos la propiedad, conservando el valor original.
            if (!string.IsNullOrEmpty(dto.Nombre)) usuario.Nombre = dto.Nombre;
            if (!string.IsNullOrEmpty(dto.Apellidos)) usuario.Apellidos = dto.Apellidos;
            if (!string.IsNullOrEmpty(dto.Dni)) usuario.Dni = dto.Dni;
            if (!string.IsNullOrEmpty(dto.Telefono)) usuario.Telefono = dto.Telefono;

            // Nota: No hace falta asignar usuario.Email ni Id, ya vienen en el objeto original.

            var usuarioResponse = await client.From<Usuario>().Update(usuario);
            var usuarioNuevo = usuarioResponse.Models.First();

            // 3. Actualizar Tabla CLIENTE
            if (!string.IsNullOrEmpty(dto.Calle)) cliente.Calle = dto.Calle;
            if (!string.IsNullOrEmpty(dto.Numero)) cliente.Numero = dto.Numero;
            if (!string.IsNullOrEmpty(dto.PisoPuerta)) cliente.PisoPuerta = dto.PisoPuerta;
            if (!string.IsNullOrEmpty(dto.CodigoPostal)) cliente.CodigoPostal = dto.CodigoPostal;
            if (!string.IsNullOrEmpty(dto.Ciudad)) cliente.Ciudad = dto.Ciudad;
            if (!string.IsNullOrEmpty(dto.Provincia)) cliente.Provincia = dto.Provincia;
            if (!string.IsNullOrEmpty(dto.Pais)) cliente.Pais = dto.Pais;

            // Para booleanos (nullable), verificamos si tiene valor (no es null)
            if (dto.SuscritoNewsletter.HasValue)
                cliente.SuscritoNewsletter = dto.SuscritoNewsletter.Value;

            var clienteResponse = await client.From<Cliente>().Update(cliente);
            var clienteNuevo = clienteResponse.Models.First();

            var resultado = new
            {
                status = "success",
                message = "Perfil actualizado correctamente",
                data = new
                {
                    nombre = usuarioNuevo.Nombre,
                    apellidos = usuarioNuevo.Apellidos,
                    dni = usuarioNuevo.Dni,
                    telefono = usuarioNuevo.Telefono,
                    calle = clienteNuevo.Calle,
                    numero = clienteNuevo.Numero,
                    piso = clienteNuevo.PisoPuerta,
                    cp = clienteNuevo.CodigoPostal,
                    ciudad = clienteNuevo.Ciudad,
                    provincia = clienteNuevo.Provincia,
                    pais = clienteNuevo.Pais,
                    newsletter = clienteNuevo.SuscritoNewsletter
                }
            };

            return Results.Ok(resultado);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error actualizando perfil: " + ex.Message);
        }
    }

    public record ProfileUpdateDto(
        string? Nombre,
        string? Apellidos,
        string? Dni,
        string? Telefono,
        string? Calle,
        string? Numero,
        string? PisoPuerta,
        string? CodigoPostal,
        string? Ciudad,
        string? Provincia,
        string? Pais,
        bool? SuscritoNewsletter
    );
}