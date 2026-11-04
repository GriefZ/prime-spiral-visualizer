namespace BeautyOfNumbers.Core.Requests;

public sealed record OutputOptions(int Width, int Height, double Dpi)
{
    public const int MaxDimension = 16_384;
    public const int MaxPixelCount = 64_000_000;
    public const double MaxDpi = 2_400;

    public static OutputOptions Default { get; } = new(2000, 2000, 300);
}
