using BeautyOfNumbers.Core;

namespace BeautyOfNumbers.Rendering;

public static class SceneFitter
{
    public static Viewport Fit(
        Scene scene,
        double width,
        double height,
        double userScale = 1.0,
        double userOffsetX = 0,
        double userOffsetY = 0)
    {
        double sceneW = scene.MaxX - scene.MinX;
        double sceneH = scene.MaxY - scene.MinY;

        if (sceneW <= 0 || sceneH <= 0)
        {
            return new Viewport(1.0, (width * 0.5) + userOffsetX, (height * 0.5) + userOffsetY);
        }

        double scale = Math.Min(width / sceneW, height / sceneH) * userScale;
        double renderedW = sceneW * scale;
        double renderedH = sceneH * scale;
        double offsetX = ((width - renderedW) * 0.5) - (scene.MinX * scale) + userOffsetX;
        double offsetY = ((height - renderedH) * 0.5) - (scene.MinY * scale) + userOffsetY;
        return new Viewport(scale, offsetX, offsetY);
    }
}