using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Style;

namespace BeautyOfNumbers.Core.Requests;

public sealed record RenderRequest(
    NumberRange Range,
    bool ShowOnlyPrimes,
    string LayoutId,
    LayoutOptions Layout,
    RenderStyle Style,
    OutputOptions Output);
