using BeautyOfNumbers.Core.Colors;

namespace BeautyOfNumbers.Core.Style;

public sealed record RenderStyle(
    RgbaColor Background,
    RgbaColor[] Palette,
    PointSizeFunction PointSize,
    bool Antialias)
{
    public static RenderStyle Default { get; } = new(
        new RgbaColor(255, 255, 255, 255),
        [new RgbaColor(179, 20, 20, 203), new RgbaColor(134, 134, 134, 203)],
        new PointSizeFunction(0.5, 5.0, PointSizeCurve.Linear),
        true);
}
