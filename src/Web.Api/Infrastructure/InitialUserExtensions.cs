using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.Register;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Web.Api.Infrastructure;

public static class InitialUserExtensions
{
    public const string SectionName = "InitialUser";

    /// <summary>
    /// Creates the user configured in the <c>InitialUser</c> section when the database has no users yet. Registering
    /// requires a signed-in user, so this is how the first account gets created. Does nothing when the section isn't
    /// set, and throws when the configured user can't be created, so a bad configuration stops startup.
    /// </summary>
    public static async Task CreateInitialUserAsync(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IConfiguration>()
            .GetSection(SectionName)
            .Get<InitialUserOptions>();

        if (string.IsNullOrWhiteSpace(options?.Email))
        {
            return;
        }

        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(InitialUserExtensions));

        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        if (await dbContext.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping the initial user.");
            return;
        }

        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<RegisterUserCommand, RegisterResponse>>();
        var command = new RegisterUserCommand(options.Email, options.Password ?? string.Empty,
            options.FirstName ?? string.Empty, options.LastName);

        var result = await handler.HandleAsync(command, CancellationToken.None);
        if (result.IsFailed)
        {
            throw new InvalidOperationException(
                $"Could not create the initial user '{options.Email}': {Describe(result.Error)}");
        }

        logger.LogInformation("Created the initial user '{Email}'.", options.Email);
    }

    private static string Describe(Error error)
    {
        if (error is not ValidationError validationError)
        {
            return $"{error.Code} {error.Description}";
        }

        return string.Join(" ", validationError.FieldErrors
            .SelectMany(field => field.Value)
            .Select(fieldError => fieldError.Description));
    }

    private sealed class InitialUserOptions
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
    }
}
