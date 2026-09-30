using System.Windows.Input;

namespace Biblioteca.WPF.ViewModels;

public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _ejecutar;
    private readonly Func<bool>? _puedeEjecutar;
    private bool _enEjecucion;

    public AsyncRelayCommand(Func<Task> ejecutar, Func<bool>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        !_enEjecucion && (_puedeEjecutar?.Invoke() ?? true);

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
            await _ejecutar();
        }
        finally
        {
            _enEjecucion = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return;
        }

        _enEjecucion = true;
        RaiseCanExecuteChanged();

        try
        {
            await _ejecutar();
        }
        finally
        {
            _enEjecucion = false;
            RaiseCanExecuteChanged();
        }
    }
}
