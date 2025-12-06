using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Filters;

public class SwaggerEmptyStringDefaultFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Verificamos si el esquema tiene propiedades (es un objeto/clase)
        if (schema.Properties == null) return;

        foreach (var property in schema.Properties)
        {
            // Verificamos:
            // 1. Que la propiedad sea de tipo String
            // 2. Que no tenga ya un ejemplo específico asignado
            if (property.Value.Type == "string" && property.Value.Example == null)
            {
                // Asignamos comillas vacías como ejemplo predeterminado
                property.Value.Example = new OpenApiString("");
            }
        }
    }
}