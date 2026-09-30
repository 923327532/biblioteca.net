namespace Biblioteca.Entidades;

public interface ILibroRepositorio
{
    Task<List<Libro>> ListarAsync(bool soloActivos = true);

    Task<List<Libro>> BuscarAsync(string filtro);

    Task<Libro?> ObtenerPorIdAsync(int libroId);

    Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId = 0);

    Task<int> InsertarAsync(Libro libro);

    Task<bool> ActualizarAsync(Libro libro);

    Task<bool> EliminarLogicoAsync(int libroId);

    Task<int> ContarPrestamosPendientesAsync(int libroId);
}
