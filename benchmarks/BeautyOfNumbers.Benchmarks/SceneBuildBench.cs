using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Rendering;
using BenchmarkDotNet.Attributes;

namespace BeautyOfNumbers.Benchmarks;

[MemoryDiagnoser]
public class SceneBuildBench
{
    [Params(100_000, 1_000_000)]
    public int Count { get; set; }

    private SceneBuilder builder = null!;
    private RenderRequest request = null!;

    [GlobalSetup]
    public void Setup()
    {
        request = BenchmarkScenes.CreateRequest(Count);
        builder = BenchmarkScenes.CreateBuilder(Count);
    }

    [Benchmark]
    public Scene Build() => builder.Build(request);
}
