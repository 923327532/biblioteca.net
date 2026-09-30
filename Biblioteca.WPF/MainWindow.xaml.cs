using System.Windows;

namespace Biblioteca.WPF;

public partial class MainWindow : Window
{
    private readonly ViewModels.MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = (ViewModels.MainViewModel)DataContext;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.IniciarAsync();
    }
}