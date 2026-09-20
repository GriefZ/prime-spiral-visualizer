namespace BeautyOfNumbers.Core;

public sealed record Scene(
    ScenePoint[] Points,
    double MinX,
    double MaxX,
    double MinY,
    double MaxY);