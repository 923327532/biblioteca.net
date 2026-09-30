using System.Data;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class PrestamoDatos
{
    public async Task<int> RegistrarAsync(Prestamo prestamo)
    {
        await using SqlConnection cn = new(Conexion.Cadena);
        await cn.OpenAsync();
        await using SqlTransaction tx = (SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            await using SqlCommand cmd = new("usp_RegistrarPrestamo", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@SocioId", prestamo.SocioId));
            cmd.Parameters.Add(new SqlParameter("@FechaPrestamo", SqlDbType.Date) { Value = prestamo.FechaPrestamo });
            cmd.Parameters.Add(new SqlParameter("@FechaLimite", SqlDbType.Date) { Value = prestamo.FechaLimite });
            cmd.Parameters.Add(new SqlParameter("@Detalle", SqlDbType.Structured)
            {
                TypeName = "dbo.DetallePrestamoTipo",
                Value = ConstruirTablaDetalle(prestamo)
            });

            SqlParameter salida = new("@PrestamoId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(salida);

            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();

            return Convert.ToInt32(salida.Value);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Prestamo>> ListarPendientesAsync()
    {
        const string sql = @"
            SELECT p.PrestamoId, p.SocioId, p.FechaPrestamo, p.FechaLimite, p.Estado,
                   s.Nombre AS NombreSocio,
                   d.LibroId, d.FechaDevolucion, l.Titulo, l.ISBN
            FROM Prestamos p
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            WHERE p.Estado = @Estado AND d.FechaDevolucion IS NULL
            ORDER BY p.PrestamoId, l.Titulo";

        List<Prestamo> prestamos = new();
        Dictionary<int, Prestamo> indice = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@Estado", EstadoPrestamo.Pendiente));

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int prestamoId = reader.GetInt32(0);
            if (!indice.TryGetValue(prestamoId, out Prestamo? prestamo))
            {
                prestamo = new Prestamo
                {
                    PrestamoId = prestamoId,
                    SocioId = reader.GetInt32(1),
                    FechaPrestamo = reader.GetDateTime(2),
                    FechaLimite = reader.GetDateTime(3),
                    Estado = reader.GetString(4),
                    NombreSocio = reader.GetString(5)
                };
                indice[prestamoId] = prestamo;
                prestamos.Add(prestamo);
            }

            prestamo.Detalles.Add(new DetallePrestamo
            {
                PrestamoId = prestamoId,
                LibroId = reader.GetInt32(6),
                FechaDevolucion = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                Titulo = reader.GetString(8),
                ISBN = reader.GetString(9),
                NombreSocio = prestamo.NombreSocio,
                FechaLimite = prestamo.FechaLimite,
                Estado = prestamo.Estado
            });
        }

        return prestamos;
    }

    public async Task<int> ContarLibrosPendientesAsync(int socioId)
    {
        const string sql = "SELECT dbo.fn_LibrosPendientesSocio(@SocioId)";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", socioId));

        await cn.OpenAsync();
        object? resultado = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(resultado);
    }

    public async Task<List<int>> ListarLibrosPendientesAsync(int socioId)
    {
        const string sql = @"
            SELECT d.LibroId
            FROM DetallePrestamo d
            INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
            WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL";

        List<int> libros = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", socioId));

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            libros.Add(reader.GetInt32(0));
        }

        return libros;
    }

    public async Task<DateTime?> ObtenerFechaLimiteAsync(int prestamoId)
    {
        const string sql = "SELECT FechaLimite FROM Prestamos WHERE PrestamoId = @PrestamoId";

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@PrestamoId", prestamoId));

        await cn.OpenAsync();
        object? valor = await cmd.ExecuteScalarAsync();
        return valor is null || valor is DBNull ? null : Convert.ToDateTime(valor);
    }

    public async Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion)
    {
        await using SqlConnection cn = new(Conexion.Cadena);
        await cn.OpenAsync();
        await using SqlTransaction tx = (SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            await using (SqlCommand cmdDevolver = new(
                @"UPDATE DetallePrestamo
                  SET FechaDevolucion = @Fecha
                  WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL",
                cn, tx))
            {
                cmdDevolver.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.Date) { Value = fechaDevolucion });
                cmdDevolver.Parameters.Add(new SqlParameter("@PrestamoId", prestamoId));
                cmdDevolver.Parameters.Add(new SqlParameter("@LibroId", libroId));

                if (await cmdDevolver.ExecuteNonQueryAsync() == 0)
                {
                    throw new InvalidOperationException("El libro no pertenece al préstamo o ya fue devuelto.");
                }
            }

            await using SqlCommand cmdEstado = new("usp_ActualizarEstadoPrestamo", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmdEstado.Parameters.Add(new SqlParameter("@PrestamoId", prestamoId));
            cmdEstado.Parameters.Add(new SqlParameter("@LibroId", libroId));
            cmdEstado.Parameters.Add(new SqlParameter("@FechaDevolucion", SqlDbType.Date) { Value = fechaDevolucion });
            await cmdEstado.ExecuteNonQueryAsync();

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PrestamoReporte>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT p.PrestamoId, s.Nombre AS Socio, s.DNI, l.Titulo AS Libro, l.ISBN,
                   p.FechaPrestamo, p.FechaLimite, d.FechaDevolucion, p.Estado
            FROM Prestamos p
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            WHERE p.FechaPrestamo BETWEEN @Desde AND @Hasta
            ORDER BY p.FechaPrestamo, s.Nombre, l.Titulo";

        List<PrestamoReporte> reporte = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@Desde", SqlDbType.Date) { Value = desde });
        cmd.Parameters.Add(new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta });

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reporte.Add(new PrestamoReporte
            {
                PrestamoId = reader.GetInt32(0),
                Socio = reader.GetString(1),
                DNI = reader.GetString(2),
                Libro = reader.GetString(3),
                ISBN = reader.GetString(4),
                FechaPrestamo = reader.GetDateTime(5),
                FechaLimite = reader.GetDateTime(6),
                FechaDevolucion = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                Estado = reader.GetString(8)
            });
        }

        return reporte;
    }

    private static DataTable ConstruirTablaDetalle(Prestamo prestamo)
    {
        DataTable tabla = new();
        tabla.Columns.Add("LibroId", typeof(int));
        tabla.Columns.Add("FechaDevolucion", typeof(DateTime)).AllowDBNull = true;

        foreach (DetallePrestamo detalle in prestamo.Detalles)
        {
            tabla.Rows.Add(detalle.LibroId, DBNull.Value);
        }

        return tabla;
    }
}

