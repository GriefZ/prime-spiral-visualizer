using BeautyOfNumbers.Core.Classifiers;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;

namespace BeautyOfNumbers.Core.Validation;

public sealed class RequestValidator : IRequestValidator
{
    private readonly int classifierLimit;

    public RequestValidator(IPrimeClassifier classifier)
        : this(classifier.Limit)
    {
    }

    public RequestValidator(int classifierLimit) => this.classifierLimit = classifierLimit;

    public IReadOnlyList<ValidationError> Validate(RenderRequest request)
    {
        List<ValidationError> errors = [];

        if (request.Range.Start < 1)
        {
            errors.Add(new ValidationError(nameof(request.Range.Start), "Start must be at least 1."));
        }

        if (request.Range.Count <= 0)
        {
            errors.Add(new ValidationError(nameof(request.Range.Count), "Count must be greater than 0."));
        }
        else if ((long)request.Range.Start + request.Range.Count - 1 > int.MaxValue)
        {
            errors.Add(new ValidationError(nameof(request.Range), "Range end exceeds Int32."));
        }
        else if (request.Range.End > classifierLimit)
        {
            errors.Add(new ValidationError(
                nameof(request.Range),
                $"Range end {request.Range.End} exceeds the classifier limit of {classifierLimit}."));
        }

        if (request.Style.Palette.Length == 0)
        {
            errors.Add(new ValidationError(nameof(request.Style.Palette), "Palette must not be empty."));
        }

        if (request.Output.Width <= 0 || request.Output.Height <= 0)
        {
            errors.Add(new ValidationError(nameof(request.Output), "Output width and height must be greater than 0."));
        }

        if (request.Output.Dpi <= 0)
        {
            errors.Add(new ValidationError(nameof(request.Output), "Output DPI must be greater than 0."));
        }

        if (request.LayoutId != ArchimedeanSpiral.LayoutId)
        {
            errors.Add(new ValidationError(nameof(request.LayoutId), $"Unknown layout '{request.LayoutId}'."));
        }

        return errors;
    }
}
