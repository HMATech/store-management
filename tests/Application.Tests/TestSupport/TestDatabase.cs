using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.TestSupport;

/// <summary>
/// An isolated EF Core InMemory database. Each CreateContext call returns a new context, so tests can seed, act and
/// assert with separate contexts the same way separate HTTP requests would.
/// </summary>
internal sealed class TestDatabase
{
    private readonly DbContextOptions<ApplicationDbContext> options =
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    public ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(options, NullLogger<ApplicationDbContext>.Instance);
    }

    public async Task SeedAsync(params object[] entities)
    {
        await using var context = CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    public async Task<TResult> ReadAsync<TResult>(Func<ApplicationDbContext, Task<TResult>> read)
    {
        await using var context = CreateContext();
        return await read(context);
    }
}
