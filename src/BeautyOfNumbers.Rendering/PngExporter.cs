using System.Buffers.Binary;
using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;

namespace BeautyOfNumbers.Rendering;

public static class PngExporter
{
    private const int PngSignatureLength = 8;
    private const int ChunkHeaderLength = 8;
    private const int ChunkCrcLength = 4;
    private const int PhysicalResolutionDataLength = 9;

    public static void Export(
        Scene scene,
        RenderStyle style,
        OutputOptions output,
        string filePath,
        ISceneRenderer renderer,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var surface = SKSurface.Create(new SKImageInfo(output.Width, output.Height));
        if (surface is null)
        {
            throw new InvalidOperationException("Failed to create Skia surface.");
        }

        SKCanvas canvas = surface.Canvas;
        Viewport viewport = SceneFitter.Fit(scene, output.Width, output.Height);

        progress?.Report(new ProgressReport(RenderStage.Encoding, 0.5f));
        renderer.Draw(canvas, scene, style, viewport);
        progress?.Report(new ProgressReport(RenderStage.Encoding, 0.95f));
        cancellationToken.ThrowIfCancellationRequested();

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

        cancellationToken.ThrowIfCancellationRequested();

        byte[] png = data.ToArray();
        if (output.Dpi > 0)
        {
            png = AddPhysicalResolution(png, output.Dpi);
        }

        File.WriteAllBytes(filePath, png);
        progress?.Report(new ProgressReport(RenderStage.Encoding, 1.0f));
    }

    private static byte[] AddPhysicalResolution(byte[] png, double dpi)
    {
        int ihdrDataLength = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(PngSignatureLength, sizeof(int)));
        int insertAt = PngSignatureLength + ChunkHeaderLength + ihdrDataLength + ChunkCrcLength;

        uint pixelsPerMeter = (uint)Math.Round(dpi / 0.0254, MidpointRounding.AwayFromZero);
        var chunk = new byte[ChunkHeaderLength + PhysicalResolutionDataLength + ChunkCrcLength];
        BinaryPrimitives.WriteInt32BigEndian(chunk.AsSpan(0, sizeof(int)), PhysicalResolutionDataLength);
        "pHYs"u8.CopyTo(chunk.AsSpan(4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8, sizeof(uint)), pixelsPerMeter);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(12, sizeof(uint)), pixelsPerMeter);
        chunk[16] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(17, sizeof(uint)), ComputeCrc32(chunk.AsSpan(4, 13)));

        var result = new byte[png.Length + chunk.Length];
        png.AsSpan(0, insertAt).CopyTo(result);
        chunk.AsSpan().CopyTo(result.AsSpan(insertAt));
        png.AsSpan(insertAt).CopyTo(result.AsSpan(insertAt + chunk.Length));
        return result;
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                uint mask = (uint)-(int)(crc & 1u);
                crc = (crc >> 1) ^ (0xEDB88320u & mask);
            }
        }

        return ~crc;
    }
}