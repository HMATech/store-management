using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Decorators;
using Application.Tests.TestSupport;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Application.Tests;

public class DependencyInjectionTests
{
    private static readonly Type[] CommandInterfaces = [typeof(ICommandHandler<>), typeof(ICommandHandler<,>)];

    public static TheoryData<Type> CommandHandlers => new(HandlerServices(CommandInterfaces));
    public static TheoryData<Type> QueryHandlers => new(HandlerServices([typeof(IQueryHandler<,>)]));

    [Theory]
    [MemberData(nameof(CommandHandlers))]
    public void CommandHandler_IsRegisteredBehindValidationDecorator(Type service)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService(service);

        Assert.Equal(typeof(ValidationDecorator), resolved.GetType().DeclaringType);
    }

    [Theory]
    [MemberData(nameof(QueryHandlers))]
    public void QueryHandler_IsRegisteredBehindValidationDecorator(Type service)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService(service);

        Assert.Equal(typeof(ValidationDecorator), resolved.GetType().DeclaringType);
    }

    [Fact]
    public void Validators_AreRegisteredIncludingInternalOnes()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var validators = scope.ServiceProvider
            .GetServices<IValidator<Application.Brands.Create.CreateBrandCommand>>();

        Assert.Single(validators);
    }

    [Fact]
    public void Handlers_AreScoped()
    {
        using var provider = BuildProvider();
        var service = typeof(IQueryHandler<Application.Brands.GetById.GetBrandByIdQuery,
            Application.Brands.BrandResponse>);
        using var scope = provider.CreateScope();
        using var otherScope = provider.CreateScope();

        var first = scope.ServiceProvider.GetRequiredService(service);

        Assert.Same(first, scope.ServiceProvider.GetRequiredService(service));
        Assert.NotSame(first, otherScope.ServiceProvider.GetRequiredService(service));
    }

    [Fact]
    public void UnregisteredHandler_CannotBeResolved()
    {
        using var provider = BuildProvider();

        // The open generic decorator matches any command, then finds no handler to wrap but itself.
        Assert.Throws<InvalidOperationException>(() => provider.GetService<ICommandHandler<UnhandledCommand>>());
    }

    private static IEnumerable<Type> HandlerServices(Type[] openInterfaces)
    {
        return typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, DeclaringType: null })
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && openInterfaces.Contains(i.GetGenericTypeDefinition()))
            .Distinct();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var database = new TestDatabase();

        services.AddLogging();
        services.AddSingleton<IDateTimeProvider, FixedClock>();
        services.AddSingleton<IPasswordHasher, FakePasswordHasher>();
        services.AddSingleton<ITokenProvider, FakeTokenProvider>();
        services.AddScoped<IApplicationDbContext>(_ => database.CreateContext());
        services.AddApplication();

        return services.BuildServiceProvider();
    }

    public sealed record UnhandledCommand : ICommand;
}
