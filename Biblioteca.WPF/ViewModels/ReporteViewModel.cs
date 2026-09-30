using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public class ReporteViewModel : ViewModelBase
{
    private readonly PrestamoNegocio _prestamoNegocio;

    public ReporteViewModel(PrestamoNegocio prestamoNegocio)
    {
        _prestamoNegocio = prestamoNegocio;

        GenerarCommand = new AsyncRelayCommand(GenerarAsync, () => !Ocupado);
    }

    public ObservableCollection<PrestamoReporte> Resultado { get; } = new();

    public AsyncRelayCommand GenerarCommand { get; }

    private DateTime _desde = DateTime.Today.AddMonths(-1);
    public DateTime Desde
    {
        get => _desde;
        set => SetProperty(ref _desde, value);
    }

    private DateTime _hasta = DateTime.Today;
    public DateTime Hasta
    {
        get => _hasta;
        set => SetProperty(ref _hasta, value);
    }

    private decimal _multaTotal;
    public decimal MultaTotal
    {
        get => _multaTotal;
        private set
        {
            if (SetProperty(ref _multaTotal, value))
            {
                OnPropertyChanged(nameof(ResumenMulta));
            }
        }
    }

    public string ResumenMulta => $"Multa potencial en el rango: S/ {MultaTotal:F2}";

    public Task IniciarAsync() => EjecutarAsync(GenerarAsync);

    private async Task GenerarAsync()
    {
        await EjecutarAsync(async () =>
        {
            List<PrestamoReporte> reporte = await _prestamoNegocio.ReportePorFechasAsync(Desde, Hasta);

            Resultado.Clear();
            decimal multa = 0m;
            foreach (PrestamoReporte fila in reporte)
            {
                Resultado.Add(fila);
                multa += PrestamoNegocio.CalcularMulta(fila.FechaLimite, fila.FechaDevolucion ?? DateTime.Today);
            }

            MultaTotal = multa;
            Informar($"{Resultado.Count} fila(s) encontradas entre {Desde:dd/MM/yyyy} y {Hasta:dd/MM/yyyy}.");
        });
    }
}
