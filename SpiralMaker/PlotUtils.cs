using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace SpiralMaker
{
    public class PlotUtils
    {
        /* TODO: Generate a file name based on the remaining parameters*/
        public static void CreatePlot(IEnumerable<int> nums, int figsize = 8, double maxPointSize = 5, bool showAnnot = false,
            string filePath = "plot.png", OxyColor? backgroundColor = null, OxyColor? primeColor = null, OxyColor? nonPrimeColor = null)
        {
            var numsList = new List<int>(nums);
            var (x, y) = GetCoordinate(numsList);

            var plotModel = new PlotModel {
                PlotType = PlotType.Polar,
                Background = backgroundColor.GetValueOrDefault(OxyColors.White),
                IsLegendVisible = false,
                Title = "Prime Spiral"
            };
            plotModel.PlotAreaBorderColor = OxyColors.Transparent;

            // Axes and remove grid lines
            var angleAxis = new AngleAxis
            {
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                Minimum = 0,
                Maximum = 2 * Math.PI
            };
            var magnitudeAxis = new MagnitudeAxis
            {
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                Title = "Polar Plot",
                Minimum = 0,
                Maximum = x[^1] * 1.03
            };

            plotModel.Axes.Add(angleAxis);
            plotModel.Axes.Add(magnitudeAxis);
            plotModel.TextColor = OxyColors.Transparent;

            ScatterSeries scatterSeries;

            OxyColor color;

            for (int i = 0; i < x.Count; i++)
            {
                bool isPrime = IsPrime(numsList[i]);
                color = isPrime ?
                    primeColor.GetValueOrDefault(OxyColor.Parse("#cbb31414")) :
                    nonPrimeColor.GetValueOrDefault(OxyColor.Parse("#cb868686"));
                scatterSeries = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerFill = color };

                double minPointSize = 0.5;
                double maxRadius = x[^1] / 6.28;
                double curRadius = x[i] / 6.28;
                double growSpeed = 1; // > 0 only; < 1 - growth slowing down; > 1 - growth accelerating
                double pointSizeCooficient = minPointSize + (maxPointSize - minPointSize) * Math.Pow(curRadius / maxRadius, growSpeed);

                scatterSeries.Points.Add(new ScatterPoint(x[i], y[i], pointSizeCooficient));


                if (showAnnot)
                {
                    var textAnnotation = new OxyPlot.Annotations.TextAnnotation
                    {
                        Text = numsList[i].ToString(),
                        TextPosition = new DataPoint(x[i], y[i]),
                        Stroke = OxyColors.Transparent
                    };
                    plotModel.Annotations.Add(textAnnotation);
                }
                plotModel.Series.Add(scatterSeries);
            }
            SaveToPng(plotModel, filePath, figsize);
        }

        public static void SaveToPng(PlotModel model, string filePath, int size)
        {
            using (var stream = File.Create(filePath))
            {
                var pngExporter = new PngExporter { Width = (int)(size * 100), Height = (int)(size * 100), Dpi = 300 };
                pngExporter.Export(model, stream);
            }
        }
        private static bool IsPrime(int number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            var boundary = (int)Math.Floor(Math.Sqrt(number));

            for (int i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0) return false;
            }

            return true;
        }

        private static (List<double> x, List<double> y) GetCoordinate(List<int> nums) // FIXME: Coordinates don't need separate numbers for axes
        {
            List<double> xCoords = [];
            List<double> yCoords = [];
            foreach (var i in nums)
            {
                xCoords.Add(i);
                yCoords.Add(i);
            }
            return (xCoords, yCoords);
        }
    }
}
