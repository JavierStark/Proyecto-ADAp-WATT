using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Backend.Models;

[Table("reserva_entrada")]
public class ReservaEntrada : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public Guid IdReserva { get; set; }

    [Column("fk_entrada_evento")] public Guid FkEntradaEvento { get; set; }

    [Column("fk_usuario")] public Guid FkUsuario { get; set; }

    [Column("cantidad")] public int Cantidad { get; set; }

    [Column("fecha_expiracion")] public DateTime FechaExpiracion { get; set; }
}