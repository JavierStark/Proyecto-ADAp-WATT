using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("entrada")]
class Entrada : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }
    
    [Column("fk_usuario")]
    public Guid FkUsuario { private get; set; }

    [Column("fk_evento")]
    public Guid FkEvento { private get; set; }

    [Column("fk_pago")]
    public Guid FkPago { private get; set; }
    
    [Column("fk_entrada_evento")]
    public Guid FkEntradaEvento { private get; set; }

    [Column("codigo_qr")]
    public string? CodigoQr { get; set; }

    [Column("precio")]
    public decimal Precio { get; set; }

    [Column("fecha_compra")]
    public DateTime FechaCompra { get; set; }
    
    [Column("estado")]
    public string Estado { get; set; } 
    
    [Reference(typeof(Evento))]
    public Evento? Evento { get; set; }
    [Reference(typeof(Pago))]
    public Pago? Pago { get; set; }
    [Reference(typeof(EntradaEvento))]
    public EntradaEvento? EntradaEvento { get; set; }
    [Reference(typeof(Usuario))]
    public Usuario? Usuario { get; set; }
}