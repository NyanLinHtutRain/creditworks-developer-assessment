using VehicleManager.Api.Contracts;
using VehicleManager.Api.ErrorHandling;
using VehicleManager.Api.Services;

namespace VehicleManager.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", async (CategoryService service, CancellationToken cancellationToken) =>
            Results.Ok((await service.ListAsync(cancellationToken)).Select(CategoryResponse.From)));

        group.MapPost("/", async (CategoryRequest request, CategoryService service, CancellationToken cancellationToken) =>
        {
            if (request.MinWeightKg is not { } min)
            {
                return MissingStart();
            }

            var result = await service.CreateAsync(request.Name, request.Icon, min, request.MaxWeightKg, cancellationToken);

            return result.Category is { } created
                ? Results.Json(CategoryResponse.From(created), statusCode: StatusCodes.Status201Created)
                : Failure(result);
        });

        group.MapPut("/{id:int}", async (
            int id, CategoryRequest request, CategoryService service, CancellationToken cancellationToken) =>
        {
            if (request.MinWeightKg is not { } min)
            {
                return MissingStart();
            }

            var result = await service.EditAsync(id, request.Name, request.Icon, min, request.MaxWeightKg, cancellationToken);

            return result.Category is { } edited ? Results.Ok(CategoryResponse.From(edited)) : Failure(result);
        });

        group.MapDelete("/{id:int}", async (int id, CategoryService service, CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);

            return result.Status == ChangeStatus.Success ? Results.NoContent() : Failure(result);
        });
    }

    private const string InvalidTitle = "The category change is not valid.";

    private static IResult MissingStart() => ProblemResults.Invalid(InvalidTitle, ["A starting weight is required."]);

    private static IResult Failure(CategoryChangeResult result) =>
        result.Status == ChangeStatus.NotFound
            ? ProblemResults.NotFound("Category not found.")
            : ProblemResults.Invalid(InvalidTitle, result.Errors ?? []);
}
