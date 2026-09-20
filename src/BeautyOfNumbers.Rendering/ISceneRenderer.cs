using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Style;
using SkiaSharp;

namespace BeautyOfNumbers.Rendering;

public interface ISceneRenderer
{
    void Draw(SKCanvas canvas, Scene scene, RenderStyle style, Viewport viewport);
}