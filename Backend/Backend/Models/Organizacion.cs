using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("corporativo")]
public class Organizacion : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid? Id { get; set; }

    [Column("fk_cliente")]
    public Guid FkCliente { get; set; }

    [Column("nombre_empresa")]
    public string NombreEmpresa { get; set; }
}