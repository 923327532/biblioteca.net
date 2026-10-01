using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.ViewModels;

public class LibroViewModel : ViewModelBase
{
    private readonly LibroNegocio _libroNegocio;

    public LibroViewModel(LibroNegocio libroNegocio)
    {
        _libroNegocio = libroNegocio;

        NuevoCommand = new RelayCommand(Nuevo);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, () => !Ocupado);
        EliminarCommand = new AsyncRelayCommand(EliminarAsync, () => LibroSeleccionado is not null && !Ocupado);
        BuscarCommand = new AsyncRelayCommand(BuscarAsync, () => !Ocupado);
        ListarCommand = new AsyncRelayCommand(ListarAsync, () => !Ocupado);
    }

    public ObservableCollection<Libro> Libros { get; } = new();

    public ObservableCollection<Autor> Autores { get; } = new();

    public RelayCommand NuevoCommand { get; }

    public AsyncRelayCommand GuardarCommand { get; }

    public AsyncRelayCommand EliminarCommand { get; }

    public AsyncRelayCommand BuscarCommand { get; }

    public AsyncRelayCommand ListarCommand { get; }

    private Libro? _libroSeleccionado;
    public Libro? LibroSeleccionado
    {
        get => _libroSeleccionado;
        set
        {
            if (SetProperty(ref _libroSeleccionado, value))
            {
                EliminarCommand.RaiseCanExecuteChanged();
                if (value is not null)
                {
                    CargarDesdeSeleccion(value);
                }
            }
        }
    }

    private int _libroId;
    public int LibroId
    {
        get => _libroId;
        set => SetProperty(ref _libroId, value);
    }

    private string _titulo = string.Empty;
    public string Titulo
    {
        get => _titulo;
        set => SetProperty(ref _titulo, value);
    }

    private string _isbn = string.Empty;
    public string ISBN
    {
        get => _isbn;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length == 13 && !value.Contains('-'))
                {
                    value = $"{digits[..3]}-{digits[3..]}";
                }
            }
            SetProperty(ref _isbn, value);
        }
    }

    private Autor? _autorSeleccionado;
    public Autor? AutorSeleccionado
    {
        get => _autorSeleccionado;
        set => SetProperty(ref _autorSeleccionado, value);
    }

    private int _ejemplares;
    public int Ejemplares
    {
        get => _ejemplares;
        set => SetProperty(ref _ejemplares, value);
    }

    private string _filtro = string.Empty;
    public string Filtro
    {
        get => _filtro;
        set => SetProperty(ref _filtro, value);
    }

    public async Task IniciarAsync()
    {
        await EjecutarAsync(async () =>
        {
            await CargarAutoresAsync();
            await ListarAsync();
        });
    }

    private void Nuevo()
    {
        LibroSeleccionado = null;
        LibroId = 0;
        Titulo = string.Empty;
        ISBN = string.Empty;
        Ejemplares = 0;
        AutorSeleccionado = null;
        Informar("Complete los datos para registrar un nuevo libro.");
    }

    private async Task GuardarAsync()
    {
        Libro libro = new()
        {
            LibroId = LibroId,
            Titulo = Titulo?.Trim() ?? string.Empty,
            ISBN = ISBN?.Trim() ?? string.Empty,
            AutorId = AutorSeleccionado?.AutorId ?? 0,
            Ejemplares = Ejemplares,
            Activo = true
        };

        await EjecutarAsync(async () =>
        {
            if (libro.LibroId == 0)
            {
                await _libroNegocio.InsertarAsync(libro);
                Informar($"Libro '{libro.Titulo}' registrado correctamente.");
            }
            else
            {
                await _libroNegocio.ActualizarAsync(libro);
                Informar($"Libro '{libro.Titulo}' actualizado correctamente.");
            }

            Nuevo();
            await ListarAsync();
        });
    }

    private async Task EliminarAsync()
    {
        if (LibroSeleccionado is not Libro seleccionado)
        {
            return;
        }

        await EjecutarAsync(async () =>
        {
            await _libroNegocio.EliminarAsync(seleccionado.LibroId);
            Informar($"El libro '{seleccionado.Titulo}' fue dado de baja (eliminación lógica).");
            Nuevo();
            await ListarAsync();
        });
    }

    private async Task BuscarAsync()
    {
        await EjecutarAsync(async () =>
        {
            List<Libro> resultado = await _libroNegocio.BuscarAsync(Filtro);

            Libros.Clear();
            foreach (Libro libro in resultado)
            {
                Libros.Add(libro);
            }

            Informar($"Se encontraron {Libros.Count} libro(s) para el filtro '{Filtro}'.");
        });
    }

    private async Task ListarAsync()
    {
        List<Libro> libros = await _libroNegocio.ListarAsync();

        Libros.Clear();
        foreach (Libro libro in libros)
        {
            Libros.Add(libro);
        }

        Informar($"{Libros.Count} libro(s) activos.");
    }

    private async Task CargarAutoresAsync()
    {
        List<Autor> autores = await _libroNegocio.ListarAutoresAsync();

        Autores.Clear();
        foreach (Autor autor in autores)
        {
            Autores.Add(autor);
        }
    }

    private void CargarDesdeSeleccion(Libro libro)
    {
        LibroId = libro.LibroId;
        Titulo = libro.Titulo;
        ISBN = libro.ISBN;
        Ejemplares = libro.Ejemplares;
        AutorSeleccionado = Autores.FirstOrDefault(a => a.AutorId == libro.AutorId);
    }
}
