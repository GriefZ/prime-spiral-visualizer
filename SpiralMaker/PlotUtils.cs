using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace SpiralMaker
{
    public class PlotUtils
    {   
        /* TODO: Generate a file name based on the remaining parameters*/
        public static void CreatePlot(IEnumerable<int> nums, double figsize = 8, double? s = null, bool showAnnot = false, string filePath = "plot.png") 
        {
            var numsList = new List<int>(nums);
            var (x, y) = GetCoordinate(numsList);

            var plotModel = new PlotModel { PlotType = PlotType.Polar, Background = OxyColors.White, IsLegendVisible = false, Title = "Prime Spiral" };
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
            double pointSize = s ?? 5;
            OxyColor color;

            for (int i = 0; i < x.Count; i++)
            {
                color = (x[i] % 10) switch
                {
                    1 => OxyColor.Parse("#99868686"),
                    3 => OxyColor.Parse("#99d5c29f"),
                    7 => OxyColor.Parse("#99b31414"),
                    9 => OxyColor.Parse("#9928324c"),
                    _ => OxyColor.Parse("#99777777"),
                };
                scatterSeries = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerFill = color };

                double pointSizeCooficient = ((double)(x[i]) / x[^1]) + 0.2;
                scatterSeries.Points.Add(new ScatterPoint(x[i], y[i], pointSizeCooficient * pointSize));


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

            // Export to a PNG file TODO: Dedicated service
            using (var stream = File.Create(filePath))
            {
                var pngExporter = new PngExporter { Width = (int)(figsize * 100), Height = (int)(figsize * 100), Dpi = 300 };
                pngExporter.Export(plotModel, stream);
            }
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
