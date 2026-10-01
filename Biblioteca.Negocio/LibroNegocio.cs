using System.Text.RegularExpressions;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class LibroNegocio
{
    private static readonly Regex IsbnRegex = new(@"^\d{3}-\d{10}$", RegexOptions.Compiled);
    private readonly ILibroRepositorio _repositorio;
    private readonly AutorDatos _autorDatos;

    public LibroNegocio(ILibroRepositorio repositorio, AutorDatos? autorDatos = null)
    {
        _repositorio = repositorio;
        _autorDatos = autorDatos ?? new AutorDatos();
    }

    public Task<List<Libro>> ListarAsync() => _repositorio.ListarAsync();

    public Task<List<Autor>> ListarAutoresAsync() => _autorDatos.ListarAsync();

    public Task<List<Libro>> BuscarAsync(string filtro) =>
        _repositorio.BuscarAsync(filtro?.Trim() ?? string.Empty);

    public async Task<int> InsertarAsync(Libro libro)
    {
        Validar(libro, esNuevo: true);

        if (await _repositorio.ExisteIsbnAsync(libro.ISBN, libro.LibroId))
        {
            throw new ReglaNegocioException($"El ISBN '{libro.ISBN}' ya está registrado en otro libro.");
        }

        return await _repositorio.InsertarAsync(libro);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        if (libro.LibroId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar un libro para actualizar.");
        }

        Validar(libro, esNuevo: false);

        if (await _repositorio.ExisteIsbnAsync(libro.ISBN, libro.LibroId))
        {
            throw new ReglaNegocioException($"El ISBN '{libro.ISBN}' ya está registrado en otro libro.");
        }

        await _repositorio.ActualizarAsync(libro);
    }

    public async Task EliminarAsync(int libroId)
    {
        if (libroId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar un libro para eliminar.");
        }

        int pendientes = await _repositorio.ContarPrestamosPendientesAsync(libroId);
        if (pendientes > 0)
        {
            throw new ReglaNegocioException(
                $"No se puede dar de baja el libro: tiene {pendientes} ejemplar(es) pendiente(s) de devolución.");
        }

        await _repositorio.EliminarLogicoAsync(libroId);
    }

    private static void Validar(Libro libro, bool esNuevo)
    {
        if (libro is null)
        {
            throw new ReglaNegocioException("No se recibió la información del libro.");
        }

        if (string.IsNullOrWhiteSpace(libro.Titulo))
        {
            throw new ReglaNegocioException("El título del libro es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(libro.ISBN))
        {
            throw new ReglaNegocioException("El ISBN del libro es obligatorio.");
        }

        libro.ISBN = libro.ISBN.Trim();

        if (!IsbnRegex.IsMatch(libro.ISBN))
        {
            throw new ReglaNegocioException("El ISBN no tiene un formato válido. Debe ser similar a '978-8437604909' (3 dígitos, un guion y 10 dígitos numéricos).");
        }

        if (libro.AutorId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar un autor para el libro.");
        }

        if (libro.Ejemplares < 0)
        {
            throw new ReglaNegocioException("La cantidad de ejemplares no puede ser negativa.");
        }

        if (esNuevo)
        {
            libro.Activo = true;
            libro.LibroId = 0;
        }
    }
}
