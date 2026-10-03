using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;

namespace BeautyOfNumbers.Rendering;

public sealed class SkiaSceneRenderer : ISceneRenderer
{
    private const float MinimumPointSize = 0.5f;
    private const int MaxBucketCount = 256;

    private readonly RendererOptions options;

    private SKPaint[]? naivePaints;
    private RgbaColor[]? naivePalette;
    private bool naiveAntialias;

    private readonly Dictionary<(int ColorIndex, int SizeIndex), SKPaint> batchedPaints = [];
    private RgbaColor[]? batchedPalette;
    private bool batchedAntialias;
    private float batchedMinSize = float.NaN;
    private float batchedStep = float.NaN;

    private GroupBuffer?[] groups = [];
    private int groupStride;
    private readonly List<GroupBuffer> usedGroups = [];

    public SkiaSceneRenderer()
        : this(RendererOptions.Default)
    {
    }

    public SkiaSceneRenderer(RendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
    }

    public void Draw(SKCanvas canvas, Scene scene, RenderStyle style, Viewport viewport)
    {
        canvas.Clear(ToSkColor(style.Background));

        if (scene.Points.Length == 0 || style.Palette.Length == 0)
        {
            return;
        }

        float viewScale = (float)viewport.Scale;
        float offsetX = (float)viewport.OffsetX;
        float offsetY = (float)viewport.OffsetY;
        float canvasWidth = canvas.LocalClipBounds.Width;
        float canvasHeight = canvas.LocalClipBounds.Height;

        if (options.UseBatching)
        {
            DrawBatched(canvas, scene.Points, style, viewScale, offsetX, offsetY, canvasWidth, canvasHeight);
        }
        else
        {
            DrawNaive(canvas, scene.Points, style, viewScale, offsetX, offsetY, canvasWidth, canvasHeight);
        }
    }

    private void DrawNaive(
        SKCanvas canvas,
        ScenePoint[] points,
        RenderStyle style,
        float viewScale,
        float offsetX,
        float offsetY,
        float canvasWidth,
        float canvasHeight)
    {
        SKPaint[] currentPaints = GetNaivePaints(style);
        int maxColor = currentPaints.Length - 1;

        for (int i = 0; i < points.Length; i++)
        {
            ScenePoint pt = points[i];
            float sx = (float)(pt.X * viewScale) + offsetX;
            float sy = (float)(pt.Y * viewScale) + offsetY;

            if (IsOutside(sx, sy, pt.Size, canvasWidth, canvasHeight))
            {
                continue;
            }

            int ci = pt.ColorIndex;
            if (ci > maxColor)
            {
                ci = maxColor;
            }

            canvas.DrawCircle(sx, sy, pt.Size * 0.5f, currentPaints[ci]);
        }
    }

    private void DrawBatched(
        SKCanvas canvas,
        ScenePoint[] points,
        RenderStyle style,
        float viewScale,
        float offsetX,
        float offsetY,
        float canvasWidth,
        float canvasHeight)
    {
        float minSize = float.MaxValue;
        float maxSize = float.MinValue;
        for (int i = 0; i < points.Length; i++)
        {
            float size = points[i].Size;
            if (size < minSize)
            {
                minSize = size;
            }

            if (size > maxSize)
            {
                maxSize = size;
            }
        }

        if (minSize < MinimumPointSize)
        {
            minSize = MinimumPointSize;
        }

        if (maxSize < minSize)
        {
            maxSize = minSize;
        }

        float step = options.SizeQuantizationStep;
        if (step <= 0f)
        {
            step = 0.5f;
        }

        float span = maxSize - minSize;
        int maxSteps = Math.Max(1, options.MaxSizeSteps);
        if (span > step * maxSteps)
        {
            step = span / maxSteps;
        }

        if (step <= 0f)
        {
            step = 0.5f;
        }

        int bucketCount = Math.Max(1, (int)MathF.Round(span / step) + 1);
        if (bucketCount > MaxBucketCount)
        {
            bucketCount = MaxBucketCount;
        }

        EnsureBatchedPaints(style, minSize, step);
        EnsureGroupTable(style.Palette.Length, bucketCount);
        ResetGroups();

        int maxColor = style.Palette.Length - 1;
        for (int i = 0; i < points.Length; i++)
        {
            ScenePoint pt = points[i];
            float sx = (float)(pt.X * viewScale) + offsetX;
            float sy = (float)(pt.Y * viewScale) + offsetY;

            if (IsOutside(sx, sy, pt.Size, canvasWidth, canvasHeight))
            {
                continue;
            }

            int ci = pt.ColorIndex;
            if (ci > maxColor)
            {
                ci = maxColor;
            }

            int si = SizeIndex(pt.Size, minSize, step, bucketCount);
            GroupBuffer buffer = GetGroup(ci, si);
            buffer.Count++;
        }

        for (int i = 0; i < usedGroups.Count; i++)
        {
            usedGroups[i].Prepare();
        }

        for (int i = 0; i < points.Length; i++)
        {
            ScenePoint pt = points[i];
            float sx = (float)(pt.X * viewScale) + offsetX;
            float sy = (float)(pt.Y * viewScale) + offsetY;

            if (IsOutside(sx, sy, pt.Size, canvasWidth, canvasHeight))
            {
                continue;
            }

            int ci = pt.ColorIndex;
            if (ci > maxColor)
            {
                ci = maxColor;
            }

            int si = SizeIndex(pt.Size, minSize, step, bucketCount);
            GroupBuffer buffer = groups[(ci * groupStride) + si]!;
            buffer.Points[buffer.Filled++] = new SKPoint(sx, sy);
        }

        for (int i = 0; i < usedGroups.Count; i++)
        {
            GroupBuffer buffer = usedGroups[i];
            SKPaint paint = GetBatchedPaint(style, buffer.ColorIndex, buffer.SizeIndex, minSize, step);
            canvas.DrawPoints(SKPointMode.Points, buffer.Points, paint);
        }
    }

    private GroupBuffer GetGroup(int colorIndex, int sizeIndex)
    {
        int key = (colorIndex * groupStride) + sizeIndex;
        GroupBuffer? buffer = groups[key];
        if (buffer is null)
        {
            buffer = new GroupBuffer
            {
                ColorIndex = colorIndex,
                SizeIndex = sizeIndex,
            };
            groups[key] = buffer;
            usedGroups.Add(buffer);
            return buffer;
        }

        if (buffer.Count == 0)
        {
            usedGroups.Add(buffer);
        }

        buffer.ColorIndex = colorIndex;
        buffer.SizeIndex = sizeIndex;
        return buffer;
    }

    private void ResetGroups()
    {
        usedGroups.Clear();
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i]?.Reset();
        }
    }

    private void EnsureGroupTable(int paletteLength, int bucketCount)
    {
        if (groupStride == bucketCount && groups.Length == paletteLength * bucketCount)
        {
            return;
        }

        groups = new GroupBuffer?[paletteLength * bucketCount];
        groupStride = bucketCount;
        usedGroups.Clear();
    }

    private void EnsureBatchedPaints(RenderStyle style, float minSize, float step)
    {
        if (ReferenceEquals(batchedPalette, style.Palette) &&
            batchedAntialias == style.Antialias &&
            batchedMinSize == minSize &&
            batchedStep == step)
        {
            return;
        }

        DisposeBatchedPaints();
        batchedPalette = style.Palette;
        batchedAntialias = style.Antialias;
        batchedMinSize = minSize;
        batchedStep = step;
    }

    private SKPaint GetBatchedPaint(RenderStyle style, int colorIndex, int sizeIndex, float minSize, float step)
    {
        var key = (colorIndex, sizeIndex);
        if (batchedPaints.TryGetValue(key, out SKPaint? paint))
        {
            return paint;
        }

        float width = MathF.Max(MinimumPointSize, minSize + (sizeIndex * step));
        paint = new SKPaint
        {
            Color = ToSkColor(style.Palette[colorIndex]),
            IsAntialias = style.Antialias,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeWidth = width,
        };
        batchedPaints.Add(key, paint);
        return paint;
    }

    private SKPaint[] GetNaivePaints(RenderStyle style)
    {
        SKPaint[]? current = naivePaints;
        if (current is null || !ReferenceEquals(naivePalette, style.Palette) || naiveAntialias != style.Antialias)
        {
            DisposeNaivePaints();
            RgbaColor[] stylePalette = style.Palette;
            naivePalette = stylePalette;
            naiveAntialias = style.Antialias;
            current = new SKPaint[stylePalette.Length];
            for (int i = 0; i < stylePalette.Length; i++)
            {
                current[i] = new SKPaint
                {
                    Color = ToSkColor(stylePalette[i]),
                    IsAntialias = naiveAntialias,
                    Style = SKPaintStyle.Fill,
                };
            }

            naivePaints = current;
        }

        return current;
    }

    private void DisposeNaivePaints()
    {
        SKPaint[]? current = naivePaints;
        if (current is null)
        {
            return;
        }

        foreach (SKPaint paint in current)
        {
            paint.Dispose();
        }

        naivePaints = null;
    }

    private void DisposeBatchedPaints()
    {
        foreach (SKPaint paint in batchedPaints.Values)
        {
            paint.Dispose();
        }

        batchedPaints.Clear();
    }

    private static int SizeIndex(float size, float minSize, float step, int bucketCount)
    {
        int index = (int)MathF.Round((size - minSize) / step);
        if (index < 0)
        {
            return 0;
        }

        return index >= bucketCount ? bucketCount - 1 : index;
    }

    private static bool IsOutside(float sx, float sy, float size, float canvasWidth, float canvasHeight) =>
        sx < -size || sx > canvasWidth + size ||
        sy < -size || sy > canvasHeight + size;

    private static SKColor ToSkColor(RgbaColor color) => new(color.R, color.G, color.B, color.A);

    private sealed class GroupBuffer
    {
        public SKPoint[] Points = [];

        public int Count;

        public int Filled;

        public int ColorIndex;

        public int SizeIndex;

        public void Reset()
        {
            Count = 0;
            Filled = 0;
        }

        public void Prepare()
        {
            if (Points.Length != Count)
            {
                Array.Resize(ref Points, Count);
            }
        }
    }
}
