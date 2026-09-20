namespace BeautyOfNumbers.Core.Classifiers;

public interface IPrimeClassifier
{
    int Limit { get; }

    bool IsPrime(int value);
}
