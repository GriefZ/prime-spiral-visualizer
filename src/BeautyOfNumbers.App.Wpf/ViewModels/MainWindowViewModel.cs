using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BeautyOfNumbers.App.Wpf.Services;
using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Presets;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Core.Validation;
using BeautyOfNumbers.Rendering;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace BeautyOfNumbers.App.Wpf.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private const int MinNumber = 10;
    private const int MaxNumber = 100000;
    private const int MaxRangeEnd = 100_000_000;
    private const double MinPointSize = 0.5;
    private const double MinSliderPointSize = 1;
    private const double MaxSliderPointSize = 10;
    private const string BaseWindowTitle = "Prime Spiral Visualizer";
    private const string DefaultPresetName = "My preset";

    private readonly RenderScheduler scheduler = new();
    private readonly PresetStore presetStore = new();

    private string numberRangeStart = "1";
    private string numberRangeCount = "1000";
    private string exportWidth = "2000";
    private string exportHeight = "2000";
    private string exportDpi = "300";
    private bool showOnlyPrimes = true;
    private Scene? scene;
    private RenderRequest? sceneRequest;
    private double pointSize = 5;
    private double pointSizeMin = MinPointSize;
    private PointSizeCurve pointSizeCurve = PointSizeCurve.Linear;
    private bool antialias = true;
    private string selectedLayoutId = ArchimedeanSpiral.LayoutId;
    private LayoutOptions layoutOptions = new();
    private Color backgroundColor = Colors.White;
    private Color primeColor = Colors.Red;
    private Color nonPrimeColor = Colors.Gray;
    private bool isGenerating;
    private bool isSaving;
    private CancellationTokenSource? saveCancellation;
    private int progress;
    private string validationMessage = string.Empty;
    private bool isValid = true;
    private string progressStage = string.Empty;
    private string currentPresetName = string.Empty;
    private bool hasPresets;

    public MainWindowViewModel()
    {
        SaveCommand = new RelayCommand(SaveImage, () => IsValid && !IsGenerating);
        CancelCommand = new RelayCommand(CancelRender, () => IsGenerating);
        SavePresetCommand = new RelayCommand(SavePreset, () => IsValid && !IsGenerating);
        RefreshPresetsCommand = new RelayCommand(() => RefreshPresets(showErrors: true));

        scheduler.SceneReady += OnSceneReady;
        scheduler.ProgressChanged += OnProgress;
        scheduler.BusyChanged += OnBusyChanged;
        scheduler.Failed += OnRenderFailed;

        RefreshPresets(showErrors: false);
        ValidateInput();
        RequestRender();
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand SavePresetCommand { get; }

    public ICommand RefreshPresetsCommand { get; }

    public ObservableCollection<PresetListItemViewModel> Presets { get; } = [];

    public RenderStyle CurrentStyle => BuildStyle();

    public bool HasPresets
    {
        get => hasPresets;
        private set
        {
            if (hasPresets != value)
            {
                hasPresets = value;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentPresetName
    {
        get => currentPresetName;
        private set
        {
            if (currentPresetName != value)
            {
                currentPresetName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string WindowTitle => string.IsNullOrEmpty(CurrentPresetName)
        ? BaseWindowTitle
        : $"{BaseWindowTitle} — {CurrentPresetName}";

    public bool IsGenerating
    {
        get => isGenerating;
        private set
        {
            if (isGenerating != value)
            {
                isGenerating = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int Progress
    {
        get => progress;
        private set
        {
            if (progress != value)
            {
                progress = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProgressStage
    {
        get => progressStage;
        private set
        {
            if (progressStage != value)
            {
                progressStage = value;
                OnPropertyChanged();
            }
        }
    }

    public string NumberRangeStart
    {
        get => numberRangeStart;
        set
        {
            if (numberRangeStart != value)
            {
                numberRangeStart = value;
                ValidateInput();
                OnPropertyChanged();
                RequestRender();
            }
        }
    }

    public string NumberRangeCount
    {
        get => numberRangeCount;
        set
        {
            if (numberRangeCount != value)
            {
                numberRangeCount = value;
                ValidateInput();
                OnPropertyChanged();
                RequestRender();
            }
        }
    }

    public string ExportWidth
    {
        get => exportWidth;
        set
        {
            if (exportWidth != value)
            {
                exportWidth = value;
                ValidateInput();
                OnPropertyChanged();
            }
        }
    }

    public string ExportHeight
    {
        get => exportHeight;
        set
        {
            if (exportHeight != value)
            {
                exportHeight = value;
                ValidateInput();
                OnPropertyChanged();
            }
        }
    }

    public string ExportDpi
    {
        get => exportDpi;
        set
        {
            if (exportDpi != value)
            {
                exportDpi = value;
                ValidateInput();
                OnPropertyChanged();
            }
        }
    }

    public string ValidationMessage
    {
        get => validationMessage;
        private set
        {
            if (validationMessage != value)
            {
                validationMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsValid
    {
        get => isValid;
        private set
        {
            if (isValid != value)
            {
                isValid = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool ShowOnlyPrimes
    {
        get => showOnlyPrimes;
        set
        {
            if (showOnlyPrimes != value)
            {
                showOnlyPrimes = value;
                OnPropertyChanged();
                RequestRender();
            }
        }
    }

    public double PointSize
    {
        get => pointSize;
        set
        {
            if (pointSize != value)
            {
                pointSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentStyle));
                RequestRender();
            }
        }
    }

    public Color BackgroundColor
    {
        get => backgroundColor;
        set
        {
            if (backgroundColor != value)
            {
                backgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentStyle));
            }
        }
    }

    public Color PrimeColor
    {
        get => primeColor;
        set
        {
            if (primeColor != value)
            {
                primeColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentStyle));
            }
        }
    }

    public Color NonPrimeColor
    {
        get => nonPrimeColor;
        set
        {
            if (nonPrimeColor != value)
            {
                nonPrimeColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentStyle));
            }
        }
    }

    public Scene? Scene
    {
        get => scene;
        private set
        {
            if (!ReferenceEquals(scene, value))
            {
                scene = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal void LoadPreset(PresetEntry entry)
    {
        if (!string.Equals(entry.Preset.LayoutId, ArchimedeanSpiral.LayoutId, StringComparison.Ordinal))
        {
            ShowPresetMessage(
                "This preset uses a layout that is not supported yet.",
                "Preset not loaded",
                MessageBoxImage.Warning);
            return;
        }

        NumberRange range = entry.Preset.Range;
        long rangeEnd = (long)range.Start + range.Count - 1;
        if (range.Count < MinNumber || range.Count > MaxNumber || rangeEnd > MaxRangeEnd)
        {
            ShowPresetMessage(
                $"This preset's number range is outside the current limits ({MinNumber}..{MaxNumber}, end up to {MaxRangeEnd:N0}).",
                "Preset not loaded",
                MessageBoxImage.Warning);
            return;
        }

        if (entry.Preset.Style.Palette is null)
        {
            ShowPresetMessage("This preset has no palette.", "Preset not loaded", MessageBoxImage.Warning);
            return;
        }

        if (entry.Preset.Style.PointSize is null)
        {
            ShowPresetMessage("This preset has no point size settings.", "Preset not loaded", MessageBoxImage.Warning);
            return;
        }

        var request = new RenderRequest(
            range,
            entry.Preset.ShowOnlyPrimes,
            entry.Preset.LayoutId,
            entry.Preset.Layout,
            entry.Preset.Style,
            entry.Preset.Output);
        if (!TryValidate(request, out string? error))
        {
            ShowPresetMessage(error ?? "This preset is not valid.", "Preset not loaded", MessageBoxImage.Warning);
            return;
        }

        ApplyPreset(entry.Preset);
        CurrentPresetName = entry.DisplayName;
    }

    internal void DeletePreset(PresetEntry entry)
    {
        MessageBoxResult confirmation = MessageBox.Show(
            $"Delete preset \"{entry.DisplayName}\"?",
            "Delete preset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        bool deleted = false;
        try
        {
            presetStore.Delete(entry);
            deleted = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowPresetMessage($"Could not delete preset: {ex.Message}", "Error", MessageBoxImage.Error);
        }

        RefreshPresets(showErrors: false);
        if (deleted && string.Equals(CurrentPresetName, entry.DisplayName, StringComparison.Ordinal))
        {
            CurrentPresetName = string.Empty;
        }
    }

    private void ValidateInput()
    {
        if (TryBuildRequest(out _, out string? error))
        {
            ValidationMessage = string.Empty;
            IsValid = true;
            return;
        }

        ValidationMessage = error ?? "Invalid input";
        IsValid = false;
    }

    private void RequestRender()
    {
        if (!TryParseRange(out int start, out int count, out _))
        {
            return;
        }

        RenderRequest request = CreateRequest(start, count, OutputOptions.Default);
        if (!TryValidate(request, out _))
        {
            return;
        }

        scheduler.Schedule(request);
    }

    private bool TryBuildRequest(
        [NotNullWhen(true)] out RenderRequest? request,
        out string? error)
    {
        request = null;

        if (!TryParseRange(out int start, out int count, out error))
        {
            return false;
        }

        if (!TryBuildOutput(out OutputOptions? output, out error))
        {
            return false;
        }

        RenderRequest candidate = CreateRequest(start, count, output);
        if (!TryValidate(candidate, out error))
        {
            return false;
        }

        request = candidate;
        return true;
    }

    private bool TryParseRange(out int start, out int count, out string? error)
    {
        start = 0;
        count = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(NumberRangeStart))
        {
            error = "Range start is required";
            return false;
        }

        if (!int.TryParse(NumberRangeStart, out start))
        {
            error = "Please enter a valid start number";
            return false;
        }

        if (string.IsNullOrWhiteSpace(NumberRangeCount))
        {
            error = "Number count is required";
            return false;
        }

        if (!int.TryParse(NumberRangeCount, out count))
        {
            error = "Please enter a valid number";
            return false;
        }

        if (count < MinNumber || count > MaxNumber)
        {
            error = $"Number must be between {MinNumber} and {MaxNumber}";
            return false;
        }

        long rangeEnd = (long)start + count - 1;
        if (start >= 1 && rangeEnd > MaxRangeEnd)
        {
            error = $"Range end must not exceed {MaxRangeEnd:N0}";
            return false;
        }

        return true;
    }

    private RenderRequest CreateRequest(int start, int count, OutputOptions output)
    {
        return new RenderRequest(
            new NumberRange(start, count),
            ShowOnlyPrimes,
            selectedLayoutId,
            layoutOptions,
            BuildStyle(),
            output);
    }

    private static bool TryValidate(RenderRequest request, out string? error)
    {
        error = null;
        IReadOnlyList<ValidationError> errors = new RequestValidator(SievePrimeClassifier.LimitFor(request.Range)).Validate(request);
        if (errors.Count == 0)
        {
            return true;
        }

        error = errors[0].Message;
        return false;
    }

    private bool TryBuildOutput([NotNullWhen(true)] out OutputOptions? output, out string? error)
    {
        output = null;
        error = null;

        if (string.IsNullOrWhiteSpace(ExportWidth))
        {
            error = "Export width is required";
            return false;
        }

        if (!int.TryParse(ExportWidth, NumberStyles.Integer, CultureInfo.InvariantCulture, out int width))
        {
            error = "Please enter a valid export width";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ExportHeight))
        {
            error = "Export height is required";
            return false;
        }

        if (!int.TryParse(ExportHeight, NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
        {
            error = "Please enter a valid export height";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ExportDpi))
        {
            error = "Export DPI is required";
            return false;
        }

        if (!double.TryParse(ExportDpi, NumberStyles.Float, CultureInfo.InvariantCulture, out double dpi)
            || !double.IsFinite(dpi))
        {
            error = "Please enter a valid export DPI";
            return false;
        }

        output = new OutputOptions(width, height, dpi);
        return true;
    }

    private void RefreshPresets(bool showErrors)
    {
        PresetScanResult result = presetStore.LoadAll();

        Presets.Clear();
        foreach (PresetEntry entry in result.Entries)
        {
            Presets.Add(new PresetListItemViewModel(entry, this));
        }

        HasPresets = Presets.Count > 0;

        if (showErrors && result.Errors.Count > 0)
        {
            ShowPresetLoadErrors(result.Errors);
        }
    }

    private void SavePreset()
    {
        if (!IsValid || IsGenerating)
        {
            return;
        }

        var dialog = new PresetNameDialog(
            string.IsNullOrWhiteSpace(CurrentPresetName) ? DefaultPresetName : CurrentPresetName);
        if (Application.Current?.MainWindow is Window owner)
        {
            dialog.Owner = owner;
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        string name = dialog.PresetName;
        string path = Path.Combine(presetStore.DirectoryPath, PresetStore.FileNameFor(name));
        if (File.Exists(path))
        {
            MessageBoxResult overwrite = MessageBox.Show(
                $"A preset named \"{name}\" already exists. Overwrite it?",
                "Overwrite preset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (overwrite != MessageBoxResult.Yes)
            {
                return;
            }
        }

        if (!TryBuildRequest(out RenderRequest? request, out _))
        {
            return;
        }

        var preset = new Preset(
            PresetJson.CurrentSchemaVersion,
            name,
            request.Range,
            ShowOnlyPrimes,
            selectedLayoutId,
            layoutOptions,
            BuildStyle(),
            request.Output);

        try
        {
            presetStore.Save(preset);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            ShowPresetMessage($"Could not save preset: {ex.Message}", "Error", MessageBoxImage.Error);
            return;
        }

        RefreshPresets(showErrors: false);
        CurrentPresetName = name;
    }

    private void ApplyPreset(Preset preset)
    {
        numberRangeStart = preset.Range.Start.ToString(CultureInfo.InvariantCulture);
        numberRangeCount = preset.Range.Count.ToString(CultureInfo.InvariantCulture);
        showOnlyPrimes = preset.ShowOnlyPrimes;
        exportWidth = preset.Output.Width.ToString(CultureInfo.InvariantCulture);
        exportHeight = preset.Output.Height.ToString(CultureInfo.InvariantCulture);
        exportDpi = preset.Output.Dpi.ToString(CultureInfo.InvariantCulture);
        selectedLayoutId = preset.LayoutId;
        layoutOptions = preset.Layout;
        backgroundColor = ToColor(preset.Style.Background);
        primeColor = ToColor(preset.Style.Palette[0]);
        nonPrimeColor = ToColor(preset.Style.Palette.Length > 1 ? preset.Style.Palette[1] : preset.Style.Palette[0]);
        pointSize = Math.Clamp(preset.Style.PointSize.Max, MinSliderPointSize, MaxSliderPointSize);
        pointSizeMin = preset.Style.PointSize.Min;
        pointSizeCurve = preset.Style.PointSize.Curve;
        antialias = preset.Style.Antialias;

        OnPropertyChanged(nameof(NumberRangeStart));
        OnPropertyChanged(nameof(NumberRangeCount));
        OnPropertyChanged(nameof(ShowOnlyPrimes));
        OnPropertyChanged(nameof(ExportWidth));
        OnPropertyChanged(nameof(ExportHeight));
        OnPropertyChanged(nameof(ExportDpi));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(PrimeColor));
        OnPropertyChanged(nameof(NonPrimeColor));
        OnPropertyChanged(nameof(PointSize));
        OnPropertyChanged(nameof(CurrentStyle));

        ValidateInput();
        RequestRender();
    }

    private void CancelRender()
    {
        scheduler.Cancel();
        saveCancellation?.Cancel();
    }

    private void OnSceneReady(object? sender, SceneReadyEventArgs e)
    {
        sceneRequest = e.Request;
        Scene = e.Scene;
    }

    private void OnProgress(object? sender, ProgressReport report)
    {
        ApplyProgress(report);
    }

    private void OnBusyChanged(object? sender, bool isBusy)
    {
        if (isBusy)
        {
            Progress = 0;
            ProgressStage = "Building...";
        }

        UpdateIsGenerating();
    }

    private void OnRenderFailed(object? sender, Exception exception)
    {
        System.Windows.MessageBox.Show($"Error generating preview: {exception.Message}", "Error",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    private void UpdateIsGenerating()
    {
        IsGenerating = isSaving || scheduler.IsBusy;
    }

    private void ApplyProgress(ProgressReport report)
    {
        ProgressStage = report.Stage == RenderStage.Building ? "Building scene..." : "Encoding PNG...";
        Progress = (int)(report.Fraction * 100);
    }

    private async void SaveImage()
    {
        if (IsGenerating)
        {
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            DefaultExt = ".png",
            FileName = "spiral.png"
        };

        if (saveFileDialog.ShowDialog() != true)
        {
            return;
        }

        if (!TryBuildRequest(out RenderRequest? request, out _))
        {
            return;
        }

        string fileName = saveFileDialog.FileName;
        var cancellation = new CancellationTokenSource();
        saveCancellation = cancellation;
        isSaving = true;
        UpdateIsGenerating();
        bool fileWritten = false;

        try
        {
            Progress = 0;
            ProgressStage = "Building...";

            var progressReporter = new Progress<ProgressReport>(ApplyProgress);
            Scene exportScene;
            if (Scene is not null && sceneRequest is not null && sceneRequest.SceneKey == request.SceneKey)
            {
                exportScene = Scene;
            }
            else
            {
                Scene? previousScene = Scene;
                scheduler.Cancel();
                exportScene = await RenderScheduler.BuildAsync(request, progressReporter, cancellation.Token);

                // A newer live delivery during the build wins over the exported scene.
                if (ReferenceEquals(Scene, previousScene))
                {
                    sceneRequest = request;
                    Scene = exportScene;
                }
            }

            await Task.Run(
                () =>
                {
                    PngExporter.Export(
                        exportScene,
                        request.Style,
                        request.Output,
                        fileName,
                        new SkiaSceneRenderer(),
                        progressReporter,
                        cancellation.Token);
                    fileWritten = true;
                },
                cancellation.Token);

            cancellation.Token.ThrowIfCancellationRequested();

            System.Windows.MessageBox.Show($"Image saved to: {fileName}", "Success",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            DeleteCanceledExport(fileName, fileWritten);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving image: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            cancellation.Dispose();
            saveCancellation = null;
            isSaving = false;
            UpdateIsGenerating();
            ProgressStage = string.Empty;
            Progress = 0;
        }
    }

    private static void DeleteCanceledExport(string fileName, bool fileWritten)
    {
        if (!fileWritten)
        {
            return;
        }

        try
        {
            File.Delete(fileName);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void ShowPresetLoadErrors(IReadOnlyList<PresetLoadError> errors)
    {
        List<string> lines = [];
        int shown = 0;
        foreach (PresetLoadError error in errors)
        {
            if (shown == 5)
            {
                break;
            }

            lines.Add($"{Path.GetFileName(error.FilePath)}: {error.Message}");
            shown++;
        }

        if (errors.Count > shown)
        {
            lines.Add($"...and {errors.Count - shown} more");
        }

        MessageBox.Show(
            "Some presets could not be loaded:" + Environment.NewLine + string.Join(Environment.NewLine, lines),
            "Presets",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static void ShowPresetMessage(string message, string title, MessageBoxImage image)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, image);
    }

    private RenderStyle BuildStyle()
    {
        return new RenderStyle(
            ToRgbaColor(backgroundColor),
            [ToRgbaColor(primeColor), ToRgbaColor(nonPrimeColor)],
            new PointSizeFunction(pointSizeMin, PointSize, pointSizeCurve),
            antialias);
    }

    private static Color ToColor(RgbaColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    private static RgbaColor ToRgbaColor(Color color) => new(color.R, color.G, color.B, color.A);
}
