using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("socio")]
public class Socio : BaseModel
{
    [PrimaryKey("id", false)] public Guid? Id { get; set; }

    [Column("fk_cliente")] public Guid FkCliente { get; set; }

    [Column("tipo_suscripcion")] public string TipoSuscripcion { get; set; }
    
    [Column("cuota")] public decimal Cuota { get; set; }

    [Column("fecha_fin")] public DateTime FechaFin { get; set; }
    
    [Column("fecha_inicio")] public DateTime FechaInicio { get; set; }
}