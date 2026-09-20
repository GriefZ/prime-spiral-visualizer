using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Requests;

namespace BeautyOfNumbers.App.Wpf.Services;

public sealed class SceneReadyEventArgs : EventArgs
{
    public SceneReadyEventArgs(Scene scene, RenderRequest request)
    {
        Scene = scene;
        Request = request;
    }

    public Scene Scene { get; }

    public RenderRequest Request { get; }
}
