using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public class DevolucionViewModel : ViewModelBase
{
    private readonly PrestamoNegocio _prestamoNegocio;

    public DevolucionViewModel(PrestamoNegocio prestamoNegocio)
    {
        _prestamoNegocio = prestamoNegocio;

        DevolverCommand = new AsyncRelayCommand(DevolverAsync, () => DetalleSeleccionado is not null && !Ocupado);
        ActualizarCommand = new AsyncRelayCommand(CargarPendientesAsync, () => !Ocupado);
    }

    public ObservableCollection<Prestamo> PrestamosPendientes { get; } = new();

    public ObservableCollection<DetallePrestamo> DetallesPendientes { get; } = new();

    public AsyncRelayCommand DevolverCommand { get; }

    public AsyncRelayCommand ActualizarCommand { get; }

    private Prestamo? _prestamoSeleccionado;
    public Prestamo? PrestamoSeleccionado
    {
        get => _prestamoSeleccionado;
        set
        {
            if (SetProperty(ref _prestamoSeleccionado, value))
            {
                DetallesPendientes.Clear();
                if (value is not null)
                {
                    foreach (DetallePrestamo detalle in value.Detalles)
                    {
                        DetallesPendientes.Add(detalle);
                    }
                }

                DetalleSeleccionado = null;
                RecalcularMulta();
            }
        }
    }

    private DetallePrestamo? _detalleSeleccionado;
    public DetallePrestamo? DetalleSeleccionado
    {
        get => _detalleSeleccionado;
        set
        {
            if (SetProperty(ref _detalleSeleccionado, value))
            {
                DevolverCommand.RaiseCanExecuteChanged();
                RecalcularMulta();
            }
        }
    }

    private DateTime _fechaDevolucion = DateTime.Today;
    public DateTime FechaDevolucion
    {
        get => _fechaDevolucion;
        set
        {
            if (SetProperty(ref _fechaDevolucion, value))
            {
                RecalcularMulta();
            }
        }
    }

    private decimal _multa;
    public decimal Multa
    {
        get => _multa;
        private set
        {
            if (SetProperty(ref _multa, value))
            {
                OnPropertyChanged(nameof(ResumenMulta));
            }
        }
    }

    public string ResumenMulta => Multa > 0
        ? $"Multa por retraso: S/ {Multa:F2}"
        : "Sin multa: la devolución está dentro del plazo.";

    public Task IniciarAsync() => EjecutarAsync(CargarPendientesAsync);

    private async Task CargarPendientesAsync()
    {
        await EjecutarAsync(async () =>
        {
            List<Prestamo> pendientes = await _prestamoNegocio.ListarPendientesAsync();

            PrestamosPendientes.Clear();
            foreach (Prestamo prestamo in pendientes)
            {
                PrestamosPendientes.Add(prestamo);
            }

            DetallesPendientes.Clear();
            Multa = 0;
            Informar($"{PrestamosPendientes.Count} préstamo(s) con libros pendientes.");
        });
    }

    private async Task DevolverAsync()
    {
        if (PrestamoSeleccionado is not Prestamo prestamo || DetalleSeleccionado is not DetallePrestamo detalle)
        {
            return;
        }

        await EjecutarAsync(async () =>
        {
            decimal multa = await _prestamoNegocio.RegistrarDevolucionAsync(
                prestamo.PrestamoId, detalle.LibroId, FechaDevolucion);

            Multa = multa;
            Informar(multa > 0
                ? $"Devolución registrada. Multa calculada: S/ {multa:F2}."
                : "Devolución registrada sin multa.");

            Prestamo? seleccionado = PrestamoSeleccionado;
            await CargarPendientesAsync();
            PrestamoSeleccionado = PrestamosPendientes.FirstOrDefault(p => p.PrestamoId == seleccionado?.PrestamoId);
        });
    }

    private void RecalcularMulta()
    {
        if (DetalleSeleccionado is DetallePrestamo detalle)
        {
            Multa = PrestamoNegocio.CalcularMulta(detalle.FechaLimite, FechaDevolucion);
        }
        else
        {
            Multa = 0;
        }
    }
}
