using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Rendering;
using BenchmarkDotNet.Attributes;

namespace BeautyOfNumbers.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Heavy")]
public class SceneBuildHeavyBench
{
    [Params(10_000_000)]
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
