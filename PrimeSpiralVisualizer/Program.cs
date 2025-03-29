using SpiralMaker;

DateTime start = DateTime.Now;
PlotUtils.CreatePlot(GeneratePrimes(80000), 50, 8, filePath: "plot80000TrColored8.png");
Console.WriteLine($"Done for {(DateTime.Now - start).Milliseconds} milliseconds!");

static List<double> GenerateNumbers(int count)
{
    var numbers = new List<double>();
    for (int i = 1; i <= count; i++)
    {
        numbers.Add(i);
    }
    return numbers;
}

static List<int> GeneratePrimes(int max) // TODO: Separate servies with different algorithms 
{
    List<int> primes = new List<int>();
    
    if (max < 2)
        return primes;
    
    primes.Add(2);
    
    int nextPrime = 3;
    bool isPrime;
    
    while (nextPrime <= max)
    {
        int sqrt = (int)Math.Sqrt(nextPrime);
        isPrime = true;
        for (int i = 0; (int)primes[i] <= sqrt; i++)
        {
            if (nextPrime % primes[i] == 0)
            {
                isPrime = false;
                break;
            }
        }
        if (isPrime)
        {
            primes.Add(nextPrime);
        }
        nextPrime += 2;
    }
    return primes;
}