using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend;

static class Profile
{
    public static async Task<IResult> GetMyProfile([FromHeader(Name = "Authorization")] string? authHeader, Supabase.Client client)
    {
        // Obtener Token y validar
        if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();

        string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "")
            .Trim();

        if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

        try
        {
            await client.Auth.SetSession(token, "token_falso");
            var userAuth = client.Auth.CurrentUser;
            if (userAuth == null) return Results.Unauthorized();

            // CONSULTA 1: Datos Generales (Tabla Usuario)
            // Buscamos por el UUID de Supabase
            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
                .Single(); // Si falla aquí es que el usuario no existe en tu tabla

            // CONSULTA 2: Datos Específicos (Tabla Cliente)
            // Usamos el ID numérico que acabamos de obtener
            var clienteDb = await client
                .From<Cliente>()
                .Filter("id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario.ToString())
                .Single();

            // COMBINAR DATOS
            // Creamos un objeto para el frontend
            var perfilCompleto = new
            {
                // Datos de identificación
                id_interno = usuarioDb.IdUsuario,
                email = usuarioDb.Email,

                // Datos personales (Tabla Usuario)
                dni = usuarioDb.Dni,
                nombre = usuarioDb.Nombre,
                apellidos = usuarioDb.Apellidos,
                telefono = usuarioDb.Telefono,

                // Datos de cliente (Tabla Cliente)
                direccion = clienteDb.Direccion,
                suscrito_newsletter = clienteDb.SuscritoNewsletter,
            };

            return Results.Ok(perfilCompleto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo el perfil completo: " + ex.Message);
        }
    }

    public static async Task<IResult> UpdateProfile(
        [FromHeader(Name = "Authorization")] string? authHeader, 
        ProfileUpdateDto dto, 
        Supabase.Client client)
    {
        // Validación
        if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();
        string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Replace("\"", "").Trim();

        try
        {
            await client.Auth.SetSession(token, "token_falso");
            var userAuth = client.Auth.CurrentUser;
            if (userAuth == null) return Results.Unauthorized();

            // Obtener los DATOS ACTUALES
            var usuarioDb = await client
                .From<Usuario>()
                .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
                .Single();

            var clienteDb = await client
                .From<Cliente>()
                .Filter("id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario.ToString())
                .Single();

            // Actualizar Tabla USUARIO
            var usuarioUpdate = new Usuario
            {
                IdUsuario = usuarioDb.IdUsuario, // PK obligatoria para update
                IdAuthSupabase = usuarioDb.IdAuthSupabase,
                Email = usuarioDb.Email, // El email no se toca aquí
                
                Nombre = !string.IsNullOrEmpty(dto.Nombre) ? dto.Nombre : usuarioDb.Nombre,
                Apellidos = !string.IsNullOrEmpty(dto.Apellidos) ? dto.Apellidos : usuarioDb.Apellidos,
                Dni = !string.IsNullOrEmpty(dto.Dni) ? dto.Dni : usuarioDb.Dni,
                Telefono = !string.IsNullOrEmpty(dto.Telefono) ? dto.Telefono : usuarioDb.Telefono
            };

            // Enviamos update a Usuario
            var usuarioResponse = await client.From<Usuario>().Update(usuarioUpdate);
            var usuarioNuevo = usuarioResponse.Models.First();

            // Actualizar Tabla CLIENTE ---
            var clienteUpdate = new Cliente
            {
                IdCliente = clienteDb.IdCliente, // PK obligatoria
                Tipo = clienteDb.Tipo, // No dejamos cambiar el tipo
                
                Direccion = !string.IsNullOrEmpty(dto.Direccion) ? dto.Direccion : clienteDb.Direccion,
                
                SuscritoNewsletter = dto.SuscritoNewsletter ?? clienteDb.SuscritoNewsletter
            };

            // Enviamos update a Cliente
            var clienteResponse = await client.From<Cliente>().Update(clienteUpdate);
            var clienteNuevo = clienteResponse.Models.First();

            // Respuesta Combinada
            var resultado = new
            {
                status = "success",
                message = "Perfil actualizado correctamente",
                data = new {
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
    
    [Table("usuario")]
    public class Usuario : BaseModel
    {
        [PrimaryKey("id_usuario")]
        public long IdUsuario { get; set; }

        [Column("id_auth_supabase")]
        public string IdAuthSupabase { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("dni")]
        public string? Dni { get; set; }

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("apellidos")]
        public string? Apellidos { get; set; }

        [Column("telefono")]
        public string? Telefono { get; set; }
    }
    
    [Table("cliente")]
    private class Cliente : BaseModel
    {
        // Coincide con el ID_usuario
        [PrimaryKey("id_cliente")] public long IdCliente { get; set; }

        [Column("direccion")] public string? Direccion { get; set; }

        [Column("suscritonewsletter")] public bool SuscritoNewsletter { get; set; } // bool normal (true/false)

        [Column("tipo")] public string? Tipo { get; set; } // "Socio" o "Corporativo"
    }
}