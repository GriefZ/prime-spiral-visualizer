namespace BeautyOfNumbers.Core.Layouts;

public sealed record LayoutOptions(
    double Rotation = 0,
    bool Clockwise = false,
    double Scale = 1.0);
