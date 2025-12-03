using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("donacion")]
class Donacion : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid Id { get; set; }

    [Column("fk_pago")]
    public Guid FkPago { get; set; }
    
    [Reference(typeof(Pago))]
    public Pago? Pago { get; set; }
}