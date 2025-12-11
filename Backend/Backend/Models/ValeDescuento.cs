using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("vale_descuento")]
public class ValeDescuento : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("codigo")] public string Codigo { get; set; }
    [Column("descuento")] public decimal Descuento { get; set; }
    [Column("fecha_expiracion")] public DateTime? FechaExpiracion { get; set; }
    [Column("cantidad")] public int? Cantidad { get; set; }
}