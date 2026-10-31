using FluentAssertions;
using Portfolio.Domain.Enums;

namespace Portfolio.Test.Domain;

public class NotificationTypeTests
{
    [Fact]
    public void NotificationType_DefaultValue_ShouldBeUnknownWithZeroValue()
    {
        var defaultValue = default(NotificationType);

        defaultValue.Should().Be(NotificationType.Unknown);
        ((int)defaultValue).Should().Be(0);
    }

    [Theory]
    [InlineData(NotificationType.Unknown, 0)]
    [InlineData(NotificationType.Info, 1)]
    [InlineData(NotificationType.Success, 2)]
    [InlineData(NotificationType.Error, 3)]
    [InlineData(NotificationType.Warning, 4)]
    [InlineData(NotificationType.Fatal, 5)]
    public void NotificationType_Members_ShouldHaveExpectedIntegerValues(NotificationType type, int expectedValue)
    {
        ((int)type).Should().Be(expectedValue);
    }
}
