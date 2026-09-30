using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class LibroDatos : ILibroRepositorio
{
    private const string SelectBase = @"
        SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, l.Ejemplares, l.Activo,
               a.Nombre AS NombreAutor
        FROM Libros l
        INNER JOIN Autores a ON a.AutorId = l.AutorId";

    public async Task<List<Libro>> ListarAsync(bool soloActivos = true)
    {
        string sql = SelectBase + (soloActivos ? " WHERE l.Activo = 1" : string.Empty) + " ORDER BY l.Titulo";

        List<Libro> libros = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            libros.Add(Mapear(reader));
        }

        return libros;
    }

    public async Task<List<Libro>> BuscarAsync(string filtro)
    {
        const string sql = SelectBase + @"
        WHERE l.Activo = 1
          AND (l.Titulo LIKE @Filtro OR a.Nombre LIKE @Filtro)
        ORDER BY l.Titulo";

        List<Libro> libros = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@Filtro", $"%{filtro}%"));

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            libros.Add(Mapear(reader));
        }

        return libros;
    }

    public async Task<Libro?> ObtenerPorIdAsync(int libroId)
    {
        const string sql = SelectBase + " WHERE l.LibroId = @LibroId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@LibroId", libroId));

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Mapear(reader) : null;
    }

    public async Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId = 0)
    {
        const string sql = "SELECT COUNT(1) FROM Libros WHERE ISBN = @ISBN AND LibroId <> @ExcluirLibroId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@ISBN", isbn));
        cmd.Parameters.Add(new SqlParameter("@ExcluirLibroId", excluirLibroId));

        await cn.OpenAsync();
        object? resultado = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(resultado) > 0;
    }

    public async Task<int> InsertarAsync(Libro libro)
    {
        const string sql = @"
            INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares, Activo)
            OUTPUT INSERTED.LibroId
            VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares, @Activo)";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        AgregarParametros(cmd, libro, incluirId: false);

        await cn.OpenAsync();
        object? id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<bool> ActualizarAsync(Libro libro)
    {
        const string sql = @"
            UPDATE Libros
            SET Titulo = @Titulo,
                ISBN = @ISBN,
                AutorId = @AutorId,
                Ejemplares = @Ejemplares
            WHERE LibroId = @LibroId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        AgregarParametros(cmd, libro, incluirId: true);

        await cn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> EliminarLogicoAsync(int libroId)
    {
        const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @LibroId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@LibroId", libroId));

        await cn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> ContarPrestamosPendientesAsync(int libroId)
    {
        const string sql = "SELECT COUNT(1) FROM DetallePrestamo WHERE LibroId = @LibroId AND FechaDevolucion IS NULL";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@LibroId", libroId));

        await cn.OpenAsync();
        object? resultado = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(resultado);
    }

    private static void AgregarParametros(SqlCommand cmd, Libro libro, bool incluirId)
    {
        if (incluirId)
        {
            cmd.Parameters.Add(new SqlParameter("@LibroId", libro.LibroId));
        }

        cmd.Parameters.Add(new SqlParameter("@Titulo", libro.Titulo));
        cmd.Parameters.Add(new SqlParameter("@ISBN", libro.ISBN));
        cmd.Parameters.Add(new SqlParameter("@AutorId", libro.AutorId));
        cmd.Parameters.Add(new SqlParameter("@Ejemplares", libro.Ejemplares));
        cmd.Parameters.Add(new SqlParameter("@Activo", libro.Activo));
    }

    private static Libro Mapear(SqlDataReader reader) => new()
    {
        LibroId = reader.GetInt32(0),
        Titulo = reader.GetString(1),
        ISBN = reader.GetString(2),
        AutorId = reader.GetInt32(3),
        Ejemplares = reader.GetInt32(4),
        Activo = reader.GetBoolean(5),
        NombreAutor = reader.GetString(6)
    };
}
