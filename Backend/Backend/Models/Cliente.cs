using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;


    
[Table("cliente")]
class Cliente : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)] 
    public Guid Id { get; set; }

    [Column("direccion")] 
    public string? Direccion { get; set; }

    [Column("suscrito_newsletter")] 
    public bool SuscritoNewsletter { get; set; } // bool normal (true/false)
}