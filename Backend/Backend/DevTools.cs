using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Backend;

public static class DevTools
{
    public static IResult GetAllDtoStructures()
    {
        // 1. Obtenemos el ensamblado actual (tu proyecto)
        var assembly = Assembly.GetExecutingAssembly();

        // 2. Buscamos todas las clases/records que terminen en "Dto"
        var dtoTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto"))
            .OrderBy(t => t.Name)
            .ToList();

        var resultado = new Dictionary<string, object>();

        // 3. Política de conversión oficial de .NET (Pascal -> camelCase)
        var namingPolicy = JsonNamingPolicy.CamelCase;

        foreach (var type in dtoTypes)
        {
            // Obtenemos las propiedades públicas
            var propiedades = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var listaProps = new Dictionary<string, string>();

            foreach (var prop in propiedades)
            {
                // Convertimos el nombre C# (NombreEvento) a JSON (nombreEvento)
                var nombreCamel = namingPolicy.ConvertName(prop.Name);
                
                // Guardamos: "nombreEvento": "String" (o el tipo que sea)
                listaProps.Add(nombreCamel, CleanTypeName(prop.PropertyType));
            }

            resultado.Add(type.Name, listaProps);
        }

        return Results.Ok(resultado);
    }

    // Pequeña ayuda para limpiar nombres de tipos (ej: "Nullable`1" -> "Int32?")
    private static string CleanTypeName(Type type)
    {
        var typeName = type.Name;
        
        // Si es una lista
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            var itemType = type.GetGenericArguments()[0];
            return $"List<{CleanTypeName(itemType)}>";
        }

        // Si es Nullable (ej: int?)
        if (Nullable.GetUnderlyingType(type) != null)
        {
            return $"{Nullable.GetUnderlyingType(type).Name} (Opcional)";
        }

        return typeName switch
        {
            "String" => "String",
            "Int32" => "Number (Integer)",
            "Decimal" => "Number (Decimal)",
            "Boolean" => "Boolean",
            "Guid" => "UUID (String)",
            "DateTime" => "Date (ISO String)",
            "DateTimeOffset" => "Date (ISO String)",
            "IFormFile" => "File (Binary)",
            _ => typeName // Para otros DTOs anidados
        };
    }
}