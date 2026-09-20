namespace BeautyOfNumbers.Core.Classifiers;

/// <summary>
/// Trial-division reference implementation used by tests as an oracle, not in production.
/// </summary>
public sealed class ReferencePrimeClassifier : IPrimeClassifier
{
    public int Limit => int.MaxValue;

    public bool IsPrime(int value)
    {
        if (value < 2)
        {
            return false;
        }

        if (value == 2)
        {
            return true;
        }

        if ((value & 1) == 0)
        {
            return false;
        }

        for (int divisor = 3; (long)divisor * divisor <= value; divisor += 2)
        {
            if (value % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }
}
