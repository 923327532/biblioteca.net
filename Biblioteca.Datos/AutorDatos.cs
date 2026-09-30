using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class AutorDatos
{
    public async Task<List<Autor>> ListarAsync(bool soloActivos = true)
    {
        string sql = @"SELECT AutorId, Nombre, Nacionalidad, Activo
                       FROM Autores"
                     + (soloActivos ? " WHERE Activo = 1" : string.Empty)
                     + " ORDER BY Nombre";

        List<Autor> autores = new();

        await using SqlConnection cn = new(Conexion.Cadena);
        await using SqlCommand cmd = new(sql, cn);

        await cn.OpenAsync();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            autores.Add(new Autor
            {
                AutorId = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Nacionalidad = reader.GetString(2),
                Activo = reader.GetBoolean(3)
            });
        }

        return autores;
    }
}
