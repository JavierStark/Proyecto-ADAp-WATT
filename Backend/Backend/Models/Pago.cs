﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("pago")]
public class Pago : BaseModel
{
    [PrimaryKey("id_pago", shouldInsert: false)]
    public long IdPago { get; set; }

    [Column("monto")]
    public decimal Monto { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("estado")]
    public string Estado { get; set; } // "Pendiente", "Pagado"
    
    [Column("metododepago")]
    public string? MetodoDePago { get; set; }

    [Column("id_cliente")]
    public long IdCliente { get; set; }
}