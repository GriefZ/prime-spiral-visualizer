using System.Windows;
using System.Windows.Input;
using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Rendering;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace BeautyOfNumbers.App.Wpf.Controls;

public sealed class SpiralPreviewControl : SKElement
{
    public static readonly DependencyProperty SceneProperty =
        DependencyProperty.Register(
            nameof(Scene),
            typeof(Scene),
            typeof(SpiralPreviewControl),
            new PropertyMetadata(null, OnSceneChanged));

    public static readonly DependencyProperty CurrentStyleProperty =
        DependencyProperty.Register(
            nameof(CurrentStyle),
            typeof(RenderStyle),
            typeof(SpiralPreviewControl),
            new PropertyMetadata(RenderStyle.Default, OnStyleChanged));

    private readonly SkiaSceneRenderer renderer = new();
    private Viewport viewport = new(1.0, 0, 0);
    private Viewport lastFittedViewport = new(1.0, 0, 0);
    private double lastCanvasWidth;
    private double lastCanvasHeight;
    private double lastSceneCenterX;
    private double lastSceneCenterY;
    private bool hasRenderMetrics;
    private Point lastMousePosition;
    private bool isPanning;

    public SpiralPreviewControl()
    {
        Focusable = true;
    }

    public Scene? Scene
    {
        get => (Scene?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public RenderStyle CurrentStyle
    {
        get => (RenderStyle)GetValue(CurrentStyleProperty);
        set => SetValue(CurrentStyleProperty, value);
    }

    public void ResetView()
    {
        viewport = new Viewport(1.0, 0, 0);
        InvalidateVisual();
    }

    private static void OnSceneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpiralPreviewControl control)
        {
            control.ResetView();
        }
    }

    private static void OnStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpiralPreviewControl control)
        {
            control.InvalidateVisual();
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        SKCanvas canvas = e.Surface.Canvas;
        Scene? scene = Scene;
        RenderStyle style = CurrentStyle;

        if (scene is null)
        {
            canvas.Clear(SKColors.White);
            return;
        }

        double width = e.Info.Width;
        double height = e.Info.Height;

        Viewport fitted = SceneFitter.Fit(scene, width, height, viewport.Scale, viewport.OffsetX, viewport.OffsetY);
        lastFittedViewport = fitted;
        lastCanvasWidth = width;
        lastCanvasHeight = height;
        lastSceneCenterX = scene.MinX + ((scene.MaxX - scene.MinX) * 0.5);
        lastSceneCenterY = scene.MinY + ((scene.MaxY - scene.MinY) * 0.5);
        hasRenderMetrics = true;

        renderer.Draw(canvas, scene, style, fitted);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        double zoomFactor = e.Delta > 0 ? 1.15 : 0.85;
        double newScale = Math.Clamp(viewport.Scale * zoomFactor, 0.05, 50.0);

        Scene? scene = Scene;
        if (scene is not null && hasRenderMetrics && ActualWidth > 0 && ActualHeight > 0)
        {
            double newEffectiveScale = SceneFitter.Fit(scene, lastCanvasWidth, lastCanvasHeight, newScale).Scale;
            double ratio = newEffectiveScale / lastFittedViewport.Scale;

            Point mouse = e.GetPosition(this);
            double cursorX = mouse.X * (lastCanvasWidth / ActualWidth);
            double cursorY = mouse.Y * (lastCanvasHeight / ActualHeight);

            double newTotalOffsetX = (cursorX * (1 - ratio)) + (lastFittedViewport.OffsetX * ratio);
            double newTotalOffsetY = (cursorY * (1 - ratio)) + (lastFittedViewport.OffsetY * ratio);

            viewport = new Viewport(
                newScale,
                newTotalOffsetX - (lastCanvasWidth * 0.5) + (newEffectiveScale * lastSceneCenterX),
                newTotalOffsetY - (lastCanvasHeight * 0.5) + (newEffectiveScale * lastSceneCenterY));
        }
        else
        {
            viewport = new Viewport(newScale, viewport.OffsetX, viewport.OffsetY);
        }

        InvalidateVisual();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            isPanning = true;
            lastMousePosition = e.GetPosition(this);
            CaptureMouse();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (isPanning)
        {
            Point current = e.GetPosition(this);
            double dx = current.X - lastMousePosition.X;
            double dy = current.Y - lastMousePosition.Y;

            double scaleX = hasRenderMetrics && ActualWidth > 0 ? lastCanvasWidth / ActualWidth : 1.0;
            double scaleY = hasRenderMetrics && ActualHeight > 0 ? lastCanvasHeight / ActualHeight : 1.0;

            viewport = new Viewport(
                viewport.Scale,
                viewport.OffsetX + (dx * scaleX),
                viewport.OffsetY + (dy * scaleY));

            lastMousePosition = current;
            InvalidateVisual();
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Left && isPanning)
        {
            isPanning = false;
            ReleaseMouseCapture();
        }
    }
}