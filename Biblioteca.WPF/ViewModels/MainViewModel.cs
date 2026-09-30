using Biblioteca.Datos;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _moduloActual = null!;

    public MainViewModel()
    {
        LibroNegocio libroNegocio = new(new LibroDatos());
        SocioNegocio socioNegocio = new(new SocioDatos());
        PrestamoNegocio prestamoNegocio = new(new PrestamoDatos());

        Libros = new LibroViewModel(libroNegocio);
        Socios = new SocioViewModel(socioNegocio);
        Prestamos = new PrestamoViewModel(prestamoNegocio);
        Devoluciones = new DevolucionViewModel(prestamoNegocio);
        Reportes = new ReporteViewModel(prestamoNegocio);

        MostrarLibrosCommand = new AsyncRelayCommand(() => MostrarAsync(Libros));
        MostrarSociosCommand = new AsyncRelayCommand(() => MostrarAsync(Socios));
        MostrarPrestamosCommand = new AsyncRelayCommand(() => MostrarAsync(Prestamos));
        MostrarDevolucionesCommand = new AsyncRelayCommand(() => MostrarAsync(Devoluciones));
        MostrarReportesCommand = new AsyncRelayCommand(() => MostrarAsync(Reportes));

        ModuloActual = Libros;
    }

    public LibroViewModel Libros { get; }

    public SocioViewModel Socios { get; }

    public PrestamoViewModel Prestamos { get; }

    public DevolucionViewModel Devoluciones { get; }

    public ReporteViewModel Reportes { get; }

    public AsyncRelayCommand MostrarLibrosCommand { get; }

    public AsyncRelayCommand MostrarSociosCommand { get; }

    public AsyncRelayCommand MostrarPrestamosCommand { get; }

    public AsyncRelayCommand MostrarDevolucionesCommand { get; }

    public AsyncRelayCommand MostrarReportesCommand { get; }

    public ViewModelBase ModuloActual
    {
        get => _moduloActual;
        set => SetProperty(ref _moduloActual, value);
    }

    public async Task IniciarAsync()
    {
        await EjecutarAsync(async () =>
        {
            await Libros.IniciarAsync();
            await Socios.IniciarAsync();
            await Prestamos.IniciarAsync();
            await Devoluciones.IniciarAsync();
            await Reportes.IniciarAsync();
        });
    }

    private async Task MostrarAsync(ViewModelBase modulo)
    {
        ModuloActual = modulo;

        switch (modulo)
        {
            case LibroViewModel libros:
                await libros.ListarCommand.ExecuteAsync();
                break;
            case SocioViewModel socios:
                await socios.ListarCommand.ExecuteAsync();
                break;
            case PrestamoViewModel prestamos:
                await prestamos.IniciarAsync();
                break;
            case DevolucionViewModel devoluciones:
                await devoluciones.IniciarAsync();
                break;
            case ReporteViewModel reportes:
                await reportes.IniciarAsync();
                break;
        }
    }
}
