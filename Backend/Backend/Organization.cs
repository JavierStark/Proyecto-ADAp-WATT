using Backend.Models;
using static Supabase.Postgrest.Constants;

namespace Backend;

public class Organization
{
    public static async Task<IResult> UpdateCompany(HttpContext httpContext, CorporativoDto dto, 
        Supabase.Client client)
    {
        try
        {
            // Auth
            var userIdString = httpContext.Items["user_id"]?.ToString();
            if (string.IsNullOrEmpty(userIdString)) return Results.Unauthorized();
            var userGuid = Guid.Parse(userIdString);

            // Validaciones
            if (string.IsNullOrWhiteSpace(dto.NombreEmpresa))
                return Results.BadRequest("El nombre de la empresa es obligatorio.");
            
            var response = await client
                .From<Organizacion>()
                .Filter("fk_cliente", Operator.Equals, userIdString)
                .Get();

            var empresaExistente = response.Models.FirstOrDefault();

            Organizacion resultadoFinal;
            string mensaje;
            
            if (empresaExistente != null)
            {
                // Actualizar
                await client.From<Organizacion>()
                    .Filter("id", Operator.Equals, empresaExistente.Id.ToString())
                    .Set(x => x.NombreEmpresa, dto.NombreEmpresa.Trim())
                    .Update();
                
                empresaExistente.NombreEmpresa = dto.NombreEmpresa.Trim();
                resultadoFinal = empresaExistente;
                mensaje = "Perfil corporativo actualizado correctamente.";
            }
            else
            {
                // Insertar
                var nuevaEmpresa = new Organizacion
                {
                    FkCliente = userGuid,
                    NombreEmpresa = dto.NombreEmpresa.Trim()
                };

                // Insertamos y capturamos la respuesta para devolver el objeto creado (con su nuevo ID)
                var insertResponse = await client
                    .From<Organizacion>()
                    .Insert(nuevaEmpresa);

                resultadoFinal = insertResponse.Models.First();
                mensaje = "Perfil corporativo creado correctamente.";
            }

            return Results.Ok(new 
            { 
                message = mensaje, 
                datos = new 
                {
                    id = resultadoFinal.Id,
                    nombreEmpresa = resultadoFinal.NombreEmpresa,
                    fkCliente = resultadoFinal.FkCliente
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error gestionando empresa: " + ex.Message);
        }
    }

    public record CorporativoDto(string NombreEmpresa);
}