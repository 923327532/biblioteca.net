namespace Biblioteca.Entidades;

public class Prestamo
{
    public int PrestamoId { get; set; }

    public int SocioId { get; set; }

    public DateTime FechaPrestamo { get; set; } = DateTime.Today;

    public DateTime FechaLimite { get; set; } = DateTime.Today.AddDays(14);

    public string Estado { get; set; } = EstadoPrestamo.Pendiente;

    public string NombreSocio { get; set; } = string.Empty;

    public List<DetallePrestamo> Detalles { get; set; } = new();
}

public static class EstadoPrestamo
{
    public const string Pendiente = "Pendiente";
    public const string Devuelto = "Devuelto";
}
