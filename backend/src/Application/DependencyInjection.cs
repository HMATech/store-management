using Application.Abstractions.Messaging;
using Application.Decorators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(selector => selector.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(filter => filter.AssignableTo(typeof(IQueryHandler<,>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(filter => filter.AssignableTo(typeof(ICommandHandler<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(filter => filter.AssignableTo(typeof(ICommandHandler<,>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationDecorator.QueryHandler<,>));

        return services;
    }
}