using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("evento")] 
public class Evento : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; }

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("fecha_y_hora")]
    public DateTime FechaEvento { get; set; }

    [Column("ubicacion")]
    public string? Ubicacion { get; set; }

    [Column("aforo")]
    public int? Aforo { get; set; }
    
    [Column("entradas_vendidas")] 
    public int EntradasVendidas { get; set; }

    [Column("entrada_valida")]
    public bool EntradaValida { get; set; }
    
    [Column("objeto_recaudacion")]
    public string? ObjetoRecaudacion { get; set; }
    
    [Reference(typeof(EntradaEvento))]
    public List<EntradaEvento> Entradas { get; set; } = [];
}