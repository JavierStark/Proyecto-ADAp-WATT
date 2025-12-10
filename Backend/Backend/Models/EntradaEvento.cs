using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("entrada_evento")]
public class EntradaEvento : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Column("fk_evento")] public Guid FkEvento { get; set; }

    [Column("tipo")] public string Tipo { get; set; }

    [Column("cantidad")] public int Cantidad { get; set; }

    [Column("precio")] public decimal Precio { get; set; }
}