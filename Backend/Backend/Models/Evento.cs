using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("evento")] 
public class Evento : BaseModel
{
    [PrimaryKey("id_evento")] public long IdEvento { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; }

    [Column("descripcion")] public string? Descripcion { get; set; }

    [Column("fecha_evento")]
    public DateTime FechaEvento { get; set; }

    [Column("precio_entrada")]
    public decimal PrecioEntrada { get; set; }

    [Column("aforo")]
    public int? Aforo { get; set; }

    [Column("entradavalida")]
    public bool EntradaValida { get; set; }
    
    [Column("objetorecaudacion")]
    public string? ObjetoRecaudacion { get; set; }
}
