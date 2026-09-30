using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public class SocioViewModel : ViewModelBase
{
    private readonly SocioNegocio _socioNegocio;

    public SocioViewModel(SocioNegocio socioNegocio)
    {
        _socioNegocio = socioNegocio;

        NuevoCommand = new RelayCommand(Nuevo);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, () => !Ocupado);
        EliminarCommand = new AsyncRelayCommand(EliminarAsync, () => SocioSeleccionado is not null && !Ocupado);
        BuscarCommand = new AsyncRelayCommand(BuscarAsync, () => !Ocupado);
        ListarCommand = new AsyncRelayCommand(ListarAsync, () => !Ocupado);
    }

    public ObservableCollection<Socio> Socios { get; } = new();

    public RelayCommand NuevoCommand { get; }

    public AsyncRelayCommand GuardarCommand { get; }

    public AsyncRelayCommand EliminarCommand { get; }

    public AsyncRelayCommand BuscarCommand { get; }

    public AsyncRelayCommand ListarCommand { get; }

    private Socio? _socioSeleccionado;
    public Socio? SocioSeleccionado
    {
        get => _socioSeleccionado;
        set
        {
            if (SetProperty(ref _socioSeleccionado, value))
            {
                EliminarCommand.RaiseCanExecuteChanged();
                if (value is not null)
                {
                    CargarDesdeSeleccion(value);
                }
            }
        }
    }

    private int _socioId;
    public int SocioId
    {
        get => _socioId;
        set => SetProperty(ref _socioId, value);
    }

    private string _dni = string.Empty;
    public string DNI
    {
        get => _dni;
        set => SetProperty(ref _dni, value);
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _filtro = string.Empty;
    public string Filtro
    {
        get => _filtro;
        set => SetProperty(ref _filtro, value);
    }

    public Task IniciarAsync() => EjecutarAsync(ListarAsync);

    private void Nuevo()
    {
        SocioSeleccionado = null;
        SocioId = 0;
        DNI = string.Empty;
        Nombre = string.Empty;
        Email = string.Empty;
        Informar("Complete los datos para registrar un nuevo socio.");
    }

    private async Task GuardarAsync()
    {
        Socio socio = new()
        {
            SocioId = SocioId,
            DNI = DNI,
            Nombre = Nombre,
            Email = Email,
            Activo = true
        };

        await EjecutarAsync(async () =>
        {
            if (socio.SocioId == 0)
            {
                await _socioNegocio.InsertarAsync(socio);
                Informar($"Socio '{socio.Nombre}' registrado correctamente.");
            }
            else
            {
                await _socioNegocio.ActualizarAsync(socio);
                Informar($"Socio '{socio.Nombre}' actualizado correctamente.");
            }

            Nuevo();
            await ListarAsync();
        });
    }

    private async Task EliminarAsync()
    {
        if (SocioSeleccionado is not Socio seleccionado)
        {
            return;
        }

        await EjecutarAsync(async () =>
        {
            await _socioNegocio.EliminarAsync(seleccionado.SocioId);
            Informar($"El socio '{seleccionado.Nombre}' fue dado de baja (eliminación lógica).");
            Nuevo();
            await ListarAsync();
        });
    }

    private async Task BuscarAsync()
    {
        await EjecutarAsync(async () =>
        {
            List<Socio> resultado = await _socioNegocio.BuscarAsync(Filtro);

            Socios.Clear();
            foreach (Socio socio in resultado)
            {
                Socios.Add(socio);
            }

            Informar($"Se encontraron {Socios.Count} socio(s) para el filtro '{Filtro}'.");
        });
    }

    private async Task ListarAsync()
    {
        List<Socio> socios = await _socioNegocio.ListarAsync();

        Socios.Clear();
        foreach (Socio socio in socios)
        {
            Socios.Add(socio);
        }

        Informar($"{Socios.Count} socio(s) activos.");
    }

    private void CargarDesdeSeleccion(Socio socio)
    {
        SocioId = socio.SocioId;
        DNI = socio.DNI;
        Nombre = socio.Nombre;
        Email = socio.Email;
    }
}
