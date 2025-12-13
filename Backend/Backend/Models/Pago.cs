﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("pago")]
class Pago : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("monto")] public decimal Monto { get; set; }

    [Column("fecha")] public DateTime Fecha { get; set; }

    [Column("estado")] public string Estado { get; set; } // "Pendiente", "Pagado"

    [Column("metodo_pago")] public string? MetodoDePago { get; set; }

    [Column("fk_cliente")] public Guid? FkCliente { get; set; }

    [Reference(typeof(Cliente))] public Cliente? Cliente { get; set; }
}