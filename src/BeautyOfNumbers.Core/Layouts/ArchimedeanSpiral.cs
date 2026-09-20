using BeautyOfNumbers.Core.Geometry;

namespace BeautyOfNumbers.Core.Layouts;

/// <summary>
/// Archimedean spiral: r = Scale * n, theta = n + Rotation (docs/04-math/layouts.md).
/// </summary>
public sealed class ArchimedeanSpiral : ISpiralLayout
{
    public const string LayoutId = "archimedean";

    public string Id => LayoutId;

    public Point2D Map(int number, LayoutOptions options)
    {
        double angle = number + options.Rotation;
        double radius = options.Scale * number;
        double y = options.Clockwise ? -radius * Math.Sin(angle) : radius * Math.Sin(angle);
        return new Point2D(radius * Math.Cos(angle), y);
    }
}
