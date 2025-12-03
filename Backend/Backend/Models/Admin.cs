﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("admin")]
public class Admin : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("fk_usuario")]
    private Guid fkUsuario { get; set; }
    
    [Reference(typeof(Usuario))]
    public Usuario? Usuario { get; set; }
}

