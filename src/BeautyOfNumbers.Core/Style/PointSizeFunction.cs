namespace BeautyOfNumbers.Core.Style;

public sealed record PointSizeFunction(double Min, double Max, PointSizeCurve Curve)
{
    public double Evaluate(double normalizedRadius)
    {
        double ratio = Math.Clamp(normalizedRadius, 0.0, 1.0);
        double factor = Curve switch
        {
            PointSizeCurve.Constant => 1.0,
            PointSizeCurve.Sqrt => Math.Sqrt(ratio),
            _ => ratio,
        };

        return Min + ((Max - Min) * factor);
    }
}
