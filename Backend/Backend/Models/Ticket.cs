﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("entrada")]
public class Ticket : BaseModel
{
    [PrimaryKey("id_entrada", shouldInsert: false)]
    public long IdTicket { get; set; }
    
    [Column("id_usuario")]
    public long IdUsuario { get; set; }

    [Column("id_evento")]
    public long IdEvento { get; set; }

    [Column("id_pago")]
    public long IdPago { get; set; }
    
    [Column("id_entrada_evento")]
    public long IdEntradaEvento { get; set; }

    [Column("codigoqr")]
    public string? CodigoQr { get; set; }

    [Column("precio")]
    public decimal Precio { get; set; }

    [Column("fechacompra")]
    public DateTime FechaCompra { get; set; }
    
    [Column("tipodeentrada")]
    public string? TipoDeEntrada { get; set; }
}