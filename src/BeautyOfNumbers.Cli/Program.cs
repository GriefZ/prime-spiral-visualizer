using System.Diagnostics;
using System.Globalization;
using BeautyOfNumbers.Core;
using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Core.Validation;
using BeautyOfNumbers.Rendering;

int start = 1;
int count = 80000;
string outputPath = "plot.png";
bool onlyPrimes = false;

for (int index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--start":
            if (!TryReadIntArgument(args, ref index, out start))
            {
                return Fail("--start requires an integer value.");
            }

            break;
        case "--count":
            if (!TryReadIntArgument(args, ref index, out count))
            {
                return Fail("--count requires an integer value.");
            }

            break;
        case "--output":
            if (index + 1 >= args.Length)
            {
                return Fail("--output requires a file path.");
            }

            outputPath = args[++index];
            break;
        case "--only-primes":
            onlyPrimes = true;
            break;
        case "--help" or "-h":
            PrintUsage();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument '{args[index]}'.");
            PrintUsage();
            return 1;
    }
}

const int maxCount = 100_000;
// M0 practical bound: monolithic sieve and per-point OxyPlot series (docs/03-guides/build-and-run.md).
const int maxRangeEnd = 100_000_000;

long rangeEnd = (long)start + count - 1;
if (count > maxCount)
{
    Console.Error.WriteLine($"Count: Count must not exceed {maxCount} in this version.");
    return 1;
}

if (count > 0 && start >= 1 && rangeEnd > maxRangeEnd)
{
    Console.Error.WriteLine($"Range: Range end must not exceed {maxRangeEnd} in this version.");
    return 1;
}

var request = new RenderRequest(
    new NumberRange(start, count),
    onlyPrimes,
    ArchimedeanSpiral.LayoutId,
    new LayoutOptions(),
    RenderStyle.Default,
    OutputOptions.Default);

IReadOnlyList<ValidationError> errors = new RequestValidator(SievePrimeClassifier.LimitFor(request.Range)).Validate(request);
if (errors.Count > 0)
{
    foreach (ValidationError error in errors)
    {
        Console.Error.WriteLine($"{error.Field}: {error.Message}");
    }

    return 1;
}

var classifier = new SievePrimeClassifier(SievePrimeClassifier.LimitFor(request.Range));
var stopwatch = Stopwatch.StartNew();

var builder = new SceneBuilder(classifier);
var scene = builder.Build(request);

var renderer = new SkiaSceneRenderer();
PngExporter.Export(scene, request.Style, request.Output, outputPath, renderer);

stopwatch.Stop();
Console.WriteLine($"Saved {outputPath} in {stopwatch.ElapsedMilliseconds} ms.");
return 0;

static bool TryReadIntArgument(string[] arguments, ref int index, out int value)
{
    if (index + 1 >= arguments.Length)
    {
        value = 0;
        return false;
    }

    return int.TryParse(arguments[++index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: BeautyOfNumbers.Cli [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --start <n>      First number in the range (default: 1).");
    Console.WriteLine("  --count <n>      How many numbers to render (default: 80000, max: 100000).");
    Console.WriteLine("  --output <path>  Output PNG path (default: plot.png).");
    Console.WriteLine("  --only-primes    Skip composite numbers.");
    Console.WriteLine("  --help           Show this help.");
    Console.WriteLine();
    Console.WriteLine("The range end (start + count - 1) must not exceed 100000000.");
}