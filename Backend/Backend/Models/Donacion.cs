using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("donacion")]
public class Donacion : BaseModel
{
    [PrimaryKey("id_donacion")]
    public long IdDonacion { get; set; }

    [Column("id_pago")]
    public long IdPago { get; set; }
        
    // Una Donacion tiene un Pago asociado.
    [Reference(typeof(Pago))]
    public Pago Pago { get; set; }
}