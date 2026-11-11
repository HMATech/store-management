using System.Reflection;

namespace Web.Api.Endpoints;

public static class EndpointExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app, Assembly assembly)
    {
        var endpoints = assembly
            .DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => Activator.CreateInstance(t) as IEndpoint)
            .ToArray();

        foreach (var endpoint in endpoints)
        {
            if (endpoint == null) continue;

            endpoint.MapEndpoint(app);
        }

        return app;
    }
}