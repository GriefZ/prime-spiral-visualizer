namespace BeautyOfNumbers.Core.Requests;

public sealed record OutputOptions(int Width, int Height, double Dpi)
{
    public static OutputOptions Default { get; } = new(2000, 2000, 300);
}
