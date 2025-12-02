﻿using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("admin")]
public class Admin : BaseModel
{
    [PrimaryKey("id_admin", shouldInsert: false)]
    public long IdAdmin { get; set; }

    [Column("id_usuario")]
    public long IdUsuario { get; set; }
}

