using System;
using System.Collections.Generic;

namespace assestment.Models;

public partial class Usuario
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public DateOnly FechaNacimiento { get; set; }

    public string Rol { get; set; } = null!;

    public bool KycAprobado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual ICollection<Inmueble> Inmuebles { get; set; } = new List<Inmueble>();

    public virtual KycVerification? KycVerification { get; set; }

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
