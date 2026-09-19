using System.Windows;
using PrimeSpiralVisualizerUI.ViewModels;

namespace PrimeSpiralVisualizerUI
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}