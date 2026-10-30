using FluentAssertions;
using Portfolio.Domain.Extensions;

namespace Portfolio.Test.Domain;

public class StringExtensionsTests
{
    [Fact]
    public void Truncate_WhenValueIsNull_ShouldReturnEmptyString()
    {
        string? nullString = null;
        nullString.Truncate(10).Should().Be(string.Empty);
    }

    [Fact]
    public void Truncate_WhenValueIsEmpty_ShouldReturnEmptyString()
    {
        string.Empty.Truncate(10).Should().Be(string.Empty);
    }

    [Fact]
    public void Truncate_WhenValueIsShorterThanMaxLength_ShouldReturnOriginalString()
    {
        var input = "hello";
        input.Truncate(10).Should().Be("hello");
    }

    [Fact]
    public void Truncate_WhenValueIsEqualToMaxLength_ShouldReturnOriginalString()
    {
        var input = "1234567890";
        input.Truncate(10).Should().Be("1234567890");
    }

    [Fact]
    public void Truncate_WhenValueIsLongerThanMaxLength_ShouldReturnTruncatedString()
    {
        var input = "1234567890EXTRA";
        input.Truncate(10).Should().Be("123456 ...");
    }

    [Fact]
    public void Truncate_WhenMaxLengthIsSmallerThanEllipsis_ShouldSliceWithoutCrashing()
    {
        var input = "abcdef";
        input.Truncate(3).Should().Be("abc");
        input.Truncate(0).Should().Be(string.Empty);
    }
}
