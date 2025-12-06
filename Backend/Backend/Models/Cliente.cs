using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("cliente")]
class Cliente : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)] public Guid Id { get; set; }

    [Column("calle")] public string? Calle { get; set; }

    [Column("numero")] public string? Numero { get; set; }

    [Column("piso_puerta")] public string? PisoPuerta { get; set; }

    [Column("codigo_postal")] public string? CodigoPostal { get; set; }

    [Column("ciudad")] public string? Ciudad { get; set; }

    [Column("provincia")] public string? Provincia { get; set; }

    [Column("pais")] public string? Pais { get; set; }

    [Column("suscrito_newsletter")] public bool SuscritoNewsletter { get; set; }
}