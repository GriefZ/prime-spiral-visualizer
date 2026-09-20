using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Geometry;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace BeautyOfNumbers.Rendering;

/// <summary>
/// Temporary OxyPlot renderer kept until the SkiaSharp pipeline lands in M1 (docs/02-architecture/adr/0002-skiasharp-renderer.md).
/// </summary>
public sealed class OxyPlotSpiralRenderer
{
    private readonly IPrimeClassifier primes;

    public OxyPlotSpiralRenderer(IPrimeClassifier primes) => this.primes = primes;

    public void Render(RenderRequest request, string filePath)
    {
        var layout = new ArchimedeanSpiral();
        double maxRadius = Radius(layout.Map(request.Range.End, request.Layout));

        var model = new PlotModel
        {
            Background = ToOxyColor(request.Style.Background),
            IsLegendVisible = false,
            Title = "Prime Spiral",
            PlotAreaBorderColor = OxyColors.Transparent,
            TextColor = OxyColors.Transparent,
        };
        model.Axes.Add(CreateAxis(AxisPosition.Bottom, maxRadius));
        model.Axes.Add(CreateAxis(AxisPosition.Left, maxRadius));

        int end = request.Range.End;
        for (long number = request.Range.Start; number <= end; number++)
        {
            bool isPrime = primes.IsPrime((int)number);
            if (request.ShowOnlyPrimes && !isPrime)
            {
                continue;
            }

            Point2D point = layout.Map((int)number, request.Layout);
            double size = request.Style.PointSize.Evaluate(Radius(point) / maxRadius);
            var series = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = ToOxyColor(request.Style.Palette[isPrime ? 0 : 1]),
            };
            series.Points.Add(new ScatterPoint(point.X, point.Y, size));
            model.Series.Add(series);
        }

        SaveToPng(model, filePath, request.Output);
    }

    private static LinearAxis CreateAxis(AxisPosition position, double maxRadius) => new()
    {
        Position = position,
        Minimum = -maxRadius * 1.03,
        Maximum = maxRadius * 1.03,
        IsAxisVisible = false,
    };

    private static double Radius(Point2D point) => Math.Sqrt((point.X * point.X) + (point.Y * point.Y));

    private static OxyColor ToOxyColor(RgbaColor color) => OxyColor.FromArgb(color.A, color.R, color.G, color.B);

    private static void SaveToPng(PlotModel model, string filePath, OutputOptions output)
    {
        using var stream = File.Create(filePath);
        var exporter = new PngExporter { Width = output.Width, Height = output.Height, Dpi = (float)output.Dpi };
        exporter.Export(model, stream);
    }
}
