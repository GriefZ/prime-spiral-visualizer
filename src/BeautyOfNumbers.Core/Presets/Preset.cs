using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;

namespace BeautyOfNumbers.Core.Presets;

public sealed record Preset(
    int SchemaVersion,
    string Name,
    NumberRange Range,
    bool ShowOnlyPrimes,
    string LayoutId,
    LayoutOptions Layout,
    RenderStyle Style,
    OutputOptions Output);
