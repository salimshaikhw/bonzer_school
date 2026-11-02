using API.ExceptionHandling;
using Application.Common;
using Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BonzerSchool.UnitTests.ExceptionHandling;

public class ExceptionClassifierTests
{
    [Theory]
    [InlineData(ExceptionCodes.Validation, 400)]
    [InlineData(ExceptionCodes.ItemNotFound, 404)]
    [InlineData(ExceptionCodes.UnAuthorized, 403)]
    [InlineData(ExceptionCodes.Restriction, 403)]
    [InlineData(ExceptionCodes.Operation, 409)]
    [InlineData(ExceptionCodes.Database, 500)]
    [InlineData(ExceptionCodes.Default, 500)]
    public void Application_exception_codes_map_to_http_statuses(ExceptionCodes code, int status)
    {
        var result = ExceptionClassifier.Classify(new BaseServiceException("x", code), requestAborted: false);

        Assert.Equal(status, result.StatusCode);
    }

    [Fact]
    public void A_not_found_wrapped_in_a_database_error_is_still_a_404()
    {
        var inner = new BaseServiceException("Branch not found", ExceptionCodes.ItemNotFound);
        var wrapped = new BaseServiceException("Error getting Branch", inner, ExceptionCodes.Database);

        var result = ExceptionClassifier.Classify(wrapped, requestAborted: false);

        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Branch not found", result.ClientMessage); // prefix "Service Exception: " removed
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "insert", 409, "UniqueViolation")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "update or delete on table \"branches\"", 409, "ForeignKeyViolation")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "insert or update on table \"classes\"", 400, "ForeignKeyViolation")]
    [InlineData(PostgresErrorCodes.NotNullViolation, "null value", 400, "Validation")]
    [InlineData(PostgresErrorCodes.RaiseException, "TC must be PENDING for this step", 400, "BusinessRule")]
    [InlineData(PostgresErrorCodes.DeadlockDetected, "deadlock", 409, "Concurrency")]
    [InlineData(PostgresErrorCodes.TooManyConnections, "too many", 503, "DatabaseUnavailable")]
    public void Database_errors_inside_ef_exceptions_are_classified(string sqlState, string messageText, int status, string errorCode)
    {
        var postgres = new PostgresException(messageText, "ERROR", "ERROR", sqlState);
        var efError = new DbUpdateException("save failed", postgres);
        var serviceError = new BaseServiceException("Error adding X", efError, ExceptionCodes.Database);

        var result = ExceptionClassifier.Classify(serviceError, requestAborted: false);

        Assert.Equal(status, result.StatusCode);
        Assert.Equal(errorCode, result.ErrorCode);
    }

    [Fact]
    public void Business_rule_messages_from_database_functions_reach_the_user()
    {
        var result = ExceptionClassifier.Classify(
            new PostgresException("TC must be PENDING for this step. Current status: ISSUED", "ERROR", "ERROR", PostgresErrorCodes.RaiseException),
            requestAborted: false);

        Assert.Equal("TC must be PENDING for this step. Current status: ISSUED", result.ClientMessage);
    }

    [Fact]
    public void Fluent_validation_errors_are_grouped_by_field()
    {
        var exception = new FluentValidation.ValidationException(new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Name is required."),
            new FluentValidation.Results.ValidationFailure("Email", "Email must be a valid email address."),
        });

        var result = ExceptionClassifier.Classify(exception, requestAborted: false);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(new[] { "Name", "Email" }, result.Errors!.Keys);
    }

    [Fact]
    public void A_cancelled_request_is_not_an_error()
    {
        var result = ExceptionClassifier.Classify(new OperationCanceledException(), requestAborted: true);

        Assert.Equal(ExceptionClassifier.ClientClosedRequest, result.StatusCode);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result.LogLevel);
    }

    [Fact]
    public void Unknown_exceptions_hide_their_message()
    {
        var result = ExceptionClassifier.Classify(new InvalidOperationException("connection string Password=secret"), requestAborted: false);

        Assert.Equal(500, result.StatusCode);
        Assert.Null(result.ClientMessage);
    }
}
