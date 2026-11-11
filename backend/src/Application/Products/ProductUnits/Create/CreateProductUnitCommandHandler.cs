using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductUnits.Create;

internal sealed class CreateProductUnitCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateProductUnitCommandHandler> logger
) : ICommandHandler<CreateProductUnitCommand, CreateProductUnitResponse>
{
    public async Task<Result<CreateProductUnitResponse>> HandleAsync(CreateProductUnitCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtNow = dtProvider.UtcNow;

            var productUnit = ProductUnit.CreateNew(command.Name);

            var existing = await dbContext.ProductUnits
                .FirstOrDefaultAsync(pu => pu.Name.Normalized == productUnit.Name.Normalized,
                    cancellationToken);

            if (existing != null)
            {
                return ProductErrors.UnitAlreadyExists(command.Name);
            }

            dbContext.ProductUnits.Add(productUnit);
            await dbContext.SaveChangesAsync(cancellationToken);

            return productUnit.ToCreateResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new product unit '{command.Name}' to DB",
                command.Name);
            return ApplicationErrors.DBOperationError(nameof(CreateProductUnitCommandHandler),
                $"DB error has occurred while adding new product unit '{command.Name}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateProductUnitCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new product unit '{command.Name}' to DB",
                command.Name);
            return ApplicationErrors.UnexpectedError(nameof(CreateProductUnitCommandHandler),
                $"Unexpected error has occurred while adding new product unit '{command.Name}' to DB");
        }
    }
}