using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Geometry;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;

namespace BeautyOfNumbers.Rendering;

public sealed class SceneBuilder
{
    private readonly IPrimeClassifier primes;

    public SceneBuilder(IPrimeClassifier primes) => this.primes = primes;

    public Scene Build(
        RenderRequest request,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArchimedeanSpiral layout = new();
        int total = request.Range.Count;
        var points = new ScenePoint[total];
        int written = 0;

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        double maxRadius = Radius(layout.Map(request.Range.End, request.Layout));

        int checkpointInterval = Math.Max(1, total / 100);
        int end = request.Range.End;
        for (int number = request.Range.Start; number <= end; number++)
        {
            if (number % checkpointInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float fraction = (float)(number - request.Range.Start) / total;
                progress?.Report(new ProgressReport(RenderStage.Building, fraction));
            }

            bool isPrime = primes.IsPrime(number);
            if (request.ShowOnlyPrimes && !isPrime)
            {
                continue;
            }

            Point2D pt = layout.Map(number, request.Layout);
            float size = ComputePointSize(pt, maxRadius, request.Style.PointSize);
            byte colorIndex = (byte)(isPrime ? 0 : 1);

            points[written++] = new ScenePoint(pt.X, pt.Y, size, colorIndex);

            if (pt.X < minX) minX = pt.X;
            if (pt.X > maxX) maxX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        if (written < total)
        {
            var trimmed = new ScenePoint[written];
            Array.Copy(points, trimmed, written);
            points = trimmed;
        }

        progress?.Report(new ProgressReport(RenderStage.Building, 1.0f));

        if (written == 0)
        {
            return new Scene(points, 0, 0, 0, 0);
        }

        return new Scene(points, minX, maxX, minY, maxY);
    }

    private static float ComputePointSize(Point2D pt, double maxRadius, PointSizeFunction psf)
    {
        double r = Math.Sqrt((pt.X * pt.X) + (pt.Y * pt.Y));
        double normalized = maxRadius > 0 ? r / maxRadius : 0;
        return (float)psf.Evaluate(normalized);
    }

    private static double Radius(Point2D pt) => Math.Sqrt((pt.X * pt.X) + (pt.Y * pt.Y));
}