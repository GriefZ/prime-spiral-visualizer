using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace BeautyOfNumbers.Rendering.Tests;

public sealed class SkiaSceneRendererBatchingTests
{
    private const int CanvasSize = 512;

    private readonly ITestOutputHelper output;

    public SkiaSceneRendererBatchingTests(ITestOutputHelper output) => this.output = output;

    // A single opaque colour isolates geometry: draw order cannot change the result,
    // so any difference comes from size quantization and point rasterization.
    private static readonly RenderStyle OpaqueStyle = new(
        new RgbaColor(255, 255, 255, 255),
        [new RgbaColor(20, 20, 20, 255)],
        new PointSizeFunction(0.5, 5.0, PointSizeCurve.Linear),
        true);

    [Fact]
    public void Draw_BatchedAndNaive_ProduceSimilarPixels()
    {
        Scene scene = TestScenes.Build(100_000);

        byte[] naive = RenderPixels(scene, OpaqueStyle, new RendererOptions(UseBatching: false));
        byte[] batched = RenderPixels(scene, OpaqueStyle, new RendererOptions(UseBatching: true));

        long channelDiff = 0;
        int maxChannelDiff = 0;
        int differingPixels = 0;
        int pixels = naive.Length / 4;

        for (int offset = 0; offset < naive.Length; offset += 4)
        {
            int pixelMax = 0;
            for (int channel = 0; channel < 4; channel++)
            {
                int diff = Math.Abs(naive[offset + channel] - batched[offset + channel]);
                channelDiff += diff;
                if (diff > pixelMax)
                {
                    pixelMax = diff;
                }
            }

            if (pixelMax > 32)
            {
                differingPixels++;
            }

            if (pixelMax > maxChannelDiff)
            {
                maxChannelDiff = pixelMax;
            }
        }

        double meanChannelDiff = (double)channelDiff / naive.Length;
        double differingShare = (double)differingPixels / pixels;

        output.WriteLine($"mean={meanChannelDiff:F3} max={maxChannelDiff} differingShare={differingShare:P3}");

        Assert.True(meanChannelDiff < 2.0, $"Mean channel difference was {meanChannelDiff:F2}.");
        Assert.True(differingShare < 0.01, $"Share of differing pixels was {differingShare:P2}.");
    }

    [Fact]
    public void Draw_WithBatchingDisabled_DrawsScene()
    {
        Scene scene = TestScenes.Build(10_000);

        byte[] pixels = RenderPixels(scene, RenderStyle.Default, new RendererOptions(UseBatching: false));

        Assert.Contains(pixels, value => value != 255);
    }

    [Fact]
    public void Draw_WithMoreThanMaxBuckets_RendersWithoutException()
    {
        var style = new RenderStyle(
            new RgbaColor(255, 255, 255, 255),
            [new RgbaColor(0, 0, 0, 255)],
            new PointSizeFunction(0.1, 5000, PointSizeCurve.Linear),
            false);

        Scene scene = TestScenes.Build(1_000);

        // Span / 0.5 > 256 buckets, so the bucket count is clamped to MaxBucketCount.
        var options = new RendererOptions(UseBatching: true, MaxSizeSteps: 1000);
        byte[] pixels = RenderPixels(scene, style, options);

        Assert.Contains(pixels, value => value != 255);
    }

    private static byte[] RenderPixels(Scene scene, RenderStyle style, RendererOptions options)
    {
        using var bitmap = new SKBitmap(CanvasSize, CanvasSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        Viewport viewport = SceneFitter.Fit(scene, CanvasSize, CanvasSize);
        new SkiaSceneRenderer(options).Draw(canvas, scene, style, viewport);
        return bitmap.GetPixelSpan().ToArray();
    }
}
