using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("evento")]
public class Evento : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("nombre")] public string? Nombre { get; set; }

    [Column("descripcion")] public string? Descripcion { get; set; }

    [Column("fecha_y_hora")] public DateTimeOffset? FechaEvento { get; set; }

    [Column("ubicacion")] public string? Ubicacion { get; set; }

    [Column("aforo")] public int? Aforo { get; set; }

    [Column("entradas_vendidas")] public int EntradasVendidas { get; set; }

    [Column("evento_visible")] public bool? EventoVisible { get; set; }

    [Column("objetivo_recaudacion")] public decimal? ObjetivoRecaudacion { get; set; }
    
    [Column("recaudacion_extra")] public decimal? RecaudacionExtra { get; set; }
    
    [Column("imagen_url")] public string? ImagenUrl { get; set; }
}