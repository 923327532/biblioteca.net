using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class PrestamoNegocio
{
    public const decimal MultaPorDia = 1.50m;

    public const int MaxLibrosPendientes = 3;

    private readonly PrestamoDatos _prestamoDatos;
    private readonly LibroDatos _libroDatos;
    private readonly SocioDatos _socioDatos;

    public PrestamoNegocio(PrestamoDatos? prestamoDatos = null)
    {
        _prestamoDatos = prestamoDatos ?? new PrestamoDatos();
        _libroDatos = new LibroDatos();
        _socioDatos = new SocioDatos();
    }

    public Task<List<Prestamo>> ListarPendientesAsync() => _prestamoDatos.ListarPendientesAsync();

    public Task<List<Socio>> ListarSociosAsync() => _socioDatos.ListarAsync();

    public Task<List<Libro>> ListarLibrosAsync() => _libroDatos.ListarAsync();

    public Task<List<PrestamoReporte>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
    {
        if (desde.Date > hasta.Date)
        {
            throw new ReglaNegocioException("La fecha inicial no puede ser mayor que la fecha final.");
        }

        return _prestamoDatos.ReportePorFechasAsync(desde.Date, hasta.Date);
    }

    public async Task<int> RegistrarAsync(int socioId, IEnumerable<int> libroIds, DateTime? fechaPrestamo = null)
    {
        List<int> ids = libroIds?.Distinct().ToList() ?? new List<int>();

        if (socioId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar un socio.");
        }

        if (ids.Count == 0)
        {
            throw new ReglaNegocioException("Debe agregar al menos un libro al préstamo.");
        }

        Socio socio = await _socioDatos.ObtenerPorIdAsync(socioId)
                      ?? throw new ReglaNegocioException("El socio seleccionado no existe.");

        if (!socio.Activo)
        {
            throw new ReglaNegocioException($"El socio {socio.Nombre} está inactivo: no se le pueden prestar libros.");
        }

        int pendientesActuales = await _prestamoDatos.ContarLibrosPendientesAsync(socioId);
        if (pendientesActuales + ids.Count > MaxLibrosPendientes)
        {
            throw new ReglaNegocioException(
                $"El socio {socio.Nombre} ya tiene {pendientesActuales} libro(s) pendiente(s) y solo se permiten {MaxLibrosPendientes} como máximo.");
        }

        List<int> yaPendientes = await _prestamoDatos.ListarLibrosPendientesAsync(socioId);

        Prestamo prestamo = new()
        {
            SocioId = socioId,
            FechaPrestamo = (fechaPrestamo ?? DateTime.Today).Date,
            FechaLimite = (fechaPrestamo ?? DateTime.Today).Date.AddDays(14),
            Estado = EstadoPrestamo.Pendiente,
            NombreSocio = socio.Nombre
        };

        foreach (int libroId in ids)
        {
            Libro libro = await _libroDatos.ObtenerPorIdAsync(libroId)
                          ?? throw new ReglaNegocioException("Uno de los libros seleccionados no existe.");

            if (!libro.Activo)
            {
                throw new ReglaNegocioException($"El libro '{libro.Titulo}' está dado de baja y no puede prestarse.");
            }

            if (libro.Ejemplares <= 0)
            {
                throw new ReglaNegocioException($"El libro '{libro.Titulo}' no tiene ejemplares disponibles.");
            }

            if (yaPendientes.Contains(libroId))
            {
                throw new ReglaNegocioException($"El socio ya tiene el libro '{libro.Titulo}' pendiente de devolución.");
            }

            prestamo.Detalles.Add(new DetallePrestamo
            {
                LibroId = libroId,
                Titulo = libro.Titulo,
                ISBN = libro.ISBN
            });
        }

        return await _prestamoDatos.RegistrarAsync(prestamo);
    }

    public async Task<decimal> RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime? fechaDevolucion = null)
    {
        if (prestamoId <= 0 || libroId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar el préstamo y el libro a devolver.");
        }

        DateTime fecha = (fechaDevolucion ?? DateTime.Today).Date;

        DateTime? fechaLimite = await _prestamoDatos.ObtenerFechaLimiteAsync(prestamoId);
        if (fechaLimite is null)
        {
            throw new ReglaNegocioException("El préstamo seleccionado no existe.");
        }

        await _prestamoDatos.RegistrarDevolucionAsync(prestamoId, libroId, fecha);
        return CalcularMulta(fechaLimite.Value, fecha);
    }

    public static decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion)
    {
        int diasRetraso = (fechaDevolucion.Date - fechaLimite.Date).Days;
        return diasRetraso > 0 ? diasRetraso * MultaPorDia : 0m;
    }
}
