using Backend.Models;
using static Supabase.Postgrest.Constants;

namespace Backend;

public class Corporate
{
    public static async Task<IResult> UpdateCorporate(HttpContext httpContext, CorporativoDto dto, 
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
                .From<Models.Corporativo>()
                .Filter("fk_cliente", Operator.Equals, userIdString)
                .Get();

            var empresaExistente = response.Models.FirstOrDefault();

            Models.Corporativo resultadoFinal;
            string mensaje;
            
            if (empresaExistente != null)
            {
                // Actualizar
                await client.From<Models.Corporativo>()
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
                var nuevaEmpresa = new Models.Corporativo
                {
                    FkCliente = userGuid,
                    NombreEmpresa = dto.NombreEmpresa.Trim()
                };

                // Insertamos y capturamos la respuesta para devolver el objeto creado (con su nuevo ID)
                var insertResponse = await client
                    .From<Models.Corporativo>()
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
    
    public static async Task<IResult> GetCorporateData(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = httpContext.Items["user_id"]?.ToString();
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            
            var response = await client
                .From<Corporativo>()
                .Filter("fk_cliente", Operator.Equals, userId)
                .Get();

            var empresa = response.Models.FirstOrDefault();

            if (empresa == null)
            {
                return Results.NotFound(new { error = "El usuario no tiene perfil corporativo." });
            }

            return Results.Ok(new
            {
                nombreEmpresa = empresa.NombreEmpresa
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo datos corporativos: " + ex.Message);
        }
    }

    public record CorporativoDto(string NombreEmpresa);
}