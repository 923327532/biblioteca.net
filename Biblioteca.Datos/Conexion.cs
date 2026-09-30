using System.Configuration;

namespace Biblioteca.Datos;

internal static class Conexion
{
    private const string NombreCadena = "BibliotecaDB";

    private static string? _cadena;

    public static string Cadena =>
        _cadena ??= ConfigurationManager.ConnectionStrings[NombreCadena]?.ConnectionString
                    ?? throw new InvalidOperationException(
                        $"No se encontró la cadena de conexión '{NombreCadena}' en el App.config del proyecto de inicio.");
}
