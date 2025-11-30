using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;


[Table("usuario")]
public  class Usuario : BaseModel
{
    // Clave primaria numérica (1, 2, 3...)
    [PrimaryKey("id_usuario")] public long IdUsuario { get; set; }

    // El puente con el login (UUID)
    [Column("id_auth_supabase")] public string IdAuthSupabase { get; set; }

    [Column("Email")] public string? Email { get; set; }

    [Column("dni")] public string? Dni { get; set; }

    [Column("nombre")] public string? Nombre { get; set; }

    [Column("apellidos")] public string? Apellidos { get; set; }

    [Column("telefono")] public string? Telefono { get; set; }
}