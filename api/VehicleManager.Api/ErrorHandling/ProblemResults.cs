namespace VehicleManager.Api.ErrorHandling;

// Shared shapes for expected failures, so every endpoint reports them the same way.
public static class ProblemResults
{
    // The `errors` extension is a list of messages written to be shown to the user as they are.
    public static IResult Invalid(string title, IReadOnlyList<string> errors) =>
        Results.Problem(
            title: title,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?> { ["errors"] = errors });

    public static IResult NotFound(string title) =>
        Results.Problem(title: title, statusCode: StatusCodes.Status404NotFound);
}
