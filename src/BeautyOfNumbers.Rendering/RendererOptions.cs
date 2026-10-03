namespace BeautyOfNumbers.Rendering;

/// <summary>
/// Drawing behaviour of <see cref="SkiaSceneRenderer"/>. Batching is on by default;
/// tests and benchmarks can turn it off to compare against the naive per-point path.
/// </summary>
public sealed record RendererOptions(
    bool UseBatching = true,
    float SizeQuantizationStep = 0.5f,
    int MaxSizeSteps = 32)
{
    public static RendererOptions Default { get; } = new();
}
