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

    public static IResult UpdateMyProfile(UserUpdateDto dto) => Results.Ok();
    public static IResult PartialUpdateProfile(UserUpdatePartialDto dto) => Results.Ok();
    
    public record UserUpdateDto(string Name, string Email, string Phone);
    public record UserUpdatePartialDto(string? Name, string? Phone);

    
    [Table("cliente")]
    private class Cliente : BaseModel
    {
        // Coincide con el ID_usuario
        [PrimaryKey("id_cliente")] public long IdCliente { get; set; }

        [Column("direccion")] public string? Direccion { get; set; }

        [Column("suscritonewsletter")] public bool SuscritoNewsletter { get; set; } // bool normal (true/false)

        [Column("Tipo")] public string? Tipo { get; set; } // "Socio" o "Corporativo"
    }
    

}