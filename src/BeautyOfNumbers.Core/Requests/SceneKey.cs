using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Style;

namespace BeautyOfNumbers.Core.Requests;

/// <summary>
/// Inputs of a <see cref="RenderRequest"/> that determine the built scene.
/// The remaining request fields affect only drawing and encoding.
/// </summary>
public readonly record struct SceneKey(
    NumberRange Range,
    bool ShowOnlyPrimes,
    string LayoutId,
    LayoutOptions Layout,
    PointSizeFunction PointSize);
