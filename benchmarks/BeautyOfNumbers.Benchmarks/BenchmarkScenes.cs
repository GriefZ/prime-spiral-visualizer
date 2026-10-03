using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Rendering;

namespace BeautyOfNumbers.Benchmarks;

internal static class BenchmarkScenes
{
    public static RenderRequest CreateRequest(int count) => new(
        new NumberRange(1, count),
        false,
        ArchimedeanSpiral.LayoutId,
        new LayoutOptions(),
        RenderStyle.Default,
        OutputOptions.Default);

    public static Scene Build(int count)
    {
        RenderRequest request = CreateRequest(count);
        var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
        return new SceneBuilder(classifier).Build(request);
    }

    public static SceneBuilder CreateBuilder(int count)
    {
        RenderRequest request = CreateRequest(count);
        var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
        return new SceneBuilder(classifier);
    }
}
