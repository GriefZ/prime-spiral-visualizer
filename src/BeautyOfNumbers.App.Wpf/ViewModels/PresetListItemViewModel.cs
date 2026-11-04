using System.Windows.Input;
using BeautyOfNumbers.App.Wpf.Services;

namespace BeautyOfNumbers.App.Wpf.ViewModels;

public sealed class PresetListItemViewModel
{
    internal PresetListItemViewModel(PresetEntry entry, MainWindowViewModel owner)
    {
        DisplayName = entry.DisplayName;
        LoadCommand = new RelayCommand(() => owner.LoadPreset(entry));
        DeleteCommand = new RelayCommand(() => owner.DeletePreset(entry));
    }

    public string DisplayName { get; }

    public ICommand LoadCommand { get; }

    public ICommand DeleteCommand { get; }
}
