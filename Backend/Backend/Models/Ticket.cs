﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("ticket")]
public class Ticket : BaseModel
{
    [PrimaryKey("id_ticket", shouldInsert: false)] public long IdTicket { get; set; }

    [Column("id_usuario")] public long IdUsuario { get; set; }

    [Column("id_evento")] public long IdEvento { get; set; }

    [Column("fecha_compra")] public DateTime FechaCompra { get; set; }

    [Column("cantidad")] public int Cantidad { get; set; }

    [Column("importe_total")] public decimal ImporteTotal { get; set; }

    [Reference(typeof(Evento))] public Evento Evento { get; set; }
}