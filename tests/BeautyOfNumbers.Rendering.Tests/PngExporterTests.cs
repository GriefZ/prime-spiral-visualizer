using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;
using Xunit;

namespace BeautyOfNumbers.Rendering.Tests;

public sealed class PngExporterTests
{
    [Fact]
    public void Export_ValidScene_WritesPngWithRequestedDimensions()
    {
        Scene scene = TestScenes.Build(1_000);
        var output = new OutputOptions(64, 48, 72);
        string path = Path.Combine(Path.GetTempPath(), $"bon-export-{Guid.NewGuid():N}.png");

        try
        {
            PngExporter.Export(scene, RenderStyle.Default, output, path, new SkiaSceneRenderer());

            Assert.True(File.Exists(path));
            using SKBitmap? decoded = SKBitmap.Decode(path);
            Assert.NotNull(decoded);
            Assert.Equal(output.Width, decoded.Width);
            Assert.Equal(output.Height, decoded.Height);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
