using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Rendering;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace BeautyOfNumbers.Benchmarks;

[MemoryDiagnoser]
public class DrawExportBench
{
    [Params(100_000, 1_000_000)]
    public int Count { get; set; }

    private Scene scene = null!;
    private SKSurface surface = null!;
    private SkiaSceneRenderer renderer = null!;
    private Viewport viewport;

    [GlobalSetup]
    public void Setup()
    {
        OutputOptions output = OutputOptions.Default;
        scene = BenchmarkScenes.Build(Count);
        surface = BenchmarkSurfaces.Create(output.Width, output.Height);
        renderer = new SkiaSceneRenderer();
        viewport = SceneFitter.Fit(scene, output.Width, output.Height);
    }

    [GlobalCleanup]
    public void Cleanup() => surface.Dispose();

    [Benchmark]
    public void Draw() => renderer.Draw(surface.Canvas, scene, RenderStyle.Default, viewport);
}
