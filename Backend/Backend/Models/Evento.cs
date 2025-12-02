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
    
    [Reference(typeof(EntradaEvento))]
    public List<EntradaEvento> Entradas { get; set; } = new();
}

[Table("entrada_evento")]
public class EntradaEvento : BaseModel
{
    [PrimaryKey("id_entrada_evento")]
    public long IdEntradaEvento { get; set; }
    
    [Column("id_evento")]
    public long IdEvento { get; set; }
    
    [Column("tipo")]
    public string Tipo { get; set; }
    
    [Column("numero")]
    public int Numero { get; set; }
    
    [Column("precio")]
    public decimal Precio { get; set; }
}