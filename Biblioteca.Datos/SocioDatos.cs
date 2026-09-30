using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class SocioDatos
{
    private const string SelectBase = "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios";

    public async Task<List<Socio>> ListarAsync(bool soloActivos = true)
    {
        string sql = SelectBase + (soloActivos ? " WHERE Activo = 1" : string.Empty) + " ORDER BY Nombre";

        List<Socio> socios = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            socios.Add(Mapear(reader));
        }

        return socios;
    }

    public async Task<List<Socio>> BuscarAsync(string filtro)
    {
        const string sql = SelectBase + @"
            WHERE Activo = 1
              AND (Nombre LIKE @Filtro OR DNI LIKE @Filtro)
            ORDER BY Nombre";

        List<Socio> socios = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@Filtro", $"%{filtro}%"));

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            socios.Add(Mapear(reader));
        }

        return socios;
    }

    public async Task<Socio?> ObtenerPorIdAsync(int socioId)
    {
        const string sql = SelectBase + " WHERE SocioId = @SocioId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", socioId));

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Mapear(reader) : null;
    }

    public async Task<bool> ExisteDniAsync(string dni, int excluirSocioId = 0)
    {
        const string sql = "SELECT COUNT(1) FROM Socios WHERE DNI = @DNI AND SocioId <> @ExcluirSocioId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@DNI", dni));
        cmd.Parameters.Add(new SqlParameter("@ExcluirSocioId", excluirSocioId));

        await cn.OpenAsync();
        object? resultado = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(resultado) > 0;
    }

    public async Task<int> InsertarAsync(Socio socio)
    {
        const string sql = @"
            INSERT INTO Socios (DNI, Nombre, Email, Activo)
            OUTPUT INSERTED.SocioId
            VALUES (@DNI, @Nombre, @Email, @Activo)";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@DNI", socio.DNI));
        cmd.Parameters.Add(new SqlParameter("@Nombre", socio.Nombre));
        cmd.Parameters.Add(new SqlParameter("@Email", socio.Email));
        cmd.Parameters.Add(new SqlParameter("@Activo", socio.Activo));

        await cn.OpenAsync();
        object? id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<bool> ActualizarAsync(Socio socio)
    {
        const string sql = @"
            UPDATE Socios
            SET DNI = @DNI, Nombre = @Nombre, Email = @Email
            WHERE SocioId = @SocioId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", socio.SocioId));
        cmd.Parameters.Add(new SqlParameter("@DNI", socio.DNI));
        cmd.Parameters.Add(new SqlParameter("@Nombre", socio.Nombre));
        cmd.Parameters.Add(new SqlParameter("@Email", socio.Email));

        await cn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> EliminarLogicoAsync(int socioId)
    {
        const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @SocioId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", socioId));

        await cn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> ContarLibrosPendientesAsync(int socioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM DetallePrestamo d
            INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
            WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", socioId));

        await cn.OpenAsync();
        object? resultado = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(resultado);
    }

    private static Socio Mapear(SqlDataReader reader) => new()
    {
        SocioId = reader.GetInt32(0),
        DNI = reader.GetString(1),
        Nombre = reader.GetString(2),
        Email = reader.GetString(3),
        Activo = reader.GetBoolean(4)
    };
}
