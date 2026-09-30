namespace Biblioteca.Entidades;

public class DetallePrestamo
{
    public int PrestamoId { get; set; }

    public int LibroId { get; set; }

    public DateTime? FechaDevolucion { get; set; }

    public bool Devuelto => FechaDevolucion.HasValue;

    public string Titulo { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public string NombreSocio { get; set; } = string.Empty;

    public DateTime FechaLimite { get; set; }

    public string Estado { get; set; } = string.Empty;
}
