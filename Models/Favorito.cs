using System;
using System.Collections.Generic;

namespace assestment.Models;

public partial class Favorito
{
    public Guid UsuarioId { get; set; }

    public Guid InmuebleId { get; set; }

    public DateTime FechaAgregado { get; set; }

    public virtual Inmueble Inmueble { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
