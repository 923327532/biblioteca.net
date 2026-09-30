using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class SocioNegocio
{
    private readonly SocioDatos _socioDatos;

    public SocioNegocio(SocioDatos? socioDatos = null)
    {
        _socioDatos = socioDatos ?? new SocioDatos();
    }

    public Task<List<Socio>> ListarAsync() => _socioDatos.ListarAsync();

    public Task<List<Socio>> BuscarAsync(string filtro) =>
        _socioDatos.BuscarAsync(filtro?.Trim() ?? string.Empty);

    public async Task<int> InsertarAsync(Socio socio)
    {
        Validar(socio, esNuevo: true);

        if (await _socioDatos.ExisteDniAsync(socio.DNI, socio.SocioId))
        {
            throw new ReglaNegocioException($"El DNI '{socio.DNI}' ya está registrado en otro socio.");
        }

        return await _socioDatos.InsertarAsync(socio);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        if (socio.SocioId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar un socio para actualizar.");
        }

        Validar(socio, esNuevo: false);

        if (await _socioDatos.ExisteDniAsync(socio.DNI, socio.SocioId))
        {
            throw new ReglaNegocioException($"El DNI '{socio.DNI}' ya está registrado en otro socio.");
        }

        await _socioDatos.ActualizarAsync(socio);
    }

    public async Task EliminarAsync(int socioId)
    {
        if (socioId <= 0)
        {
            throw new ReglaNegocioException("Debe seleccionar un socio para eliminar.");
        }

        int pendientes = await _socioDatos.ContarLibrosPendientesAsync(socioId);
        if (pendientes > 0)
        {
            throw new ReglaNegocioException(
                $"No se puede dar de baja el socio: tiene {pendientes} libro(s) pendiente(s) de devolución.");
        }

        await _socioDatos.EliminarLogicoAsync(socioId);
    }

    private static void Validar(Socio socio, bool esNuevo)
    {
        if (socio is null)
        {
            throw new ReglaNegocioException("No se recibió la información del socio.");
        }

        if (string.IsNullOrWhiteSpace(socio.DNI))
        {
            throw new ReglaNegocioException("El DNI del socio es obligatorio.");
        }

        if (socio.DNI.Trim().Length is < 8 or > 15)
        {
            throw new ReglaNegocioException("El DNI debe tener entre 8 y 15 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(socio.Nombre))
        {
            throw new ReglaNegocioException("El nombre del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(socio.Email))
        {
            throw new ReglaNegocioException("El email del socio es obligatorio.");
        }

        if (!socio.Email.Contains('@') || !socio.Email.Contains('.'))
        {
            throw new ReglaNegocioException("El email del socio no tiene un formato válido.");
        }

        if (esNuevo)
        {
            socio.Activo = true;
            socio.SocioId = 0;
        }
    }
}
