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
