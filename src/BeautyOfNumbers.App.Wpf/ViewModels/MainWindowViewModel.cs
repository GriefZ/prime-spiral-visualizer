using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BeautyOfNumbers.App.Wpf.Services;
using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Layouts;
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

    private readonly RenderScheduler scheduler = new();

    private string numberRangeStart = "1";
    private string numberRangeCount = "1000";
    private bool showOnlyPrimes = true;
    private Scene? scene;
    private RenderRequest? sceneRequest;
    private double pointSize = 5;
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

    public MainWindowViewModel()
    {
        SaveCommand = new RelayCommand(SaveImage, () => IsValid && !IsGenerating);
        CancelCommand = new RelayCommand(CancelRender, () => IsGenerating);

        scheduler.SceneReady += OnSceneReady;
        scheduler.ProgressChanged += OnProgress;
        scheduler.BusyChanged += OnBusyChanged;
        scheduler.Failed += OnRenderFailed;

        ValidateInput();
        RequestRender();
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public RenderStyle CurrentStyle => BuildStyle();

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

    private void ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(NumberRangeStart))
        {
            ValidationMessage = "Range start is required";
            IsValid = false;
            return;
        }

        if (!int.TryParse(NumberRangeStart, out int start))
        {
            ValidationMessage = "Please enter a valid start number";
            IsValid = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(NumberRangeCount))
        {
            ValidationMessage = "Number count is required";
            IsValid = false;
            return;
        }

        if (!int.TryParse(NumberRangeCount, out int count))
        {
            ValidationMessage = "Please enter a valid number";
            IsValid = false;
            return;
        }

        if (count < MinNumber || count > MaxNumber)
        {
            ValidationMessage = $"Number must be between {MinNumber} and {MaxNumber}";
            IsValid = false;
            return;
        }

        long rangeEnd = (long)start + count - 1;
        if (start >= 1 && rangeEnd > MaxRangeEnd)
        {
            ValidationMessage = $"Range end must not exceed {MaxRangeEnd:N0}";
            IsValid = false;
            return;
        }

        RenderRequest request = BuildRequest(start, count);
        IReadOnlyList<ValidationError> errors = new RequestValidator(SievePrimeClassifier.LimitFor(request.Range)).Validate(request);
        if (errors.Count > 0)
        {
            ValidationMessage = errors[0].Message;
            IsValid = false;
            return;
        }

        ValidationMessage = string.Empty;
        IsValid = true;
    }

    private void RequestRender()
    {
        RenderRequest? request = BuildCurrentRequest();
        if (request is null)
        {
            return;
        }

        scheduler.Schedule(request);
    }

    private RenderRequest? BuildCurrentRequest()
    {
        if (!IsValid)
        {
            return null;
        }

        if (!int.TryParse(NumberRangeStart, out int start) || !int.TryParse(NumberRangeCount, out int count))
        {
            return null;
        }

        return BuildRequest(start, Math.Clamp(count, MinNumber, MaxNumber));
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

        RenderRequest? request = BuildCurrentRequest();
        if (request is null)
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

    private RenderRequest BuildRequest(int start, int count)
    {
        return new RenderRequest(
            new NumberRange(start, count),
            ShowOnlyPrimes,
            ArchimedeanSpiral.LayoutId,
            new LayoutOptions(),
            BuildStyle(),
            OutputOptions.Default);
    }

    private RenderStyle BuildStyle()
    {
        return new RenderStyle(
            ToRgbaColor(backgroundColor),
            [ToRgbaColor(primeColor), ToRgbaColor(nonPrimeColor)],
            new PointSizeFunction(MinPointSize, PointSize, PointSizeCurve.Linear),
            true);
    }

    private static RgbaColor ToRgbaColor(Color color) => new(color.R, color.G, color.B, color.A);
}
