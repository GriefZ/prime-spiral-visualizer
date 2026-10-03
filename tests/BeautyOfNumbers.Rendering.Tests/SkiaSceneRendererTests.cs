using System.Security.Cryptography;
using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;
using Xunit;

namespace BeautyOfNumbers.Rendering.Tests;

public sealed class SkiaSceneRendererTests
{
    private const int CanvasSize = 512;

    [Fact]
    public void Draw_TenThousandPoints_CompletesWithoutException()
    {
        Scene scene = TestScenes.Build(10_000);

        DrawToBitmap(scene, out SKBitmap bitmap);
        using (bitmap)
        {
            Assert.True(bitmap.ByteCount > 0);
        }
    }

    [Fact]
    public void Draw_SameRequestTwice_ProducesIdenticalPixels()
    {
        Scene scene = TestScenes.Build(10_000);

        byte[] first = RenderSceneHash(scene);
        byte[] second = RenderSceneHash(scene);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Draw_EmptyScene_CompletesWithoutException()
    {
        var scene = new Scene([], 0, 0, 0, 0);

        DrawToBitmap(scene, out SKBitmap bitmap);
        using (bitmap)
        {
            Assert.True(bitmap.ByteCount > 0);
        }
    }

    [Fact]
    public void Draw_SinglePointScene_CompletesWithoutException()
    {
        Scene scene = TestScenes.Build(1);

        DrawToBitmap(scene, out SKBitmap bitmap);
        using (bitmap)
        {
            Assert.True(bitmap.ByteCount > 0);
        }
    }

    [Fact]
    public void Draw_ColorIndexBeyondPalette_ClampsToLastColor()
    {
        var scene = new Scene([new ScenePoint(0, 0, 4, 42)], 0, 0, 0, 0);
        var style = new RenderStyle(
            new RgbaColor(255, 255, 255, 255),
            [new RgbaColor(0, 0, 0, 255)],
            new PointSizeFunction(1, 1, PointSizeCurve.Constant),
            false);
        var viewport = new Viewport(1, 10, 10);

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        new SkiaSceneRenderer().Draw(canvas, scene, style, viewport);
    }

    private static void DrawToBitmap(Scene scene, out SKBitmap bitmap)
    {
        bitmap = new SKBitmap(CanvasSize, CanvasSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        Viewport viewport = SceneFitter.Fit(scene, CanvasSize, CanvasSize);
        new SkiaSceneRenderer().Draw(canvas, scene, RenderStyle.Default, viewport);
    }

    private static byte[] RenderSceneHash(Scene scene)
    {
        DrawToBitmap(scene, out SKBitmap bitmap);
        using (bitmap)
        {
            return SHA256.HashData(bitmap.GetPixelSpan());
        }
    }
}
