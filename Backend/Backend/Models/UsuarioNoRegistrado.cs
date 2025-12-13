using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("usuario_no_registrado")]
public class UsuarioNoRegistrado : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("email")] public string? Email { get; set; }

    [Column("dni")] public string? Dni { get; set; }

    [Column("nombre")] public string? Nombre { get; set; }

    [Column("apellidos")] public string? Apellidos { get; set; }

    [Column("telefono")] public string? Telefono { get; set; }
}

