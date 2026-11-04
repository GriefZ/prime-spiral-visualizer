using System.Windows;
using System.Windows.Controls;

namespace BeautyOfNumbers.App.Wpf;

public partial class PresetNameDialog : Window
{
    public PresetNameDialog(string initialName)
    {
        InitializeComponent();
        NameTextBox.Text = initialName;
        Loaded += OnLoaded;
    }

    public string PresetName { get; private set; } = string.Empty;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        string name = NameTextBox.Text.Trim();
        if (name.Length == 0)
        {
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        PresetName = name;
        DialogResult = true;
    }

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        if (ErrorText is not null)
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}
