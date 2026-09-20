using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Rendering;

namespace BeautyOfNumbers.App.Wpf.Services;

/// <summary>
/// Debounces scene rebuilds, cancels stale requests and builds off the UI thread.
/// All members are expected to be used from the UI thread.
/// </summary>
public sealed class RenderScheduler
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(200);

    private CancellationTokenSource? pending;

    public event EventHandler<SceneReadyEventArgs>? SceneReady;

    public event EventHandler<ProgressReport>? ProgressChanged;

    public event EventHandler<bool>? BusyChanged;

    public event EventHandler<Exception>? Failed;

    public bool IsBusy { get; private set; }

    public void Schedule(RenderRequest request)
    {
        Cancel();

        var cancellation = new CancellationTokenSource();
        pending = cancellation;
        _ = RunAsync(request, cancellation);
    }

    public void Cancel()
    {
        CancellationTokenSource? cancellation = pending;
        pending = null;
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        SetBusy(false);
    }

    public static Task<Scene> BuildAsync(
        RenderRequest request,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Build(request, progress, cancellationToken), cancellationToken);
    }

    private async Task RunAsync(RenderRequest request, CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(DebounceDelay, cancellation.Token);

            SetBusy(true);
            var progress = new Progress<ProgressReport>(report => ProgressChanged?.Invoke(this, report));
            Scene scene = await Task.Run(() => Build(request, progress, cancellation.Token), cancellation.Token);

            cancellation.Token.ThrowIfCancellationRequested();
            SceneReady?.Invoke(this, new SceneReadyEventArgs(scene, request));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Failed?.Invoke(this, ex);
        }
        finally
        {
            if (ReferenceEquals(pending, cancellation))
            {
                pending = null;
                SetBusy(false);
            }

            cancellation.Dispose();
        }
    }

    private static Scene Build(
        RenderRequest request,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
        var builder = new SceneBuilder(classifier);
        return builder.Build(request, progress, cancellationToken);
    }

    private void SetBusy(bool value)
    {
        if (IsBusy == value)
        {
            return;
        }

        IsBusy = value;
        BusyChanged?.Invoke(this, value);
    }
}
