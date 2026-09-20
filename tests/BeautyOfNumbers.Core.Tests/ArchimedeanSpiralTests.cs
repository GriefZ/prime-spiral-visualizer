using BeautyOfNumbers.Core.Geometry;
using BeautyOfNumbers.Core.Layouts;
using Xunit;

namespace BeautyOfNumbers.Core.Tests;

public sealed class ArchimedeanSpiralTests
{
    private static readonly ArchimedeanSpiral Spiral = new();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void Map_ReturnsPolarPoint_ForNumber(int number)
    {
        Point2D point = Spiral.Map(number, new LayoutOptions());

        Assert.Equal(number * Math.Cos(number), point.X, 9);
        Assert.Equal(number * Math.Sin(number), point.Y, 9);
    }

    [Fact]
    public void Map_WhenRotationIsTwoPi_ReturnsSamePoint()
    {
        Point2D expected = Spiral.Map(7, new LayoutOptions());
        Point2D actual = Spiral.Map(7, new LayoutOptions { Rotation = 2 * Math.PI });

        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
    }

    [Fact]
    public void Map_WhenClockwise_KeepsXAndMirrorsY()
    {
        Point2D counterClockwise = Spiral.Map(5, new LayoutOptions());
        Point2D clockwise = Spiral.Map(5, new LayoutOptions { Clockwise = true });

        Assert.Equal(counterClockwise.X, clockwise.X, 9);
        Assert.Equal(-counterClockwise.Y, clockwise.Y, 9);
    }

    [Fact]
    public void Map_WhenScaleIsDoubled_DoublesRadius()
    {
        Point2D single = Spiral.Map(5, new LayoutOptions());
        Point2D doubled = Spiral.Map(5, new LayoutOptions { Scale = 2 });

        Assert.Equal(2 * Radius(single), Radius(doubled), 9);
    }

    private static double Radius(Point2D point) => Math.Sqrt((point.X * point.X) + (point.Y * point.Y));
}
