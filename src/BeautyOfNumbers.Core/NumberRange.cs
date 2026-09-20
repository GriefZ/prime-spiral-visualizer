namespace BeautyOfNumbers.Core;

public readonly record struct NumberRange(int Start, int Count)
{
    public int End => Start + Count - 1;
}
