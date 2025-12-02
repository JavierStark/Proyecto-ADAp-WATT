﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("evento")] 
public class Evento : BaseModel
{
    [PrimaryKey("id_evento", shouldInsert: false)]
    public long IdEvento { get; set; }

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

    [Column("entradavalida")]
    public bool EntradaValida { get; set; }
    
    [Column("objetorecaudacion")]
    public string? ObjetoRecaudacion { get; set; }
}

[Table("entrada_evento")]
public class EntradaEvento : BaseModel
{
    // BIGINT en SQL -> long en C#
    [PrimaryKey("id_entrada_evento")]
    public long IdEntradaEvento { get; set; }

    // FK hacia la tabla Evento
    [Column("id_evento")]
    public long IdEvento { get; set; }

    // Ej: "General", "VIP"
    [Column("tipo")]
    public string Tipo { get; set; }

    // Stock disponible (INT en SQL -> int en C#)
    [Column("numero")]
    public int Numero { get; set; }

    // Precio específico (DECIMAL en SQL -> decimal en C#)
    [Column("precio")]
    public decimal Precio { get; set; }
}