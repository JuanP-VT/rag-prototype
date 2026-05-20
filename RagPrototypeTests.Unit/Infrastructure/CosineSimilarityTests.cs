using RagPrototype.Infrastructure.VectorMath;
using FluentAssertions;

namespace RagPrototype.Tests.Unit.Infrastructure;

public class CosineSimilarityTests
{
    [Fact]
    public void Calculate_IdenticalVectors_ReturnsOne()
    {
        float[] a = [1f, 0f, 0f];

        var result = CosineSimilarity.Calculate(a, a);

        result.Should().BeApproximately(1f, 0.0001f);
    }

    [Fact]
    public void Calculate_OrthogonalVectors_ReturnsZero()
    {
        float[] a = [1f, 0f];
        float[] b = [0f, 1f];

        var result = CosineSimilarity.Calculate(a, b);

        result.Should().BeApproximately(0f, 0.0001f);
    }

    [Fact]
    public void Calculate_ZeroVector_ReturnsZeroWithoutThrowing()
    {
        float[] zero = [0f, 0f, 0f];
        float[] b = [1f, 0f, 0f];

        var act = () => CosineSimilarity.Calculate(zero, b);

        act.Should().NotThrow();
        act().Should().Be(0f);
    }

    [Fact]
    public void Calculate_DifferentLengthVectors_ThrowsArgumentException()
    {
        float[] a = [1f, 0f];
        float[] b = [1f, 0f, 0f];

        var act = () => CosineSimilarity.Calculate(a, b);

        act.Should().Throw<ArgumentException>();
    }
}
