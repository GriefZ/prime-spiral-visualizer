using SkiaSharp;

namespace BeautyOfNumbers.Benchmarks;

internal static class BenchmarkSurfaces
{
    public static SKSurface Create(int width, int height) =>
        SKSurface.Create(new SKImageInfo(width, height))
        ?? throw new InvalidOperationException("Failed to create Skia surface.");
}
