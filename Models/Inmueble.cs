using System;
using System.Collections.Generic;

namespace assestment.Models;

public partial class Inmueble
{
    public Guid Id { get; set; }

    public Guid PropietarioId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string Ubicacion { get; set; } = null!;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public decimal TarifaPorNoche { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? UrlImagen { get; set; }

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual Usuario Propietario { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
