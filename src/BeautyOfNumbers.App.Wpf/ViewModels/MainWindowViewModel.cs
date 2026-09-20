using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
using ScaleTransform = System.Windows.Media.ScaleTransform;

namespace BeautyOfNumbers.App.Wpf.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private const string TempImagePath = "temp_preview.png";
    private const double ZoomIncrement = 0.2;
    private const double MinZoom = 0.2;
    private const double MaxZoom = 5.0;
    private const int MinNumber = 10;
    private const int MaxNumber = 100000;
    // M0 practical bound: monolithic sieve and per-point OxyPlot series (docs/03-guides/build-and-run.md).
    private const int MaxRangeEnd = 100_000_000;
    private const double MinPointSize = 0.5;

    private string numberRangeStart = "1";
    private string numberRangeCount = "1000";
    private bool showOnlyPrimes = true;
    private BitmapImage previewImage = new();
    private ScaleTransform imageTransform = new(1.0, 1.0);
    private double pointSize = 5;
    private Color backgroundColor = Colors.White;
    private Color primeColor = Colors.Red;
    private Color nonPrimeColor = Colors.Gray;
    private bool isGenerating;
    private int progress;
    private string validationMessage = string.Empty;
    private bool isValid = true;
    private double zoomLevel = 1.0;

    public MainWindowViewModel()
    {
        GenerateCommand = new RelayCommand(GenerateImage, () => IsValid && !IsGenerating);
        SaveCommand = new RelayCommand(SaveImage, () => IsValid && !IsGenerating);
        ZoomInCommand = new RelayCommand(ZoomIn, CanZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut, CanZoomOut);
        ResetZoomCommand = new RelayCommand(ResetZoom);

        ValidateInput();
    }

    public ICommand GenerateCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand ZoomInCommand { get; }

    public ICommand ZoomOutCommand { get; }

    public ICommand ResetZoomCommand { get; }

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
            }
        }
    }

    public BitmapImage PreviewImage
    {
        get => previewImage;
        private set
        {
            if (previewImage != value)
            {
                previewImage = value;
                OnPropertyChanged();
            }
        }
    }

    public double ZoomLevel
    {
        get => zoomLevel;
        private set
        {
            if (Math.Abs(zoomLevel - value) > 0.001)
            {
                zoomLevel = value;
                OnPropertyChanged();
                UpdateImageTransform();
            }
        }
    }

    public ScaleTransform ImageTransform
    {
        get => imageTransform;
        private set
        {
            imageTransform = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ZoomIn()
    {
        ZoomLevel = Math.Min(MaxZoom, ZoomLevel + ZoomIncrement);
    }

    private void ZoomOut()
    {
        ZoomLevel = Math.Max(MinZoom, ZoomLevel - ZoomIncrement);
    }

    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    private bool CanZoomIn()
    {
        return ZoomLevel < MaxZoom;
    }

    private bool CanZoomOut()
    {
        return ZoomLevel > MinZoom;
    }

    private void UpdateImageTransform()
    {
        ImageTransform = new ScaleTransform(ZoomLevel, ZoomLevel);
        CommandManager.InvalidateRequerySuggested();
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
            await GenerateAndDisplayImage(TempImagePath);
            Progress = 100;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error generating preview: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
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
                await GenerateAndDisplayImage(saveFileDialog.FileName);
                Progress = 100;
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
        }
    }

    private async Task GenerateAndDisplayImage(string filePath)
    {
        if (!int.TryParse(NumberRangeStart, out int start) || !int.TryParse(NumberRangeCount, out int count))
        {
            throw new ArgumentException("Please enter valid numbers");
        }

        count = Math.Clamp(count, MinNumber, MaxNumber);
        RenderRequest request = BuildRequest(start, count);

        Progress = 10;
        await Task.Run(() =>
        {
            var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
            new OxyPlotSpiralRenderer(classifier).Render(request, filePath);
        });
        Progress = 80;
        await Task.Run(() => LoadPreviewImage(filePath));
        Progress = 90;
    }

    private void LoadPreviewImage(string filePath)
    {
        if (System.IO.File.Exists(filePath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.RelativeOrAbsolute);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage = bitmap;
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
