using FulfillmentPlatform.SharedKernel;

namespace SharedKernel.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithoutValue_IsSuccessfulAndHasNoError()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WithoutValue_ContainsTheProvidedError()
    {
        Error error = Error.Validation("orders.quantity.invalid", "Quantity must be positive.");

        Result result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_WithValue_ExposesTheValue()
    {
        Result<Guid> result = Result.Success(Guid.Empty);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Value);
    }

    [Fact]
    public void Failure_WithValue_ThrowsWhenReadingValue()
    {
        Result<string> result = Result.Failure<string>(Error.NotFound("orders.not_found", "Order was not found."));

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.Failure)]
    public void ErrorFactories_PreserveTheErrorCategory(ErrorType expectedType)
    {
        Error error = expectedType switch
        {
            ErrorType.Validation => Error.Validation("validation", "Validation failed."),
            ErrorType.NotFound => Error.NotFound("not_found", "Not found."),
            ErrorType.Conflict => Error.Conflict("conflict", "Conflict."),
            ErrorType.Unauthorized => Error.Unauthorized("unauthorized", "Unauthorized."),
            ErrorType.Forbidden => Error.Forbidden("forbidden", "Forbidden."),
            ErrorType.Failure => Error.Failure("failure", "Unexpected failure."),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedType), expectedType, null)
        };

        Assert.Equal(expectedType, error.Type);
    }
}
