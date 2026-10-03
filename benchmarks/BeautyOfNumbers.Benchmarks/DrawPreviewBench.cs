using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Rendering;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace BeautyOfNumbers.Benchmarks;

[MemoryDiagnoser]
public class DrawPreviewBench
{
    private const int Count = 100_000;
    private const int Width = 1280;
    private const int Height = 800;

    private Scene scene = null!;
    private SKSurface surface = null!;
    private SkiaSceneRenderer renderer = null!;

    [GlobalSetup]
    public void Setup()
    {
        scene = BenchmarkScenes.Build(Count);
        surface = BenchmarkSurfaces.Create(Width, Height);
        renderer = new SkiaSceneRenderer();
    }

    [GlobalCleanup]
    public void Cleanup() => surface.Dispose();

    [Benchmark]
    public void FitAndDraw()
    {
        Viewport viewport = SceneFitter.Fit(scene, Width, Height);
        renderer.Draw(surface.Canvas, scene, RenderStyle.Default, viewport);
    }
}
