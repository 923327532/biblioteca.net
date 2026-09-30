namespace Biblioteca.Entidades;

public class PrestamoReporte
{
    public int PrestamoId { get; set; }

    public string Socio { get; set; } = string.Empty;

    public string DNI { get; set; } = string.Empty;

    public string Libro { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public DateTime FechaPrestamo { get; set; }

    public DateTime FechaLimite { get; set; }

    public DateTime? FechaDevolucion { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int DiasRetraso
    {
        get
        {
            DateTime referencia = FechaDevolucion ?? DateTime.Today;
            int dias = (referencia - FechaLimite).Days;
            return dias > 0 ? dias : 0;
        }
    }
}
