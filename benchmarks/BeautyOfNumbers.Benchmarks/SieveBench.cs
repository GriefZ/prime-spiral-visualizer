using BeautyOfNumbers.Core.Classifiers;
using BenchmarkDotNet.Attributes;

namespace BeautyOfNumbers.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Sieve")]
public class SieveBench
{
    [Params(1_000_000, 10_000_000, 100_000_000)]
    public int Limit { get; set; }

    [Benchmark]
    public bool BuildSieve() => new SievePrimeClassifier(Limit).IsPrime(Limit);
}
