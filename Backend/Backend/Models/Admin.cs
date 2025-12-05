using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("admin")]
public class Admin : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("fk_usuario")] public Guid fkUsuario { get; set; }
}