namespace BeautyOfNumbers.Core;

public enum RenderStage
{
    Building,
    Encoding
}

public readonly record struct ProgressReport(RenderStage Stage, float Fraction);