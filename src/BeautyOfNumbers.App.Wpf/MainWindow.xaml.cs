using System.Windows;
using BeautyOfNumbers.App.Wpf.ViewModels;

namespace BeautyOfNumbers.App.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
