namespace BeautyOfNumbers.Core.Classifiers;

/// <summary>
/// Sieve of Eratosthenes over an odd-only bitset (docs/04-math/sequences.md).
/// </summary>
public sealed class SievePrimeClassifier : IPrimeClassifier
{
    private readonly ulong[] compositeBits;

    public SievePrimeClassifier(int limit)
    {
        Limit = limit;
        if (limit < 2)
        {
            compositeBits = [];
            return;
        }

        int oddCount = (limit - 1) / 2;
        compositeBits = new ulong[(oddCount + 63) >> 6];

        for (int prime = 3; (long)prime * prime <= limit; prime += 2)
        {
            if (IsComposite(prime))
            {
                continue;
            }

            long step = 2L * prime;
            for (long multiple = (long)prime * prime; multiple <= limit; multiple += step)
            {
                SetComposite((int)multiple);
            }
        }
    }

    public int Limit { get; }

    /// <summary>
    /// Computes the inclusive limit needed to classify a range without building the sieve,
    /// so callers can validate large inputs cheaply.
    /// </summary>
    public static int LimitFor(NumberRange range)
    {
        long end = (long)range.Start + range.Count - 1;
        return end is >= 2 and <= int.MaxValue ? (int)end : 2;
    }

    public bool IsPrime(int value)
    {
        if (value < 2 || value > Limit)
        {
            return false;
        }

        return value == 2 || ((value & 1) != 0 && !IsComposite(value));
    }

    private bool IsComposite(int oddValue)
    {
        int index = (oddValue - 3) >> 1;
        return (compositeBits[index >> 6] & (1UL << (index & 63))) != 0;
    }

    private void SetComposite(int oddValue)
    {
        int index = (oddValue - 3) >> 1;
        compositeBits[index >> 6] |= 1UL << (index & 63);
    }
}
