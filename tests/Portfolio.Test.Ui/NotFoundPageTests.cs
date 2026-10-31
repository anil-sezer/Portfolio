using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Portfolio.Ui.Components.Pages;

namespace Portfolio.Test.Ui;

public class NotFoundPageTests
{
    [Fact]
    public void OnInitialized_WhenHttpContextAccessorProvided_SetsResponseStatusCodeTo404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(context);

        var notFound = new NotFound();
        var accessorProp = typeof(NotFound).GetProperty("HttpContextAccessor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        accessorProp?.SetValue(notFound, accessorMock.Object);

        var onInitializedMethod = typeof(NotFound).GetMethod("OnInitialized", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        onInitializedMethod?.Invoke(notFound, null);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void OnInitialized_WhenCascadingHttpContextProvided_SetsResponseStatusCodeTo404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var notFound = new NotFound();

        var httpContextProp = typeof(NotFound).GetProperty("HttpContext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        httpContextProp?.SetValue(notFound, context);

        var onInitializedMethod = typeof(NotFound).GetMethod("OnInitialized", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        onInitializedMethod?.Invoke(notFound, null);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
