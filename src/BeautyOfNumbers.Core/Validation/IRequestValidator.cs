using BeautyOfNumbers.Core.Requests;

namespace BeautyOfNumbers.Core.Validation;

public interface IRequestValidator
{
    IReadOnlyList<ValidationError> Validate(RenderRequest request);
}
