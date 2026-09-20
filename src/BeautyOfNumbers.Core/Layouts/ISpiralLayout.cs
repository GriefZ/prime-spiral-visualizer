using BeautyOfNumbers.Core.Geometry;

namespace BeautyOfNumbers.Core.Layouts;

public interface ISpiralLayout
{
    string Id { get; }

    Point2D Map(int number, LayoutOptions options);
}
