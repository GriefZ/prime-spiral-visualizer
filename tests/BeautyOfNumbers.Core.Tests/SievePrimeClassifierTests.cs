using BeautyOfNumbers.Core.Classifiers;
using Xunit;

namespace BeautyOfNumbers.Core.Tests;

public sealed class SievePrimeClassifierTests
{
    [Fact]
    public void IsPrime_MatchesReferenceClassifier_UpTo100000()
    {
        var reference = new ReferencePrimeClassifier();
        var sieve = new SievePrimeClassifier(100_000);

        for (int value = 0; value <= 100_000; value++)
        {
            Assert.Equal(reference.IsPrime(value), sieve.IsPrime(value));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void IsPrime_ReturnsFalseForEveryValue_WhenLimitIsBelowTwo(int limit)
    {
        var sieve = new SievePrimeClassifier(limit);

        Assert.False(sieve.IsPrime(2));
        Assert.False(sieve.IsPrime(3));
        Assert.False(sieve.IsPrime(int.MaxValue));
    }

    [Fact]
    public void IsPrime_ClassifiesOnlyTwo_WhenLimitIsTwo()
    {
        var sieve = new SievePrimeClassifier(2);

        Assert.True(sieve.IsPrime(2));
        Assert.False(sieve.IsPrime(1));
        Assert.False(sieve.IsPrime(3));
    }

    [Fact]
    public void IsPrime_ClassifiesTwoAndThree_WhenLimitIsThree()
    {
        var sieve = new SievePrimeClassifier(3);

        Assert.True(sieve.IsPrime(2));
        Assert.True(sieve.IsPrime(3));
        Assert.False(sieve.IsPrime(4));
    }

    [Fact]
    public void IsPrime_FindsFirst10000Primes_UpTo104729()
    {
        const int tenthThousandPrime = 104_729;
        var reference = new ReferencePrimeClassifier();
        var sieve = new SievePrimeClassifier(tenthThousandPrime);

        int primeCount = 0;
        for (int value = 2; value <= tenthThousandPrime; value++)
        {
            if (reference.IsPrime(value))
            {
                primeCount++;
                Assert.True(sieve.IsPrime(value), $"Sieve missed prime {value}.");
            }
        }

        Assert.Equal(10_000, primeCount);
    }

    [Fact]
    public void IsPrime_ReturnsFalse_OutsideClassifierRange()
    {
        var sieve = new SievePrimeClassifier(100);

        Assert.False(sieve.IsPrime(-1));
        Assert.False(sieve.IsPrime(0));
        Assert.False(sieve.IsPrime(101));
        Assert.False(sieve.IsPrime(int.MaxValue));
        Assert.False(sieve.IsPrime(int.MinValue));
    }

    [Fact]
    public void LimitFor_ReturnsRangeEnd_ForValidRange()
    {
        Assert.Equal(1000, SievePrimeClassifier.LimitFor(new NumberRange(1, 1000)));
        Assert.Equal(105, SievePrimeClassifier.LimitFor(new NumberRange(5, 101)));
    }

    [Fact]
    public void LimitFor_ReturnsFallbackLimit_WhenRangeIsDegenerateOrOverflows()
    {
        Assert.Equal(2, SievePrimeClassifier.LimitFor(new NumberRange(1, 0)));
        Assert.Equal(2, SievePrimeClassifier.LimitFor(new NumberRange(1, 1)));
        Assert.Equal(2, SievePrimeClassifier.LimitFor(new NumberRange(-10, 5)));
        Assert.Equal(2, SievePrimeClassifier.LimitFor(new NumberRange(int.MaxValue - 1, 10)));
    }
}
