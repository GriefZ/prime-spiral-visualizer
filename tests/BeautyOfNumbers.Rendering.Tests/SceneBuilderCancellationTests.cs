using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Requests;
using Xunit;

namespace BeautyOfNumbers.Rendering.Tests;

public sealed class SceneBuilderCancellationTests
{
    [Fact]
    public void Build_WithPreCancelledToken_ThrowsOperationCanceledException()
    {
        RenderRequest request = TestScenes.CreateRequest(10_000);
        var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
        var builder = new SceneBuilder(classifier);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => builder.Build(request, null, cts.Token));
    }
}
