using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("entrada")]
public class Entrada : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }
    
    [Column("fk_usuario")]
    public Guid FkUsuario { get; set; }

    [Column("fk_evento")]
    public Guid FkEvento { get; set; }

    [Column("fk_pago")]
    public Guid FkPago { get; set; }
    
    [Column("fk_entrada_evento")]
    public Guid FkEntradaEvento { get; set; }

    [Column("codigo_qr")]
    public string? CodigoQr { get; set; }

    [Column("precio")]
    public decimal Precio { get; set; }

    [Column("fecha_compra")]
    public DateTime FechaCompra { get; set; }
    
    [Column("estado")]
    public string Estado { get; set; } 
}