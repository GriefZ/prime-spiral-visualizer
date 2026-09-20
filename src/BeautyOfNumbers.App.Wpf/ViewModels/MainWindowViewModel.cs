using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

    private string numberRangeStart = "1";
    private string numberRangeCount = "1000";
    private bool showOnlyPrimes = true;
    private Scene? scene;
    private double pointSize = 5;
    private Color backgroundColor = Colors.White;
    private Color primeColor = Colors.Red;
    private Color nonPrimeColor = Colors.Gray;
    private bool isGenerating;
    private int progress;
    private string validationMessage = string.Empty;
    private bool isValid = true;
    private string progressStage = string.Empty;

    public MainWindowViewModel()
    {
        GenerateCommand = new RelayCommand(GenerateImage, () => IsValid && !IsGenerating);
        SaveCommand = new RelayCommand(SaveImage, () => IsValid && !IsGenerating);

        ValidateInput();
    }

    public ICommand GenerateCommand { get; }

    public ICommand SaveCommand { get; }

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
            scene = value;
            OnPropertyChanged();
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

    private async void GenerateImage()
    {
        if (IsGenerating)
        {
            return;
        }

        try
        {
            IsGenerating = true;
            Progress = 0;
            ProgressStage = "Building...";

            if (!int.TryParse(NumberRangeStart, out int start) || !int.TryParse(NumberRangeCount, out int count))
            {
                return;
            }

            count = Math.Clamp(count, MinNumber, MaxNumber);
            RenderRequest request = BuildRequest(start, count);
            var progressReporter = new Progress<ProgressReport>(OnProgress);

            Scene? builtScene = await Task.Run(() =>
            {
                var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
                var builder = new SceneBuilder(classifier);
                return builder.Build(request, progressReporter);
            });

            Scene = builtScene;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error generating preview: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
            ProgressStage = string.Empty;
        }
    }

    private async void SaveImage()
    {
        if (IsGenerating)
        {
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                DefaultExt = ".png",
                FileName = "spiral.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsGenerating = true;
                Progress = 0;
                ProgressStage = "Building...";

                if (!int.TryParse(NumberRangeStart, out int start) || !int.TryParse(NumberRangeCount, out int count))
                {
                    return;
                }

                count = Math.Clamp(count, MinNumber, MaxNumber);
                RenderRequest request = BuildRequest(start, count);
                var progressReporter = new Progress<ProgressReport>(OnProgress);

                await Task.Run(() =>
                {
                    var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
                    var builder = new SceneBuilder(classifier);
                    Scene exportScene = builder.Build(request, progressReporter);

                    var renderer = new SkiaSceneRenderer();
                    PngExporter.Export(exportScene, request.Style, request.Output, saveFileDialog.FileName, renderer, progressReporter);
                });

                System.Windows.MessageBox.Show($"Image saved to: {saveFileDialog.FileName}", "Success",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving image: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
            ProgressStage = string.Empty;
            Progress = 100;
        }
    }

    private void OnProgress(ProgressReport report)
    {
        ProgressStage = report.Stage == RenderStage.Building ? "Building scene..." : "Encoding PNG...";
        Progress = (int)(report.Fraction * 100);
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