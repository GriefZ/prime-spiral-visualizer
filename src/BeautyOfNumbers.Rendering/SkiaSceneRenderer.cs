using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;

namespace BeautyOfNumbers.Rendering;

public sealed class SkiaSceneRenderer : ISceneRenderer
{
    private SKPaint[]? paints;
    private RgbaColor[]? palette;
    private bool antialias;

    public void Draw(SKCanvas canvas, Scene scene, RenderStyle style, Viewport viewport)
    {
        canvas.Clear(ToSkColor(style.Background));

        if (scene.Points.Length == 0)
        {
            return;
        }

        float viewScale = (float)viewport.Scale;
        float offsetX = (float)viewport.OffsetX;
        float offsetY = (float)viewport.OffsetY;
        float canvasWidth = canvas.LocalClipBounds.Width;
        float canvasHeight = canvas.LocalClipBounds.Height;

        SKPaint[] currentPaints = GetPaints(style);

        Span<ScenePoint> points = scene.Points;
        for (int i = 0; i < points.Length; i++)
        {
            ScenePoint pt = points[i];
            float sx = (float)(pt.X * viewScale) + offsetX;
            float sy = (float)(pt.Y * viewScale) + offsetY;

            if (sx < -pt.Size || sx > canvasWidth + pt.Size ||
                sy < -pt.Size || sy > canvasHeight + pt.Size)
            {
                continue;
            }

            float half = pt.Size * 0.5f;
            int ci = pt.ColorIndex;
            if (ci >= currentPaints.Length)
            {
                ci = currentPaints.Length - 1;
            }

            canvas.DrawCircle(sx, sy, half, currentPaints[ci]);
        }
    }

    private SKPaint[] GetPaints(RenderStyle style)
    {
        SKPaint[]? current = paints;
        if (current is null || !ReferenceEquals(palette, style.Palette) || antialias != style.Antialias)
        {
            DisposePaints();
            RgbaColor[] stylePalette = style.Palette;
            palette = stylePalette;
            antialias = style.Antialias;
            current = new SKPaint[stylePalette.Length];
            for (int i = 0; i < stylePalette.Length; i++)
            {
                current[i] = new SKPaint
                {
                    Color = ToSkColor(stylePalette[i]),
                    IsAntialias = antialias,
                    Style = SKPaintStyle.Fill,
                };
            }

            paints = current;
        }

        return current;
    }

    private void DisposePaints()
    {
        SKPaint[]? current = paints;
        if (current is null)
        {
            return;
        }

        foreach (SKPaint paint in current)
        {
            paint.Dispose();
        }

        paints = null;
    }

    private static SKColor ToSkColor(RgbaColor color) => new(color.R, color.G, color.B, color.A);
}