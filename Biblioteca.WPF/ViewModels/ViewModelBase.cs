using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool _ocupado;
    private string _mensaje = string.Empty;
    private bool _esError;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Ocupado
    {
        get => _ocupado;
        private set
        {
            if (SetProperty(ref _ocupado, value))
            {
                OnPropertyChanged(nameof(NoOcupado));
            }
        }
    }

    public bool NoOcupado => !Ocupado;

    public string Mensaje
    {
        get => _mensaje;
        private set => SetProperty(ref _mensaje, value);
    }

    public bool EsError
    {
        get => _esError;
        private set => SetProperty(ref _esError, value);
    }

    protected void Informar(string mensaje)
    {
        EsError = false;
        Mensaje = mensaje;
    }

    protected void Advertir(string mensaje)
    {
        EsError = true;
        Mensaje = mensaje;
    }

    protected async Task EjecutarAsync(Func<Task> operacion)
    {
        if (Ocupado)
        {
            return;
        }

        Ocupado = true;
        try
        {
            await operacion();
        }
        catch (ReglaNegocioException ex)
        {
            Advertir(ex.Message);
        }
        catch (Exception ex)
        {
            Advertir($"Ocurrió un error: {ex.Message}");
        }
        finally
        {
            Ocupado = false;
        }
    }

    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nombre = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return false;
        }

        campo = valor;
        OnPropertyChanged(nombre);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
