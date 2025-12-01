using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;


    
[Table("cliente")]
class Cliente : BaseModel
{
    // Coincide con el ID_usuario
    [PrimaryKey("id_cliente")] public long IdCliente { get; set; }

    [Column("direccion")] public string? Direccion { get; set; }

    [Column("suscritonewsletter")] public bool SuscritoNewsletter { get; set; } // bool normal (true/false)

    [Column("tipo")] public string? Tipo { get; set; } // "Socio" o "Corporativo"
}