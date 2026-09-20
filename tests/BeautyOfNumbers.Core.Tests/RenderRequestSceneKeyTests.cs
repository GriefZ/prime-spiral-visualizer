using BeautyOfNumbers.Core.Colors;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using Xunit;

namespace BeautyOfNumbers.Core.Tests;

public sealed class RenderRequestSceneKeyTests
{
    [Fact]
    public void SceneKey_WhenOnlyDrawingFieldsChange_StaysEqual()
    {
        RenderRequest request = CreateRequest();
        RenderRequest changed = request with
        {
            Style = request.Style with
            {
                Background = new RgbaColor(0, 0, 0, 255),
                Palette = [new RgbaColor(0, 0, 255, 255)],
                Antialias = false,
            },
            Output = new OutputOptions(800, 600, 72),
        };

        Assert.Equal(request.SceneKey, changed.SceneKey);
    }

    [Fact]
    public void SceneKey_WhenPointSizeChanges_Differs()
    {
        RenderRequest request = CreateRequest();
        RenderRequest changed = request with
        {
            Style = request.Style with { PointSize = new PointSizeFunction(1, 9, PointSizeCurve.Sqrt) },
        };

        Assert.NotEqual(request.SceneKey, changed.SceneKey);
    }

    [Fact]
    public void SceneKey_WhenRangeOrPrimeFilterChanges_Differs()
    {
        RenderRequest request = CreateRequest();

        Assert.NotEqual(request.SceneKey, (request with { Range = new NumberRange(2, 1000) }).SceneKey);
        Assert.NotEqual(request.SceneKey, (request with { ShowOnlyPrimes = !request.ShowOnlyPrimes }).SceneKey);
    }

    private static RenderRequest CreateRequest() => new(
        new NumberRange(1, 1000),
        true,
        ArchimedeanSpiral.LayoutId,
        new LayoutOptions(),
        RenderStyle.Default,
        OutputOptions.Default);
}
