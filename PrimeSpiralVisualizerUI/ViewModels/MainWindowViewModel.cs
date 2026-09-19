using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using SpiralMaker;
using OxyPlot;

namespace PrimeSpiralVisualizerUI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string numberCount = "1000";
        private bool isPrimeNumbersSelected = true;
        private BitmapImage previewImage;
        private double pointSize = 5;
        private System.Windows.Media.Color backgroundColor = System.Windows.Media.Colors.White;
        private System.Windows.Media.Color primeColor = System.Windows.Media.Colors.Red;
        private System.Windows.Media.Color nonPrimeColor = System.Windows.Media.Colors.Gray;
        private const string TempImagePath = "temp_preview.png";
        private bool isGenerating;
        private int progress;
        private string validationMessage;
        private bool isValid = true;
        private double zoomLevel = 1.0;
        private ScaleTransform imageTransform;

        private const double ZoomIncrement = 0.2;
        private const double MinZoom = 0.2;
        private const double MaxZoom = 5.0;

        private const int MinNumber = 10;
        private const int MaxNumber = 100000;

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

        public string NumberCount
        {
            get => numberCount;
            set
            {
                if (numberCount != value)
                {
                    numberCount = value;
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

        public bool IsPrimeNumbersSelected
        {
            get => isPrimeNumbersSelected;
            set
            {
                if (isPrimeNumbersSelected != value)
                {
                    isPrimeNumbersSelected = value;
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

        public System.Windows.Media.Color BackgroundColor
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

        public System.Windows.Media.Color PrimeColor
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

        public System.Windows.Media.Color NonPrimeColor
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

        public ICommand GenerateCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }

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

        public MainWindowViewModel()
        {
            GenerateCommand = new RelayCommand(GenerateImage, () => IsValid && !IsGenerating);
            SaveCommand = new RelayCommand(SaveImage, () => IsValid && !IsGenerating);
            ZoomInCommand = new RelayCommand(ZoomIn, CanZoomIn);
            ZoomOutCommand = new RelayCommand(ZoomOut, CanZoomOut);
            ResetZoomCommand = new RelayCommand(ResetZoom);
            
            imageTransform = new ScaleTransform(zoomLevel, zoomLevel);
            previewImage = new BitmapImage();
            validationMessage = string.Empty;
            PropertyChanged += (s, e) => { };
            ValidateInput();
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
            if (string.IsNullOrWhiteSpace(NumberCount))
            {
                ValidationMessage = "Number count is required";
                IsValid = false;
                return;
            }

            if (!int.TryParse(NumberCount, out int count))
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

            ValidationMessage = string.Empty;
            IsValid = true;
        }

        private async void GenerateImage()
        {
            if (IsGenerating) return;

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
            if (IsGenerating) return;

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
            if (!int.TryParse(NumberCount, out int count))
            {
                throw new ArgumentException("Please enter a valid number for count");
            }

            count = Math.Max(MinNumber, Math.Min(count, MaxNumber));

            Progress = 10;
            List<int> numbers = IsPrimeNumbersSelected ?
                await Task.Run(() => GeneratePrimes(count)) :
                await Task.Run(() => GenerateSequentialNumbers(count));
            Progress = 50;

            await Task.Run(() => PlotUtils.CreatePlot(
                numbers,
                figsize: 20,
                maxPointSize: PointSize,
                showAnnot: false,
                filePath: filePath,
                backgroundColor: OxyColor.FromArgb(
                    backgroundColor.A,
                    backgroundColor.R,
                    backgroundColor.G,
                    backgroundColor.B),
                primeColor: OxyColor.FromArgb(
                    primeColor.A,
                    primeColor.R,
                    primeColor.G,
                    primeColor.B),
                nonPrimeColor: OxyColor.FromArgb(
                    nonPrimeColor.A,
                    nonPrimeColor.R,
                    nonPrimeColor.G,
                    nonPrimeColor.B)));

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

        private List<int> GenerateSequentialNumbers(int count)
        {
            var numbers = new List<int>();
            for (int i = 1; i <= count; i++)
            {
                numbers.Add(i);
            }
            return numbers;
        }

        private List<int> GeneratePrimes(int max)
        {
            List<int> primes = new List<int>();

            if (max < 2)
                return primes;

            primes.Add(2);

            int nextPrime = 3;
            bool isPrime;

            while (primes.Count < max && nextPrime < int.MaxValue - 2)
            {
                int sqrt = (int)Math.Sqrt(nextPrime);
                isPrime = true;

                for (int i = 0; i < primes.Count && primes[i] <= sqrt; i++)
                {
                    if (nextPrime % primes[i] == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }

                if (isPrime)
                {
                    primes.Add(nextPrime);
                }

                nextPrime += 2;
            }

            return primes;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}