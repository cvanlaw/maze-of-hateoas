using Microsoft.AspNetCore.Mvc;

namespace MazeOfHateoas.Api.Helpers;

public static class ApiProblemDetails
{
    private const string BadRequestType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    private const string NotFoundType = "https://tools.ietf.org/html/rfc7231#section-6.5.4";

    public static ProblemDetails BadRequest(string detail, string instance) => new()
    {
        Type = BadRequestType,
        Title = "Bad Request",
        Status = 400,
        Detail = detail,
        Instance = instance
    };

    public static ProblemDetails NotFound(string detail, string instance) => new()
    {
        Type = NotFoundType,
        Title = "Not Found",
        Status = 404,
        Detail = detail,
        Instance = instance
    };
}
