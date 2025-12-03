﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("donacion")]
class Donacion : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("fk_pago")]
    public Guid FkPago {private  get; set; }
        
    // Una Donacion tiene un Pago asociado.
    [Reference(typeof(Pago))]
    public Pago? Pago { get; set; }
}