using Backend.Models;
using Supabase.Gotrue;
using static Supabase.Postgrest.Constants;

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
                direccion = cliente.Direccion,
                suscrito_newsletter = cliente.SuscritoNewsletter,
            };

            return Results.Ok(perfilCompleto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo el perfil completo: " + ex.Message);
        }
    }

    public static async Task<IResult> UpdateProfile(ProfileUpdateDto dto, HttpContext httpContext, Supabase.Client client)
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

            var usuarioUpdate = new Usuario
            {
                Id = usuario.Id, // PK obligatoria para update
                Email = usuario.Email, // El email no se toca aquí
                
                Nombre = !string.IsNullOrEmpty(dto.Nombre) ? dto.Nombre : usuario.Nombre,
                Apellidos = !string.IsNullOrEmpty(dto.Apellidos) ? dto.Apellidos : usuario.Apellidos,
                Dni = !string.IsNullOrEmpty(dto.Dni) ? dto.Dni : usuario.Dni,
                Telefono = !string.IsNullOrEmpty(dto.Telefono) ? dto.Telefono : usuario.Telefono
            };

            var usuarioResponse = await client.From<Usuario>().Update(usuarioUpdate);
            var usuarioNuevo = usuarioResponse.Models.First();

            // Actualizar Tabla CLIENTE ---
            var clienteUpdate = new Cliente
            {
                Id = cliente.Id, // PK obligatoria
                Direccion = !string.IsNullOrEmpty(dto.Direccion) ? dto.Direccion : cliente.Direccion,

                SuscritoNewsletter = dto.SuscritoNewsletter ?? cliente.SuscritoNewsletter
            };

            var clienteResponse = await client.From<Cliente>().Update(clienteUpdate);
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
                    direccion = clienteNuevo.Direccion,
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
        string? Direccion,
        bool? SuscritoNewsletter
    );
}