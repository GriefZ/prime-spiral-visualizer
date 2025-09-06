using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SpiralMaker;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using RadioButton = System.Windows.Controls.RadioButton;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;

namespace PrimeSpiralVisualizerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TextBox txtNumberCount;
        private RadioButton rbPrimes;
        private RadioButton rbAllNumbers;
        private Image imgPreview;
        private Button btnGenerate;
        private Button btnSave;
        
        private const string TempImagePath = "temp_preview.png";

        public MainWindow()
        {
            // Manual UI initialization since we're building the UI in code
            Title = "Prime Spiral Visualizer";
            Width = 600;
            Height = 500;
            
            // Create main grid
            var grid = new Grid();
            Content = grid;
            
            // Set up row definitions
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Add header
            var header = new TextBlock
            {
                Text = "Prime Spiral Generator",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Add count input
            var countPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 5, 10, 5)
            };
            Grid.SetRow(countPanel, 1);
            
            countPanel.Children.Add(new TextBlock
            {
                Text = "Number Count:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
            
            txtNumberCount = new TextBox
            {
                Width = 100,
                Text = "1000"
            };
            countPanel.Children.Add(txtNumberCount);
            grid.Children.Add(countPanel);
            
            // Add radio buttons
            var radioPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 5, 10, 5)
            };
            Grid.SetRow(radioPanel, 2);
            
            rbPrimes = new RadioButton
            {
                Content = "Prime Numbers",
                IsChecked = true,
                Margin = new Thickness(0, 0, 10, 0)
            };
            radioPanel.Children.Add(rbPrimes);
            
            rbAllNumbers = new RadioButton
            {
                Content = "All Numbers",
                Margin = new Thickness(0, 0, 10, 0)
            };
            radioPanel.Children.Add(rbAllNumbers);
            grid.Children.Add(radioPanel);
            
            // Add buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 5, 10, 5)
            };
            Grid.SetRow(buttonPanel, 3);
            
            btnGenerate = new Button
            {
                Content = "Generate Image",
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnGenerate.Click += BtnGenerate_Click;
            buttonPanel.Children.Add(btnGenerate);
            
            btnSave = new Button
            {
                Content = "Save Image",
                Width = 120
            };
            btnSave.Click += BtnSave_Click;
            buttonPanel.Children.Add(btnSave);
            grid.Children.Add(buttonPanel);
            
            // Add image preview
            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10)
            };
            Grid.SetRow(border, 4);
            
            imgPreview = new Image
            {
                Stretch = System.Windows.Media.Stretch.Uniform
            };
            border.Child = imgPreview;
            grid.Children.Add(border);
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GenerateAndDisplayImage(TempImagePath);
                MessageBox.Show("Preview generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
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
                    GenerateAndDisplayImage(saveFileDialog.FileName);
                    MessageBox.Show($"Image saved to: {saveFileDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateAndDisplayImage(string filePath)
        {
            // Parse count from text box
            if (!int.TryParse(txtNumberCount.Text, out int count))
            {
                throw new ArgumentException("Please enter a valid number for count");
            }

            // Limit the count to a reasonable range
            count = Math.Max(10, Math.Min(count, 100000));

            // Generate numbers based on radio button selection
            List<int> numbers;
            if (rbPrimes.IsChecked == true)
            {
                numbers = GeneratePrimes(count);
            }
            else
            {
                numbers = GenerateSequentialNumbers(count);
            }

            // Create the plot image
            PlotUtils.CreatePlot(
                numbers,
                figsize: 20,
                maxPointSize: 5,
                showAnnot: false,
                filePath: filePath);

            // Display the image in the preview
            if (File.Exists(filePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.RelativeOrAbsolute);
                bitmap.EndInit();
                imgPreview.Source = bitmap;
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
    }
}