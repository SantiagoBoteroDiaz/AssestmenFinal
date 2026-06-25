using System;
using System.Collections.Generic;

namespace assestment.Models;

public partial class Reserva
{
    public Guid Id { get; set; }

    public Guid InmuebleId { get; set; }

    public Guid UsuarioId { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public TimeOnly HoraCheckin { get; set; }

    public TimeOnly HoraCheckout { get; set; }

    public decimal PrecioTotal { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual Inmueble Inmueble { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
