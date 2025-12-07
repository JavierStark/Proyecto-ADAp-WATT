using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("periodo_socio")]
public class PeriodoSocio : BaseModel
{
    [PrimaryKey("id", false)] public Guid Id { get; set; }

    [Column("fk_socio")] public Guid FkSocio { get; set; }

    [Column("fk_pago")] public Guid FkPago { get; set; }

    [Column("concepto")] public string Concepto { get; set; }
}