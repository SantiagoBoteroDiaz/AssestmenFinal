using System;
using System.Collections.Generic;

namespace assestment.Models;

public partial class KycVerification
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Estado { get; set; } = null!;

    public string? NombresExtraidos { get; set; }

    public string? ApellidosExtraidos { get; set; }

    public string? NumeroDocumentoExtraido { get; set; }

    public DateOnly? FechaNacimientoExtraida { get; set; }

    public decimal ConfianzaOcr { get; set; }

    public bool? NombreCoincide { get; set; }

    public bool? DocumentoCoincide { get; set; }

    public bool? FechaNacimientoCoincide { get; set; }

    public string Razones { get; set; } = null!;

    public DateTime FechaVerificacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
