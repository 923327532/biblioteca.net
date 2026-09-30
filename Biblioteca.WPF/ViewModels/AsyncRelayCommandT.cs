using System.Windows.Input;

namespace Biblioteca.WPF.ViewModels;

public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _ejecutar;
    private readonly Func<T?, bool>? _puedeEjecutar;
    private bool _enEjecucion;

    public AsyncRelayCommand(Func<T?, Task> ejecutar, Func<T?, bool>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        !_enEjecucion && (_puedeEjecutar?.Invoke(Convertir(parameter)) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _enEjecucion = true;
        RaiseCanExecuteChanged();

        try
        {
            await _ejecutar(Convertir(parameter));
        }
        finally
        {
            _enEjecucion = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static T? Convertir(object? parameter) =>
        parameter is T valor ? valor : default;
}
