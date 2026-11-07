using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Web.Api.Middlewares;

namespace Web.Api;

public static class DependencyInjection
{
    public const string CorsSectionName = "Cors";

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGenWithAuth();

        services.AddCorsFromConfiguration(configuration);

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.AllowTrailingCommas = true;
        });

        return services;
    }

    /// <summary>
    /// Allows the origins listed in <c>Cors:AllowedOrigins</c> to call the API from a browser. With none listed, no
    /// cross-origin request is allowed. Location is exposed so a frontend can read the id of a created resource.
    /// </summary>
    private static IServiceCollection AddCorsFromConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection($"{CorsSectionName}:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(origins
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(HeaderNames.Location)));

        return services;
    }

    private static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
    {
        services
            .AddSwaggerGen(options =>
            {
                options.CustomSchemaIds(type =>
                {
                    var fullName = type.FullName;
                    if (type.IsGenericType)
                    {
                        var genericArguments = type.GetGenericArguments();
                        var genericArgumentNames = string.Join(", ", genericArguments
                            .Select(arg => arg.ShortDisplayName()));

                        var name = type.Name.Remove(type.Name.IndexOf('`'), 2);
                        return $"{type.Namespace}.{name}<{genericArgumentNames}>";
                    }

                    return fullName!.Replace('+', '.');
                });

                var securityScheme = new OpenApiSecurityScheme()
                {
                    Name = "JWT Authentication",
                    Description = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                };

                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

                var securityRequirement = new OpenApiSecurityRequirement()
                {
                    { new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme,
                            }
                        },
                        []
                    }
                };

                options.AddSecurityRequirement(securityRequirement);
            });

        return services;
    }
}