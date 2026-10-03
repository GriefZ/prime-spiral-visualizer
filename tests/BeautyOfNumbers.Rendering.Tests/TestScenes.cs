using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;

namespace BeautyOfNumbers.Rendering.Tests;

internal static class TestScenes
{
    public static RenderRequest CreateRequest(int count, bool showOnlyPrimes = false) => new(
        new NumberRange(1, count),
        showOnlyPrimes,
        ArchimedeanSpiral.LayoutId,
        new LayoutOptions(),
        RenderStyle.Default,
        OutputOptions.Default);

    public static Scene Build(int count, bool showOnlyPrimes = false)
    {
        RenderRequest request = CreateRequest(count, showOnlyPrimes);
        var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
        return new SceneBuilder(classifier).Build(request);
    }
}
