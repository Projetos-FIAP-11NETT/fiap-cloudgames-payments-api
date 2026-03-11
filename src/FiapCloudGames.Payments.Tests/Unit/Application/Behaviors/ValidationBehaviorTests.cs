using FiapCloudGames.Payments.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Payments.Tests.Unit.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidators()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([], NullLogger<ValidationBehavior<TestRequest, TestResponse>>.Instance);
        var request = new TestRequest();
        var next = new RequestHandlerDelegate<TestResponse>(ct => Task.FromResult(new TestResponse()));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var validator = new TestValidator();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([validator], NullLogger<ValidationBehavior<TestRequest, TestResponse>>.Instance);
        var request = new TestRequest();
        var next = new RequestHandlerDelegate<TestResponse>(ct => Task.FromResult(new TestResponse()));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        // Arrange
        var validator = new TestValidator();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([validator], NullLogger<ValidationBehavior<TestRequest, TestResponse>>.Instance);
        var request = new TestRequest { Name = "Valid" };
        var next = new RequestHandlerDelegate<TestResponse>(ct => Task.FromResult(new TestResponse()));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    private class TestRequest : MediatR.IRequest<TestResponse>
    {
        public string? Name { get; set; }
    }

    private class TestResponse
    {
    }

    private class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}