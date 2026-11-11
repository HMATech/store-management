using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Brands.Create;

internal sealed class CreateBrandCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateBrandCommandHandler> logger
) : ICommandHandler<CreateBrandCommand, CreateBrandResponse>
{
    public async Task<Result<CreateBrandResponse>> HandleAsync(CreateBrandCommand command,
        CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.Name == command.Name, cancellationToken);

        if (brand != null)
        {
            return BrandErrors.AlreadyExists(command.Name);
        }

        try
        {
            brand = Brand.CreateNew(command.Name, dtProvider.UtcNow);
            dbContext.Brands.Add(brand);

            await dbContext.SaveChangesAsync(cancellationToken);
            return new CreateBrandResponse(brand.Id);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new brand '{command.Name}' to DB",
                command.Name);
            return ApplicationErrors.DBOperationError(nameof(CreateBrandCommandHandler),
                $"DB error has occurred while adding new brand '{command.Name}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateBrandCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new brand '{command.Name}' to DB",
                command.Name);
            return ApplicationErrors.UnexpectedError(nameof(CreateBrandCommandHandler),
                $"Unexpected error has occurred while adding new brand '{command.Name}'' to DB");
        }
    }
}