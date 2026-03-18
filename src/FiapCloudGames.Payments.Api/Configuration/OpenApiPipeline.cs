using Scalar.AspNetCore;

namespace FiapCloudGames.Payments.Api.Configuration;

public static class OpenApiPipeline
{
    public static IEndpointRouteBuilder MapOpenApiConfiguration(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi();
        endpoints.MapScalarApiReference();
        return endpoints;
    }
}