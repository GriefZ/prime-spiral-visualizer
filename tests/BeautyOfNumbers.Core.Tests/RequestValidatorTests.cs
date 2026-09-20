using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using BeautyOfNumbers.Core.Validation;
using Xunit;

namespace BeautyOfNumbers.Core.Tests;

public sealed class RequestValidatorTests
{
    [Fact]
    public void Validate_WhenCountIsNegative_ReturnsCountError()
    {
        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(CreateRequest(new NumberRange(1, -5)));

        Assert.Contains(errors, error => error.Field == "Count");
    }

    [Fact]
    public void Validate_WhenCountIsZero_ReturnsCountError()
    {
        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(CreateRequest(new NumberRange(1, 0)));

        Assert.Contains(errors, error => error.Field == "Count");
    }

    [Fact]
    public void Validate_WhenStartIsBelowOne_ReturnsStartError()
    {
        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(CreateRequest(new NumberRange(0, 10)));

        Assert.Contains(errors, error => error.Field == "Start");
    }

    [Fact]
    public void Validate_WhenRangeEndOverflows_ReturnsRangeError()
    {
        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(CreateRequest(new NumberRange(int.MaxValue - 1, 10)));

        Assert.Contains(errors, error => error.Field == "Range");
    }

    [Fact]
    public void Validate_WhenClassifierLimitIsBelowRangeEnd_ReturnsRangeError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 100));
        IReadOnlyList<ValidationError> errors = new RequestValidator(new SievePrimeClassifier(50)).Validate(request);

        Assert.Contains(errors, error => error.Field == "Range");
    }

    [Fact]
    public void Validate_WhenPaletteIsEmpty_ReturnsPaletteError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 10), style: RenderStyle.Default with { Palette = [] });

        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(request);

        Assert.Contains(errors, error => error.Field == "Palette");
    }

    [Fact]
    public void Validate_WhenOutputWidthIsZero_ReturnsOutputError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 10), output: new OutputOptions(0, 100, 300));

        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(request);

        Assert.Contains(errors, error => error.Field == "Output");
    }

    [Fact]
    public void Validate_WhenOutputHeightIsNegative_ReturnsOutputError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 10), output: new OutputOptions(100, -1, 300));

        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(request);

        Assert.Contains(errors, error => error.Field == "Output");
    }

    [Fact]
    public void Validate_WhenDpiIsZero_ReturnsOutputError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 10), output: new OutputOptions(100, 100, 0));

        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(request);

        Assert.Contains(errors, error => error.Field == "Output");
    }

    [Fact]
    public void Validate_WhenLayoutIsUnknown_ReturnsLayoutIdError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 10), layoutId: "unknown-layout");

        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(request);

        Assert.Contains(errors, error => error.Field == "LayoutId");
    }

    [Fact]
    public void Validate_WhenRequestIsValid_ReturnsNoErrors()
    {
        IReadOnlyList<ValidationError> errors = CreateValidator().Validate(CreateRequest(new NumberRange(1, 1000)));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenLimitIsPassedAsInteger_ReturnsRangeError()
    {
        RenderRequest request = CreateRequest(new NumberRange(1, 100));

        IReadOnlyList<ValidationError> errors = new RequestValidator(50).Validate(request);

        Assert.Contains(errors, error => error.Field == "Range");
    }

    private static RequestValidator CreateValidator() => new(new SievePrimeClassifier(1_000_000));

    private static RenderRequest CreateRequest(
        NumberRange range,
        bool showOnlyPrimes = false,
        string layoutId = ArchimedeanSpiral.LayoutId,
        RenderStyle? style = null,
        OutputOptions? output = null) =>
        new(range, showOnlyPrimes, layoutId, new LayoutOptions(), style ?? RenderStyle.Default, output ?? OutputOptions.Default);
}
