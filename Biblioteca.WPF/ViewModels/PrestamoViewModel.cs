using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public class PrestamoViewModel : ViewModelBase
{
    private readonly PrestamoNegocio _prestamoNegocio;

    public PrestamoViewModel(PrestamoNegocio prestamoNegocio)
    {
        _prestamoNegocio = prestamoNegocio;

        AgregarLibroCommand = new RelayCommand(AgregarLibro, () => LibroSeleccionado is not null && !Ocupado);
        QuitarLibroCommand = new RelayCommand(QuitarLibro, () => CarritoSeleccionado is not null && !Ocupado);
        RegistrarCommand = new AsyncRelayCommand(RegistrarAsync, () => SocioSeleccionado is not null && Carrito.Count > 0 && !Ocupado);
        LimpiarCommand = new RelayCommand(Limpiar);
    }

    public ObservableCollection<Socio> Socios { get; } = new();

    public ObservableCollection<Libro> Libros { get; } = new();

    public ObservableCollection<Libro> Carrito { get; } = new();

    public ObservableCollection<Prestamo> PrestamosPendientes { get; } = new();

    public RelayCommand AgregarLibroCommand { get; }

    public RelayCommand QuitarLibroCommand { get; }

    public AsyncRelayCommand RegistrarCommand { get; }

    public RelayCommand LimpiarCommand { get; }

    private Socio? _socioSeleccionado;
    public Socio? SocioSeleccionado
    {
        get => _socioSeleccionado;
        set
        {
            if (SetProperty(ref _socioSeleccionado, value))
            {
                RegistrarCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(ResumenSocio));
            }
        }
    }

    private Libro? _libroSeleccionado;
    public Libro? LibroSeleccionado
    {
        get => _libroSeleccionado;
        set
        {
            if (SetProperty(ref _libroSeleccionado, value))
            {
                AgregarLibroCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private Libro? _carritoSeleccionado;
    public Libro? CarritoSeleccionado
    {
        get => _carritoSeleccionado;
        set
        {
            if (SetProperty(ref _carritoSeleccionado, value))
            {
                QuitarLibroCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ResumenSocio => SocioSeleccionado is null
        ? "Ningún socio seleccionado"
        : $"{SocioSeleccionado.Nombre} ({SocioSeleccionado.DNI}) - Libros en carrito: {Carrito.Count} de {PrestamoNegocio.MaxLibrosPendientes}";

    public async Task IniciarAsync()
    {
        await EjecutarAsync(async () =>
        {
            await CargarCombosAsync();
            await CargarPendientesAsync();
        });
    }

    private void AgregarLibro()
    {
        if (LibroSeleccionado is not Libro libro)
        {
            return;
        }

        if (Carrito.Any(l => l.LibroId == libro.LibroId))
        {
            Advertir($"El libro '{libro.Titulo}' ya está agregado al préstamo.");
            return;
        }

        if (Carrito.Count >= PrestamoNegocio.MaxLibrosPendientes)
        {
            Advertir($"Solo se pueden prestar {PrestamoNegocio.MaxLibrosPendientes} libros por operación.");
            return;
        }

        Carrito.Add(libro);
        RegistrarCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(ResumenSocio));
        Informar($"Libro '{libro.Titulo}' agregado. Total: {Carrito.Count}.");
    }

    private void QuitarLibro()
    {
        if (CarritoSeleccionado is Libro libro)
        {
            Carrito.Remove(libro);
            RegistrarCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(ResumenSocio));
            Informar($"Libro '{libro.Titulo}' quitado del préstamo.");
        }
    }

    private void Limpiar()
    {
        Carrito.Clear();
        LibroSeleccionado = null;
        SocioSeleccionado = null;
        RegistrarCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(ResumenSocio));
        Informar("Formulario limpiado.");
    }

    private async Task RegistrarAsync()
    {
        if (SocioSeleccionado is not Socio socio)
        {
            return;
        }

        List<int> libroIds = Carrito.Select(l => l.LibroId).ToList();

        await EjecutarAsync(async () =>
        {
            int prestamoId = await _prestamoNegocio.RegistrarAsync(socio.SocioId, libroIds);
            Informar($"Préstamo N° {prestamoId} registrado para {socio.Nombre} con {libroIds.Count} libro(s).");

            Carrito.Clear();
            await CargarCombosAsync();
            await CargarPendientesAsync();
            OnPropertyChanged(nameof(ResumenSocio));
        });
    }

    private async Task CargarCombosAsync()
    {
        List<Socio> socios = await _prestamoNegocio.ListarSociosAsync();
        Socios.Clear();
        foreach (Socio socio in socios)
        {
            Socios.Add(socio);
        }

        List<Libro> libros = await _prestamoNegocio.ListarLibrosAsync();
        Libros.Clear();
        foreach (Libro libro in libros)
        {
            Libros.Add(libro);
        }
    }

    private async Task CargarPendientesAsync()
    {
        List<Prestamo> pendientes = await _prestamoNegocio.ListarPendientesAsync();
        PrestamosPendientes.Clear();
        foreach (Prestamo prestamo in pendientes)
        {
            PrestamosPendientes.Add(prestamo);
        }
    }
}
